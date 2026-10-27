// -----------------------------------------------------------------------
// <copyright file="BinarySerializer.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using ThaumielMapEditor.API.Binary.Attributes;
using ThaumielMapEditor.API.Enums;
using ThaumielMapEditor.API.Helpers;

namespace ThaumielMapEditor.API.Binary
{
    public static class BinarySerializer
    {
        private sealed class MemberAccessor
        {
            public required string Name { get; init; }

            public required Type MemberType { get; init; }

            public PropertyInfo? Prop { get; init; }

            public FieldInfo? Field { get; init; }

            public int Order { get; init; }

            public bool Required { get; init; }

            public bool CanWrite { get; init; }

            public Func<object, object?>? Getter { get; init; }

            public Action<object, object?>? Setter { get; init; }
        }

        private sealed class TypePlan
        {
            public required MemberAccessor[] Members { get; init; }

            public required Dictionary<string, MemberAccessor> ByName { get; init; }

            public ConstructorInfo? Ctor { get; init; }

            public MemberAccessor?[] CtorMembers { get; init; } = [];

            public Func<object>? Create { get; init; }

            public Func<object?[], object>? CreateWithArgs { get; init; }
        }

        private readonly struct DictionaryShape(bool isDictionary, Type? key, Type? value)
        {
            public bool IsDictionary { get; } = isDictionary;

            public Type? Key { get; } = key;

            public Type? Value { get; } = value;
        }

        private readonly struct EnumerableShape(bool isEnumerable, Type? element)
        {
            public bool IsEnumerable { get; } = isEnumerable;

            public Type? Element { get; } = element;
        }

        private const uint GenericMagic = 827214146u;

        private const int GenericFormatVersion = 1;

        private const int MaxEntries = 10000000;

        private static readonly ConcurrentDictionary<Type, TypePlan> PlanCache = new();

        private static readonly ConcurrentDictionary<string, Type> RuntimeTypeCache = new();

        private static readonly ConcurrentDictionary<Type, Func<object>> FactoryCache = new();

        private static readonly ConcurrentDictionary<Type, Type> ListTypeCache = new();

        private static readonly ConcurrentDictionary<(Type Key, Type Value), Type> DictionaryTypeCache = new();

        private static readonly ConcurrentDictionary<Type, DictionaryShape> DictionaryShapeCache = new();

        private static readonly ConcurrentDictionary<Type, EnumerableShape> EnumerableShapeCache = new();

        public static byte[] Serialize<T>(T? value)
        {
            using MemoryStream memoryStream = new();
            using (BinaryWriter writer = new(memoryStream))
            {
                Serialize(writer, value);
            }

            return memoryStream.ToArray();
        }

        public static byte[] Serialize(object? value)
        {
            using MemoryStream memoryStream = new();
            using (BinaryWriter writer = new(memoryStream))
            {
                WriteGenericHeader(writer);
                WriteValue(writer, value, value?.GetType() ?? typeof(object));
            }

            return memoryStream.ToArray();
        }

        public static void Serialize<T>(Stream stream, T? value)
        {
            using BinaryWriter writer = new(stream, Encoding.UTF8, leaveOpen: true);
            Serialize(writer, value);
        }

        public static void Serialize<T>(BinaryWriter writer, T? value)
        {
            WriteGenericHeader(writer);
            WriteValue(writer, value, typeof(T));
        }

        public static void WritePayload<T>(BinaryWriter writer, T? value) =>
            WriteValue(writer, value, typeof(T));

        public static void SerializeToFile<T>(string path, T? value)
        {
            string directoryName = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directoryName))
                Directory.CreateDirectory(directoryName);

            File.WriteAllBytes(path, Serialize(value));
        }

        public static T? Deserialize<T>(byte[] bytes)
        {
            using MemoryStream input = new(bytes, writable: false);
            using BinaryReader reader = new(input);
            return Deserialize<T>(reader);
        }

        public static T? Deserialize<T>(Stream stream)
        {
            using BinaryReader reader = new(stream, Encoding.UTF8, leaveOpen: true);
            return Deserialize<T>(reader);
        }

        public static T? Deserialize<T>(BinaryReader reader)
        {
            ReadGenericHeader(reader);
            object? obj = ReadValue(reader, typeof(T));
            if (obj == null)
                return default;

            return (T)obj;
        }

        public static object? Deserialize(byte[] bytes, Type type)
        {
            using MemoryStream input = new(bytes, writable: false);
            using BinaryReader reader = new(input);
            ReadGenericHeader(reader);
            return ReadValue(reader, type);
        }

        public static T? ReadPayload<T>(BinaryReader reader)
        {
            object? obj = ReadValue(reader, typeof(T));
            if (obj == null)
                return default;

            return (T)obj;
        }

        public static T? DeserializeFromFile<T>(string path)
        {
            if (!File.Exists(path))
            {
                LogManager.Warn($"File not found at: {path}");
                return default;
            }

            return Deserialize<T>(File.ReadAllBytes(path));
        }

        public static bool TryDeserializeFromFile<T>(string path, out T? value)
        {
            value = default;
            try
            {
                if (!File.Exists(path))
                    return false;
                
                value = Deserialize<T>(File.ReadAllBytes(path));
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static void WriteGenericHeader(BinaryWriter writer)
        {
            writer.Write(GenericMagic);
            writer.Write(GenericFormatVersion);
        }

        private static void ReadGenericHeader(BinaryReader reader)
        {
            uint num = 0u;
            try
            {
                num = reader.ReadUInt32();
            }
            catch (EndOfStreamException arg)
            {
                LogManager.Warn($"Truncated bin file (missing header). \n {arg}");
            }

            if (num != GenericMagic)
            {
                LogManager.Warn($"Invalid bin file (bad magic 0x{num:X8}).");
                return;
            }

            int num2 = reader.ReadInt32();
            if (num2 != GenericFormatVersion)
                LogManager.Warn($"Unsupported bin version: {num2}.");
        }

        private static void WriteValue(BinaryWriter writer, object? value, Type declaredType)
        {
            if (value == null)
            {
                if (IsNonNullableValueType(declaredType))
                    LogManager.Warn($"Cannot serialize null for non nullable '{declaredType.FullName}'.");

                writer.Write((byte)0);
                return;
            }

            Type type = value.GetType();
            Type declared = Nullable.GetUnderlyingType(declaredType) ?? declaredType;
            if (NeedsRuntimeTypeName(declared, type))
            {
                writer.Write((byte)24);
                writer.Write(type.AssemblyQualifiedName ?? type.FullName ?? type.Name);
                WriteInner(writer, value, type);
            }
            else
                WriteInner(writer, value, type);
        }

        private static bool NeedsRuntimeTypeName(Type declared, Type actual)
        {
            if (declared == typeof(object))
                return true;

            if (declared.IsInterface || declared.IsAbstract)
                return true;
                
            return declared != actual;
        }

        private static void WriteInner(BinaryWriter writer, object value, Type actual)
        {
            if (actual.IsEnum)
            {
                Type underlyingType = Enum.GetUnderlyingType(actual);
                object value2 = Convert.ChangeType(value, underlyingType, CultureInfo.InvariantCulture);
                WriteInner(writer, value2, underlyingType);
                return;
            }

            if (actual == typeof(bool))
            {
                writer.Write((byte)1);
                writer.Write((bool)value);
                return;
            }

            if (actual == typeof(byte))
            {
                writer.Write((byte)2);
                writer.Write((byte)value);
                return;
            }

            if (actual == typeof(sbyte))
            {
                writer.Write((byte)3);
                writer.Write((sbyte)value);
                return;
            }

            if (actual == typeof(short))
            {
                writer.Write((byte)4);
                writer.Write((short)value);
                return;
            }

            if (actual == typeof(ushort))
            {
                writer.Write((byte)5);
                writer.Write((ushort)value);
                return;
            }

            if (actual == typeof(int))
            {
                writer.Write((byte)6);
                writer.Write((int)value);
                return;
            }

            if (actual == typeof(uint))
            {
                writer.Write((byte)7);
                writer.Write((uint)value);
                return;
            }

            if (actual == typeof(long))
            {
                writer.Write((byte)8);
                writer.Write((long)value);
                return;
            }

            if (actual == typeof(ulong))
            {
                writer.Write((byte)9);
                writer.Write((ulong)value);
                return;
            }
            
            if (actual == typeof(float))
            {
                writer.Write((byte)10);
                writer.Write((float)value);
                return;
            }

            if (actual == typeof(double))
            {
                writer.Write((byte)11);
                writer.Write((double)value);
                return;
            }

            if (actual == typeof(decimal))
            {
                writer.Write((byte)12);
                writer.Write((decimal)value);
                return;
            }

            if (actual == typeof(char))
            {
                writer.Write((byte)13);
                writer.Write((char)value);
                return;
            }

            if (actual == typeof(string))
            {
                writer.Write((byte)14);
                writer.Write((string)value);
                return;
            }

            if (actual == typeof(DateTime))
            {
                DateTime dateTime = (DateTime)value;
                writer.Write((byte)15);
                writer.Write(dateTime.Ticks);
                writer.Write((byte)dateTime.Kind);
                return;
            }

            if (actual == typeof(TimeSpan))
            {
                writer.Write((byte)16);
                writer.Write(((TimeSpan)value).Ticks);
                return;
            }

            if (actual == typeof(Guid))
            {
                writer.Write((byte)17);
                byte[] array = ((Guid)value).ToByteArray();
                writer.Write(array, 0, array.Length);
                return;
            }

            if (actual == typeof(DateTimeOffset))
            {
                DateTimeOffset dateTimeOffset = (DateTimeOffset)value;
                writer.Write((byte)19);
                writer.Write(dateTimeOffset.Ticks);
                writer.Write((short)dateTimeOffset.Offset.TotalMinutes);
                return;
            }

            if (actual == typeof(Uri))
            {
                writer.Write((byte)20);
                writer.Write(((Uri)value).ToString());
                return;
            }

            if (actual == typeof(byte[]))
            {
                byte[] array2 = (byte[])value;
                writer.Write((byte)18);
                writer.Write(array2.Length);
                writer.Write(array2, 0, array2.Length);
                return;
            }

            if (TryGetDictionaryTypes(actual, out Type? keyType, out Type? valueType))
            {
                writer.Write((byte)22);
                List<DictionaryEntry> list = [];
                foreach (DictionaryEntry item in (IDictionary)value)
                {
                    list.Add(item);
                }

                if (list.Count == 0 && value is IEnumerable enumerable)
                {
                    foreach (object item2 in enumerable)
                    {
                        if (item2 == null)
                            continue;

                        Type type = item2.GetType();
                        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(KeyValuePair<, >))
                        {
                            object? obj = type.GetProperty("Key")?.GetValue(item2);
                            object? value3 = type.GetProperty("Value")?.GetValue(item2);
                            if (obj == null)
                            {
                                LogManager.Warn("Null dictionary key.");
                                return;
                            }

                            list.Add(new DictionaryEntry(obj, value3));
                        }
                    }
                }

                writer.Write(list.Count);
                {
                    foreach (DictionaryEntry item3 in list)
                    {
                        WriteValue(writer, item3.Key, keyType!);
                        WriteValue(writer, item3.Value, valueType!);
                    }

                    return;
                }
            }
            
            if (TryGetEnumerableElementType(actual, out Type? elementType))
            {
                writer.Write((byte)21);
                if (value is ICollection collection)
                {
                    writer.Write(collection.Count);
                    {
                        foreach (object item4 in collection)
                        {
                            WriteValue(writer, item4, elementType!);
                        }

                        return;
                    }
                }

                List<object> list2 = [];
                foreach (object item5 in (IEnumerable)value)
                {
                    list2.Add(item5);
                }

                writer.Write(list2.Count);
                {
                    foreach (object item6 in list2)
                    {
                        WriteValue(writer, item6, elementType!);
                    }

                    return;
                }
            }

            TypePlan? plan = GetPlan(actual);
            if (plan == null)
            {
                writer.Write((byte)0);
                return;
            }

            writer.Write((byte)23);
            WriteObjectPayload(writer, value, actual, plan);
        }

        private static object? ReadValue(BinaryReader reader, Type declaredType)
        {
            BinKind binKind;
            try
            {
                binKind = (BinKind)reader.ReadByte();
            }
            catch (EndOfStreamException arg)
            {
                LogManager.Warn($"Truncated bin file (unexpected end of stream).\n{arg}");
                return null;
            }

            switch (binKind)
            {
                case BinKind.Null:
                    if (IsNonNullableValueType(declaredType))
                        LogManager.Warn($"Null value for non nullable '{declaredType.FullName}'.");

                    return null;

            case BinKind.RuntimeTyped:
            {
                string text = reader.ReadString();
                Type? type = ResolveType(text);
                if (type is null)
                {
                    LogManager.Warn($"Unknown runtime type '{text}'.");
                    return null;
                }
                
                object? obj = ReadValue(reader, type);
                if (obj == null)
                    return null;

                Type type2 = Nullable.GetUnderlyingType(declaredType) ?? declaredType;
                if (type2 == typeof(object) || type2.IsAssignableFrom(obj.GetType()))
                    return obj;

                try
                {
                    return Convert.ChangeType(obj, type2, CultureInfo.InvariantCulture);
                }
                catch (Exception arg2)
                {
                    LogManager.Warn($"Cannot assign '{obj.GetType().FullName}' to '{declaredType.FullName}'.\n{arg2}");
                    return null;
                }
            }

            default:
                return ReadInner(reader, binKind, declaredType);
            }
        }

        private static object? ReadInner(BinaryReader reader, BinKind kind, Type declaredType)
        {
            Type type = Nullable.GetUnderlyingType(declaredType) ?? declaredType;
            bool flag = type == typeof(object);
            
            switch (kind)
            {
                case BinKind.Bool:
                    return ConvertToTarget(reader.ReadBoolean(), declaredType, type, flag);

                case BinKind.Byte:
                    return ConvertToTarget(reader.ReadByte(), declaredType, type, flag);

                case BinKind.SByte:
                    return ConvertToTarget(reader.ReadSByte(), declaredType, type, flag);

                case BinKind.Int16:
                    return ConvertToTarget(reader.ReadInt16(), declaredType, type, flag);

                case BinKind.UInt16:
                    return ConvertToTarget(reader.ReadUInt16(), declaredType, type, flag);

                case BinKind.Int32:
                    return ConvertToTarget(reader.ReadInt32(), declaredType, type, flag);

                case BinKind.UInt32:
                    return ConvertToTarget(reader.ReadUInt32(), declaredType, type, flag);

                case BinKind.Int64:
                    return ConvertToTarget(reader.ReadInt64(), declaredType, type, flag);

                case BinKind.UInt64:
                    return ConvertToTarget(reader.ReadUInt64(), declaredType, type, flag);

                case BinKind.Float:
                    return ConvertToTarget(reader.ReadSingle(), declaredType, type, flag);

                case BinKind.Double:
                    return ConvertToTarget(reader.ReadDouble(), declaredType, type, flag);

                case BinKind.Decimal:
                    return ConvertToTarget(reader.ReadDecimal(), declaredType, type, flag);

                case BinKind.Char:
                    return ConvertToTarget(reader.ReadChar(), declaredType, type, flag);

                case BinKind.String:
                {
                    string text2 = reader.ReadString();
                    if (flag || type == typeof(string))
                        return text2;
                        
                    if (type.IsEnum)
                        return Enum.Parse(type, text2, ignoreCase: true);

                    if (type == typeof(Guid))
                        return Guid.Parse(text2);

                    if (type == typeof(TimeSpan))
                        return TimeSpan.Parse(text2, CultureInfo.InvariantCulture);

                    if (type == typeof(DateTime))
                        return DateTime.Parse(text2, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);

                    if (type == typeof(DateTimeOffset))
                        return DateTimeOffset.Parse(text2, CultureInfo.InvariantCulture);

                    if (type == typeof(Uri))
                        return new Uri(text2);

                    if (type == typeof(Version))
                        return Version.Parse(text2);

                    return Convert.ChangeType(text2, type, CultureInfo.InvariantCulture);
                }

                case BinKind.DateTime:
                {
                    long ticks2 = reader.ReadInt64();
                    DateTimeKind kind2 = (DateTimeKind)reader.ReadByte();
                    DateTime dateTime = new(ticks2, kind2);
                    return ConvertToTarget(dateTime, declaredType, type, flag);
                }

                case BinKind.TimeSpan:
                {
                    TimeSpan timeSpan = new(reader.ReadInt64());
                    return ConvertToTarget(timeSpan, declaredType, type, flag);
                }

                case BinKind.Guid:
                {
                    byte[]? array2 = ReadBytesExact(reader, 16);
                    if (array2 == null)
                        return null;

                    Guid guid = new(array2);
                    return ConvertToTarget(guid, declaredType, type, flag);
                }

                case BinKind.DateTimeOffset:
                {
                    long ticks = reader.ReadInt64();
                    short num = reader.ReadInt16();
                    DateTimeOffset dateTimeOffset = new(ticks, TimeSpan.FromMinutes(num));
                    return ConvertToTarget(dateTimeOffset, declaredType, type, flag);
                }

                case BinKind.Uri:
                {
                    string text = reader.ReadString();
                    if (flag || type == typeof(Uri))
                        return new Uri(text);

                    return Convert.ChangeType(text, type, CultureInfo.InvariantCulture);
                }

                case BinKind.ByteArray:
                {
                    int count = reader.ReadInt32();
                    if (!CheckCount(count))
                        return null;

                    byte[]? array = ReadBytesExact(reader, count);
                    if (array == null)
                        return null;

                    if (flag || type == typeof(byte[]) || type.IsAssignableFrom(typeof(byte[])))
                        return array;

                    LogManager.Warn($"Cannot assign byte[] to '{declaredType.FullName}'.");
                    return null;
                }

                case BinKind.List:
                    return ReadList(reader, declaredType, type);

                case BinKind.Dictionary:
                    return ReadDictionary(reader, declaredType, type);

                case BinKind.Object:
                    return ReadObject(reader, type);

                default:
                    LogManager.Warn($"Unknown bin kind tag {(byte)kind}.");
                    return null;
            }
        }

        private static object? ConvertToTarget(object raw, Type declaredType, Type target, bool targetIsObject)
        {
            if (targetIsObject)
                return raw;

            Type type = raw.GetType();
            if (target.IsAssignableFrom(type))
                return raw;

            if (target.IsEnum)
            {
                if (raw is string value)
                    return Enum.Parse(target, value, ignoreCase: true);

                return Enum.ToObject(target, raw);
            }

            if (target == typeof(TimeSpan) && raw is string input)
                return TimeSpan.Parse(input, CultureInfo.InvariantCulture);

            if (target == typeof(Guid) && raw is string input2)
                return Guid.Parse(input2);

            if (target == typeof(DateTime) && raw is string s)
                return DateTime.Parse(s, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);

            try
            {
                return Convert.ChangeType(raw, target, CultureInfo.InvariantCulture);
            }
            catch (Exception arg)
            {
                LogManager.Warn($"Cannot convert '{type.FullName}' to '{declaredType.FullName}'.\n{arg}");
                return null;
            }
        }

        private static object? ReadList(BinaryReader reader, Type declaredType, Type target)
        {
            int num = reader.ReadInt32();
            if (!CheckCount(num))
                return null;
                
            Type type = (TryGetEnumerableElementType(target, out Type? elementType) && elementType is not null) ? elementType : ((TryGetEnumerableElementType(declaredType, out Type? elementType2) && elementType2 is not null) ? elementType2 : typeof(object));
            List<object> list = new(num);
            for (int i = 0; i < num; i++)
            {
                list.Add(ReadValue(reader, type)!);
            }

            if (target.IsArray)
            {
                Type type2 = target.GetElementType() ?? typeof(object);
                Array array = Array.CreateInstance(type2, num);
                for (int j = 0; j < num; j++)
                {
                    array.SetValue(ConvertForCollection(list[j], type2), j);
                }

                return array;
            }

            Type listType = GetListType(type);
            if (target == listType || target.IsAssignableFrom(listType))
            {
                IList? list2 = (IList?)CreateInstance(listType, nonPublic: false);
                foreach (object item in list)
                {
                    list2?.Add(ConvertForCollection(item, type));
                }

                if (target == listType || target.IsAssignableFrom(list2?.GetType()))
                    return list2;
            }

            Type type3 = (target.IsInterface || target.IsAbstract) ? listType : target;
            if (type3.IsArray)
                return list.ToArray();

            object? obj;
            try
            {
                obj = CreateInstance(type3, nonPublic: true);
            }
            catch (Exception arg)
            {
                LogManager.Warn($"Cannot create collection '{target.FullName}'. Add a parameterless constructor.\n{arg}");
                return null;
            }

            if (obj is IList list3)
            {
                foreach (object item2 in list)
                {
                    list3.Add(ConvertForCollection(item2, type));
                }

                return obj;
            }

            MethodInfo methodInfo = type3.GetMethod("Add", BindingFlags.Instance | BindingFlags.Public) ?? type3.GetMethod("Enqueue", BindingFlags.Instance | BindingFlags.Public) ?? type3.GetMethod("Push", BindingFlags.Instance | BindingFlags.Public);
            if (methodInfo is not null && methodInfo.GetParameters().Length == 1)
            {
                Type parameterType = methodInfo.GetParameters()[0].ParameterType;
                Action<object, object>? action = CompileArgCall(type3, methodInfo);
                foreach (object item3 in list)
                {
                    object? obj2 = ConvertForCollection(item3, parameterType);
                    if (action != null)
                    {
                        action(obj!, obj2!);
                        continue;
                    }

                    methodInfo.Invoke(obj, [obj2]);
                }

                return obj;
            }
            
            LogManager.Warn($"Collection '{target.FullName}' has no Add/Enqueue/Push method.");
            return null;
        }

        private static object? ConvertForCollection(object? value, Type dest)
        {
            if (value == null)
                return (dest.IsValueType && Nullable.GetUnderlyingType(dest) is null) ? Activator.CreateInstance(dest) : null;

            if (dest.IsAssignableFrom(value.GetType()))
                return value;

            Type type = Nullable.GetUnderlyingType(dest) ?? dest;
            if (type.IsEnum)
                return Enum.ToObject(type, value);

            return Convert.ChangeType(value, type, CultureInfo.InvariantCulture);
        }

        private static object? ReadDictionary(BinaryReader reader, Type declaredType, Type target)
        {
            int num = reader.ReadInt32();
            if (!CheckCount(num))
                return null;

            (Type, Type) tuple = (TryGetDictionaryTypes(target, out Type? keyType, out Type? valueType) && keyType is not null && valueType is not null) ? (keyType, valueType) : ((!TryGetDictionaryTypes(declaredType, out Type? keyType2, out Type? valueType2) || keyType2 is null || valueType2 is null) ? (typeof(object), typeof(object)) : (keyType2, valueType2));
            Type type = (target.IsInterface || target.IsAbstract) ? GetDictionaryType(tuple.Item1, tuple.Item2) : target;
            object? obj;
            try
            {
                obj = CreateInstance(type, nonPublic: true);
            }
            catch (Exception arg)
            {
                LogManager.Warn($"Cannot create dictionary '{target.FullName}'. Add a parameterless constructor.\n{arg}");
                return null;
            }

            MethodInfo method = type.GetMethod("Add", BindingFlags.Instance | BindingFlags.Public);
            Type dest = tuple.Item1;
            Type dest2 = tuple.Item2;
            bool flag = false;
            if (method is not null)
            {
                ParameterInfo[] parameters = method.GetParameters();
                if (parameters != null && parameters.Length == 2)
                {
                    flag = true;
                    dest = parameters[0].ParameterType;
                    dest2 = parameters[1].ParameterType;
                }
            }

            if (obj is IDictionary dictionary)
            {
                for (int i = 0; i < num; i++)
                {
                    object? key = ConvertForCollection(ReadValue(reader, tuple.Item1), dest);
                    object? value = ConvertForCollection(ReadValue(reader, tuple.Item2), dest2);
                    dictionary.Add(key, value);
                }

                return obj;
            }

            if (!flag)
            {
                LogManager.Warn($"Dictionary '{target.FullName}' has no Add method.");
                return null;
            }

            MethodInfo? methodInfo = method;
            for (int j = 0; j < num; j++)
            {
                object? obj2 = ConvertForCollection(ReadValue(reader, tuple.Item1), dest);
                object? obj3 = ConvertForCollection(ReadValue(reader, tuple.Item2), dest2);
                methodInfo?.Invoke(obj, [obj2, obj3]);
            }

            return obj;
        }

        private static object? CreateInstance(Type type, bool nonPublic)
        {
            if (FactoryCache.TryGetValue(type, out Func<object> value))
                return value();

            ConstructorInfo constructor = type.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null);
            Func<object>? func = CompileFactory(type, constructor);
            if (func != null)
                FactoryCache.TryAdd(type, func);

            return (func != null) ? func() : Activator.CreateInstance(type, nonPublic);
        }

        private static Type GetListType(Type elementType) =>
            ListTypeCache.GetOrAdd(elementType, t => typeof(List<>).MakeGenericType(t));

        private static Type GetDictionaryType(Type keyType, Type valueType) =>
            DictionaryTypeCache.GetOrAdd((keyType, valueType), t => typeof(Dictionary<, >).MakeGenericType(t.Key, t.Value));

        private static Type? ResolveType(string assemblyQualifiedName)
        {
            if (RuntimeTypeCache.TryGetValue(assemblyQualifiedName, out Type value))
                return value;

            Type type = Type.GetType(assemblyQualifiedName, throwOnError: false);
            if (type is not null)
                RuntimeTypeCache.TryAdd(assemblyQualifiedName, type);

            return type;
        }

        private static Func<object, object?>? CompileGetter(Type declaring, PropertyInfo prop)
        {
            if (declaring.IsValueType)
                return null;

            try
            {
                ParameterExpression parameterExpression = Expression.Parameter(typeof(object), "obj");
                Expression body = Expression.Convert(Expression.Property(Expression.Convert(parameterExpression, declaring), prop), typeof(object));
                return Expression.Lambda<Func<object, object>>(body, [parameterExpression]).Compile();
            }
            catch
            {
                return null;
            }
        }

        private static Func<object, object?>? CompileGetter(Type declaring, FieldInfo field)
        {
            if (declaring.IsValueType)
                return null;

            try
            {
                ParameterExpression parameterExpression = Expression.Parameter(typeof(object), "obj");
                Expression body = Expression.Convert(Expression.Field(Expression.Convert(parameterExpression, declaring), field), typeof(object));
                return Expression.Lambda<Func<object, object>>(body, [parameterExpression]).Compile();
            }
            catch
            {
                return null;
            }
        }

        private static Action<object, object?>? CompileArgCall(Type declaring, MethodInfo method)
        {
            if (declaring.IsValueType || method.GetParameters().Length != 1)
                return null;

            try
            {
                ParameterExpression parameterExpression = Expression.Parameter(typeof(object), "obj");
                ParameterExpression parameterExpression2 = Expression.Parameter(typeof(object), "val");
                Expression body = Expression.Call(Expression.Convert(parameterExpression, declaring), method, Expression.Convert(parameterExpression2, method.GetParameters()[0].ParameterType));
                return Expression.Lambda<Action<object, object?>>(body, [parameterExpression, parameterExpression2]).Compile();
            }
            catch
            {
                return null;
            }
        }

        private static Action<object, object?>? CompileSetter(Type declaring, FieldInfo field)
        {
            if (declaring.IsValueType || field.IsInitOnly)
                return null;

            try
            {
                ParameterExpression parameterExpression = Expression.Parameter(typeof(object), "obj");
                ParameterExpression parameterExpression2 = Expression.Parameter(typeof(object), "val");
                Expression body = Expression.Assign(Expression.Field(Expression.Convert(parameterExpression, declaring), field), Expression.Convert(parameterExpression2, field.FieldType));
                return Expression.Lambda<Action<object, object?>>(body, [parameterExpression, parameterExpression2]).Compile();
            }
            catch
            {
                return null;
            }
        }

        private static Func<object>? CompileFactory(Type type, ConstructorInfo? ctor)
        {
            try
            {
                NewExpression expression;
                if (ctor is not null)
                {
                    expression = Expression.New(ctor);
                }
                else
                {
                    if (!type.IsValueType)
                        return null;

                    expression = Expression.New(type);
                }

                return Expression.Lambda<Func<object>>(Expression.Convert(expression, typeof(object)), []).Compile();
            }
            catch
            {
                return null;
            }
        }

        private static Func<object?[], object>? CompileFactory(ConstructorInfo ctor)
        {
            try
            {
                ParameterInfo[] parameters = ctor.GetParameters();
                ParameterExpression parameterExpression = Expression.Parameter(typeof(object[]), "args");
                Expression[] array = new Expression[parameters.Length];
                for (int i = 0; i < parameters.Length; i++)
                {
                    array[i] = Expression.Convert(Expression.ArrayIndex(parameterExpression, Expression.Constant(i)), parameters[i].ParameterType);
                }

                Expression body = Expression.Convert(Expression.New(ctor, array), typeof(object));
                return Expression.Lambda<Func<object?[], object>>(body, [parameterExpression]).Compile();
            }
            catch
            {
                return null;
            }
        }

        private static void WriteObjectPayload(BinaryWriter writer, object value, Type actual, TypePlan plan)
        {
            writer.Write(plan.Members.Length);
            MemberAccessor[] members = plan.Members;
            foreach (MemberAccessor memberAccessor in members)
            {
                writer.Write(memberAccessor.Name);
                object? value2;
                try
                {
                    value2 = (memberAccessor.Getter != null) ? memberAccessor.Getter(value) : null;
                }
                catch (Exception arg)
                {
                    LogManager.Warn($"Cannot read '{actual.FullName}.{memberAccessor.Name}'.\n{arg}");
                    writer.Write((byte)0);
                    continue;
                }

                WriteValue(writer, value2, memberAccessor.MemberType);
            }
        }

        private static object? ReadObject(BinaryReader reader, Type target)
        {
            if (target.IsInterface || target.IsAbstract)
            {
                LogManager.Warn($"Cannot create abstract '{target.FullName}' without runtime type info.");
                return null;
            }

            if (IsWellKnownStruct(target))
            {
                LogManager.Warn($"Unsupported object type '{target.FullName}'.");
                return null;
            }

            TypePlan? plan = GetPlan(target);
            if (plan == null)
                return null;
            
            int num = reader.ReadInt32();
            if (!CheckCount(num))
                return null;
            
            Dictionary<string, object> dictionary = new(num, StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < num; i++)
            {
                string key = reader.ReadString();
                if (plan.ByName.TryGetValue(key, out MemberAccessor value))
                {
                    object? value2 = ReadValue(reader, value.MemberType);
                    dictionary[value.Name] = value2!;
                }
                else
                    SkipValue(reader);
            }
            
            MemberAccessor[] members = plan.Members;
            foreach (MemberAccessor memberAccessor in members)
            {
                if (memberAccessor.Required && !dictionary.ContainsKey(memberAccessor.Name))
                {
                    LogManager.Warn($"Missing required '{target.FullName}.{memberAccessor.Name}.");
                    return null;
                }
            }

            object? obj;
            if (plan.Ctor is null || plan.Ctor.GetParameters().Length == 0)
            {
                try
                {
                    obj = (plan.Create != null) ? plan.Create() : CreateInstance(target, nonPublic: true);
                }
                catch (Exception arg)
                {
                    LogManager.Warn($"Cannot create '{target.FullName}'.\n{arg}");
                    return null;
                }

                foreach (KeyValuePair<string, object> item in dictionary)
                {
                    MemberAccessor memberAccessor2 = plan.ByName[item.Key];
                    if (memberAccessor2.CanWrite && memberAccessor2.Setter != null)
                        memberAccessor2.Setter(obj!, Coerce(item.Value, memberAccessor2.MemberType));
                }

                return obj;
            }

            ParameterInfo[] parameters = plan.Ctor.GetParameters();
            object?[] array = new object[parameters.Length];
            HashSet<string> hashSet = new(StringComparer.OrdinalIgnoreCase);
            for (int k = 0; k < parameters.Length; k++)
            {
                MemberAccessor? memberAccessor3 = plan.CtorMembers[k];
                if (memberAccessor3 != null)
                    hashSet.Add(memberAccessor3.Name);
                    
                if (memberAccessor3 != null && dictionary.TryGetValue(memberAccessor3.Name, out var value3))
                {
                    array[k] = Coerce(value3, parameters[k].ParameterType);
                }
                else if (parameters[k].HasDefaultValue)
                {
                    array[k] = parameters[k].DefaultValue;
                }
                else if (!parameters[k].ParameterType.IsValueType || Nullable.GetUnderlyingType(parameters[k].ParameterType) is not null)
                {
                    array[k] = null!;
                }
                else
                    array[k] = Activator.CreateInstance(parameters[k].ParameterType);
            }

            try
            {
                obj = (plan.CreateWithArgs != null) ? plan.CreateWithArgs(array) : plan.Ctor.Invoke(array);
            }
            catch (Exception arg2)
            {
                LogManager.Warn($"Cannot construct '{target.FullName}'.\n{arg2}");
                return null;
            }

            foreach (KeyValuePair<string, object> item2 in dictionary)
            {
                MemberAccessor memberAccessor4 = plan.ByName[item2.Key];
                if ((!hashSet.Contains(item2.Key) || !IsCtorOnly(memberAccessor4)) && memberAccessor4.CanWrite && memberAccessor4.Setter != null)
                    memberAccessor4.Setter(obj, Coerce(item2.Value, memberAccessor4.MemberType));
            }

            return obj;
        }

        private static bool IsCtorOnly(MemberAccessor m) =>
            !HasUsableSetter(m.Prop, m.Field);

        private static void PopulateInto(BinaryReader reader, object instance, Type target)
        {
            BinKind binKind;
            try
            {
                binKind = (BinKind)reader.ReadByte();
            }
            catch (EndOfStreamException arg)
            {
                LogManager.Warn($"Truncated bin file.\n{arg}");
                return;
            }

            switch (binKind)
            {
                case BinKind.Null:
                    LogManager.Warn("Null payload for existing instance.");
                    break;
                    
                case BinKind.RuntimeTyped:
                {
                    string text = reader.ReadString();
                    Type? type = ResolveType(text);
                    if (type is null)
                    {
                        LogManager.Warn($"Unknown runtime type '{text}'.");
                    }
                    else if (!target.IsAssignableFrom(type))
                    {
                        LogManager.Warn($"Runtime type '{type.FullName}' is not '{target.FullName}'.");
                    }
                    else
                        PopulateInto(reader, instance, type);

                    break;
                }
                default:
                    LogManager.Warn($"Expected object payload for '{target.FullName}' but got {binKind}.");
                    break;

                case BinKind.Object:
                {
                    TypePlan? plan = GetPlan(target);
                    if (plan == null)
                        break;

                    int num = reader.ReadInt32();
                    if (!CheckCount(num))
                        break;

                    for (int i = 0; i < num; i++)
                    {
                        string key = reader.ReadString();
                        if (plan.ByName.TryGetValue(key, out MemberAccessor value))
                        {
                            object? value2 = ReadValue(reader, value.MemberType);
                            if (value.CanWrite && value.Setter != null)
                                value.Setter(instance, Coerce(value2, value.MemberType));
                        }
                        else
                            SkipValue(reader);
                    }

                    break;
                }
            }
        }

        private static object? Coerce(object? value, Type dest)
        {
            if (value == null)
                return (dest.IsValueType && Nullable.GetUnderlyingType(dest) is null) ? Activator.CreateInstance(dest) : null;

            if (dest.IsAssignableFrom(value.GetType()))
                return value;

            Type type = Nullable.GetUnderlyingType(dest) ?? dest;
            if (type.IsEnum)
            {
                if (value is string value2)
                    return Enum.Parse(type, value2, ignoreCase: true);

                return Enum.ToObject(type, value);
            }

            return Convert.ChangeType(value, type, CultureInfo.InvariantCulture);
        }

        private static void SkipValue(BinaryReader reader)
        {
            BinKind binKind;
            try
            {
                binKind = (BinKind)reader.ReadByte();
            }
            catch (EndOfStreamException arg)
            {
                LogManager.Warn($"Truncated bin file while skipping.\n{arg}");
                return;
            }
            
            switch (binKind)
            {
                case BinKind.Null:
                    break;

                case BinKind.Bool:
                    reader.ReadBoolean();
                    break;

                case BinKind.Byte:
                    reader.ReadByte();
                    break;

                case BinKind.SByte:
                    reader.ReadSByte();
                    break;

                case BinKind.Int16:
                    reader.ReadInt16();
                    break;

                case BinKind.UInt16:
                    reader.ReadUInt16();
                    break;

                case BinKind.Int32:
                    reader.ReadInt32();
                    break;

                case BinKind.UInt32:
                    reader.ReadUInt32();
                    break;
                    
                case BinKind.Int64:
                    reader.ReadInt64();
                    break;
                    
                case BinKind.UInt64:
                    reader.ReadUInt64();
                    break;

                case BinKind.Float:
                    reader.ReadSingle();
                    break;
                    
                case BinKind.Double:
                    reader.ReadDouble();
                    break;

                case BinKind.Decimal:
                    reader.ReadDecimal();
                    break;

                case BinKind.Char:
                    reader.ReadChar();
                    break;

                case BinKind.String or BinKind.Uri:
                    reader.ReadString();
                    break;

                case BinKind.DateTime:
                    reader.ReadInt64();
                    reader.ReadByte();
                    break;

                case BinKind.TimeSpan:
                    reader.ReadInt64();
                    break;
                    
                case BinKind.Guid:
                    ReadBytesExact(reader, 16);
                    break;

                case BinKind.DateTimeOffset:
                    reader.ReadInt64();
                    reader.ReadInt16();
                    break;

                case BinKind.ByteArray:
                {
                    int count = reader.ReadInt32();
                    if (CheckCount(count))
                        ReadBytesExact(reader, count);

                    break;
                }

                case BinKind.RuntimeTyped:
                    reader.ReadString();
                    SkipValue(reader);
                    break;

                case BinKind.List:
                {
                    int num3 = reader.ReadInt32();
                    if (CheckCount(num3))
                    {
                        for (int k = 0; k < num3; k++)
                        {
                            SkipValue(reader);
                        }
                    }

                    break;
                }

                case BinKind.Dictionary:
                {
                    int num2 = reader.ReadInt32();
                    if (CheckCount(num2))
                    {
                        for (int j = 0; j < num2; j++)
                        {
                            SkipValue(reader);
                            SkipValue(reader);
                        }
                    }
                    
                    break;
                }

                case BinKind.Object:
                {
                    int num = reader.ReadInt32();
                    if (CheckCount(num))
                    {
                        for (int i = 0; i < num; i++)
                        {
                            reader.ReadString();
                            SkipValue(reader);
                        }
                    }

                    break;
                }

                default:
                    LogManager.Warn($"Unknown bin kind tag {(byte)binKind}.");
                    break;
            }
        }

        private static TypePlan? GetPlan(Type type)
        {
            if (PlanCache.TryGetValue(type, out TypePlan value))
                return value;

            TypePlan? typePlan = BuildPlan(type);
            if (typePlan != null)
                PlanCache.TryAdd(type, typePlan);

            return typePlan;
        }

        private static TypePlan? BuildPlan(Type type)
        {
            if (IsPrimitiveLike(type))
            {
                LogManager.Warn($"'{type.FullName}' has no object plan.");
                return null;
            }

            List<MemberAccessor> list = [];
            PropertyInfo[] properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public);
            foreach (PropertyInfo propertyInfo in properties)
            {
                if (propertyInfo.GetIndexParameters().Length != 0 || propertyInfo.GetCustomAttribute<BinaryIgnoreAttribute>() != null || HasAttributeNamed(propertyInfo, "YamlIgnoreAttribute") || HasAttributeNamed(propertyInfo, "JsonIgnoreAttribute"))
                    continue;

                MethodInfo getMethod = propertyInfo.GetGetMethod(nonPublic: false);
                if (getMethod is null)
                    continue;

                BinaryPropertyAttribute customAttribute = propertyInfo.GetCustomAttribute<BinaryPropertyAttribute>();
                string text = customAttribute?.Name ?? propertyInfo.Name;
                if (string.IsNullOrWhiteSpace(text))
                    text = propertyInfo.Name;
                    
                MethodInfo setMethod = propertyInfo.GetSetMethod(nonPublic: true);
                FieldInfo? fieldInfo = null;
                bool flag = setMethod is not null;
                if (!flag)
                {
                    fieldInfo = type.GetField($"<{propertyInfo.Name}>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
                    if (fieldInfo is not null)
                        flag = true;
                }

                Func<object, object?>? func = CompileGetter(type, propertyInfo);
                Func<object, object?> getter = func ?? new Func<object, object>(propertyInfo.GetValue);
                Action<object, object?>? setter = null;
                if (setMethod is not null)
                {
                    MethodInfo s = setMethod;
                    setter = CompileArgCall(type, s) ?? ((obj, val) => s.Invoke(obj, [val]));
                }
                else if (fieldInfo is not null)
                {
                    FieldInfo f = fieldInfo;
                    setter = CompileSetter(type, f) ?? ((obj, val) => f.SetValue(obj, val));
                }

                list.Add(new MemberAccessor
                {
                    Name = text,
                    MemberType = propertyInfo.PropertyType,
                    Prop = propertyInfo,
                    Field = null,
                    Order = customAttribute?.Order ?? 0,
                    Required = customAttribute?.Required ?? false,
                    CanWrite = flag,
                    Getter = getter,
                    Setter = setter
                });
            }

            FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public);
            foreach (FieldInfo fieldInfo2 in fields)
            {
                if (fieldInfo2.GetCustomAttribute<BinaryIgnoreAttribute>() == null && !HasAttributeNamed(fieldInfo2, "YamlIgnoreAttribute") && !HasAttributeNamed(fieldInfo2, "JsonIgnoreAttribute"))
                {
                    BinaryPropertyAttribute customAttribute2 = fieldInfo2.GetCustomAttribute<BinaryPropertyAttribute>();
                    string name = customAttribute2?.Name ?? fieldInfo2.Name;
                    FieldInfo f2 = fieldInfo2;
                    Func<object, object?>? getter2 = CompileGetter(type, fieldInfo2) ?? (obj => f2.GetValue(obj));
                    Action<object, object?>? setter2 = fieldInfo2.IsInitOnly ? null : CompileSetter(type, fieldInfo2) ?? ((obj, val) => f2.SetValue(obj, val));

                    list.Add(new MemberAccessor
                    {
                        Name = name,
                        MemberType = fieldInfo2.FieldType,
                        Prop = null,
                        Field = fieldInfo2,
                        Order = customAttribute2?.Order ?? 0,
                        Required = customAttribute2?.Required ?? false,
                        CanWrite = !fieldInfo2.IsInitOnly,
                        Getter = getter2,
                        Setter = setter2
                    });
                }
            }

            list.Sort((a, b) =>
            {
                int num9 = a.Order.CompareTo(b.Order);
                return (num9 != 0) ? num9 : string.Compare(a.Name, b.Name, StringComparison.Ordinal);
            });

            ConstructorInfo[] constructors = type.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            ConstructorInfo? constructorInfo = null;
            MemberAccessor[] array = [];
            if (type.IsValueType)
            {
                ConstructorInfo? constructorInfo2 = null;
                MemberAccessor[] array2 = [];
                int num2 = -1;
                ConstructorInfo[] array3 = constructors;
                foreach (ConstructorInfo constructorInfo3 in array3)
                {
                    ParameterInfo[] parameters = constructorInfo3.GetParameters();
                    if (parameters.Length == 0)
                        continue;
                    
                    MemberAccessor[] array4 = new MemberAccessor[parameters.Length];
                    bool flag2 = true;
                    for (int num4 = 0; num4 < parameters.Length; num4++)
                    {
                        MemberAccessor? memberAccessor = FindMember(list, parameters[num4].Name ?? string.Empty);
                        if (memberAccessor == null)
                        {
                            flag2 = false;
                            break;
                        }

                        array4[num4] = memberAccessor;
                    }

                    if (flag2 && parameters.Length > num2)
                    {
                        num2 = parameters.Length;
                        constructorInfo2 = constructorInfo3;
                        array2 = array4;
                    }
                }

                bool flag3 = list.Any(m => !m.CanWrite);
                if (constructorInfo2 is not null && (flag3 || num2 > 0))
                {
                    constructorInfo = constructorInfo2;
                    array = array2;
                }
            }
            else
            {
                constructorInfo = constructors.FirstOrDefault(c => c.GetParameters().Length == 0);
                if (constructorInfo is not null)
                {
                    array = [];
                }
                else
                {
                    ConstructorInfo? constructorInfo4 = null;
                    MemberAccessor[] array5 = [];
                    int num5 = -1;
                    ConstructorInfo[] array6 = constructors;
                    foreach (ConstructorInfo constructorInfo5 in array6)
                    {
                        ParameterInfo[] parameters2 = constructorInfo5.GetParameters();
                        MemberAccessor[] array7 = new MemberAccessor[parameters2.Length];
                        bool flag4 = true;
                        for (int num7 = 0; num7 < parameters2.Length; num7++)
                        {
                            MemberAccessor? memberAccessor2 = FindMember(list, parameters2[num7].Name ?? string.Empty);
                            if (memberAccessor2 == null)
                            {
                                flag4 = false;
                                break;
                            }

                            array7[num7] = memberAccessor2;
                        }

                        if (flag4 && parameters2.Length > num5)
                        {
                            num5 = parameters2.Length;
                            constructorInfo4 = constructorInfo5;
                            array5 = array7;
                        }
                    }
                    if (constructorInfo4 is not null)
                    {
                        constructorInfo = constructorInfo4;
                        array = array5;
                    }
                }
            }

            HashSet<string> coveredByCtor = new(StringComparer.OrdinalIgnoreCase);
            if (constructorInfo is not null)
            {
                MemberAccessor[] array8 = array;
                foreach (MemberAccessor memberAccessor3 in array8)
                {
                    if (memberAccessor3 != null)
                        coveredByCtor.Add(memberAccessor3.Name);
                }
            }

            list.RemoveAll(m => !m.CanWrite && !coveredByCtor.Contains(m.Name));
            if (!type.IsValueType && constructorInfo is null && constructors.Length != 0)
            {
                LogManager.Warn($"'{type.FullName}' has no parameterless constructor and no constructor matching its properties.");
                return null;
            }
            
            if (!type.IsValueType && constructorInfo is null && constructors.Length == 0)
                constructorInfo = null;

            Dictionary<string, MemberAccessor> dictionary = new(StringComparer.OrdinalIgnoreCase);
            foreach (MemberAccessor item in list)
            {
                if (!dictionary.ContainsKey(item.Name))
                    dictionary.Add(item.Name, item);

                if (item.Prop is not null)
                {
                    if (!dictionary.ContainsKey(item.Prop.Name))
                        dictionary.Add(item.Prop.Name, item);
                }
                else if (item.Field is not null && !dictionary.ContainsKey(item.Field.Name))
                {
                    dictionary.Add(item.Field.Name, item);
                }
            }
            Func<object>? create = (constructorInfo is null || constructorInfo.GetParameters().Length == 0) ? CompileFactory(type, constructorInfo) : null;
            Func<object?[], object>? createWithArgs = (constructorInfo is not null && constructorInfo.GetParameters().Length != 0) ? CompileFactory(constructorInfo) : null;
            return new TypePlan
            {
                Members = list.ToArray(),
                ByName = dictionary,
                Ctor = constructorInfo,
                CtorMembers = array,
                Create = create,
                CreateWithArgs = createWithArgs
            };
        }

        private static MemberAccessor? FindMember(List<MemberAccessor> members, string paramName)
        {
            if (string.IsNullOrWhiteSpace(paramName))
                return null;

            foreach (MemberAccessor member in members)
            {
                string a = member.Prop?.Name ?? member.Field?.Name ?? member.Name;
                if (string.Equals(a, paramName, StringComparison.OrdinalIgnoreCase) || string.Equals(member.Name, paramName, StringComparison.OrdinalIgnoreCase))
                    return member;
            }

            return null;
        }

        private static bool HasUsableSetter(PropertyInfo? prop, FieldInfo? field)
        {
            if (prop?.GetSetMethod(nonPublic: true) is not null)
                return true;

            return field is not null;
        }

        private static bool HasAttributeNamed(MemberInfo member, string attributeTypeName)
        {
            foreach (Attribute customAttribute in member.GetCustomAttributes())
            {
                if (customAttribute.GetType().Name == attributeTypeName)
                    return true;
            }

            return false;
        }

        private static bool IsNonNullableValueType(Type t) =>
            t.IsValueType && Nullable.GetUnderlyingType(t) is null;

        private static bool IsPrimitiveLike(Type t)
        {
            Type type = Nullable.GetUnderlyingType(t) ?? t;
            if (type.IsEnum)
                return true;

            return type == typeof(bool) || type == typeof(byte) || type == typeof(sbyte) || type == typeof(short) || type == typeof(ushort) || type == typeof(int) || type == typeof(uint) || type == typeof(long) || type == typeof(ulong) || type == typeof(float) || type == typeof(double) || type == typeof(decimal) || type == typeof(char) || type == typeof(string) || type == typeof(DateTime) || type == typeof(TimeSpan) || type == typeof(Guid) || type == typeof(DateTimeOffset) || type == typeof(Uri) || type == typeof(byte[]) || type == typeof(Version);
        }

        private static bool IsWellKnownStruct(Type t) =>
            t == typeof(DateTime) || t == typeof(TimeSpan) || t == typeof(Guid) || t == typeof(DateTimeOffset) || t == typeof(decimal);

        private static bool TryGetDictionaryTypes(Type type, out Type? keyType, out Type? valueType)
        {
            DictionaryShape orAdd = DictionaryShapeCache.GetOrAdd(type, ComputeDictionaryShape);
            keyType = orAdd.Key;
            valueType = orAdd.Value;
            return orAdd.IsDictionary;
        }

        private static DictionaryShape ComputeDictionaryShape(Type type)
        {
            if (type == typeof(string))
                return default;
            
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(KeyValuePair<, >))
                return default;
            
            if (type.IsArray)
                return default;

            if (type.IsGenericType)
            {
                Type genericTypeDefinition = type.GetGenericTypeDefinition();
                if (genericTypeDefinition == typeof(Dictionary<, >) || genericTypeDefinition == typeof(SortedDictionary<, >) || genericTypeDefinition == typeof(SortedList<, >))
                {
                    Type[] genericArguments = type.GetGenericArguments();
                    return new DictionaryShape(isDictionary: true, genericArguments[0], genericArguments[1]);
                }
            }

            Type[] interfaces = type.GetInterfaces();
            Type type2 = interfaces.FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IDictionary<, >)) ?? interfaces.FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IReadOnlyDictionary<, >));
            if (type2 is not null)
            {
                Type[] genericArguments2 = type2.GetGenericArguments();
                return new DictionaryShape(isDictionary: true, genericArguments2[0], genericArguments2[1]);
            }

            if (typeof(IDictionary).IsAssignableFrom(type))
                return new DictionaryShape(isDictionary: true, typeof(object), typeof(object));
            
            return default;
        }

        private static bool TryGetEnumerableElementType(Type type, out Type? elementType)
        {
            EnumerableShape orAdd = EnumerableShapeCache.GetOrAdd(type, ComputeEnumerableShape);
            elementType = orAdd.Element;
            return orAdd.IsEnumerable;
        }

        private static EnumerableShape ComputeEnumerableShape(Type type)
        {
            if (type == typeof(string) || type == typeof(byte[]))
                return default;
            
            if (type.IsArray)
            {
                if (type.GetArrayRank() != 1)
                    return default;

                Type elementType = type.GetElementType();
                return (elementType is null) ? default : new EnumerableShape(isEnumerable: true, elementType);
            }
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(KeyValuePair<, >))
                return default;
            
            if (type.IsGenericType)
            {
                Type genericTypeDefinition = type.GetGenericTypeDefinition();
                if (genericTypeDefinition == typeof(List<>) || genericTypeDefinition == typeof(HashSet<>) || genericTypeDefinition == typeof(Queue<>) || genericTypeDefinition == typeof(Stack<>) || genericTypeDefinition == typeof(LinkedList<>) || genericTypeDefinition == typeof(SortedSet<>))
                    return new EnumerableShape(isEnumerable: true, type.GetGenericArguments()[0]);

                if (genericTypeDefinition == typeof(IEnumerable<>))
                    return new EnumerableShape(isEnumerable: true, type.GetGenericArguments()[0]);
            }

            Type type2 = type.GetInterfaces().FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));
            if (type2 is not null)
                return new EnumerableShape(isEnumerable: true, type2.GetGenericArguments()[0]);
            
            return default;
        }

        private static bool CheckCount(int count)
        {
            if (count < 0 || count > MaxEntries)
            {
                LogManager.Warn($"Invalid bin collection count: {count}.");
                return false;
            }

            return true;
        }

        private static byte[]? ReadBytesExact(BinaryReader reader, int count)
        {
            if (count < 0)
            {
                LogManager.Warn($"Invalid byte length: {count}.");
                return null;
            }

            byte[] array = reader.ReadBytes(count);
            if (array.Length != count)
            {
                LogManager.Warn($"Truncated bin file (expected {count} bytes, got {array.Length}).");
                return null;
            }

            return array;
        }
    }
}
