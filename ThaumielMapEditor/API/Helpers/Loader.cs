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
using MEC;
using Mirror;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
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
            _nextId = 0;
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
        /// Destroys the specified <see cref="SchematicData"/>
        /// </summary>
        /// <param name="data">The schematic to be destroyed</param>
        public static void DestroySchematic(SchematicData data)
        {
            SchematicDestroyed?.Invoke(data);
            SchematicsById.Remove(data.Id);

            if (data.Id < _nextId)
                _nextId = data.Id;

            data.Destroy();
        }

        /// <summary>
        /// Loads the schematics
        /// </summary>
        public static void LoadSchematics()
        {
            string schematicDir = ThaumFileManager.Dir(["Schematics"]);
            ThaumFileManager.TryCreateDirectory(schematicDir);

            foreach (string path in ThaumFileManager.GetFilesInDirectory(schematicDir))
            {
                string name = Path.GetFileNameWithoutExtension(path);

                try
                {
                    if (Main.Instance.Config.ReadFilesInBackground)
                    {
                        ThaumFileManager.ReadFileInBackground(path, (value) => 
                        {
                            try
                            {
                                SerializableSchematic schematic = Deserializer.Deserialize<SerializableSchematic>(value);
                                schematic.FileName = name;
                                LoadedSchematics[schematic.FileName] = schematic;
                                LogManager.Debug($"Loaded schematic {name} on background thread");
                            }
                            catch (YamlException yamlex)
                            {
                                LogManager.Warn($"Failed to parse Schematic {name}. \n\n {yamlex}");
                            }
                            catch (Exception ex)
                            {
                                LogManager.Warn($"Exception when trying to parse Schematic {name}. \n\n {ex}");
                            }
                        });
                    }
                    else
                    {
                        SerializableSchematic schematic = Deserializer.Deserialize<SerializableSchematic>(File.ReadAllText(path));
                        schematic.FileName = name;
                        LoadedSchematics[schematic.FileName] = schematic;
                        LogManager.Debug($"Loaded schematic {name} on main thread");
                    }
                }
                catch (YamlException yamlex)
                {
                    LogManager.Warn($"Failed to parse Schematic {name}. \n\n {yamlex}");
                    continue;
                }
                catch (Exception ex)
                {
                    LogManager.Warn($"Exception when trying to parse Schematic {name}. \n\n {ex}");
                    continue;
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

            foreach (string path in ThaumFileManager.GetFilesInDirectory(mapsDir))
            {
                string name = Path.GetFileNameWithoutExtension(path);

                try
                {
                    if (Main.Instance.Config.ReadFilesInBackground)
                    {
                        ThaumFileManager.ReadFileInBackground(path, (value) => 
                        {
                            try
                            {
                                SerializableMap map = Deserializer.Deserialize<SerializableMap>(value);
                                map.FileName = name;
                                LoadedMaps.Add(map.FileName, map);
                                LogManager.Debug($"Loaded map {name} on background thread");
                            }
                            catch (YamlException yamlex)
                            {
                                LogManager.Warn($"Failed to parse Map {name}. \n\n {yamlex}");
                            }
                            catch (Exception ex)
                            {
                                LogManager.Warn($"Exception when trying to parse Map {name}. \n\n {ex}");
                            }
                        });
                    }
                    else
                    {
                        SerializableMap map = Deserializer.Deserialize<SerializableMap>(File.ReadAllText(path));
                        map.FileName = name;
                        LoadedMaps.Add(map.FileName, map);
                        LogManager.Debug($"Loaded map {name} on main thread");
                    }
                }
                catch (YamlException yamlex)
                {
                    LogManager.Warn($"Failed to parse Map {name}. \n\n {yamlex}");
                    continue;
                }
                catch (Exception ex)
                {
                    LogManager.Warn($"Exception when trying to parse Map {name}. \n\n {ex}");
                    continue;
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

        /// <summary>
        /// Gets a unique id for all <see cref="SchematicData"/>
        /// </summary>
        /// <returns><see cref="uint"/> id</returns>
        public static uint GetId()
        {
            while (SchematicsById.ContainsKey(_nextId))
                _nextId++;

            return _nextId;
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
                    });

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
                    parentDict[obj.ParentId] = list;
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
                    TaskCompletionSource<bool> tcs = new();

                    MainThreadDispatcher.Dispatch(() =>
                    {
                        try
                        {
                            foreach ((int currentId, uint parentNetId) in currentBatch)
                            {
                                uint currentNetId = parentNetId;

                                if (objectsById.TryGetValue(currentId, out var obj))
                                {
                                    currentNetId = SpawnSerializableObject(obj.Item1, schematicData, parentNetId, lodZones, serverside: obj.Item2);
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
                            tcs.SetResult(true);
                        }
                    });

                    await tcs.Task;

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

                TaskCompletionSource<bool> completionTcs = new();
                MainThreadDispatcher.Dispatch(() =>
                {
                    try
                    {
                        schematicData.Executor = new(schematicData);
                        ApplyAnimators(schematic, schematicData);
                        ApplyTools(schematic, schematicData);

                        SchematicHandler.OnSchematicSpawned(new(schematicData));
                        SchematicSpawned?.Invoke(schematicData);
                        LogManager.Info($"Schematic '{schematic.FileName}' fully spawned.");
                        SchematicsById.Add(schematicData.Id, schematicData);

                        if (Main.Instance.Config!.SchematicAnimationPlayOnLoad.TryGetValue(schematicData.FileName, out var animationname))
                            schematicData.AnimationController.Play(animationname);
                    }
                    catch (Exception ex)
                    {
                        LogManager.Error($"Exception completing schematic spawn: {ex}");
                    }
                    finally
                    {
                        completionTcs.SetResult(true);
                    }
                });

                await completionTcs.Task;
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

            foreach (string filename in ThaumFileManager.GetFilesInDirectory(directory))
            {
                try
                {
                    SerializableSchematic schematic = Deserializer.Deserialize<SerializableSchematic>(File.ReadAllText(filename));
                    schematic.FileName = Path.GetFileNameWithoutExtension(filename);
                    LoadedSchematics[schematic.FileName] = schematic;
                    LogManager.Debug($"Loaded schematic '{schematic.FileName}' from custom directory '{directory}'.");
                }
                catch (YamlException yamlex)
                {
                    LogManager.Warn($"Failed to parse schematic '{Path.GetFileNameWithoutExtension(filename)}' in '{directory}'.\n\n{yamlex}");
                }
                catch (Exception ex)
                {
                    LogManager.Warn($"Exception when loading schematic '{Path.GetFileNameWithoutExtension(filename)}' in '{directory}'.\n\n{ex}");
                }
            }
        }

        private static SerializableSchematic? TryLoadSchematicFromDirectory(string directory, string schematicName)
        {
            if (!Directory.Exists(directory))
            {
                LogManager.Warn($"Custom schematic directory not found: '{directory}'.");
                return null;
            }

            string path = Path.Combine(directory, $"{schematicName}.yml");
            if (!File.Exists(path))
            {
                LogManager.Warn($"Schematic '{schematicName}' not found in directory '{directory}'.");
                return null;
            }

            return TryLoadSchematicFromPath(path);
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
        /// <param name="path">The full file path to the schematic YAML file.</param>
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
        /// <param name="path">The full file path to the schematic YAML file.</param>
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
        /// <param name="path">The full file path to the schematic YAML file.</param>
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
        /// <param name="path">The full file path to the schematic YAML file.</param>
        /// <returns>A <see cref="SerializableSchematic"/> if successful, otherwise <see langword="null"/>.</returns>
        private static SerializableSchematic? TryLoadSchematicFromPath(string path)
        {
            if (!File.Exists(path))
            {
                LogManager.Warn($"Schematic file not found at path '{path}'.");
                return null;
            }

            try
            {
                SerializableSchematic schematic = Deserializer.Deserialize<SerializableSchematic>(File.ReadAllText(path));
                schematic.FileName = Path.GetFileNameWithoutExtension(path);
                LogManager.Debug($"Loaded schematic from path '{path}'.");
                return schematic;
            }
            catch (YamlException yamlex)
            {
                LogManager.Warn($"Failed to parse schematic at '{path}'.\n\n{yamlex}");
                return null;
            }
            catch (Exception ex)
            {
                LogManager.Warn($"Exception when loading schematic at '{path}'.\n\n{ex}");
                return null;
            }
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
            foreach (SerializableObject obj in schematic.Objects)
            {
                if (obj.ObjectType == ObjectType.GameObject)
                {
                    GameObject dummyNode = new($"[SchematicNode] {obj.Name}");

                    if (schematicData.ServerSideTransforms.TryGetValue(obj.ParentId, out Transform parentTransform))
                    {
                        dummyNode.transform.SetParent(parentTransform, false);
                    }
                    else
                        dummyNode.transform.SetParent(schematicData.Primitive?.Transform, false);

                    dummyNode.transform.localPosition = obj.Position;
                    dummyNode.transform.localRotation = obj.Rotation;
                    dummyNode.transform.localScale = obj.Scale;

                    LogManager.Debug($"Added transform with local position: {dummyNode.transform.localPosition}");
                    schematicData.ServerSideTransforms[obj.ObjectId] = dummyNode.transform;
                }
            }
        }

        private static bool TryLoadAnimatorController(string schematicFileName, string animatorName, out RuntimeAnimatorController controller, out AssetBundle? outbundle)
        {
            controller = null!;
            outbundle = null;

            foreach (AssetBundle bundle in AssetBundle.GetAllLoadedAssetBundles())
            {
                RuntimeAnimatorController[] controllers = bundle.LoadAllAssets<RuntimeAnimatorController>();
                if (controllers.Length == 0)
                    continue;

                RuntimeAnimatorController? match = controllers.FirstOrDefault(c => c.name == animatorName);
                if (match == null)
                    continue;

                controller = match;
                return true;
            }

            string path = Path.Combine(ThaumFileManager.Dir(["Schematics"]), $"{schematicFileName}-{animatorName}");
            if (!File.Exists(path))
            {
                LogManager.Warn($"Animator bundle not found at '{path}'.");
                return false;
            }

            outbundle = AssetBundle.LoadFromFile(path);
            if (outbundle == null)
            {
                LogManager.Warn($"Failed to load asset bundle at '{path}'.");
                return false;
            }

            RuntimeAnimatorController[] bundleControllers = outbundle.LoadAllAssets<RuntimeAnimatorController>();
            if (bundleControllers.Length == 0)
                return false;

            controller = bundleControllers.FirstOrDefault(c => c.name == animatorName) ?? bundleControllers[0];
            return true;
        }

        private static void ApplyAnimators(SerializableSchematic schematic, SchematicData schematicData)
        {
            IEnumerable<SerializableObject> animatables = schematic.Objects.Concat(schematic.ServerSideObjects).Where(o => !string.IsNullOrEmpty(o.AnimatorName));
            Dictionary<int, ServerObject> serverObjectsById = schematicData.SpawnedServerObjects.ToDictionary(o => o.ObjectId);

            foreach (SerializableObject serializable in animatables)
            {
                if (!TryLoadAnimatorController(schematic.FileName, serializable.AnimatorName, out RuntimeAnimatorController controller, out AssetBundle? bundle))
                    continue;

                if (!serverObjectsById.TryGetValue(serializable.ObjectId, out ServerObject match) || match.Object == null)
                {
                    LogManager.Warn($"Could not find spawned object for animator '{serializable.AnimatorName}' in '{schematic.FileName}'.");
                    continue;
                }

                Animator animator = match.Object.GetComponent<Animator>() ?? match.Object.AddComponent<Animator>();
                animator.runtimeAnimatorController = controller;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                LogManager.Debug($"Applied animator '{controller.name}' to '{match.Object.name}' in '{schematic.FileName}'.");
                bundle?.Unload(false);
            }
        }

        private static void ApplyTools(SerializableSchematic schematic, SchematicData schematicData)
        {
            Dictionary<int, ServerObject> serverObjectsById = schematicData.SpawnedServerObjects.ToDictionary(o => o.ObjectId);

            foreach (SerializableObject serializable in schematic.Objects.Concat(schematic.ServerSideObjects).Where(o => o.Tools.Count > 0))
            {
                if (!serverObjectsById.TryGetValue(serializable.ObjectId, out ServerObject match) || match.Object == null)
                {
                    LogManager.Warn($"Could not find spawned object for tools on '{serializable.Name}' in '{schematic.FileName}'.");
                    continue;
                }

                foreach (SerializableTool tool in serializable.Tools)
                {
                    if (!Enum.TryParse<ToolType>(tool.ToolName, true, out ToolType type))
                    {
                        LogManager.Warn($"Unknown tool type '{tool.ToolName}' on object '{serializable.Name}'.");
                        continue;
                    }

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
                            schematicData.Executor?.Execute(ArgumentsParser.Load(blocky.Blocky!), null!, EventType.OnSpawned);
                            match.Tools.AddItem(blocky);
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// Deserializes custom block values from <see cref="SerializableObject.Values"/> into an instance of <typeparamref name="T"/> using YAML deserialization.
        /// </summary>
        /// <typeparam name="T">The object type to deserialize into.</typeparam>
        /// <param name="serializable">The serializable object containing the values dictionary.</param>
        /// <returns>An instance of <typeparamref name="T"/> populated from the YAML values.</returns>
        private static T DeserializeObject<T>(SerializableObject serializable) where T : new()
        {
            if (serializable.Values == null || serializable.Values.Count == 0)
                return new T();

            try
            {
                return Deserializer.Deserialize<T>(Serializer.Serialize(serializable.Values)) ?? new T();
            }
            catch (Exception ex)
            {
                LogManager.Warn($"Failed to deserialize object values for '{serializable.Name}' as {typeof(T).Name}: {ex}");
                return new T();
            }
        }

        private static uint SpawnSerializableObject(SerializableObject serializable, SchematicData schematicData, uint parentNetId, LODZone[] lodZones, bool serverside = false)
        {
            NetworkServer.spawned.TryGetValue(parentNetId, out var identity);

            switch (serializable.ObjectType)
            {
                case ObjectType.Primitive:
                    if (serverside)
                    {
                        PrimitiveObjectServer serverprim = DeserializeObject<PrimitiveObjectServer>(serializable);
                        serverprim.Position = serializable.Position;
                        serverprim.Rotation = serializable.Rotation;
                        serverprim.Scale = serializable.Scale;
                        serverprim.IsStatic = serializable.IsStatic;

                        serverprim.SpawnObject(schematicData, serializable);
                        if (identity != null)
                            serverprim.Object?.transform.SetParent(identity.transform, false);

                        serverprim.Name = serializable.Name;
                        SetupCulling(serializable, serverprim);
                        LogManager.Debug($"[SERVER] {serverprim.Name} - {serverprim.Color} - {serverprim.PrimitiveType} - {serverprim.PrimitiveFlags}");
                        return serverprim.NetId;
                    }
                    else
                    {
                        PrimitiveObject primitive = DeserializeObject<PrimitiveObject>(serializable);
                        primitive.Name = serializable.Name;
                        primitive.ParentNetId = parentNetId;
                        primitive.NetId = NetworkIdentity.GetNextNetworkId();
                        primitive.Scale = serializable.Scale;
                        primitive.IsStatic = serializable.IsStatic;
                        primitive.Position = serializable.Position;
                        primitive.Rotation = serializable.Rotation;
                        primitive.MovementSmoothing = serializable.MovementSmoothing;
                        primitive.AssetId = PrefabHelper.PrimitiveObject!.netIdentity.assetId;
                        primitive.Schematic = schematicData;
                        primitive.ObjectId = serializable.ObjectId;
                        primitive.ParentId = serializable.ParentId;

                        LogManager.Debug($"[CLIENT] {primitive.Name} - {primitive.Color} - {primitive.PrimitiveType} - {primitive.PrimitiveFlags}");
                        schematicData.SpawnedClientObjects.Add(primitive);
                        if (lodZones.IsEmpty())
                        {
                            foreach (Player player in Player.ReadyList)
                            {
                                primitive.SpawnForPlayer(player);
                            }
                        }
                        else
                        {
                            LODZone[] varzone = lodZones.Where(z => z.PrimitivestoUnload.Contains(primitive.PrimitiveType)).ToArray();
                            foreach (LODZone zone in varzone)
                            {
                                if (zone.Collider == null)
                                    continue;

                                foreach (Player player in Player.ReadyList)
                                {
                                    if (zone.Collider.bounds.Contains(player.Position))
                                        primitive.SpawnForPlayer(player);
                                }
                            }
                        }

                        SetupCulling(serializable, primitive, schematicData);
                        ColliderHelper.CreateCollisionMesh(primitive);
                        return primitive.NetId;
                    }

                case ObjectType.GameObject:
                    if (serverside)
                    {
                        PrimitiveObjectServer serverprim = DeserializeObject<PrimitiveObjectServer>(serializable);
                        serverprim.Position = serializable.Position;
                        serverprim.Rotation = serializable.Rotation;
                        serverprim.Scale = serializable.Scale;
                        serverprim.IsStatic = serializable.IsStatic;

                        serverprim.SpawnObject(schematicData, serializable);
                        if (identity != null)
                            serverprim.Object?.transform.SetParent(identity.transform, false);

                        serverprim.Name = serializable.Name;
                        SetupCulling(serializable, serverprim);
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
                        foreach (Player player in Player.ReadyList)
                        {
                            gameObject.SpawnForPlayer(player);
                        }

                        SetupCulling(serializable, gameObject, schematicData);
                        return gameObject.NetId;
                    }

                case ObjectType.Capybara:
                    if (serverside)
                    {
                        CapybaraObjectServer servercapy = DeserializeObject<CapybaraObjectServer>(serializable);
                        servercapy.Position = serializable.Position;
                        servercapy.Rotation = serializable.Rotation;
                        servercapy.Scale = serializable.Scale;
                        servercapy.IsStatic = serializable.IsStatic;

                        servercapy.SpawnObject(schematicData, serializable);
                        if (identity != null)
                            servercapy.Object?.transform.SetParent(identity.transform, false);

                        servercapy.Name = serializable.Name;
                        SetupCulling(serializable, servercapy);
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

                        foreach (Player player in Player.ReadyList)
                        {
                            capybara.SpawnForPlayer(player);
                        }

                        if (capybara.CollisionsEnabled)
                            ColliderHelper.CreateClientObjectColliders(capybara, schematicData);

                        SetupCulling(serializable, capybara, schematicData);
                        return capybara.NetId;
                    }

                case ObjectType.Light:
                    if (serverside)
                    {
                        LightObjectServer serverlight = DeserializeObject<LightObjectServer>(serializable);
                        serverlight.Position = serializable.Position;
                        serverlight.Rotation = serializable.Rotation;
                        serverlight.Scale = serializable.Scale;
                        serverlight.IsStatic = serializable.IsStatic;

                        serverlight.SpawnObject(schematicData, serializable);
                        if (identity != null)
                            serverlight.Object?.transform.SetParent(identity.transform, false);

                        serverlight.Name = serializable.Name;
                        SetupCulling(serializable, serverlight);
                        return serverlight.NetId;
                    }
                    else
                    {
                        LightObject light = DeserializeObject<LightObject>(serializable);
                        light.ParentNetId = parentNetId;
                        light.NetId = NetworkIdentity.GetNextNetworkId();
                        light.AssetId = PrefabHelper.LightSource!.netIdentity.assetId;
                        light.Scale = serializable.Scale;
                        light.IsStatic = serializable.IsStatic;
                        light.Position = serializable.Position;
                        light.Rotation = serializable.Rotation;
                        light.MovementSmoothing = serializable.MovementSmoothing;
                        light.Schematic = schematicData;
                        light.ObjectId = serializable.ObjectId;
                        light.ParentId = serializable.ParentId;

                        schematicData.SpawnedClientObjects.Add(light);

                        foreach (Player player in Player.ReadyList)
                        {
                            light.SpawnForPlayer(player);
                        }

                        SetupCulling(serializable, light, schematicData);
                        return light.NetId;
                    }

                case ObjectType.Clutter:
                    ClutterObject clutter = DeserializeObject<ClutterObject>(serializable);
                    clutter.Position = serializable.Position;
                    clutter.Rotation = serializable.Rotation;
                    clutter.Scale = serializable.Scale;
                    clutter.IsStatic = serializable.IsStatic;

                    clutter.SpawnObject(schematicData, serializable);
                    clutter.Name = serializable.Name;
                    SetupCulling(serializable, clutter);
                    return clutter.NetId;

                case ObjectType.Door:
                    DoorObject door = DeserializeObject<DoorObject>(serializable);
                    door.Position = serializable.Position;
                    door.Rotation = serializable.Rotation;
                    door.Scale = serializable.Scale;
                    door.IsStatic = serializable.IsStatic;

                    door.SpawnObject(schematicData, serializable);
                    door.Name = serializable.Name;
                    SetupCulling(serializable, door);
                    return door.NetId;

                case ObjectType.TextToy:
                    TextToyObject textToy = DeserializeObject<TextToyObject>(serializable);
                    textToy.Position = serializable.Position;
                    textToy.Rotation = serializable.Rotation;
                    textToy.Scale = serializable.Scale;
                    textToy.IsStatic = serializable.IsStatic;

                    textToy.SpawnObject(schematicData, serializable);
                    textToy.Name = serializable.Name;
                    SetupCulling(serializable, textToy);
                    return textToy.NetId;

                case ObjectType.Workstation:
                    WorkstationObject workstation = DeserializeObject<WorkstationObject>(serializable);
                    workstation.Position = serializable.Position;
                    workstation.Rotation = serializable.Rotation;
                    workstation.Scale = serializable.Scale;
                    workstation.IsStatic = serializable.IsStatic;

                    workstation.SpawnObject(schematicData, serializable);
                    workstation.Name = serializable.Name;
                    SetupCulling(serializable, workstation);
                    return workstation.NetId;

                case ObjectType.Camera:
                    CameraObject camera = DeserializeObject<CameraObject>(serializable);
                    camera.Position = serializable.Position;
                    camera.Rotation = serializable.Rotation;
                    camera.Scale = serializable.Scale;
                    camera.IsStatic = serializable.IsStatic;

                    camera.SpawnObject(schematicData, serializable);
                    camera.Name = serializable.Name;
                    SetupCulling(serializable, camera);
                    return camera.NetId;

                case ObjectType.Interactable:
                    InteractionObject interaction = DeserializeObject<InteractionObject>(serializable);
                    interaction.Position = serializable.Position;
                    interaction.Rotation = serializable.Rotation;
                    interaction.Scale = serializable.Scale;
                    interaction.IsStatic = serializable.IsStatic;

                    interaction.SpawnObject(schematicData, serializable);
                    interaction.Name = serializable.Name;
                    SetupCulling(serializable, interaction);
                    return interaction.NetId;

                case ObjectType.Waypoint:
                    WaypointObject waypoint = DeserializeObject<WaypointObject>(serializable);
                    waypoint.Position = serializable.Position;
                    waypoint.Rotation = serializable.Rotation;
                    waypoint.Scale = serializable.Scale;
                    waypoint.IsStatic = serializable.IsStatic;

                    waypoint.SpawnObject(schematicData, serializable);
                    waypoint.Name = serializable.Name;
                    SetupCulling(serializable, waypoint);
                    return waypoint.NetId;

                case ObjectType.Locker:
                    LockerObject locker = DeserializeObject<LockerObject>(serializable);
                    locker.Position = serializable.Position;
                    locker.Rotation = serializable.Rotation;
                    locker.Scale = serializable.Scale;
                    locker.IsStatic = serializable.IsStatic;

                    locker.SpawnObject(schematicData, serializable);
                    locker.Name = serializable.Name;
                    SetupCulling(serializable, locker);
                    return locker.NetId;

                case ObjectType.Pickup:
                    PickupObject pickup = DeserializeObject<PickupObject>(serializable);
                    pickup.Position = serializable.Position;
                    pickup.Rotation = serializable.Rotation;
                    pickup.Scale = serializable.Scale;
                    pickup.IsStatic = serializable.IsStatic;

                    pickup.SpawnObject(schematicData, serializable);
                    pickup.Name = serializable.Name;
                    return pickup.NetId;

                case ObjectType.Target:
                    TargetDummyObject target = DeserializeObject<TargetDummyObject>(serializable);
                    target.Position = serializable.Position;
                    target.Rotation = serializable.Rotation;
                    target.Scale = serializable.Scale;
                    target.IsStatic = serializable.IsStatic;

                    target.SpawnObject(schematicData, serializable);
                    target.Name = serializable.Name;
                    SetupCulling(serializable, target);
                    return target.NetId;

                case ObjectType.Teleporter:
                    TeleporterObject teleporter = DeserializeObject<TeleporterObject>(serializable);
                    teleporter.Position = serializable.Position;
                    teleporter.Rotation = serializable.Rotation;
                    teleporter.Scale = serializable.Scale;
                    teleporter.IsStatic = serializable.IsStatic;

                    teleporter.SpawnObject(schematicData, serializable);
                    teleporter.Name = serializable.Name;
                    return teleporter.NetId;

                case ObjectType.Speaker:
                    SpeakerObject speaker = DeserializeObject<SpeakerObject>(serializable);
                    speaker.Position = serializable.Position;
                    speaker.Rotation = serializable.Rotation;
                    speaker.Scale = serializable.Scale;
                    speaker.IsStatic = serializable.IsStatic;

                    speaker.SpawnObject(schematicData, serializable);
                    speaker.Name = serializable.Name;
                    return speaker.NetId;

                case ObjectType.PlayerSpawnPoint:
                    PlayerSpawnPoint spawn = DeserializeObject<PlayerSpawnPoint>(serializable);
                    spawn.Position = serializable.Position;
                    spawn.Rotation = serializable.Rotation;
                    spawn.Scale = serializable.Scale;
                    spawn.IsStatic = serializable.IsStatic;

                    spawn.SpawnObject(schematicData, serializable);
                    spawn.Name = serializable.Name;
                    return spawn.NetId;

                case ObjectType.RagdollSpawner:
                    RagdollSpawner ragdoll = DeserializeObject<RagdollSpawner>(serializable);
                    ragdoll.Position = serializable.Position;
                    ragdoll.Rotation = serializable.Rotation;
                    ragdoll.Scale = serializable.Scale;
                    ragdoll.IsStatic = serializable.IsStatic;

                    ragdoll.SpawnObject(schematicData, serializable);
                    ragdoll.Name = serializable.Name;
                    return 0;

                default:
                    LogManager.Warn($"Unhandled ObjectType '{serializable.ObjectType}' on object '{serializable.Name}', skipping.");
                    return 0;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="serializable"></param>
        /// <param name="obj"></param>
        public static void SetupCulling(SerializableObject serializable, ServerObject obj)
        {
            if (serializable.CullingSettings.Bounds != Vector3.zero)
            {
                GameObject gameObject = new($"{obj.Name} - Culling Object");
                gameObject.transform.SetParent(obj.Object?.transform);
                gameObject.AddComponent<CullingObject>().Init(obj, serializable.CullingSettings.Bounds);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="serializable"></param>
        /// <param name="obj"></param>
        /// <param name="schematic"></param>
        public static void SetupCulling(SerializableObject serializable, ClientObject obj, SchematicData schematic)
        {
            if (serializable.CullingSettings.Bounds != Vector3.zero)
            {
                GameObject gameObject = new($"{serializable.Name} - Culling Object");
                Transform? targetParent = ColliderHelper.ResolveServerParentTransform(serializable.ParentId, schematic);
                gameObject.transform.SetParent(targetParent, false);
                gameObject.transform.localPosition = serializable.Position;
                gameObject.transform.localRotation = serializable.Rotation;
                gameObject.transform.localScale = new Vector3(Math.Abs(serializable.Scale.x), Math.Abs(serializable.Scale.y), Math.Abs(serializable.Scale.z));
                gameObject.AddComponent<CullingObject>().Init(obj, serializable.CullingSettings.Bounds);
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

            string mapsDir = ThaumFileManager.Dir(["Maps"]);
            ThaumFileManager.TryCreateDirectory(mapsDir);
            File.WriteAllText(Path.Combine(mapsDir, $"{map.FileName}.yml"), Serializer.Serialize(map));
            return map;
        }
    }
}