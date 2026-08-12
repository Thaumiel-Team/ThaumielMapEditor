// -----------------------------------------------------------------------
// <copyright file="MapParser.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using ThaumielMapEditor.API.Data;
using Random = UnityEngine.Random;

namespace ThaumielMapEditor.API.Helpers
{
    [Obsolete($"{nameof(MapLoader)} has been renamed to {nameof(MapParser)}. Please update your code to use {nameof(MapParser)} instead. This will be removed in version 1.0.0")]
    public class MapLoader : MapParser;

    public class MapParser
    {
        private const string LoadIfPrefix = "LoadIf::";
        private const string UnloadIfPrefix = "UnloadIf::";

        /// <summary>
        /// The file names of every currently spawned map.
        /// </summary>
        public static IEnumerable<string> LoadedMapNames
            => Loader.SpawnedMaps.Select(map => map.FileName);

        /// <summary>
        /// Whether a map with the given file name is currently spawned (case-insensitive).
        /// </summary>
        /// <param name="name">The map file name to check.</param>
        public static bool IsLoaded(string name)
            => Loader.SpawnedMaps.Any(map => string.Equals(map.FileName, name, StringComparison.OrdinalIgnoreCase));

        /// <summary>
        /// Spawns a map by its file name. Does nothing if the map is already spawned.
        /// </summary>
        /// <param name="name">The map file name to load.</param>
        /// <returns><see langword="true"/> if the map was spawned, otherwise <see langword="false"/>.</returns>
        public static bool Load(string name)
        {
            if (!Loader.LoadedMaps.TryGetValue(name, out var map))
            {
                LogManager.Warn($"Map name '{name}' is invalid or was not loaded from disk!");
                return false;
            }

            if (IsLoaded(name))
            {
                LogManager.Debug($"Map '{name}' is already spawned. Skipping.");
                return false;
            }

            Loader.SpawnMap(map);
            return true;
        }

        /// <summary>
        /// Destroys a spawned map by its file name.
        /// </summary>
        /// <param name="name">The map file name to unload.</param>
        /// <returns><see langword="true"/> if the map was unloaded, otherwise <see langword="false"/>.</returns>
        public static bool Unload(string name)
        {
            MapData? map = Loader.SpawnedMaps.FirstOrDefault(spawned => string.Equals(spawned.FileName, name, StringComparison.OrdinalIgnoreCase));
            if (map == null)
            {
                LogManager.Warn($"Map name '{name}' is not currently spawned!");
                return false;
            }

            Loader.DestroyMap(map);
            return true;
        }

        /// <summary>
        /// Destroys and respawns a map, refreshing it from disk.
        /// </summary>
        /// <param name="name">The map file name to reload.</param>
        /// <returns><see langword="true"/> if the map was respawned, otherwise <see langword="false"/>.</returns>
        public static bool Reload(string name)
        {
            if (IsLoaded(name))
                Unload(name);

            return Load(name);
        }

        /// <summary>
        /// Toggles the spawn state of a map.
        /// </summary>
        /// <param name="name">The map file name to toggle.</param>
        /// <returns><see langword="true"/> if the map is spawned after the operation, otherwise <see langword="false"/>.</returns>
        public static bool Toggle(string name)
            => IsLoaded(name) ? Unload(name) : Load(name);

        /// <summary>
        /// Spawns every listed map that is present on disk.
        /// </summary>
        /// <param name="names">The map file names to load.</param>
        /// <returns>The number of maps that were spawned.</returns>
        public static int LoadMany(params string[] names)
        {
            int count = 0;
            foreach (string name in names)
            {
                if (!string.IsNullOrWhiteSpace(name) && Load(name))
                    count++;
            }

            return count;
        }

        /// <summary>
        /// Spawns exactly one randomly chosen map from the list.
        /// </summary>
        /// <param name="names">The map file names to choose from.</param>
        /// <returns><see langword="true"/> if a map was spawned, otherwise <see langword="false"/>.</returns>
        public static bool LoadRandom(params string[] names)
        {
            string[] valid = names.Where(name => !string.IsNullOrWhiteSpace(name)).ToArray();
            if (valid.Length == 0)
                return false;

            return Load(valid[Random.Range(0, valid.Length)]);
        }

        /// <summary>
        /// Destroys every spawned map with a matching name.
        /// </summary>
        /// <param name="names">The map file names to unload.</param>
        /// <returns>The number of maps that were unloaded.</returns>
        public static int UnloadMany(params string[] names)
        {
            int count = 0;
            foreach (string name in names)
            {
                if (!string.IsNullOrWhiteSpace(name) && Unload(name))
                    count++;
            }

            return count;
        }

        /// <summary>
        /// Destroys exactly one randomly chosen spawned map from the list.
        /// </summary>
        /// <param name="names">The map file names to choose from.</param>
        /// <returns><see langword="true"/> if a map was unloaded, otherwise <see langword="false"/>.</returns>
        public static bool UnloadRandom(params string[] names)
        {
            string[] valid = names.Where(name => !string.IsNullOrWhiteSpace(name)).ToArray();
            if (valid.Length == 0)
                return false;

            return Unload(valid[Random.Range(0, valid.Length)]);
        }

        /// <summary>
        /// Destroys every currently spawned map.
        /// </summary>
        /// <returns>The number of maps that were unloaded.</returns>
        public static int UnloadAll()
        {
            string[] names = Loader.SpawnedMaps.Select(map => map.FileName).ToArray();
            return UnloadMany(names);
        }

        /// <summary>
        /// Unloads and respawns every currently loaded map.
        /// </summary>
        /// <returns>The number of maps that were reloaded.</returns>
        public static int ReloadAll()
        {
            string[] names = Loader.SpawnedMaps.Select(map => map.FileName).ToArray();
            int count = 0;
            foreach (string name in names)
            {
                if (Reload(name))
                    count++;
            }

            return count;
        }

        /// <summary>
        /// Spawns a map when <see cref="EvaluateCondition"/> passes for the given condition map.
        /// </summary>
        /// <param name="name">The map file name to load.</param>
        /// <param name="condition">The condition to evaluate (<c>IsLoaded</c> or <c>IsNotLoaded</c>).</param>
        /// <param name="conditionMap">The map file name the condition is tested against.</param>
        /// <returns><see langword="true"/> if the map was spawned, otherwise <see langword="false"/>.</returns>
        public static bool LoadIf(string name, string condition, string conditionMap)
        {
            if (!EvaluateCondition(condition, conditionMap))
                return false;

            return Load(name);
        }

        /// <summary>
        /// Destroys a map when <see cref="EvaluateCondition"/> passes for the given condition map.
        /// </summary>
        /// <param name="name">The map file name to unload.</param>
        /// <param name="condition">The condition to evaluate (<c>IsLoaded</c> or <c>IsNotLoaded</c>).</param>
        /// <param name="conditionMap">The map file name the condition is tested against.</param>
        /// <returns><see langword="true"/> if the map was unloaded, otherwise <see langword="false"/>.</returns>
        public static bool UnloadIf(string name, string condition, string conditionMap)
        {
            if (!EvaluateCondition(condition, conditionMap))
                return false;

            return Unload(name);
        }

        /// <summary>
        /// Evaluates an <c>IsLoaded</c> / <c>IsNotLoaded</c> condition against a map.
        /// </summary>
        /// <param name="condition">The condition name (<c>IsLoaded</c> or <c>IsNotLoaded</c>).</param>
        /// <param name="mapName">The map file name the condition is tested against.</param>
        /// <returns><see langword="true"/> if the condition passes, otherwise <see langword="false"/>.</returns>
        public static bool EvaluateCondition(string condition, string mapName) => condition.ToLowerInvariant() switch
        {
            "isloaded" => IsLoaded(mapName),
            "isnotloaded" => !IsLoaded(mapName),
            _ => LogUnknownCondition(condition)
        };

        /// <summary>
        /// Parses an input string and executes map load/unload operations based on the provided syntax.
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
        /// <item><description><c>Reload::MapName</c> - Unloads and respawns a map from disk.</description></item>
        /// <item><description><c>Toggle::MapName</c> - Loads a map if it is unloaded, unloads it if it is loaded.</description></item>
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

            if (TryParseConditional(input))
                return;

            if (TryParse(input, "Load::", name => Load(name)))
                return;

            if (TryParse(input, "Unload::", name => Unload(name)))
                return;

            if (TryParse(input, "Reload::", name => Reload(name)))
                return;

            if (TryParse(input, "Toggle::", name => Toggle(name)))
                return;
        }

        private static bool TryParse(string input, string prefix, Action<string> action)
        {
            if (!input.StartsWith(prefix))
                return false;

            ApplyOperation(input.Substring(prefix.Length).Trim(), action);
            return true;
        }

        private static bool TryParseConditional(string input)
        {
            bool load = input.StartsWith(LoadIfPrefix);
            bool unload = input.StartsWith(UnloadIfPrefix);
            if (!load && !unload)
                return false;

            string prefix = unload ? UnloadIfPrefix : LoadIfPrefix;
            string[] parts = input.Substring(prefix.Length).Split(["::"], StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 3)
            {
                LogManager.Warn($"Invalid {prefix} syntax: '{input}'. Expected: {prefix}MapName::IsLoaded/IsNotLoaded::ConditionMap");
                return true;
            }

            string mapName = parts[0].Trim();
            bool conditionMet = EvaluateCondition(parts[1].Trim(), parts[2].Trim());

            if (load && conditionMet)
            {
                Load(mapName);
            }
            else if (unload && conditionMet)
            {
                Unload(mapName);
            }

            return true;
        }

        private static void ApplyOperation(string mapPart, Action<string> apply)
        {
            if (mapPart.Contains("||"))
            {
                string[] options = mapPart.Split(["||"], StringSplitOptions.RemoveEmptyEntries);
                apply(options[Random.Range(0, options.Length)].Trim());
                return;
            }

            if (mapPart.Contains("&&"))
            {
                foreach (string map in mapPart.Split(["&&"], StringSplitOptions.RemoveEmptyEntries))
                {
                    apply(map.Trim());
                }

                return;
            }

            apply(mapPart);
        }

        private static bool LogUnknownCondition(string condition)
        {
            LogManager.Warn($"Unknown condition '{condition}'. Supported conditions: IsLoaded, IsNotLoaded.");
            return false;
        }
    }
}