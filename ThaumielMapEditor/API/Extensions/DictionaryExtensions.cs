// -----------------------------------------------------------------------
// <copyright file="DictionaryExtensions.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using ThaumielMapEditor.API.Helpers;
using UnityEngine;

namespace ThaumielMapEditor.API.Extensions
{
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

            try
            {
                string enumStr = value.ToString()!.Replace(" ", "");
                result = (T)Enum.Parse(typeof(T), enumStr, ignoreCase: true);
                return true;
            }
            catch
            {
                return false;
            }
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

                return false;
            }
            catch
            {
                return false;
            }
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

            if (value is string str)
            {
                str = str.Trim();

                if (str.StartsWith("#"))
                    return ColorUtility.TryParseHtmlString(str, out color);

                string[] parts = str.Split(',');
                if (parts.Length is 3 or 4)
                {
                    if (float.TryParse(parts[0].Trim(), out float r) && float.TryParse(parts[1].Trim(), out float g) && float.TryParse(parts[2].Trim(), out float b))
                    {
                        float a = parts.Length == 4 && float.TryParse(parts[3].Trim(), out float pa) ? pa : 1f;
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

            colorDict.TryConvertValue("r", out float dr);
            colorDict.TryConvertValue("g", out float dg);
            colorDict.TryConvertValue("b", out float db);
            float da = colorDict.TryConvertValue("a", out float da2) ? da2 : 1f;

            color = new Color(dr, dg, db, da);
            return true;
        }

        private static object? ConvertFromDictionary(object? item, Type targetType)
        {
            if (item is null)
                return null;

            if (targetType.IsInstanceOfType(item))
                return item;

            if (targetType.IsEnum)
            {
                string enumStr = item.ToString()!.Replace(" ", "");
                return Enum.Parse(targetType, enumStr, ignoreCase: true);
            }

            if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(List<>))
                return ConvertToListByType(item, targetType);

            if (item is Dictionary<object, object> untyped)
                item = untyped.Where(k => k.Key != null).ToDictionary(k => k.Key.ToString()!, k => k.Value);

            if (item is Dictionary<string, object> dictItem)
                return ConvertDictionaryToObject(dictItem, targetType);

            return Convert.ChangeType(item, targetType);
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

        private static object ConvertDictionaryToObject(Dictionary<string, object> dict, Type targetType)
        {
            object obj = Activator.CreateInstance(targetType)!;
            PropertyInfo[] properties = GetCachedProperties(targetType);

            foreach (PropertyInfo prop in properties)
            {
                if (!prop.CanWrite)
                    continue;

                if (!dict.TryGetValue(prop.Name, out object? propValue))
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

        private static PropertyInfo[] GetCachedProperties(Type type)
        {
            if (!PropertyCache.TryGetValue(type, out var props))
            {
                props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                PropertyCache.Add(type, props);
            }

            return props;
        }
    }
}