// -----------------------------------------------------------------------
// <copyright file="DictionaryExtensions.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using ThaumielMapEditor.API.Attributes;
using ThaumielMapEditor.API.Helpers;
using UnityEngine;
using YamlDotNet.Serialization;

namespace ThaumielMapEditor.API.Extensions
{
    [GitBookPage("Extensions/Dictionary")]
    public static class DictionaryExtensions
    {
        public static readonly ConditionalWeakTable<Type, PropertyInfo[]> PropertyCache = new();

        public static object? GetValueOrDefault(this Dictionary<string, object> dict, string key)
            => dict.TryGetValue(key, out var value) ? value : null;

        public static bool TryConvertValue<TKey, TValue, T>(this Dictionary<TKey, TValue> dict, TKey key, out T result)
        {
            result = default!;

            if (!dict.TryGetValue(key, out TValue value) || value is null)
                return false;

            try
            {
                return TryConvertToType(value, out result);
            }
            catch
            {
                return false;
            }
        }

        public static bool TryConvertValue<T>(this Dictionary<string, object> dict, string key, out T result)
            => dict.TryConvertValue<string, object, T>(key, out result);

        public static T GetConvertValue<T>(this Dictionary<string, object> dict, string key)
        {
            dict.TryConvertValue<string, object, T>(key, out T result);
            return result;
        }

        public static T GetConvertedValueOrDefault<TKey, TValue, T>(this Dictionary<TKey, TValue> dict, TKey key, T defaultValue = default!)
            => dict.TryConvertValue(key, out T result) ? result : defaultValue;

        public static bool TryConvertTo<T>(this Dictionary<string, object> dict, out T result) where T : new()
        {
            result = default!;

            try
            {
                object? converted = ConvertFromDictionary(dict, typeof(T));
                if (converted is T typed)
                {
                    result = typed;
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                LogManager.Error($"TryConvertTo<{typeof(T).Name}> failed: {ex.Message}");
                return false;
            }
        }

        public static T ConvertTo<T>(this Dictionary<string, object> dict) where T : new()
        {
            dict.TryConvertTo(out T result);
            return result;
        }

        private static bool TryConvertToType<T>(object value, out T result)
        {
            result = default!;

            if (value is T direct)
            {
                result = direct;
                return true;
            }

            if (typeof(T) == typeof(Color))
            {
                if (TryConvertToColor(value, out Color color))
                {
                    result = (T)(object)color;
                    return true;
                }

                return false;
            }

            if (typeof(T).IsEnum)
                return TryConvertToEnum(value, out result);

            if (typeof(T) == typeof(Vector2) || typeof(T) == typeof(Vector3) || typeof(T) == typeof(Vector4))
                return TryConvertToVector(value, out result);

            if (typeof(T).IsGenericType && typeof(T).GetGenericTypeDefinition() == typeof(List<>))
                return TryConvertToList(value, out result);

            if (typeof(T).IsClass && typeof(T) != typeof(string))
                return TryConvertToClass(value, out result);

            result = (T)Convert.ChangeType(value, typeof(T));
            return true;
        }

        private static bool TryConvertToEnum<T>(object value, out T result)
        {
            result = default!;

            string? enumStr = value switch
            {
                string s => s.Replace(" ", ""),
                Enum => value.ToString(),
                _ => value.ToString()?.Replace(" ", ""),
            };

            if (!string.IsNullOrEmpty(enumStr))
            {
                try
                {
                    object? parsed = Enum.Parse(typeof(T), enumStr, ignoreCase: true);
                    if (parsed is T typed)
                    {
                        result = typed;
                        return true;
                    }
                }
                catch
                {
                    return false;
                }
            }

            return false;
        }

        private static bool TryConvertToVector<T>(object value, out T result)
        {
            result = default!;

            try
            {
                if (value is Vector2 v2)
                {
                    result = (T)(object)v2;
                    return true;
                }

                if (value is Vector3 v3)
                {
                    result = (T)(object)v3;
                    return true;
                }

                if (value is Vector4 v4)
                {
                    result = (T)(object)v4;
                    return true;
                }

                if (value is string str && TryParseVectorString(str, out Vector4 parsed))
                {
                    if (typeof(T) == typeof(Vector2))
                    {
                        result = (T)(object)new Vector2(parsed.x, parsed.y);
                        return true;
                    }

                    if (typeof(T) == typeof(Vector3))
                    {
                        result = (T)(object)new Vector3(parsed.x, parsed.y, parsed.z);
                        return true;
                    }

                    if (typeof(T) == typeof(Vector4))
                    {
                        result = (T)(object)parsed;
                        return true;
                    }
                }

                if (TryParseVectorComponents(value, out Vector4 components))
                {
                    if (typeof(T) == typeof(Vector2))
                    {
                        result = (T)(object)new Vector2(components.x, components.y);
                        return true;
                    }

                    if (typeof(T) == typeof(Vector3))
                    {
                        result = (T)(object)new Vector3(components.x, components.y, components.z);
                        return true;
                    }

                    if (typeof(T) == typeof(Vector4))
                    {
                        result = (T)(object)components;
                        return true;
                    }
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        private static bool TryParseVectorComponents(object value, out Vector4 result)
        {
            result = default;

            if (value is string)
                return false;

            Dictionary<string, object>? dict = value switch
            {
                Dictionary<string, object> typed => typed,
                Dictionary<object, object> untyped => untyped.Where(kvp => kvp.Key != null).ToDictionary(kvp => kvp.Key.ToString()!, kvp => kvp.Value),
                _ => null
            };

            if (dict != null)
            {
                int found = 0;
                float x = 0f, y = 0f, z = 0f, w = 0f;

                if (TryGetNamedFloat(dict, out x, "x")) found++;
                if (TryGetNamedFloat(dict, out y, "y")) found++;
                if (TryGetNamedFloat(dict, out z, "z")) found++;
                if (TryGetNamedFloat(dict, out w, "w")) found++;

                if (found < 2)
                    return false;

                result = new Vector4(x, y, z, w);
                return true;
            }

            if (value is IEnumerable enumerable)
            {
                List<float> numbers = new(4);
                foreach (object? element in enumerable)
                {
                    if (numbers.Count >= 4)
                        break;

                    if (!TryToSingle(element, out float num))
                        return false;

                    numbers.Add(num);
                }

                if (numbers.Count is < 2 or > 4)
                    return false;

                while (numbers.Count < 4)
                    numbers.Add(0f);

                result = new Vector4(numbers[0], numbers[1], numbers[2], numbers[3]);
                return true;
            }

            return false;
        }

        private static bool TryGetNamedFloat(Dictionary<string, object> dict, out float value, params string[] names)
        {
            foreach (KeyValuePair<string, object> kvp in dict)
            {
                if (kvp.Key == null)
                    continue;

                foreach (string name in names)
                {
                    if (string.Equals(kvp.Key, name, StringComparison.OrdinalIgnoreCase) && TryToSingle(kvp.Value, out value))
                        return true;
                }
            }

            value = 0f;
            return false;
        }

        private static bool TryParseVectorString(string str, out Vector4 result)
        {
            result = default;

            str = str.Trim().Trim('(', ')', '[', ']', '{', '}').Trim();
            if (string.IsNullOrEmpty(str))
                return false;

            string[] parts = str.Split([',', ';', ' '], StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length is < 2 or > 4)
                return false;

            float[] numbers = new float[parts.Length];
            for (int i = 0; i < parts.Length; i++)
            {
                if (!float.TryParse(parts[i].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float num) || float.IsNaN(num) || float.IsInfinity(num))
                    return false;

                numbers[i] = num;
            }

            result = numbers.Length switch
            {
                2 => new Vector4(numbers[0], numbers[1], 0f, 0f),
                3 => new Vector4(numbers[0], numbers[1], numbers[2], 0f),
                4 => new Vector4(numbers[0], numbers[1], numbers[2], numbers[3]),
                _ => default,
            };

            return numbers.Length is 2 or 3 or 4;
        }

        private static bool TryConvertToVectorByType(object value, Type targetType, out object? result)
        {
            result = null;

            if (targetType == typeof(Vector2) && TryConvertToVector<Vector2>(value, out Vector2 v2))
            {
                result = v2;
                return true;
            }

            if (targetType == typeof(Vector3) && TryConvertToVector<Vector3>(value, out Vector3 v3))
            {
                result = v3;
                return true;
            }

            if (targetType == typeof(Vector4) && TryConvertToVector<Vector4>(value, out Vector4 v4))
            {
                result = v4;
                return true;
            }

            return false;
        }

        private static bool TryConvertToList<T>(object value, out T result)
        {
            result = default!;
            Type elementType = typeof(T).GetGenericArguments()[0];
            if (value is not IEnumerable enumerable)
                return false;

            IList list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(elementType))!;
            foreach (object? item in enumerable)
            {
                try
                {
                    list.Add(ConvertFromDictionary(item, elementType));
                }
                catch
                {
                    return false;
                }
            }

            result = (T)list;
            return true;
        }

        private static bool TryConvertToClass<T>(object value, out T result)
        {
            result = default!;

            try
            {
                object? converted = ConvertFromDictionary(value, typeof(T));

                if (converted is T typed)
                {
                    result = typed;
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                LogManager.Warn($"TryConvertToClass<{typeof(T).Name}> failed: {ex.Message}");
                return false;
            }
        }

        private static bool TryConvertToColor(object value, out Color color)
        {
            color = default;

            if (value is Color direct)
            {
                color = direct;
                return true;
            }

            if (value is string str)
            {
                str = str.Trim();

                if (NamedColors.TryGetValue(str, out Color named))
                {
                    color = named;
                    return true;
                }

                if (str.StartsWith("#"))
                    return ColorUtility.TryParseHtmlString(str, out color);

                string[] parts = str.Split(',');
                if (parts.Length is 3 or 4)
                {
                    if (float.TryParse(parts[0].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float r) && float.TryParse(parts[1].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float g) && float.TryParse(parts[2].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float b))
                    {
                        float a = parts.Length == 4 && float.TryParse(parts[3].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float pa) ? pa : 1f;
                        color = new Color(r, g, b, a);
                        return true;
                    }
                }

                return false;
            }

            Dictionary<string, object>? colorDict = value switch
            {
                Dictionary<string, object> typed => typed,
                Dictionary<object, object> untyped => untyped.Where(kvp => kvp.Key != null).ToDictionary(kvp => kvp.Key.ToString()!, kvp => kvp.Value),
                _ => null
            };

            if (colorDict is null)
                return false;

            TryGetNamedFloat(colorDict, out float dr, "r", "x");
            TryGetNamedFloat(colorDict, out float dg, "g", "y");
            TryGetNamedFloat(colorDict, out float db, "b", "z");
            float da = TryGetNamedFloat(colorDict, out float parsedAlpha, "a", "w") ? parsedAlpha : 1f;

            color = new Color(dr, dg, db, da);
            return true;
        }

        private static readonly Dictionary<string, Color> NamedColors = new(StringComparer.OrdinalIgnoreCase)
        {
            ["black"] = Color.black,
            ["blue"] = Color.blue,
            ["cyan"] = Color.cyan,
            ["gray"] = Color.gray,
            ["grey"] = Color.grey,
            ["green"] = Color.green,
            ["magenta"] = Color.magenta,
            ["red"] = Color.red,
            ["white"] = Color.white,
            ["yellow"] = Color.yellow,
            ["clear"] = Color.clear,
        };

        private static bool TryToSingle(object? value, out float result)
        {
            result = 0f;

            if (value == null)
                return false;

            try
            {
                if (value is string s)
                {
                    if (!float.TryParse(s.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out result))
                        return false;
                }
                else
                {
                    result = Convert.ToSingle(value, CultureInfo.InvariantCulture);
                }

                return !float.IsNaN(result) && !float.IsInfinity(result);
            }
            catch
            {
                result = 0f;
                return false;
            }
        }

        private static object? ConvertFromDictionary(object? item, Type targetType)
        {
            if (item is null)
                return null;

            if (targetType.IsInstanceOfType(item))
                return item;

            if (targetType == typeof(Color))
            {
                if (TryConvertToColor(item, out Color color))
                    return color;

                LogManager.Warn($"Could not convert '{item}' to Color. Using default (transparent black).");
                return default(Color);
            }

            if (targetType == typeof(Vector2) || targetType == typeof(Vector3) || targetType == typeof(Vector4))
            {
                if (TryConvertToVectorByType(item, targetType, out object? vector) && vector != null)
                    return vector;

                LogManager.Warn($"Could not convert '{item}' to {targetType.Name}. Using default.");
                return Activator.CreateInstance(targetType);
            }

            if (targetType.IsEnum)
            {
                string? enumStr = item.ToString()?.Replace(" ", "");
                if (!string.IsNullOrEmpty(enumStr))
                {
                    try
                    {
                        return Enum.Parse(targetType, enumStr, ignoreCase: true);
                    }
                    catch
                    {
                        return Activator.CreateInstance(targetType);
                    }
                }

                return Activator.CreateInstance(targetType);
            }

            if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(List<>))
                return ConvertToListByType(item, targetType);

            if (item is Dictionary<object, object> untyped)
                item = untyped.Where(k => k.Key != null).ToDictionary(k => k.Key.ToString()!, k => k.Value);

            if (item is Dictionary<string, object> dictItem)
                return ConvertDictionaryToObject(dictItem, targetType);

            try
            {
                return Convert.ChangeType(item, targetType, CultureInfo.InvariantCulture);
            }
            catch
            {
                return targetType.IsValueType ? Activator.CreateInstance(targetType) : null;
            }
        }

        private static object? ConvertToListByType(object item, Type targetType)
        {
            Type elementType = targetType.GetGenericArguments()[0];

            if (item is not IEnumerable enumerable)
                return null;

            IList list = (IList)Activator.CreateInstance(targetType)!;

            foreach (object? element in enumerable)
            {
                list.Add(ConvertFromDictionary(element, elementType));
            }

            return list;
        }

        public static void PopulateFrom<T>(this Dictionary<string, object> dict, T target)
        {
            PropertyInfo[] properties = GetCachedProperties(target!.GetType());

            foreach (PropertyInfo prop in properties)
            {
                if (!prop.CanWrite || prop.GetCustomAttribute<YamlIgnoreAttribute>() != null)
                    continue;

                string key = GetYamlAlias(prop) ?? prop.Name;
                if (!dict.TryGetValue(key, out object? propValue))
                    continue;

                try
                {
                    prop.SetValue(target, ConvertFromDictionary(propValue, prop.PropertyType));
                }
                catch (Exception ex)
                {
                    LogManager.Warn($"Failed to set property '{prop.Name}' on {target.GetType().Name}: {ex.Message}");
                }
            }
        }

        private static object ConvertDictionaryToObject(Dictionary<string, object> dict, Type targetType)
        {
            object obj = Activator.CreateInstance(targetType)!;
            PropertyInfo[] properties = GetCachedProperties(targetType);

            foreach (PropertyInfo prop in properties)
            {
                if (!prop.CanWrite || prop.GetCustomAttribute<YamlIgnoreAttribute>() != null)
                    continue;

                string key = GetYamlAlias(prop) ?? prop.Name;
                if (!dict.TryGetValue(key, out object? propValue))
                    continue;

                try
                {
                    prop.SetValue(obj, ConvertFromDictionary(propValue, prop.PropertyType));
                }
                catch (Exception ex)
                {
                    LogManager.Warn($"Failed to set property '{prop.Name}' on {targetType.Name}: {ex.Message}");
                }
            }

            return obj;
        }

        private static string? GetYamlAlias(PropertyInfo prop)
            => prop.GetCustomAttribute<YamlMemberAttribute>()?.Alias;

        private static PropertyInfo[] GetCachedProperties(Type type)
        {
            return PropertyCache.GetValue(type, static t => t.GetProperties(BindingFlags.Public | BindingFlags.Instance));
        }
    }
}