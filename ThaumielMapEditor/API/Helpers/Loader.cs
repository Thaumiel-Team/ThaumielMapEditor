// -----------------------------------------------------------------------
// <copyright file="Loader.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using AdminToys;
using HarmonyLib;
using LabApi.Features.Wrappers;
using LabApi.Loader.Features.Yaml.CustomConverters;
using MapGeneration;
using Mirror;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ThaumielMapEditor.API.Animation;
using ThaumielMapEditor.API.Attributes;
using ThaumielMapEditor.API.Binary;
using ThaumielMapEditor.API.Blocks;
using ThaumielMapEditor.API.Blocks.ClientSide;
using ThaumielMapEditor.API.Blocks.ServerObjects;
using ThaumielMapEditor.API.Blocks.ServerObjects.Lockers;
using ThaumielMapEditor.API.Components;
using ThaumielMapEditor.API.Components.Tools;
using ThaumielMapEditor.API.Conversion;
using ThaumielMapEditor.API.Data;
using ThaumielMapEditor.API.Enums;
using ThaumielMapEditor.API.Extensions;
using ThaumielMapEditor.API.Serialization;
using ThaumielMapEditor.Events.EventArgs.Handlers;
using UnityEngine;
using Utils.NonAllocLINQ;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using LabPrimitive = LabApi.Features.Wrappers.PrimitiveObjectToy;

namespace ThaumielMapEditor.API.Helpers
{
    [Obsolete($"{nameof(SchematicLoader)} has been renamed to {nameof(Loader)}. Please update your code to use {nameof(Loader)} instead. This will be removed in version 1.0.0")]
    public class SchematicLoader : Loader;

    [GitBookPage("Loader")]
    public class Loader
    {
        /// <summary>
        /// Fired when a <see cref="SchematicData"/> is spawned.
        /// </summary>
        public static event Action<SchematicData>? SchematicSpawned;

        /// <summary>
        /// Fired when a <see cref="SchematicData"/> is destroyed.
        /// </summary>
        public static event Action<SchematicData>? SchematicDestroyed;

        /// <summary>
        /// A dictionary of all spawned <see cref="MapData"/> instances, keyed by their <see cref="Guid"/>.
        /// </summary>
        public static Dictionary<Guid, MapData> MapsById { get; set; } = [];

        /// <summary>
        /// A dictionary of all spawned <see cref="SchematicData"/> instances, keyed by their ID.
        /// </summary>
        public static Dictionary<uint, SchematicData> SchematicsById { get; set; } = [];

        /// <summary>
        /// An enumerable of all currently spawned <see cref="SchematicData"/> instances.
        /// </summary>
        public static IEnumerable<SchematicData> SpawnedSchematics => SchematicsById.Values;

        /// <summary>
        /// An enumerable of all currently spawned <see cref="MapData"/> instances.
        /// </summary>
        public static IEnumerable<MapData> SpawnedMaps => MapsById.Values;

        /// <summary>
        /// This list contains all the schematics loaded by <see cref="LoadSchematics"/>
        /// Use <see cref="SpawnedSchematics"/> to get the spawned schematics.
        /// </summary>
        public static Dictionary<string, SerializableSchematic> LoadedSchematics = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// This list contains all the maps loaded by <see cref="LoadMaps"/>
        /// Use <see cref="SpawnedMaps"/> to get the spawned maps.
        /// </summary>
        public static Dictionary<string, SerializableMap> LoadedMaps = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// dodad2
        /// </summary>
        public static Dictionary<LODZone, SchematicData> SchematicLODZones = [];

        /// <summary>
        /// Caches resolved animator controllers so asset bundles are only scanned once per animator.
        /// A <see langword="null"/> value means the animator was previously not found.
        /// </summary>
        private static readonly Dictionary<(string Schematic, string Animator), RuntimeAnimatorController?> AnimatorControllerCache = [];

        /// <summary>
        /// Caches the <see cref="RuntimeAnimatorController"/>s already loaded from each asset bundle,
        /// so <see cref="AssetBundle.LoadAllAssets{T}"/> is never called twice on the same bundle.
        /// </summary>
        private static readonly Dictionary<AssetBundle, RuntimeAnimatorController[]> BundleControllerCache = [];

        /// <summary>
        /// The YAML deserializer used to parse schematic and map files
        /// </summary>
        public static IDeserializer Deserializer { get; } = new DeserializerBuilder()
            .WithNamingConvention(PascalCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .IgnoreFields()
            .WithTypeConverter(new Vector3ConverterYaml())
            .WithTypeConverter(new CustomColor32Converter())
            .WithTypeConverter(new CustomColorConverter())
            .WithTypeConverter(new CustomQuaternionConverter())
            .Build();

        /// <summary>
        /// The YAML serializer used to write schematic and map files, configured with Pascal case naming and custom type converters.
        /// </summary>
        public static ISerializer Serializer { get; } = new SerializerBuilder()
            .WithNamingConvention(PascalCaseNamingConvention.Instance)
            .IgnoreFields()
            .WithTypeConverter(new Vector3ConverterYaml())
            .WithTypeConverter(new CustomColor32Converter())
            .WithTypeConverter(new CustomColorConverter())
            .WithTypeConverter(new CustomQuaternionConverter())
            .Build();

        /// <summary>
        /// Cleans the <see cref="Loader"/> class.
        /// </summary>
        /// <remarks>
        /// This is automatically called when the server is ready for players.
        /// </remarks>
        public static void Cleanup()
        {
            MapsById.Clear();
            SchematicsById.Clear();
            AnimatorControllerCache.Clear();
            BundleControllerCache.Clear();
            SchematicLODZones.Clear();

            lock (IdLock)
            {
                _nextId = 0;
            }

            AnimationController.ClearAll();
            DoorLink.ClearAll();
            SyncManager.ClearClientPending();
            SyncManager.ClearServerPending();
            ServerObject.SpawnedObjects.Clear();
            ColliderHelper.SchematicColliders.Clear();
            LODHelper.PlayersInLODZones.Clear();
        }

        /// <summary>
        /// Initializes the <see cref="Loader"/>
        /// </summary>
        public static void Init()
        {
            LoadSchematics();
            LoadMaps();
        }

        /// <summary>
        /// Reloads all loaded schematics
        /// </summary>
        /// <remarks>
        /// This does not automatically respawn schematics
        /// </remarks>
        public static void ReloadSchematics()
        {
            LoadedSchematics.Clear();
            LoadSchematics();
        }

        /// <summary>
        /// Reloads all loaded maps.
        /// </summary>
        /// <remarks>
        /// This does not automatically spawn any maps.
        /// </remarks>
        public static void ReloadMaps()
        {
            LoadedMaps.Clear();
            LoadMaps();
        }

        /// <summary>
        /// Destroys the specified <see cref="SchematicData"/>
        /// </summary>
        /// <param name="data">The schematic to be destroyed</param>
        public static void DestroySchematic(SchematicData data)
        {
            SchematicDestroyed?.Invoke(data);
            SchematicsById.Remove(data.Id);

            data.Destroy();
        }

        private static bool IsYamlFile(string path)
        {
            string extension = Path.GetExtension(path);
            return extension.Equals(".yml", StringComparison.OrdinalIgnoreCase) || extension.Equals(".yaml", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsBinaryFile(string path)
            => Path.GetExtension(path).Equals(".bin", StringComparison.OrdinalIgnoreCase);

        private static IEnumerable<string> GetPreferredFiles(string directory)
        {
            Dictionary<string, string> preferred = new(StringComparer.OrdinalIgnoreCase);

            foreach (string path in ThaumFileManager.GetFilesInDirectory(directory))
            {
                if (!IsBinaryFile(path) && !IsYamlFile(path))
                    continue;

                string name = Path.GetFileNameWithoutExtension(path);
                if (!preferred.TryGetValue(name, out string? existing))
                {
                    preferred[name] = path;
                    continue;
                }

                if (IsBinaryFile(path) && !IsBinaryFile(existing))
                    preferred[name] = path;
            }

            return preferred.Values;
        }

        private static T? TryDeserializeYaml<T>(string path, string? content = null) where T : class
        {
            try
            {
                content ??= File.ReadAllText(path);
                return Deserializer.Deserialize<T>(content);
            }
            catch (YamlException yamlex)
            {
                LogManager.Warn($"Failed to parse YAML file '{path}'.\n\n{yamlex}");
                return null;
            }
            catch (Exception ex)
            {
                LogManager.Warn($"Exception when trying to parse YAML file '{path}'.\n\n{ex}");
                return null;
            }
        }

        private static T? TryDeserializeBinary<T>(string path, byte[]? content = null) where T : class
        {
            try
            {
                if (content is { Length: 0 })
                    return null;

                return content is null ? BinarySerializer.DeserializeFromFile<T>(path) : BinarySerializer.Deserialize<T>(content);
            }
            catch (Exception ex)
            {
                LogManager.Warn($"Exception when trying to parse binary file '{path}'.\n\n{ex}");
                return null;
            }
        }

        private static T? TryDeserializeFile<T>(string path) where T : class =>
            IsBinaryFile(path) ? TryDeserializeBinary<T>(path) : TryDeserializeYaml<T>(path);

        private static void ReadAndDeserializeInBackground<T>(string path, Action<T> onLoaded) where T : class
        {
            if (IsBinaryFile(path))
            {
                ThaumFileManager.ReadFileBytesInBackground(path, content =>
                {
                    T? value = TryDeserializeBinary<T>(path, content);
                    if (value != null)
                        onLoaded(value);
                });
            }
            else
            {
                ThaumFileManager.ReadFileInBackground(path, content =>
                {
                    T? value = TryDeserializeYaml<T>(path, content);
                    if (value != null)
                        onLoaded(value);
                });
            }
        }

        /// <summary>
        /// Loads the schematics
        /// </summary>
        public static void LoadSchematics()
        {
            string schematicDir = ThaumFileManager.Dir(["Schematics"]);
            ThaumFileManager.TryCreateDirectory(schematicDir);

            foreach (string path in GetPreferredFiles(schematicDir))
            {
                string name = Path.GetFileNameWithoutExtension(path);

                if (Main.Instance.Config.ReadFilesInBackground)
                {
                    ReadAndDeserializeInBackground<SerializableSchematic>(path, schematic =>
                    {
                        schematic.FileName = name;
                        LoadedSchematics[schematic.FileName] = schematic;
                        LogManager.Debug($"Loaded schematic {name} from {Path.GetExtension(path)} on background thread");
                    });
                }
                else
                {
                    SerializableSchematic? schematic = TryDeserializeFile<SerializableSchematic>(path);
                    if (schematic == null)
                        continue;

                    schematic.FileName = name;
                    LoadedSchematics[schematic.FileName] = schematic;
                    LogManager.Debug($"Loaded schematic {name} from {Path.GetExtension(path)} on main thread");
                }
            }
        }

        /// <summary>
        /// Loads the maps
        /// </summary>
        public static void LoadMaps()
        {
            string mapsDir = ThaumFileManager.Dir(["Maps"]);
            ThaumFileManager.TryCreateDirectory(mapsDir);

            foreach (string path in GetPreferredFiles(mapsDir))
            {
                string name = Path.GetFileNameWithoutExtension(path);

                if (Main.Instance.Config.ReadFilesInBackground)
                {
                    ReadAndDeserializeInBackground<SerializableMap>(path, map =>
                    {
                        map.FileName = name;
                        LoadedMaps[map.FileName] = map;
                        LogManager.Debug($"Loaded map {name} from {Path.GetExtension(path)} on background thread");
                    });
                }
                else
                {
                    SerializableMap? map = TryDeserializeFile<SerializableMap>(path);
                    if (map == null)
                        continue;

                    map.FileName = name;
                    LoadedMaps[map.FileName] = map;
                    LogManager.Debug($"Loaded map {name} from {Path.GetExtension(path)} on main thread");
                }
            }
        }

        /// <summary>
        /// Tries to get the <see cref="SchematicData"/> by its Id.
        /// </summary>
        /// <param name="id">The id to get</param>
        /// <param name="schematic">The <see cref="SchematicData"/> if found</param>
        /// <returns><see langword="true"/> if found else returns <see langword="false"/> if not</returns>
        public static bool TryGetSchematicById(uint id, out SchematicData schematic)
        {
            SchematicData? data = GetSchematicById(id);
            if (data == null)
            {
                schematic = null!;
                return false;
            }

            schematic = data;
            return true;
        }

        /// <summary>
        /// Gets the <see cref="SchematicData"/> by its id.
        /// </summary>
        /// <param name="id">The id to get</param>
        /// <returns><see cref="SchematicData"/> if found else returns <see langword="null"/></returns>
        public static SchematicData? GetSchematicById(uint id)
        {
            if (SchematicsById.TryGetValue(id, out var schem))
                return schem;

            return null;
        }

        public static SerializableSchematic? GetSchematicByName(string name)
        {
            LoadedSchematics.TryGetValue(name, out var schematic);
            return schematic;
        }

        private static uint _nextId;
        private static readonly object IdLock = new();

        /// <summary>
        /// Gets a unique id for all <see cref="SchematicData"/>
        /// </summary>
        /// <returns><see cref="uint"/> id</returns>
        public static uint GetId()
        {
            lock (IdLock)
            {
                while (SchematicsById.ContainsKey(_nextId))
                    _nextId++;

                return _nextId++;
            }
        }

        // Hopefuly this will stop clients from crashing when spawning large schematics.
        private static async Task SpawnObjectsBatchedAsync(SerializableSchematic schematic, SchematicData schematicData, uint rootNetId)
        {
            try
            {
                await Task.Run(async () =>
                {
                    Dictionary<int, (SerializableObject, bool)> objectsById = [];
                    Dictionary<int, List<SerializableObject>> objectsByParent = [];
                    Dictionary<int, List<SerializableObject>> serverObjectsByParent = [];
                    LODZone[] lodZones = [];

                    TaskCompletionSource<bool> lodTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
                    try
                    {
                        MainThreadDispatcher.Dispatch(() =>
                        {
                            try
                            {
                                if (schematicData.Primitive?.GameObject != null)
                                    lodZones = schematicData.Primitive.GameObject.GetComponents<LODZone>();
                            }
                            catch (Exception ex)
                            {
                                LogManager.Error($"Exception while fetching LOD zones: {ex}");
                            }
                            finally
                            {
                                lodTcs.TrySetResult(true);
                            }
                        });
                    }
                    catch (Exception ex)
                    {
                        LogManager.Error($"Failed to dispatch LOD fetch for '{schematic.FileName}': {ex.Message}");
                        lodTcs.TrySetResult(true);
                    }

                    await Task.WhenAny(lodTcs.Task, Task.Delay(5000));

                void CacheObject(SerializableObject obj, Dictionary<int, List<SerializableObject>> parentDict, bool serverside = false)
                {
                    if (objectsById.ContainsKey(obj.ObjectId))
                        return;

                    objectsById.Add(obj.ObjectId, (obj, serverside));

                    if (!parentDict.TryGetValue(obj.ParentId, out var list))
                    {
                        list = [];
                        parentDict.Add(obj.ParentId, list);
                    }

                    list.Add(obj);
                }

                foreach (SerializableObject obj in schematic.ServerSideObjects)
                {
                    CacheObject(obj, serverObjectsByParent, true);
                }

                foreach (SerializableObject obj in schematic.Objects)
                {
                    CacheObject(obj, objectsByParent);
                }

                Queue<(int id, uint parentNetId)> spawnQueue = new();
                HashSet<int> visited = [];
                spawnQueue.Enqueue((schematic.RootObjectId, rootNetId));

                while (spawnQueue.Count > 0)
                {
                    List<(int id, uint parentNetId)> currentBatch = [];
                    while (spawnQueue.Count > 0 && currentBatch.Count < 50)
                    {
                        (int currentId, uint parentNetId) = spawnQueue.Dequeue();
                        if (visited.Add(currentId))
                        {
                            currentBatch.Add((currentId, parentNetId));
                        }
                    }

                    if (currentBatch.Count == 0)
                        continue;

                    List<(int id, uint spawnedNetId)> batchResults = [];
                    TaskCompletionSource<bool> tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

                    try
                    {
                        MainThreadDispatcher.Dispatch(() =>
                        {
                            try
                            {
                                foreach ((int currentId, uint parentNetId) in currentBatch)
                                {
                                    uint currentNetId = parentNetId;

                                    try
                                    {
                                        if (objectsById.TryGetValue(currentId, out var obj))
                                        {
                                            currentNetId = SpawnSerializableObject(obj.Item1, schematicData, parentNetId, lodZones, serverside: obj.Item2);
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        LogManager.Error($"Exception spawning object id {currentId} in '{schematic.FileName}': {ex}");
                                    }

                                    batchResults.Add((currentId, currentNetId));
                                }
                            }
                            catch (Exception ex)
                            {
                                LogManager.Error($"Exception during object spawning batch: {ex}");
                            }
                            finally
                            {
                                tcs.TrySetResult(true);
                            }
                        });
                    }
                    catch (Exception ex)
                    {
                        LogManager.Error($"Failed to dispatch spawn batch for '{schematic.FileName}': {ex.Message}");
                        tcs.TrySetResult(true);
                    }

                    await Task.WhenAny(tcs.Task, Task.Delay(30000));

                    foreach ((int currentId, uint currentNetId) in batchResults)
                    {
                        if (objectsByParent.TryGetValue(currentId, out var children))
                        {
                            foreach (SerializableObject child in children)
                            {
                                spawnQueue.Enqueue((child.ObjectId, currentNetId));
                            }
                        }

                        if (serverObjectsByParent.TryGetValue(currentId, out var serverChildren))
                        {
                            foreach (SerializableObject child in serverChildren)
                            {
                                spawnQueue.Enqueue((child.ObjectId, currentNetId));
                            }
                        }
                    }
                }

                TaskCompletionSource<bool> completionTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
                try
                {
                    MainThreadDispatcher.Dispatch(() =>
                    {
                        try
                        {
                            schematicData.Executor = new(schematicData);
                            Dictionary<int, ServerObject> serverObjectsById = schematicData.SpawnedServerObjects.ToDictionary(o => o.ObjectId);
                            ApplyAnimatorsAndTools(schematic, schematicData, serverObjectsById);

                            SchematicHandler.OnSchematicSpawned(new(schematicData));
                            SchematicSpawned?.Invoke(schematicData);
                            LogManager.Info($"Schematic '{schematic.FileName}' fully spawned.");
                            SchematicsById[schematicData.Id] = schematicData;

                            foreach (Player player in Player.ReadyList.ToArray())
                            {
                                if (player == null || player.IsDestroyed || player.IsHost || player.IsDummy)
                                    continue;
                                
                                schematicData.SyncWithPlayer(player);
                            }

                            if (Main.Instance.Config.SchematicAnimationPlayOnLoad.TryGetValue(schematicData.FileName, out var animationname))
                            {
                                try
                                {
                                    schematicData.AnimationController.Play(animationname);
                                }
                                catch (Exception ex)
                                {
                                    LogManager.Warn($"Failed to play onload animation '{animationname}': {ex.Message}");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            LogManager.Error($"Exception completing schematic spawn: {ex}");
                            SchematicsById[schematicData.Id] = schematicData;
                        }
                        finally
                        {
                            completionTcs.TrySetResult(true);
                        }
                    });
                }
                catch (Exception ex)
                {
                    LogManager.Error($"Failed to dispatch spawn completion for '{schematic.FileName}': {ex.Message}");
                    completionTcs.TrySetResult(true);
                }

                await Task.WhenAny(completionTcs.Task, Task.Delay(30000));
                });
            }
            catch (Exception ex)
            {
                LogManager.Error($"Exception while spawning schematic '{schematic.FileName}' in background: {ex}");
            }
        }

        /// <summary>
        /// Spawns a map
        /// </summary>
        /// <param name="map">The serialized map to spawn</param>
        /// <returns><see cref="MapData"/></returns>
        public static MapData? SpawnMap(SerializableMap map)
        {
            Room? room = Room.List.FirstOrDefault(r => r.Name == map.Room);
            if (room == null && map.Room != RoomName.Unnamed)
            {
                LogManager.Warn($"Could not find room {map.Room} for map {map.FileName}!");
                return null;
            }

            MapData data = new()
            {
                Id = Guid.NewGuid(),
                Room = room,
                Position = map.LocalPosition,
                FileName = map.FileName
            };

            foreach (SerializedMapSchematic ms in map.Schematics)
            {
                Vector3 offset = data.Room?.WorldPosition(ms.Position) ?? ms.Position;
                if (!LoadedSchematics.TryGetValue(ms.SchematicName, out SerializableSchematic schematic))
                {
                    LogManager.Warn($"Schematic '{ms.SchematicName}' not found.");
                    continue;
                }

                SchematicData schematicData = SpawnSchematic(schematic, offset);
                data.Schematics.Add(new()
                {
                    LocalPosition = offset,
                    SchematicName = schematicData.FileName,
                    SchematicId = schematicData.Id
                });
            }

            MapsById.Add(data.Id, data);

            return data;
        }

        /// <summary>
        /// Destroys a map.
        /// </summary>
        /// <param name="map">The <see cref="MapData"/> to be destroyed.</param>
        public static void DestroyMap(MapData map)
        {
            foreach (MapSchematicData schematic in map.Schematics.ToArray())
            {
                if (!TryGetSchematicById(schematic.SchematicId, out var schematicData))
                    continue;

                DestroySchematic(schematicData);
                map.Schematics.Remove(schematic);
            }

            MapsById.Remove(map.Id);
        }

        /// <summary>
        /// Loads all schematics from a custom directory into <see cref="LoadedSchematics"/>.
        /// </summary>
        /// <param name="directory">The full path to the directory to load schematics from.</param>
        public static void LoadSchematicsFromDirectory(string directory)
        {
            if (!Directory.Exists(directory))
            {
                LogManager.Warn($"Custom schematic directory not found: '{directory}'.");
                return;
            }

            foreach (string path in GetPreferredFiles(directory))
            {
                SerializableSchematic? schematic = TryDeserializeFile<SerializableSchematic>(path);
                if (schematic == null)
                    continue;

                schematic.FileName = Path.GetFileNameWithoutExtension(path);
                LoadedSchematics[schematic.FileName] = schematic;
                LogManager.Debug($"Loaded schematic '{schematic.FileName}' from custom directory '{directory}'.");
            }
        }

        private static SerializableSchematic? TryLoadSchematicFromDirectory(string directory, string schematicName)
        {
            if (!Directory.Exists(directory))
            {
                LogManager.Warn($"Custom schematic directory not found: '{directory}'.");
                return null;
            }

            string[] files = ThaumFileManager.GetFilesInDirectory(directory);
            foreach (string extension in new[] { ".bin", ".yml", ".yaml" })
            {
                foreach (string path in files)
                {
                    if (Path.GetExtension(path).Equals(extension, StringComparison.OrdinalIgnoreCase) && Path.GetFileNameWithoutExtension(path).Equals(schematicName, StringComparison.OrdinalIgnoreCase))
                        return TryLoadSchematicFromPath(path);
                }
            }

            LogManager.Warn($"Schematic '{schematicName}' not found in directory '{directory}'.");
            return null;
        }

        /// <summary>
        /// Spawns a schematic by name from a custom directory at the specified position with default rotation and scale.
        /// </summary>
        /// <param name="directory">The full path to the directory containing the schematic.</param>
        /// <param name="schematicName">The file name of the schematic (without extension).</param>
        /// <param name="position">The world position at which to place the schematic.</param>
        /// <returns>A <see cref="SchematicData"/> instance representing the spawned schematic, or <see langword="null"/> if the schematic could not be loaded.</returns>
        public static SchematicData? SpawnSchematicFromDirectory(string directory, string schematicName, Vector3 position)
        {
            SerializableSchematic? schematic = TryLoadSchematicFromDirectory(directory, schematicName);
            if (schematic == null)
                return null;

            return SpawnSchematic(schematic, position);
        }

        /// <summary>
        /// Spawns a schematic by name from a custom directory at the specified position and rotation with default scale.
        /// </summary>
        /// <param name="directory">The full path to the directory containing the schematic.</param>
        /// <param name="schematicName">The file name of the schematic (without extension).</param>
        /// <param name="position">The world position at which to place the schematic.</param>
        /// <param name="rotation">The rotation to apply to the schematic.</param>
        /// <returns>A <see cref="SchematicData"/> instance representing the spawned schematic, or <see langword="null"/> if the schematic could not be loaded.</returns>
        public static SchematicData? SpawnSchematicFromDirectory(string directory, string schematicName, Vector3 position, Quaternion rotation)
        {
            SerializableSchematic? schematic = TryLoadSchematicFromDirectory(directory, schematicName);
            if (schematic == null)
                return null;

            return SpawnSchematic(schematic, position, rotation);
        }

        /// <summary>
        /// Spawns a schematic by name from a custom directory at the specified position, rotation, and scale.
        /// </summary>
        /// <param name="directory">The full path to the directory containing the schematic.</param>
        /// <param name="schematicName">The file name of the schematic (without extension).</param>
        /// <param name="position">The world position at which to place the schematic.</param>
        /// <param name="rotation">The rotation to apply to the schematic.</param>
        /// <param name="scale">The scale to apply to the schematic.</param>
        /// <returns>A <see cref="SchematicData"/> instance representing the spawned schematic, or <see langword="null"/> if the schematic could not be loaded.</returns>
        public static SchematicData? SpawnSchematicFromDirectory(string directory, string schematicName, Vector3 position, Quaternion rotation, Vector3 scale)
        {
            SerializableSchematic? schematic = TryLoadSchematicFromDirectory(directory, schematicName);
            if (schematic == null)
                return null;

            return SpawnSchematic(schematic, position, rotation, scale);
        }

        /// <summary>
        /// Spawns a schematic from a file path at the specified position with default rotation and scale.
        /// </summary>
        /// <param name="path">The full file path to the schematic file.</param>
        /// <param name="position">The world position at which to place the schematic.</param>
        /// <returns>A <see cref="SchematicData"/> instance representing the spawned schematic, or <see langword="null"/> if the file could not be loaded.</returns>
        public static SchematicData? SpawnSchematicFromPath(string path, Vector3 position)
        {
            SerializableSchematic? schematic = TryLoadSchematicFromPath(path);
            if (schematic == null)
                return null;

            return SpawnSchematic(schematic, position);
        }

        /// <summary>
        /// Spawns a schematic from a file path at the specified position and rotation with default scale.
        /// </summary>
        /// <param name="path">The full file path to the schematic file.</param>
        /// <param name="position">The world position at which to place the schematic.</param>
        /// <param name="rotation">The rotation to apply to the schematic.</param>
        /// <returns>A <see cref="SchematicData"/> instance representing the spawned schematic, or <see langword="null"/> if the file could not be loaded.</returns>
        public static SchematicData? SpawnSchematicFromPath(string path, Vector3 position, Quaternion rotation)
        {
            SerializableSchematic? schematic = TryLoadSchematicFromPath(path);
            if (schematic == null)
                return null;

            return SpawnSchematic(schematic, position, rotation);
        }

        /// <summary>
        /// Spawns a schematic from a file path at the specified position, rotation, and scale.
        /// </summary>
        /// <param name="path">The full file path to the schematic file.</param>
        /// <param name="position">The world position at which to place the schematic.</param>
        /// <param name="rotation">The rotation to apply to the schematic.</param>
        /// <param name="scale">The scale to apply to the schematic.</param>
        /// <returns>A <see cref="SchematicData"/> instance representing the spawned schematic, or <see langword="null"/> if the file could not be loaded.</returns>
        public static SchematicData? SpawnSchematicFromPath(string path, Vector3 position, Quaternion rotation, Vector3 scale)
        {
            SerializableSchematic? schematic = TryLoadSchematicFromPath(path);
            if (schematic == null)
                return null;

            return SpawnSchematic(schematic, position, rotation, scale);
        }

        /// <summary>
        /// Attempts to load and deserialize a <see cref="SerializableSchematic"/> from a file path.
        /// </summary>
        /// <param name="path">The full file path to the schematic file.</param>
        /// <returns>A <see cref="SerializableSchematic"/> if successful, otherwise <see langword="null"/>.</returns>
        private static SerializableSchematic? TryLoadSchematicFromPath(string path)
        {
            if (!File.Exists(path))
            {
                LogManager.Warn($"Schematic file not found at path '{path}'.");
                return null;
            }

            SerializableSchematic? schematic = TryDeserializeFile<SerializableSchematic>(path);
            if (schematic == null)
                return null;

            schematic.FileName = Path.GetFileNameWithoutExtension(path);
            LogManager.Debug($"Loaded schematic from path '{path}'.");
            return schematic;
        }

        /// <summary>
        /// Spawns a schematic at the specified position, rotation, and scale.
        /// </summary>
        /// <param name="schematic">The serialized schematic to spawn.</param>
        /// <param name="position">The world position at which to place the schematic.</param>
        /// <param name="rotation">The rotation to apply to the schematic.</param>
        /// <param name="scale">The scale to apply to the schematic.</param>
        /// <returns>A <see cref="SchematicData"/> instance representing the spawned schematic.</returns>
        public static SchematicData SpawnSchematic(SerializableSchematic schematic, Vector3 position, Quaternion rotation, Vector3 scale)
        {
            SchematicData schematicData = new()
            {
                FileName = schematic.FileName,
                Id = GetId(),
            };

            SpawnSchematic(schematic, schematicData, position, rotation, scale);
            return schematicData;
        }

        /// <summary>
        /// Spawns a schematic at the specified position and rotation with default scale.
        /// </summary>
        /// <param name="schematic">The serialized schematic to spawn.</param>
        /// <param name="position">The world position at which to place the schematic.</param>
        /// <param name="rotation">The rotation to apply to the schematic.</param>
        /// <returns>A <see cref="SchematicData"/> instance representing the spawned schematic.</returns>
        public static SchematicData SpawnSchematic(SerializableSchematic schematic, Vector3 position, Quaternion rotation)
        {
            SchematicData schematicData = new()
            {
                FileName = schematic.FileName,
                Id = GetId(),
            };

            SpawnSchematic(schematic, schematicData, position, rotation, default);
            return schematicData;
        }

        /// <summary>
        /// Spawns a schematic at the specified position with default rotation and scale.
        /// </summary>
        /// <param name="schematic">The serialized schematic to spawn.</param>
        /// <param name="position">The world position at which to place the schematic.</param>
        /// <returns>A <see cref="SchematicData"/> instance representing the spawned schematic.</returns>
        public static SchematicData SpawnSchematic(SerializableSchematic schematic, Vector3 position)
        {
            SchematicData schematicData = new()
            {
                FileName = schematic.FileName,
                Id = GetId(),
            };

            SpawnSchematic(schematic, schematicData, position, default, default);
            return schematicData;
        }

        /// <summary>
        /// Spawns a loaded schematic by name at the specified position with default rotation and scale.
        /// </summary>
        /// <param name="schematicname">The file name of the schematic to spawn. Must match a schematic in <see cref="LoadedSchematics"/>.</param>
        /// <param name="position">The world position at which to place the schematic.</param>
        /// <returns>A <see cref="SchematicData"/> instance representing the spawned schematic.</returns>
        public static SchematicData? SpawnSchematic(string schematicname, Vector3 position)
        {
            SerializableSchematic? schematic = GetSchematicByName(schematicname);
            if (schematic == null)
                return null;

            SchematicData schematicData = new()
            {
                FileName = schematicname,
                Id = GetId(),
            };

            SpawnSchematic(schematic, schematicData, position, default, default);
            return schematicData;
        }

        /// <summary>
        /// Spawns a loaded schematic by name at the specified position and rotation with default scale.
        /// </summary>
        /// <param name="schematicname">The file name of the schematic to spawn. Must match a schematic in <see cref="LoadedSchematics"/>.</param>
        /// <param name="position">The world position at which to place the schematic.</param>
        /// <param name="rotation">The rotation to apply to the schematic.</param>
        /// <returns>A <see cref="SchematicData"/> instance representing the spawned schematic.</returns>
        public static SchematicData? SpawnSchematic(string schematicname, Vector3 position, Quaternion rotation)
        {
            SerializableSchematic? schematic = GetSchematicByName(schematicname);
            if (schematic == null)
                return null;

            SchematicData schematicData = new()
            {
                FileName = schematicname,
                Id = GetId(),
            };


            SpawnSchematic(schematic, schematicData, position, rotation, default);
            return schematicData;
        }

        /// <summary>
        /// Spawns a loaded schematic by name at the specified position, rotation, and scale.
        /// </summary>
        /// <param name="schematicname">The file name of the schematic to spawn. Must match a schematic in <see cref="LoadedSchematics"/>.</param>
        /// <param name="position">The world position at which to place the schematic.</param>
        /// <param name="rotation">The rotation to apply to the schematic.</param>
        /// <param name="scale">The scale to apply to the schematic.</param>
        /// <returns>A <see cref="SchematicData"/> instance representing the spawned schematic.</returns>
        public static SchematicData? SpawnSchematic(string schematicname, Vector3 position, Quaternion rotation, Vector3 scale)
        {
            SerializableSchematic? schematic = GetSchematicByName(schematicname);
            if (schematic == null)
                return null;

            SchematicData schematicData = new()
            {
                FileName = schematicname,
                Id = GetId(),
            };

            SpawnSchematic(schematic, schematicData, position, rotation, scale);
            return schematicData;
        }

        /// <summary>
        /// Indicates whether the given <see cref="ObjectType"/> meaningfully uses scale.
        /// Types that return <see langword="false"/> ignore the scale argument in <c>SpawnSingleObject</c>.
        /// </summary>
        /// <param name="type">The object type to check.</param>
        /// <returns><see langword="true"/> if scale affects the spawned object.</returns>
        public static bool SupportsScale(ObjectType type) => type switch
        {
            ObjectType.Primitive or ObjectType.Light or ObjectType.TextToy or ObjectType.Capybara or ObjectType.Clutter or ObjectType.GameObject or ObjectType.Interactable or ObjectType.Teleporter or ObjectType.Waypoint or ObjectType.RagdollSpawner => true,
            _ => false,
        };

        /// <summary>
        /// Indicates whether the given <see cref="ObjectType"/> has both clientside and serverside implementations.
        /// </summary>
        /// <param name="type">The object type to check.</param>
        /// <returns><see langword="true"/> if <paramref name="type"/> can be spawned with either side.</returns>
        public static bool IsDualType(ObjectType type) => type switch
        {
            ObjectType.Primitive or ObjectType.Light or ObjectType.TextToy or ObjectType.Capybara or ObjectType.Clutter or ObjectType.CullingParent or ObjectType.GameObject => true,
            _ => false,
        };

        /// <summary>
        /// Spawns a single object without requiring a schematic file.
        /// </summary>
        /// <remarks>
        /// The object is wrapped in a single object schematic internally so lifecycle keeps working. The schematic root is placed at <paramref name="position"/>/<paramref name="rotation"/> and the object itself is spawned at local origin with <paramref name="scale"/>.
        /// </remarks>
        /// <param name="type">The type of object to spawn. <see cref="ObjectType.None"/> and <see cref="ObjectType.Schematic"/> are not supported.</param>
        /// <param name="position">The world position at which to place the object.</param>
        /// <param name="rotation">The world rotation to apply to the object.</param>
        /// <param name="scale">The scale to apply. Only used when <see cref="SupportsScale"/> returns <see langword="true"/> for <paramref name="type"/>; otherwise <see cref="Vector3.one"/> is used.</param>
        /// <param name="values">Optional extra object properties (e.g. DoorType, PrimitiveType, ItemToSpawn). Keys are case insensitive.</param>
        /// <param name="serverSide">For dualtypes (<see cref="IsDualType"/>), spawns the serverside variant when <see langword="true"/>.</param>
        /// <param name="name">Optional object name. Defaults to <c>Single_{type}</c>.</param>
        /// <param name="isStatic">Whether the spawned object is static.</param>
        /// <param name="movementSmoothing">The movement smoothing for clientside objects.</param>
        /// <param name="tools">Optional tools to attach to the object.</param>
        /// <returns>A <see cref="SchematicData"/> containing the single spawned object, or <see langword="null"/> when validation fails.</returns>
        public static SchematicData? SpawnSingleObject(ObjectType type, Vector3 position, Quaternion rotation, Vector3? scale = null, Dictionary<string, object>? values = null, bool serverSide = false, string? name = null, bool isStatic = false, byte movementSmoothing = 0, List<SerializableTool>? tools = null)
        {
            if (type is ObjectType.None or ObjectType.Schematic)
            {
                LogManager.Warn($"Cannot spawn single object of type '{type}'. Use '{nameof(SpawnSchematic)}' for schematics.");
                return null;
            }

            Vector3 finalScale = scale ?? Vector3.one;
            if (finalScale == default)
                finalScale = Vector3.one;

            if (!SupportsScale(type))
                finalScale = Vector3.one;

            if (finalScale.x == 0f || finalScale.y == 0f || finalScale.z == 0f || float.IsNaN(finalScale.x) || float.IsNaN(finalScale.y) || float.IsNaN(finalScale.z) || float.IsInfinity(finalScale.x) || float.IsInfinity(finalScale.y) || float.IsInfinity(finalScale.z))
            {
                LogManager.Warn($"Invalid scale '{finalScale}' for single object '{type}'. Falling back to Vector3.one.");
                finalScale = Vector3.one;
            }

            Dictionary<string, object> finalValues = values != null ? new Dictionary<string, object>(values, StringComparer.OrdinalIgnoreCase) : new(StringComparer.OrdinalIgnoreCase);
            ApplySingleObjectDefaults(type, finalValues);

            if (!ValidateSingleObjectValues(type, finalValues))
                return null;

            SerializableObject obj = new()
            {
                ObjectId = 0,
                ParentId = -1,
                Name = string.IsNullOrWhiteSpace(name) ? $"Single_{type}" : name!,
                Position = Vector3.zero,
                Rotation = Quaternion.identity,
                Scale = finalScale,
                IsStatic = isStatic,
                MovementSmoothing = movementSmoothing,
                ObjectType = type,
                Values = finalValues,
                Tools = tools ?? [],
            };

            SerializableSchematic schematic = new()
            {
                FileName = $"Single_{type}",
                RootObjectId = -1,
                Rotation = Quaternion.identity,
                Scale = Vector3.one,
            };

            if (serverSide)
            {
                schematic.ServerSideObjects.Add(obj);
            }
            else
                schematic.Objects.Add(obj);

            return SpawnSchematic(schematic, position, rotation);
        }

        /// <summary>
        /// Spawns a single object without requiring a schematic file, using euler angles for rotation.
        /// </summary>
        /// <param name="type">The type of object to spawn.</param>
        /// <param name="position">The world position at which to place the object.</param>
        /// <param name="rotationEuler">The world rotation in euler angles (degrees).</param>
        /// <param name="scale">The scale to apply. Only used for scalable types.</param>
        /// <param name="values">Optional extra object properties.</param>
        /// <param name="serverSide">Spawns the serverside variant for dual types.</param>
        /// <param name="name">Optional object name.</param>
        /// <param name="isStatic">Whether the spawned object is static.</param>
        /// <param name="movementSmoothing">The movement smoothing for clientside objects.</param>
        /// <param name="tools">Optional tools to attach.</param>
        /// <returns>A <see cref="SchematicData"/> containing the single spawned object, or <see langword="null"/> when validation fails.</returns>
        public static SchematicData? SpawnSingleObject(ObjectType type, Vector3 position, Vector3 rotationEuler, Vector3? scale = null, Dictionary<string, object>? values = null, bool serverSide = false, string? name = null, bool isStatic = false, byte movementSmoothing = 0, List<SerializableTool>? tools = null)
            => SpawnSingleObject(type, position, Quaternion.Euler(rotationEuler), scale, values, serverSide, name, isStatic, movementSmoothing, tools);

        private static void ApplySingleObjectDefaults(ObjectType type, Dictionary<string, object> values)
        {
            switch (type)
            {
                case ObjectType.Primitive:
                    EnsureValue(values, "PrimitiveType", PrimitiveType.Cube);
                    EnsureValue(values, "PrimitiveFlags", PrimitiveFlags.Visible | PrimitiveFlags.Collidable);
                    EnsureValue(values, "Color", Color.white);
                    break;

                case ObjectType.TextToy:
                    EnsureValue(values, "Text", string.Empty);
                    EnsureValue(values, "DisplaySize", AdminToys.TextToy.DefaultDisplaySize);
                    break;

                case ObjectType.Waypoint:
                    EnsureValue(values, "BoundsSize", Vector3.one);
                    break;

                case ObjectType.Pickup:
                    EnsureValue(values, "SpawnPercentage", 100f);
                    EnsureValue(values, "ItemToSpawn", ItemType.GunCOM15);
                    break;

                case ObjectType.RagdollSpawner:
                    EnsureValue(values, "Chance", 100f);
                    break;

                case ObjectType.Teleporter:
                    if (!HasValueKey(values, "Id"))
                        values["Id"] = Guid.NewGuid();

                    break;

                case ObjectType.Door:
                    EnsureValue(values, "DoorType", DoorType.Hcz);
                    break;

                case ObjectType.Locker:
                    EnsureValue(values, "LockerType", LockerType.Misc);
                    break;

                case ObjectType.CullingParent:
                    EnsureValue(values, "BoundsSize", new Vector3(10f, 10f, 10f));
                    break;
            }
        }

        private static bool ValidateSingleObjectValues(ObjectType type, Dictionary<string, object> values)
        {
            switch (type)
            {
                case ObjectType.Door:
                    if (!TryGetEnumValue(values, "DoorType", out DoorType doorType) || !Enum.IsDefined(typeof(DoorType), doorType) || doorType == 0)
                    {
                        LogManager.Warn($"Cannot spawn single Door without a valid 'DoorType' (Lcz, Hcz, Ez, Gate, BulkHead). Example: DoorType=Hcz.");
                        return false;
                    }

                    break;

                case ObjectType.Locker:
                    if (!TryGetEnumValue(values, "LockerType", out LockerType lockerType) || lockerType == LockerType.None)
                    {
                        LogManager.Warn($"Cannot spawn single Locker without a valid 'LockerType' (e.g. Misc, Medkit, RifleRack). Example: LockerType=Misc.");
                        return false;
                    }

                    break;

                case ObjectType.Pickup:
                    if (!HasValueKey(values, "ItemToSpawn"))
                    {
                        LogManager.Warn($"Cannot spawn single Pickup without 'ItemToSpawn'. Example: ItemToSpawn=GunCOM15.");
                        return false;
                    }

                    break;

                case ObjectType.Clutter:
                    if (!HasValueKey(values, "ClutterType"))
                        values["ClutterType"] = ClutterType.SimpleBoxes;
                    break;
            }

            return true;
        }

        private static bool HasValueKey(Dictionary<string, object> values, string key)
        {
            foreach (string existing in values.Keys)
            {
                if (string.Equals(existing, key, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private static void EnsureValue(Dictionary<string, object> values, string key, object defaultValue)
        {
            if (!HasValueKey(values, key))
                values[key] = defaultValue;
        }

        private static bool TryGetEnumValue<T>(Dictionary<string, object> values, string key, out T result) where T : struct
        {
            result = default;

            foreach (KeyValuePair<string, object> kvp in values)
            {
                if (!string.Equals(kvp.Key, key, StringComparison.OrdinalIgnoreCase) || kvp.Value == null)
                    continue;

                try
                {
                    if (kvp.Value is T direct)
                    {
                        result = direct;
                        return true;
                    }

                    object? parsed = Enum.Parse(typeof(T), kvp.Value.ToString()!.Replace(" ", string.Empty), ignoreCase: true);
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

        private static void SpawnSchematic(SerializableSchematic schematic, SchematicData schematicData, Vector3 position, Quaternion rotation, Vector3 scale)
        {
            if (!PrefabHelper.RanRegister)
            {
                LogManager.Warn($"Prefabs are not registered yet!");
                return;
            }

            schematicData.Primitive = LabPrimitive.Create();
            schematicData.Primitive.Type = PrimitiveType.Cube;
            schematicData.Primitive.Flags = PrimitiveFlags.None;
            schematicData.Primitive.Position = position;
            schematicData.Room = Room.GetRoomAtPosition(schematicData.Position);
            if (rotation != default)
            {
                schematicData.Primitive.Rotation = rotation;
                schematicData.Rotation = rotation;
            }
            else
            {
                schematicData.Primitive.Rotation = schematic.Rotation;
                schematicData.Rotation = schematic.Rotation;
            }

            if (scale != default)
            {
                schematicData.Primitive.Scale = scale;
                schematicData.Scale = scale;
            }
            else
            {
                schematicData.Primitive.Scale = schematic.Scale;
                schematicData.Scale = schematic.Scale;
            }

            schematicData.Position = position;
            schematicData.RootObjectId = schematic.RootObjectId;

            LODHelper.GenerateLODZones(schematicData, schematic);
            GetGameObjectTransforms(schematic, schematicData);

            _ = SpawnObjectsBatchedAsync(schematic, schematicData, schematicData.Primitive.Base.netId);
        }

        private static void GetGameObjectTransforms(SerializableSchematic schematic, SchematicData schematicData)
        {
            Dictionary<int, SerializableObject> objectsById = [];
            foreach (SerializableObject obj in schematic.Objects)
                objectsById[obj.ObjectId] = obj;

            foreach (SerializableObject obj in schematic.ServerSideObjects)
                objectsById[obj.ObjectId] = obj;

            HashSet<int> serverIds = [];
            foreach (SerializableObject obj in schematic.ServerSideObjects)
            {
                if (obj.ObjectType == ObjectType.CullingParent)
                    serverIds.Add(obj.ObjectId);
            }

            HashSet<int> visiting = [];
            foreach (SerializableObject obj in schematic.Objects)
                EnsureServerSideTransform(obj, objectsById, schematicData, visiting, serverIds);

            foreach (SerializableObject obj in schematic.ServerSideObjects)
            {
                if (obj.ObjectType == ObjectType.GameObject)
                    EnsureServerSideTransform(obj, objectsById, schematicData, visiting, serverIds);
            }
        }

        private static void EnsureServerSideTransform(SerializableObject obj, Dictionary<int, SerializableObject> objectsById, SchematicData schematicData, HashSet<int> visiting, HashSet<int> serverIds)
        {
            if (schematicData.ServerSideTransforms.ContainsKey(obj.ObjectId))
                return;

            if (obj.ObjectType != ObjectType.GameObject && obj.ObjectType != ObjectType.CullingParent)
                return;

            if (obj.ObjectType == ObjectType.CullingParent && serverIds.Contains(obj.ObjectId))
                return;

            if (!visiting.Add(obj.ObjectId))
                return;

            try
            {
                if (objectsById.TryGetValue(obj.ParentId, out SerializableObject? parent) && (parent.ObjectType == ObjectType.GameObject || parent.ObjectType == ObjectType.CullingParent) && !schematicData.ServerSideTransforms.ContainsKey(parent.ObjectId))
                    EnsureServerSideTransform(parent, objectsById, schematicData, visiting, serverIds);

                if (obj.ObjectType is ObjectType.GameObject or ObjectType.CullingParent)
                {
                    CreateGameObjectTransform(obj, schematicData);
                }
            }
            finally
            {
                visiting.Remove(obj.ObjectId);
            }
        }

        private static void CreateGameObjectTransform(SerializableObject obj, SchematicData schematicData)
        {
            if (obj.ObjectType != ObjectType.GameObject && obj.ObjectType != ObjectType.CullingParent)
                return;

            GameObject dummyNode = new($"[SchematicNode] {obj.Name}");

            if (schematicData.ServerSideTransforms.TryGetValue(obj.ParentId, out Transform parentTransform) && parentTransform != null)
            {
                dummyNode.transform.SetParent(parentTransform, false);
            }
            else if (schematicData.Primitive?.Transform != null)
                dummyNode.transform.SetParent(schematicData.Primitive.Transform, false);

            dummyNode.transform.localPosition = obj.Position;
            dummyNode.transform.localRotation = obj.Rotation;
            dummyNode.transform.localScale = obj.Scale;
            schematicData.ServerSideTransforms[obj.ObjectId] = dummyNode.transform;
        }

        private static void EnsureGameObjectNode(SerializableObject serializable, SchematicData schematicData)
        {
            if (serializable.ObjectType != ObjectType.GameObject && serializable.ObjectType != ObjectType.CullingParent)
                return;

            if (!schematicData.ServerSideTransforms.TryGetValue(serializable.ObjectId, out Transform? existing) || existing == null)
            {
                CreateGameObjectTransform(serializable, schematicData);
                return;
            }

            if (!schematicData.ServerSideTransforms.TryGetValue(serializable.ParentId, out Transform? parent) || parent == null)
                return;

            if (existing.parent == parent)
                return;

            Vector3 localPosition = existing.localPosition;
            Quaternion localRotation = existing.localRotation;
            Vector3 localScale = existing.localScale;
            existing.SetParent(parent, false);
            existing.localPosition = localPosition;
            existing.localRotation = localRotation;
            existing.localScale = localScale;
        }

        private static bool TryLoadAnimatorController(string schematicFileName, string animatorName, out RuntimeAnimatorController controller, out AssetBundle? outbundle)
        {
            controller = null!;
            outbundle = null;

            if (AnimatorControllerCache.TryGetValue((schematicFileName, animatorName), out RuntimeAnimatorController? cached))
            {
                if (cached != null)
                {
                    controller = cached;
                    return true;
                }

                return false;
            }

            foreach (AssetBundle bundle in AssetBundle.GetAllLoadedAssetBundles())
            {
                if (!BundleControllerCache.TryGetValue(bundle, out RuntimeAnimatorController[] controllers))
                {
                    controllers = bundle.LoadAllAssets<RuntimeAnimatorController>();
                    BundleControllerCache[bundle] = controllers;
                }

                if (controllers.Length == 0)
                    continue;

                RuntimeAnimatorController? match = null;
                foreach (RuntimeAnimatorController c in controllers)
                {
                    if (c.name == animatorName)
                    {
                        match = c;
                        break;
                    }
                }

                if (match == null)
                    continue;

                controller = match;
                AnimatorControllerCache[(schematicFileName, animatorName)] = controller;
                return true;
            }

            string safeSchematic = string.Concat(schematicFileName.Split(Path.GetInvalidFileNameChars()));
            string safeAnimator = string.Concat(animatorName.Split(Path.GetInvalidFileNameChars()));
            if (safeSchematic.Contains("..") || safeAnimator.Contains("..") || string.IsNullOrEmpty(safeSchematic) || string.IsNullOrEmpty(safeAnimator))
            {
                LogManager.Warn($"Blocked animator bundle path traversal attempt: '{schematicFileName}-{animatorName}'.");
                AnimatorControllerCache[(schematicFileName, animatorName)] = null!;
                return false;
            }

            string path = Path.Combine(ThaumFileManager.Dir(["Schematics"]), $"{safeSchematic}-{safeAnimator}");
            if (!File.Exists(path))
            {
                LogManager.Warn($"Animator bundle not found at '{path}'.");
                AnimatorControllerCache[(schematicFileName, animatorName)] = null!;
                return false;
            }

            outbundle = AssetBundle.LoadFromFile(path);
            if (outbundle == null)
            {
                LogManager.Warn($"Failed to load asset bundle at '{path}'.");
                AnimatorControllerCache[(schematicFileName, animatorName)] = null!;
                return false;
            }

            RuntimeAnimatorController[] bundleControllers = outbundle.LoadAllAssets<RuntimeAnimatorController>();
            if (bundleControllers.Length == 0)
            {
                AnimatorControllerCache[(schematicFileName, animatorName)] = null!;
                return false;
            }

            controller = bundleControllers.FirstOrDefault(c => c.name == animatorName) ?? bundleControllers[0];
            AnimatorControllerCache[(schematicFileName, animatorName)] = controller;
            return true;
        }

        private static void ApplyAnimatorsAndTools(SerializableSchematic schematic, SchematicData schematicData, Dictionary<int, ServerObject> serverObjectsById)
        {
            foreach (SerializableObject serializable in schematic.Objects)
            {
                ApplyAnimatorAndToolsForObject(serializable, schematic, schematicData, serverObjectsById);
            }

            foreach (SerializableObject serializable in schematic.ServerSideObjects)
            {
                ApplyAnimatorAndToolsForObject(serializable, schematic, schematicData, serverObjectsById);
            }
        }

        private static void ApplyAnimatorAndToolsForObject(SerializableObject serializable, SerializableSchematic schematic, SchematicData schematicData, Dictionary<int, ServerObject> serverObjectsById)
        {
            bool wantsAnimator = !string.IsNullOrEmpty(serializable.AnimatorName);
            bool wantsTools = serializable.Tools.Count > 0;
            if (!wantsAnimator && !wantsTools)
                return;

            if (!serverObjectsById.TryGetValue(serializable.ObjectId, out ServerObject match) || match.Object == null)
            {
                LogManager.Warn($"Could not find spawned object for animator/tools on '{serializable.Name}' in '{schematic.FileName}'.");
                return;
            }

            if (wantsAnimator)
                ApplyAnimatorForObject(serializable, schematic, match);

            if (wantsTools)
                ApplyToolsForObject(serializable, schematic, schematicData, match);
        }

        private static void ApplyAnimatorForObject(SerializableObject serializable, SerializableSchematic schematic, ServerObject match)
        {
            try
            {
                if (TryLoadAnimatorController(schematic.FileName, serializable.AnimatorName, out RuntimeAnimatorController controller, out AssetBundle? bundle) && match.Object != null)
                {
                    Animator animator = match.Object.GetComponent<Animator>() ?? match.Object.AddComponent<Animator>();
                    animator.runtimeAnimatorController = controller;
                    animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                    LogManager.Debug($"Applied animator '{controller.name}' to '{match.Object.name}' in '{schematic.FileName}'.");
                    bundle?.Unload(false);
                }
            }
            catch (Exception ex)
            {
                LogManager.Error($"Failed to apply animator '{serializable.AnimatorName}' on '{serializable.Name}': {ex.Message}");
            }
        }

        private static void ApplyAnimators(SerializableSchematic schematic, SchematicData schematicData, Dictionary<int, ServerObject> serverObjectsById)
        {
            foreach (SerializableObject serializable in schematic.Objects)
            {
                if (string.IsNullOrEmpty(serializable.AnimatorName))
                    continue;

                if (!serverObjectsById.TryGetValue(serializable.ObjectId, out ServerObject match) || match.Object == null)
                {
                    LogManager.Warn($"Could not find spawned object for animator '{serializable.AnimatorName}' in '{schematic.FileName}'.");
                    continue;
                }

                ApplyAnimatorForObject(serializable, schematic, match);
            }

            foreach (SerializableObject serializable in schematic.ServerSideObjects)
            {
                if (string.IsNullOrEmpty(serializable.AnimatorName))
                    continue;

                if (!serverObjectsById.TryGetValue(serializable.ObjectId, out ServerObject match) || match.Object == null)
                {
                    LogManager.Warn($"Could not find spawned object for animator '{serializable.AnimatorName}' in '{schematic.FileName}'.");
                    continue;
                }

                ApplyAnimatorForObject(serializable, schematic, match);
            }
        }

        private static void ApplyTools(SerializableSchematic schematic, SchematicData schematicData, Dictionary<int, ServerObject> serverObjectsById)
        {
            foreach (SerializableObject serializable in schematic.Objects)
            {
                if (serializable.Tools.Count == 0)
                    continue;

                if (!serverObjectsById.TryGetValue(serializable.ObjectId, out ServerObject match) || match.Object == null)
                {
                    LogManager.Warn($"Could not find spawned object for tools on '{serializable.Name}' in '{schematic.FileName}'.");
                    continue;
                }

                ApplyToolsForObject(serializable, schematic, schematicData, match);
            }

            foreach (SerializableObject serializable in schematic.ServerSideObjects)
            {
                if (serializable.Tools.Count == 0)
                    continue;

                if (!serverObjectsById.TryGetValue(serializable.ObjectId, out ServerObject match) || match.Object == null)
                {
                    LogManager.Warn($"Could not find spawned object for tools on '{serializable.Name}' in '{schematic.FileName}'.");
                    continue;
                }

                ApplyToolsForObject(serializable, schematic, schematicData, match);
            }
        }

        private static void ApplyToolsForObject(SerializableObject serializable, SerializableSchematic schematic, SchematicData schematicData, ServerObject match)
        {
            foreach (SerializableTool tool in serializable.Tools)
            {
                try
                {
                    if (!Enum.TryParse<ToolType>(tool.ToolName, true, out ToolType type))
                    {
                        LogManager.Warn($"Unknown tool type '{tool.ToolName}' on object '{serializable.Name}'.");
                        continue;
                    }

                    if (match.Object == null)
                        continue;

                    switch (type)
                    {
                        case ToolType.Health:
                            ObjectHealth health = match.Object.AddComponent<ObjectHealth>();
                            health.Init(match, schematicData, tool.Properties);
                            match.Tools.AddItem(health);
                            break;

                        case ToolType.Physics:
                            ObjectPhysics physics = match.Object.AddComponent<ObjectPhysics>();
                            physics.Init(match, schematicData, tool.Properties);
                            match.Tools.AddItem(physics);
                            break;

                        case ToolType.Doorlink:
                            DoorLink door = match.Object.AddComponent<DoorLink>();
                            door.Init(match, schematicData, tool.Properties);
                            match.Tools.AddItem(door);
                            break;

                        case ToolType.ColliderTrigger:
                            ColliderTrigger collider = match.Object.AddComponent<ColliderTrigger>();
                            collider.Init(match, schematicData, tool.Properties);
                            match.Tools.AddItem(collider);
                            break;

                        case ToolType.InteractableTrigger:
                            InteractableTrigger interactable = match.Object.AddComponent<InteractableTrigger>();
                            interactable.Init(match, schematicData, tool.Properties);
                            match.Tools.AddItem(interactable);
                            break;

                        case ToolType.BlockyRuntime:
                            BlockyRuntime blocky = match.Object.AddComponent<BlockyRuntime>();
                            blocky.Init(match, schematicData, tool.Properties);
                            if (blocky.Blocky != null && !string.IsNullOrEmpty(blocky.Blocky.Code))
                                schematicData.Executor?.Execute(ArgumentsParser.Load(blocky.Blocky), null!, EventType.OnSpawned);
                                
                            match.Tools.AddItem(blocky);
                            break;

                        case ToolType.DoorRuntime:
                            DoorRuntime doorBlocky = match.Object.AddComponent<DoorRuntime>();
                            doorBlocky.Init(match, schematicData, tool.Properties);
                            match.Tools.AddItem(doorBlocky);
                            break;

                        case ToolType.AnimationRuntime:
                            AnimationRuntime animationRuntime = match.Object.AddComponent<AnimationRuntime>();
                            animationRuntime.Init(match, schematicData, tool.Properties);
                            match.Tools.AddItem(animationRuntime);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    LogManager.Error($"Failed to apply tool '{tool.ToolName}' on '{serializable.Name}': {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Deserializes custom block values from <see cref="SerializableObject.Values"/> into an instance of <typeparamref name="T"/> without a YAML string round-trip.
        /// </summary>
        /// <typeparam name="T">The object type to deserialize into.</typeparam>
        /// <param name="serializable">The serializable object containing the values dictionary.</param>
        /// <returns>An instance of <typeparamref name="T"/> populated from the values dictionary.</returns>
        private static T DeserializeObject<T>(SerializableObject serializable) where T : new()
        {
            if (serializable.Values == null || serializable.Values.Count == 0)
                return new T();

            try
            {
                return serializable.Values.ConvertTo<T>();
            }
            catch (Exception ex)
            {
                LogManager.Warn($"Failed to deserialize object values for '{serializable.Name}' as {typeof(T).Name}: {ex.Message}");
                return new T();
            }
        }

        private static uint SpawnSerializableObject(SerializableObject serializable, SchematicData schematicData, uint parentNetId, LODZone[] lodZones, bool serverside = false)
        {
            NetworkServer.spawned.TryGetValue(parentNetId, out NetworkIdentity? identity);

            switch (serializable.ObjectType)
            {
                case ObjectType.Primitive:
                    if (serverside)
                    {
                        PrimitiveObjectServer serverprim = DeserializeObject<PrimitiveObjectServer>(serializable);
                        SetServerObjectProperties(serverprim, serializable);

                        serverprim.SpawnObject(schematicData, serializable);
                        if (identity != null)
                            serverprim.Object?.transform.SetParent(identity.transform, false);

                        serverprim.Name = serializable.Name;
                        return serverprim.NetId;
                    }
                    else
                    {
                        if (PrefabHelper.PrimitiveObject?.netIdentity == null)
                        {
                            LogManager.Warn($"Skipping primitive '{serializable.Name}': Primitive prefab not registered.");
                            return parentNetId;
                        }

                        PrimitiveObject primitive = DeserializeObject<PrimitiveObject>(serializable);
                        primitive.Name = serializable.Name;
                        primitive.ParentNetId = parentNetId;
                        primitive.NetId = NetworkIdentity.GetNextNetworkId();
                        primitive.Scale = serializable.Scale;
                        primitive.IsStatic = serializable.IsStatic;
                        primitive.Position = serializable.Position;
                        primitive.Rotation = serializable.Rotation;
                        primitive.MovementSmoothing = serializable.MovementSmoothing;
                        primitive.AssetId = PrefabHelper.PrimitiveObject.netIdentity.assetId;
                        primitive.Schematic = schematicData;
                        primitive.ObjectId = serializable.ObjectId;
                        primitive.ParentId = serializable.ParentId;

                        schematicData.SpawnedClientObjects.Add(primitive);
                        if (lodZones.IsEmpty())
                        {
                            foreach (Player player in Player.ReadyList.ToArray())
                            {
                                primitive.SpawnForPlayer(player);
                            }
                        }
                        else
                        {
                            foreach (LODZone zone in lodZones)
                            {
                                if (zone == null || !zone.PrimitivestoUnload.Contains(primitive.PrimitiveType) || zone.Collider == null)
                                    continue;

                                Bounds zoneBounds = zone.Collider.bounds;
                                foreach (Player player in Player.ReadyList.ToArray())
                                {
                                    if (zoneBounds.Contains(player.Position))
                                        primitive.SpawnForPlayer(player);
                                }
                            }
                        }

                        ColliderHelper.CreateCollisionMesh(primitive);
                        return primitive.NetId;
                    }

                case ObjectType.GameObject:
                    if (serverside)
                    {
                        PrimitiveObjectServer serverprim = DeserializeObject<PrimitiveObjectServer>(serializable);

                        serverprim.SpawnObject(schematicData, serializable);
                        if (identity != null)
                            serverprim.Object?.transform.SetParent(identity.transform, false);

                        serverprim.Name = serializable.Name;
                        EnsureGameObjectNode(serializable, schematicData);
                        return serverprim.NetId;
                    }
                    else
                    {
                        PrimitiveObject gameObject = DeserializeObject<PrimitiveObject>(serializable);
                        gameObject.Name = serializable.Name;
                        gameObject.ParentNetId = parentNetId;
                        gameObject.NetId = NetworkIdentity.GetNextNetworkId();
                        gameObject.Scale = serializable.Scale;
                        gameObject.IsStatic = serializable.IsStatic;
                        gameObject.Position = serializable.Position;
                        gameObject.Rotation = serializable.Rotation;
                        gameObject.MovementSmoothing = serializable.MovementSmoothing;
                        gameObject.AssetId = PrefabHelper.PrimitiveObject!.netIdentity.assetId;
                        gameObject.Schematic = schematicData;
                        gameObject.PrimitiveFlags = PrimitiveFlags.None;
                        gameObject.PrimitiveType = PrimitiveType.Cube;
                        gameObject.Color = Color.white;
                        gameObject.ObjectId = serializable.ObjectId;
                        gameObject.ParentId = serializable.ParentId;

                        schematicData.SpawnedClientObjects.Add(gameObject);
                        EnsureGameObjectNode(serializable, schematicData);
                        foreach (Player player in Player.ReadyList)
                        {
                            gameObject.SpawnForPlayer(player);
                        }

                        return gameObject.NetId;
                    }

                case ObjectType.Capybara:
                    if (serverside)
                    {
                        CapybaraObjectServer servercapy = DeserializeObject<CapybaraObjectServer>(serializable);

                        SetServerObjectProperties(servercapy, serializable);

                        servercapy.SpawnObject(schematicData, serializable);
                        if (identity != null)
                            servercapy.Object?.transform.SetParent(identity.transform, false);

                        servercapy.Name = serializable.Name;
                        return servercapy.NetId;
                    }
                    else
                    {
                        CapybaraObject capybara = DeserializeObject<CapybaraObject>(serializable);
                        capybara.Name = serializable.Name;
                        capybara.ParentNetId = parentNetId;
                        capybara.NetId = NetworkIdentity.GetNextNetworkId();
                        capybara.Scale = serializable.Scale;
                        capybara.IsStatic = serializable.IsStatic;
                        capybara.Position = serializable.Position;
                        capybara.Rotation = serializable.Rotation;
                        capybara.MovementSmoothing = serializable.MovementSmoothing;
                        capybara.Schematic = schematicData;
                        capybara.ObjectId = serializable.ObjectId;
                        capybara.ParentId = serializable.ParentId;

                        schematicData.SpawnedClientObjects.Add(capybara);

                        foreach (Player player in Player.ReadyList.ToArray())
                        {
                            capybara.SpawnForPlayer(player);
                        }

                        if (capybara.CollisionsEnabled)
                            ColliderHelper.CreateClientObjectColliders(capybara, schematicData);

                        return capybara.NetId;
                    }

                case ObjectType.Light:
                    if (serverside)
                    {
                        LightObjectServer serverlight = DeserializeObject<LightObjectServer>(serializable);

                        SetServerObjectProperties(serverlight, serializable);

                        serverlight.SpawnObject(schematicData, serializable);
                        if (identity != null)
                            serverlight.Object?.transform.SetParent(identity.transform, false);

                        serverlight.Name = serializable.Name;
                        return serverlight.NetId;
                    }
                    else
                    {
                        if (PrefabHelper.LightSource?.netIdentity == null)
                        {
                            LogManager.Warn($"Skipping light '{serializable.Name}': Light prefab not registered.");
                            return parentNetId;
                        }

                        LightObject light = DeserializeObject<LightObject>(serializable);
                        light.ParentNetId = parentNetId;
                        light.NetId = NetworkIdentity.GetNextNetworkId();
                        light.AssetId = PrefabHelper.LightSource.netIdentity.assetId;
                        light.Scale = serializable.Scale;
                        light.IsStatic = serializable.IsStatic;
                        light.Position = serializable.Position;
                        light.Rotation = serializable.Rotation;
                        light.MovementSmoothing = serializable.MovementSmoothing;
                        light.Schematic = schematicData;
                        light.ObjectId = serializable.ObjectId;
                        light.ParentId = serializable.ParentId;

                        schematicData.SpawnedClientObjects.Add(light);
                        ColliderHelper.CreateSelectionSphere(light, schematicData);

                        foreach (Player player in Player.ReadyList.ToArray())
                        {
                            light.SpawnForPlayer(player);
                        }

                        return light.NetId;
                    }

                case ObjectType.Clutter:
                    if (serverside)
                    {
                        ClutterObject clutter = DeserializeObject<ClutterObject>(serializable);

                        SetServerObjectProperties(clutter, serializable);

                        clutter.SpawnObject(schematicData, serializable);
                        clutter.Name = serializable.Name;
                        return clutter.NetId;
                    }
                    else
                    {
                        ClientClutterObject clientClutter = DeserializeObject<ClientClutterObject>(serializable);

                        uint clutterAssetId;
                        try
                        {
                            clutterAssetId = PrefabHelper.GetAssetId(ClutterObject.GetClutterPrefab(clientClutter.Type));
                        }
                        catch (ArgumentOutOfRangeException ex)
                        {
                            LogManager.Warn($"Skipping client clutter '{serializable.Name}': {ex.Message}");
                            return parentNetId;
                        }

                        if (clutterAssetId == 0)
                        {
                            LogManager.Warn($"Skipping client clutter '{serializable.Name}': clutter prefab has no NetworkIdentity.");
                            return parentNetId;
                        }

                        clientClutter.Name = serializable.Name;
                        clientClutter.ParentNetId = parentNetId;
                        clientClutter.NetId = NetworkIdentity.GetNextNetworkId();
                        clientClutter.Scale = serializable.Scale;
                        clientClutter.IsStatic = serializable.IsStatic;
                        clientClutter.Position = serializable.Position;
                        clientClutter.Rotation = serializable.Rotation;
                        clientClutter.MovementSmoothing = serializable.MovementSmoothing;
                        clientClutter.AssetId = clutterAssetId;
                        clientClutter.Schematic = schematicData;
                        clientClutter.ObjectId = serializable.ObjectId;
                        clientClutter.ParentId = serializable.ParentId;

                        schematicData.SpawnedClientObjects.Add(clientClutter);

                        foreach (Player player in Player.ReadyList.ToArray())
                        {
                            clientClutter.SpawnForPlayer(player);
                        }

                        ColliderHelper.CreateClientObjectColliders(clientClutter, schematicData);

                        clientClutter.ClearDirtyFlags();
                        SyncManager.RemoveFromPending(clientClutter);

                        return clientClutter.NetId;
                    }

                case ObjectType.CullingParent:
                    if (serverside)
                    {
                        CullingParentObject serverculling = DeserializeObject<CullingParentObject>(serializable);

                        if (PrefabHelper.CullingParent == null)
                        {
                            LogManager.Warn($"Skipping culling parent '{serializable.Name}': culling parent prefab not registered.");
                            return parentNetId;
                        }

                        if (serverculling.BoundsSize == Vector3.zero)
                        {
                            LogManager.Warn($"Skipping culling parent '{serializable.Name}': BoundsSize is zero.");
                            return parentNetId;
                        }

                        serverculling.Name = serializable.Name;
                        serverculling.Position = serializable.Position;
                        serverculling.Rotation = serializable.Rotation;
                        serverculling.Scale = serializable.Scale;
                        serverculling.IsStatic = serializable.IsStatic;

                        serverculling.SpawnObject(schematicData, serializable);
                        if (serverculling.Object == null)
                            return parentNetId;

                        schematicData.ServerSideTransforms[serializable.ObjectId] = serverculling.Object.transform;
                        LogManager.Debug($"Spawned server culling parent '{serverculling.Name}' with NetId {serverculling.NetId}.");
                        return serverculling.NetId;
                    }
                    else
                    {
                        ClientCullingParentObject clientculling = DeserializeObject<ClientCullingParentObject>(serializable);

                        if (PrefabHelper.CullingParent?.netIdentity == null)
                        {
                            LogManager.Warn($"Skipping client culling parent '{serializable.Name}': culling parent prefab not registered.");
                            return parentNetId;
                        }

                        if (clientculling.BoundsSize == Vector3.zero)
                        {
                            LogManager.Warn($"Skipping client culling parent '{serializable.Name}': BoundsSize is zero.");
                            return parentNetId;
                        }

                        clientculling.Name = serializable.Name;
                        clientculling.ParentNetId = parentNetId;
                        clientculling.NetId = NetworkIdentity.GetNextNetworkId();
                        clientculling.AssetId = PrefabHelper.CullingParent.netIdentity.assetId;
                        clientculling.Position = serializable.Position;
                        clientculling.Rotation = serializable.Rotation;
                        clientculling.Scale = Vector3.one;
                        clientculling.IsStatic = serializable.IsStatic;
                        clientculling.MovementSmoothing = serializable.MovementSmoothing;
                        clientculling.ObjectId = serializable.ObjectId;
                        clientculling.ParentId = serializable.ParentId;
                        clientculling.Schematic = schematicData;

                        schematicData.SpawnedClientObjects.Add(clientculling);
                        EnsureGameObjectNode(serializable, schematicData);

                        foreach (Player player in Player.ReadyList.ToArray())
                        {
                            clientculling.SpawnForPlayer(player);
                        }

                        clientculling.ClearDirtyFlags();
                        SyncManager.RemoveFromPending(clientculling);

                        LogManager.Debug($"Spawned client culling parent '{clientculling.Name}' with NetId {clientculling.NetId} at {clientculling.ResolveWorldTransform().Position} (local {clientculling.Position}).");
                        return clientculling.NetId;
                    }

                case ObjectType.Door:
                    DoorObject door = DeserializeObject<DoorObject>(serializable);

                    SetServerObjectProperties(door, serializable);

                    door.SpawnObject(schematicData, serializable);
                    door.Name = serializable.Name;
                    return door.NetId;

                case ObjectType.TextToy:
                    if (serverside)
                    {
                        TextToyObject textToy = DeserializeObject<TextToyObject>(serializable);

                        SetServerObjectProperties(textToy, serializable);

                        textToy.SpawnObject(schematicData, serializable);
                        textToy.Name = serializable.Name;
                        return textToy.NetId;
                    }
                    else
                    {
                        if (PrefabHelper.TextToy?.netIdentity == null)
                        {
                            LogManager.Warn($"Skipping text toy '{serializable.Name}': TextToy prefab not registered.");
                            return parentNetId;
                        }

                        TextObject textObject = DeserializeObject<TextObject>(serializable);
                        textObject.ParentNetId = parentNetId;
                        textObject.NetId = NetworkIdentity.GetNextNetworkId();
                        textObject.AssetId = PrefabHelper.TextToy.netIdentity.assetId;
                        textObject.Scale = serializable.Scale;
                        textObject.IsStatic = serializable.IsStatic;
                        textObject.Position = serializable.Position;
                        textObject.Rotation = serializable.Rotation;
                        textObject.MovementSmoothing = serializable.MovementSmoothing;
                        textObject.Schematic = schematicData;
                        textObject.ObjectId = serializable.ObjectId;
                        textObject.ParentId = serializable.ParentId;

                        schematicData.SpawnedClientObjects.Add(textObject);

                        foreach (Player player in Player.ReadyList.ToArray())
                        {
                            textObject.SpawnForPlayer(player);
                        }

                        return textObject.NetId;
                    }

                case ObjectType.Workstation:
                    WorkstationObject workstation = DeserializeObject<WorkstationObject>(serializable);

                    SetServerObjectProperties(workstation, serializable);

                    workstation.SpawnObject(schematicData, serializable);
                    workstation.Name = serializable.Name;
                    return workstation.NetId;

                case ObjectType.Camera:
                    CameraObject camera = DeserializeObject<CameraObject>(serializable);
                    camera.Position = serializable.Position;
                    camera.Rotation = serializable.Rotation;
                    camera.Scale = serializable.Scale;
                    camera.IsStatic = serializable.IsStatic;

                    camera.SpawnObject(schematicData, serializable);
                    camera.Name = serializable.Name;
                    return camera.NetId;

                case ObjectType.Interactable:
                    InteractionObject interaction = DeserializeObject<InteractionObject>(serializable);

                    SetServerObjectProperties(interaction, serializable);

                    interaction.SpawnObject(schematicData, serializable);
                    interaction.Name = serializable.Name;
                    return interaction.NetId;

                case ObjectType.Waypoint:
                    WaypointObject waypoint = DeserializeObject<WaypointObject>(serializable);

                    SetServerObjectProperties(waypoint, serializable);

                    waypoint.SpawnObject(schematicData, serializable);
                    waypoint.Name = serializable.Name;
                    return waypoint.NetId;

                case ObjectType.Locker:
                    LockerObject locker = DeserializeObject<LockerObject>(serializable);

                    SetServerObjectProperties(locker, serializable);

                    locker.SpawnObject(schematicData, serializable);
                    locker.Name = serializable.Name;
                    return locker.NetId;

                case ObjectType.Pickup:
                    PickupObject pickup = DeserializeObject<PickupObject>(serializable);

                    SetServerObjectProperties(pickup, serializable);

                    pickup.SpawnObject(schematicData, serializable);
                    pickup.Name = serializable.Name;
                    return pickup.NetId;

                case ObjectType.Target:
                    TargetDummyObject target = DeserializeObject<TargetDummyObject>(serializable);

                    SetServerObjectProperties(target, serializable);

                    target.SpawnObject(schematicData, serializable);
                    target.Name = serializable.Name;
                    return target.NetId;

                case ObjectType.Teleporter:
                    TeleporterObject teleporter = DeserializeObject<TeleporterObject>(serializable);

                    SetServerObjectProperties(teleporter, serializable);

                    teleporter.SpawnObject(schematicData, serializable);
                    teleporter.Name = serializable.Name;
                    return teleporter.NetId;

                case ObjectType.Speaker:
                    SpeakerObject speaker = DeserializeObject<SpeakerObject>(serializable);

                    SetServerObjectProperties(speaker, serializable);

                    speaker.SpawnObject(schematicData, serializable);
                    speaker.Name = serializable.Name;
                    return speaker.NetId;

                case ObjectType.PlayerSpawnPoint:
                    PlayerSpawnPoint spawn = DeserializeObject<PlayerSpawnPoint>(serializable);

                    SetServerObjectProperties(spawn, serializable);

                    spawn.SpawnObject(schematicData, serializable);
                    spawn.Name = serializable.Name;
                    return spawn.NetId;

                case ObjectType.RagdollSpawner:
                    RagdollSpawner ragdoll = DeserializeObject<RagdollSpawner>(serializable);

                    SetServerObjectProperties(ragdoll, serializable);

                    ragdoll.SpawnObject(schematicData, serializable);
                    ragdoll.Name = serializable.Name;
                    return 0;

                default:
                    LogManager.Warn($"Unhandled ObjectType '{serializable.ObjectType}' on object '{serializable.Name}', skipping.");
                    return 0;
            }
        }

        /// <summary>
        /// Saves a map
        /// </summary>
        /// <param name="data">The <see cref="MapData"/> to save</param>
        /// <returns><see cref="SerializableMap"/></returns>
        public static SerializableMap SaveMap(MapData data)
        {
            SerializableMap map = new()
            {
                FileName = data.FileName,
                Room = data.Room!.Name,
                Id = Guid.NewGuid()
            };

            foreach (MapSchematicData msdata in data.Schematics)
            {
                SerializedMapSchematic mapSchematic = new()
                {
                    Position = msdata.LocalPosition,
                    SchematicName = msdata.SchematicName
                };

                map.Schematics.Add(mapSchematic);
            }

            string safeName = string.Concat(map.FileName.Split(Path.GetInvalidFileNameChars()));
            if (string.IsNullOrWhiteSpace(safeName) || safeName.Contains(".."))
            {
                LogManager.Warn($"Blocked map save with unsafe file name '{map.FileName}'.");
                return map;
            }

            string mapsDir = ThaumFileManager.Dir(["Maps"]);
            ThaumFileManager.TryCreateDirectory(mapsDir);
            string finalPath = Path.Combine(mapsDir, $"{safeName}.bin");
            string tempPath = finalPath + ".tmp";
            File.WriteAllBytes(tempPath, BinarySerializer.Serialize(map));
            File.Copy(tempPath, finalPath, overwrite: true);
            try
            {
                File.Delete(tempPath);
            }
            catch { }
            return map;
        }

        private static void SetServerObjectProperties(ServerObject obj, SerializableObject serializable)
        {
            obj.Position = serializable.Position;
            obj.Rotation = serializable.Rotation;
            obj.Scale = serializable.Scale;
            obj.IsStatic = serializable.IsStatic;
        }
    }
}