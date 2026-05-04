// -----------------------------------------------------------------------
// <copyright file="MapParser.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Linq;
using ThaumielMapEditor.API.Data;
using Random = UnityEngine.Random;

namespace ThaumielMapEditor.API.Helpers
{
    [Obsolete($"{nameof(MapLoader)} has been renamed to {nameof(MapParser)}. Please update your code to use {nameof(MapParser)} instead. This will be removed in version 1.0.0")]
    public class MapLoader : MapParser;

    public class MapParser
    {
        /// <summary>
        /// Parses an input string and executes map load or unload operations based on the provided syntax.
        /// </summary>
        /// <param name="input">The input command string (e.g., "Load::MapA", "Unload::MapA||MapB").</param>
        /// <remarks>
        /// Supported syntax:
        /// <list type="bullet">
        /// <item><description><c>Load::MapName</c> - Loads a single map.</description></item>
        /// <item><description><c>Load::MapA||MapB</c> - Loads one random map from the list.</description></item>
        /// <item><description><c>Load::MapA&amp;&amp;MapB</c> - Loads all specified maps.</description></item>
        /// <item><description><c>Unload::MapName</c> - Unloads a single map.</description></item>
        /// <item><description><c>Unload::MapA||MapB</c> - Unloads one random map from the list.</description></item>
        /// <item><description><c>Unload::MapA&amp;&amp;MapB</c> - Unloads all specified maps.</description></item>
        /// <item><description><c>LoadIf::MapName::IsLoaded::ConditionMap</c> - Loads a map if the condition map is currently loaded.</description></item>
        /// <item><description><c>LoadIf::MapName::IsNotLoaded::ConditionMap</c> - Loads a map if the condition map is not currently loaded.</description></item>
        /// <item><description><c>UnloadIf::MapName::IsLoaded::ConditionMap</c> - Unloads a map if the condition map is currently loaded.</description></item>
        /// <item><description><c>UnloadIf::MapName::IsNotLoaded::ConditionMap</c> - Unloads a map if the condition map is not currently loaded.</description></item>
        /// </list>
        /// </remarks>
        public static void ParseInput(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return;

            if (TryHandleConditional(input, "LoadIf::", LoadMap))
                return;

            if (TryHandleConditional(input, "UnloadIf::", UnloadMap))
                return;

            if (TryHandleSimple(input, "Load::", LoadMap))
                return;

            if (TryHandleSimple(input, "Unload::", UnloadMap))
                return;
        }

        private static bool TryHandleConditional(string input, string prefix, Action<string> action)
        {
            if (!input.StartsWith(prefix))
                return false;

            string[] parts = input.Substring(prefix.Length).Split(["::"], StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 3)
            {
                LogManager.Warn($"Invalid {prefix} syntax: '{input}'. Expected: {prefix}MapName::IsLoaded/IsNotLoaded::ConditionMap");
                return true;
            }

            string mapName = parts[0].Trim();
            string condition = parts[1].Trim();
            string conditionMap = parts[2].Trim();

            if (EvaluateCondition(condition, conditionMap))
                action(mapName);

            return true;
        }

        private static bool TryHandleSimple(string input, string prefix, Action<string> action)
        {
            if (!input.StartsWith(prefix))
                return false;

            string mapPart = input.Substring(prefix.Length).Trim();
            if (mapPart.Contains("||"))
            {
                string[] options = mapPart.Split(["||"], StringSplitOptions.RemoveEmptyEntries);
                string selected = options[Random.Range(0, options.Length)].Trim();
                action(selected);
            }
            else if (mapPart.Contains("&&"))
            {
                foreach (string? map in mapPart.Split(["&&"], StringSplitOptions.RemoveEmptyEntries))
                {
                    action(map.Trim());
                }
            }
            else
                action(mapPart);

            return true;
        }
        
        /// <summary>
        /// Loads a map by its file name.
        /// </summary>
        /// <param name="name">The name of the map file to load.</param>
        /// <remarks>
        /// The method performs a case-insensitive search in the loaded maps collection.
        /// If the map is not found, a warning is logged.
        /// </remarks>
        public static void LoadMap(string name)
        {
            if (!Loader.LoadedMaps.TryGetValue(name, out var map))
            {
                LogManager.Warn($"Map name {name} is invalid!");
                return;
            }

            Loader.SpawnMap(map);
        }

        /// <summary>
        /// Unloads a currently spawned map by its file name.
        /// </summary>
        /// <param name="name">The name of the map file to unload.</param>
        /// <remarks>
        /// The method performs a case-insensitive search in the spawned maps collection.
        /// If the map is not found, a warning is logged.
        /// </remarks>
        public static void UnloadMap(string name)
        {
            MapData? map = Loader.SpawnedMaps.FirstOrDefault(s => string.Equals(s.FileName, name, StringComparison.CurrentCultureIgnoreCase));
            if (map == null)
            {
                LogManager.Warn($"Map name {name} is invalid!");
                return;
            }

            Loader.DestroyMap(map);
        }

        /// <summary>
        /// Determines if the specified map by name is loaded.
        /// </summary>
        /// <param name="name">The map name to check.</param>
        /// <returns><see langword="true"/> if the specified map is loaded. Otherwise <see langword="false"/>.</returns>
        private static bool IsMapLoaded(string name)
            => Loader.SpawnedMaps.Any(s => string.Equals(s.FileName, name, StringComparison.CurrentCultureIgnoreCase));

        private static bool EvaluateCondition(string condition, string mapName)
        {
            return condition.ToLowerInvariant() switch
            {
                "isloaded" => IsMapLoaded(mapName),
                "isnotloaded" => !IsMapLoaded(mapName),
                _ => LogUnknownCondition(condition)
            };
        }

        private static bool LogUnknownCondition(string condition)
        {
            LogManager.Warn($"Unknown condition '{condition}'. Supported conditions: IsLoaded, IsNotLoaded.");
            return false;
        }
    }
}