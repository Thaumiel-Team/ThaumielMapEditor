// -----------------------------------------------------------------------
// <copyright file="BinaryTypeNames.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU Lesser General Public License v3.0 (LGPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using ThaumielMapEditor.API.Helpers;

namespace ThaumielMapEditor.API.Binary
{
    public static class BinaryTypeNames
    {
        private static readonly ConcurrentDictionary<Type, string> NameCache = new();
        private static readonly ConcurrentDictionary<string, Type> Registered = new(StringComparer.Ordinal);
        private static readonly ConcurrentDictionary<string, Type> ResolveCache = new(StringComparer.Ordinal);
        private static readonly ConcurrentDictionary<string, byte> MissCache = new(StringComparer.Ordinal);
        private static readonly object IndexLock = new();
        private static Dictionary<string, List<Type>>? _index;
        private static int _indexedAssemblyCount = -1;

        static BinaryTypeNames()
        {
            Registered["Object"] = typeof(object);
            NameCache[typeof(object)] = "Object";
        }

        public static void Register(Type type, string alias)
        {
            Registered[alias] = type;
            NameCache[type] = alias;
            ResolveCache.Clear();
            MissCache.Clear();
        }

        public static void Refresh()
        {
            lock (IndexLock)
            {
                _index = null;
                _indexedAssemblyCount = -1;
                ResolveCache.Clear();
                MissCache.Clear();
            }
        }

        public static string GetName(Type type) =>
            NameCache.GetOrAdd(type, Build);

        private static string Build(Type t)
        {
            if (t.IsArray)
                return GetName(t.GetElementType()!) + "[]";

            if (t.IsGenericType)
            {
                string n = t.Name;
                int tick = n.IndexOf('`');
                if (tick >= 0)
                    n = n.Substring(0, tick);

                return n + "<" + string.Join(",", t.GetGenericArguments().Select(GetName)) + ">";
            }

            return t.Name;
        }

        public static Type? Resolve(string name)
        {
            if (ResolveCache.TryGetValue(name, out Type? cached))
                return cached;

            if (MissCache.ContainsKey(name))
            {
                if (!TryRefreshIndexForNewAssemblies())
                    return null;
            }

            Type? resolved = ResolveCore(name);
            if (resolved is not null)
            {
                ResolveCache.TryAdd(name, resolved);
                return resolved;
            }

            if (TryRefreshIndexForNewAssemblies())
            {
                resolved = ResolveCore(name);
                if (resolved is not null)
                {
                    ResolveCache.TryAdd(name, resolved);
                    return resolved;
                }
            }

            MissCache.TryAdd(name, 0);
            return resolved;
        }

        private static Type? ResolveCore(string name)
        {
            string n = name;
            if (n.Contains(", ") || n.Contains('.'))
            {
                Type? legacy = Type.GetType(n, throwOnError: false);
                if (legacy is not null)
                    return legacy;

                n = StripLegacy(n);
            }

            int pos = 0;
            return Parse(n, ref pos);
        }

        private static string StripLegacy(string s)
        {
            int comma = s.IndexOf(',');
            if (comma >= 0 && !s.Contains('<'))
                s = s.Substring(0, comma);

            return s.Trim();
        }

        private static Type? Parse(string s, ref int pos)
        {
            int start = pos;
            while (pos < s.Length && s[pos] is not ('<' or ',' or '>' or '['))
            {
                pos++;
            }

            string ident = s.Substring(start, pos - start);
            List<Type> args = [];

            if (pos < s.Length && s[pos] == '<')
            {
                pos++;
                while (true)
                {
                    Type? a = Parse(s, ref pos);
                    if (a is null)
                        return null;

                    args.Add(a);
                    if (pos < s.Length && s[pos] == ',')
                    {
                        pos++;
                        continue;
                    }

                    break;
                }
                
                if (pos < s.Length && s[pos] == '>')
                    pos++;
            }

            Type? t = Lookup(args.Count > 0 ? $"{ident}`{args.Count}" : ident);
            if (t is null)
                return null;

            if (args.Count > 0)
                t = t.MakeGenericType(args.ToArray());

            while (pos + 1 < s.Length && s[pos] == '[' && s[pos + 1] == ']')
            {
                t = t.MakeArrayType();
                pos += 2;
            }

            return t;
        }

        private static Type? Lookup(string key)
        {
            if (Registered.TryGetValue(key, out Type? reg))
                return reg;

            Dictionary<string, List<Type>> index = GetIndex();

            if (!index.TryGetValue(key, out var list))
            {
                int dot = Math.Max(key.LastIndexOf('.'), key.LastIndexOf('+'));
                if (dot < 0 || !index.TryGetValue(key.Substring(dot + 1), out list))
                    return null;
            }

            if (list.Count > 1)
                LogManager.Warn($"Ambiguous binary type name '{key}'. Use BinaryTypeNames.Register.");

            return list[0];
        }

        private static Dictionary<string, List<Type>> GetIndex()
        {
            Dictionary<string, List<Type>>? index = Volatile.Read(ref _index);
            if (index != null)
                return index;

            lock (IndexLock)
            {
                if (_index is null)
                {
                    _index = BuildIndex();
                    _indexedAssemblyCount = AppDomain.CurrentDomain.GetAssemblies().Length;
                }

                return _index;
            }
        }

        private static bool TryRefreshIndexForNewAssemblies()
        {
            int count = AppDomain.CurrentDomain.GetAssemblies().Length;
            if (count == Volatile.Read(ref _indexedAssemblyCount))
                return false;

            lock (IndexLock)
            {
                count = AppDomain.CurrentDomain.GetAssemblies().Length;
                if (count == _indexedAssemblyCount)
                    return false;

                _index = BuildIndex();
                _indexedAssemblyCount = count;
                ResolveCache.Clear();
                MissCache.Clear();
                return true;
            }
        }

        private static Dictionary<string, List<Type>> BuildIndex()
        {
            Dictionary<string, List<Type>> map = new(StringComparer.Ordinal);
            foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                HashSet<Type> types = [];
                try
                {
                    types = asm.GetTypes().ToHashSet();
                }
                catch (ReflectionTypeLoadException ex)
                {
                    foreach (Type type in ex.Types)
                    {
                        if (type is null)
                            continue;

                        types.Add(type);
                    }
                }
                catch
                {
                    continue;
                }

                foreach (Type t in types)
                {
                    Add(map, t.Name, t);
                    if (!string.IsNullOrEmpty(t.FullName) && !string.Equals(t.FullName, t.Name, StringComparison.Ordinal))
                        Add(map, t.FullName!, t);
                }
            }
            
            return map;
        }

        private static void Add(Dictionary<string, List<Type>> map, string key, Type type)
        {
            if (!map.TryGetValue(key, out var list))
                map[key] = list = [];

            list.Add(type);
        }
    }
}