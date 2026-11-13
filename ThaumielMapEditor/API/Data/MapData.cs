// -----------------------------------------------------------------------
// <copyright file="MapData.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU Lesser General Public License v3.0 (LGPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using LabApi.Features.Wrappers;
using ThaumielMapEditor.API.Blocks;
using ThaumielMapEditor.API.Enums;
using ThaumielMapEditor.API.Helpers;

namespace ThaumielMapEditor.API.Data
{
    [GitBookPage("Data/MapData")]
    public class MapData
    {
        /// <summary>
        /// Gets or sets the file name of this <see cref="MapData"/> instance.
        /// </summary>
        public string FileName = string.Empty;

        /// <summary>
        /// Gets or sets the <see cref="Guid"/> id of this <see cref="MapData"/> instance.
        /// </summary>
        public Guid Id;

        /// <summary>
        /// Gets or sets the position of this <see cref="MapData"/> instance.
        /// </summary>
        public Vector3 Position { get; set; }

        /// <summary>
        /// Gets or sets the room of this <see cref="MapData"/> instance.
        /// </summary>
        public Room? Room { get; set; }

        /// <summary>
        /// Gets or sets the schematics of this <see cref="MapData"/> instance.
        /// </summary>
        public List<MapSchematicData> Schematics { get; set; } = [];

        /// <summary>
        /// Gets the number of schematic entries in this map.
        /// </summary>
        public int SchematicCount => Schematics.Count;

        /// <summary>
        /// Gets the total number of spawned objects in this map.
        /// </summary>
        public int ObjectCount
        {
            get
            {
                int count = 0;

                foreach (SchematicData schematic in GetSchematics())
                    count += schematic.ObjectCount;

                return count;
            }
        }

        /// <summary>
        /// Gets the total number of spawned <see cref="ServerObject"/> instances in this map.
        /// </summary>
        public int ServerObjectCount
        {
            get
            {
                int count = 0;

                foreach (SchematicData schematic in GetSchematics())
                    count += schematic.ServerObjectCount;

                return count;
            }
        }

        /// <summary>
        /// Gets the total number of spawned <see cref="ClientObject"/> instances in this map.
        /// </summary>
        public int ClientObjectCount
        {
            get
            {
                int count = 0;

                foreach (SchematicData schematic in GetSchematics())
                    count += schematic.ClientObjectCount;

                return count;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this map is registered as spawned.
        /// </summary>
        public bool IsSpawned => Loader.MapsById.TryGetValue(Id, out MapData? map) && ReferenceEquals(map, this);

        /// <summary>
        /// Gets every schematic that is currently spawned for this map.
        /// </summary>
        /// <returns>An <see cref="IEnumerable{T}"/> containing the spawned schematics.</returns>
        public IEnumerable<SchematicData> GetSchematics()
        {
            foreach (MapSchematicData schematic in Schematics)
            {
                if (Loader.SchematicsById.TryGetValue(schematic.SchematicId, out SchematicData? data))
                    yield return data;
            }
        }

        /// <summary>
        /// Gets a spawned schematic by its id.
        /// </summary>
        /// <param name="schematicId">The schematic id to find.</param>
        /// <returns>The schematic, or <see langword="null"/> if it was not found.</returns>
        public SchematicData? GetSchematic(uint schematicId) => Loader.GetSchematicById(schematicId);

        /// <summary>
        /// Tries to get a spawned schematic by its id.
        /// </summary>
        /// <param name="schematicId">The schematic id to find.</param>
        /// <param name="schematic">The schematic, or <see langword="null"/>.</param>
        /// <returns><see langword="true"/> if the schematic was found.</returns>
        public bool TryGetSchematic(uint schematicId, out SchematicData? schematic)
        {
            schematic = GetSchematic(schematicId);
            return schematic != null;
        }

        /// <summary>
        /// Gets the first spawned schematic with the given file name.
        /// </summary>
        /// <param name="name">The schematic file name to find.</param>
        /// <returns>The schematic, or <see langword="null"/> if none was found.</returns>
        public SchematicData? GetSchematic(string name)
        {
            foreach (SchematicData schematic in GetSchematics())
            {
                if (string.Equals(schematic.FileName, name, StringComparison.OrdinalIgnoreCase))
                    return schematic;
            }

            return null;
        }

        /// <summary>
        /// Gets the spawned schematic for the given entry.
        /// </summary>
        /// <param name="schematic">The schematic entry to resolve.</param>
        /// <returns>The schematic, or <see langword="null"/> if it was not found.</returns>
        public SchematicData? GetSchematic(MapSchematicData schematic) =>
            schematic == null ? null : GetSchematic(schematic.SchematicId);

        /// <summary>
        /// Determines whether the given schematic belongs to this map.
        /// </summary>
        /// <param name="schematic">The schematic to check.</param>
        /// <returns><see langword="true"/> if the schematic belongs to this map.</returns>
        public bool Contains(SchematicData schematic)
        {
            if (schematic == null)
                return false;

            foreach (MapSchematicData entry in Schematics)
            {
                if (entry.SchematicId == schematic.Id)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Gets every spawned object in this map.
        /// </summary>
        /// <returns>An <see cref="IEnumerable{T}"/> containing the spawned objects.</returns>
        public IEnumerable<ObjectBase> GetObjects()
        {
            foreach (SchematicData schematic in GetSchematics())
            {
                foreach (ObjectBase obj in schematic.GetObjects())
                    yield return obj;
            }
        }

        /// <summary>
        /// Gets every spawned object of type <typeparamref name="T"/> in this map.
        /// </summary>
        /// <typeparam name="T">The object type to return.</typeparam>
        /// <returns>An <see cref="IEnumerable{T}"/> containing the matching objects.</returns>
        public IEnumerable<T> GetObjects<T>() where T : ObjectBase
        {
            foreach (SchematicData schematic in GetSchematics())
            {
                foreach (T obj in schematic.GetObjects<T>())
                    yield return obj;
            }
        }

        /// <summary>
        /// Gets every spawned <see cref="ServerObject"/> in this map.
        /// </summary>
        /// <returns>An <see cref="IEnumerable{T}"/> containing the spawned server objects.</returns>
        public IEnumerable<ServerObject> GetServerObjects()
        {
            foreach (SchematicData schematic in GetSchematics())
            {
                foreach (ServerObject server in schematic.GetServerObjects())
                    yield return server;
            }
        }

        /// <summary>
        /// Gets every spawned <see cref="ClientObject"/> in this map.
        /// </summary>
        /// <returns>An <see cref="IEnumerable{T}"/> containing the spawned client objects.</returns>
        public IEnumerable<ClientObject> GetClientObjects()
        {
            foreach (SchematicData schematic in GetSchematics())
            {
                foreach (ClientObject client in schematic.GetClientObjects())
                    yield return client;
            }
        }

        /// <summary>
        /// Gets the first spawned object of type <typeparamref name="T"/> in this map.
        /// </summary>
        /// <typeparam name="T">The object type to find.</typeparam>
        /// <returns>The first matching object, or <see langword="null"/> if none was found.</returns>
        public T? GetObject<T>() where T : ObjectBase
        {
            foreach (SchematicData schematic in GetSchematics())
            {
                if (schematic.GetObject<T>() is T obj)
                    return obj;
            }

            return null;
        }

        /// <summary>
        /// Gets the spawned object with the given object id.
        /// </summary>
        /// <param name="objectId">The object id to find.</param>
        /// <returns>The spawned object, or <see langword="null"/> if it was not found.</returns>
        public ObjectBase? GetObject(int objectId)
        {
            foreach (SchematicData schematic in GetSchematics())
            {
                if (schematic.GetObject(objectId) is ObjectBase obj)
                    return obj;
            }

            return null;
        }

        /// <summary>
        /// Gets the spawned object of type <typeparamref name="T"/> with the given object id.
        /// </summary>
        /// <typeparam name="T">The object type to find.</typeparam>
        /// <param name="objectId">The object id to find.</param>
        /// <returns>The spawned object, or <see langword="null"/> if it was not found.</returns>
        public T? GetObject<T>(int objectId) where T : ObjectBase
        {
            foreach (SchematicData schematic in GetSchematics())
            {
                if (schematic.GetObject<T>(objectId) is T obj)
                    return obj;
            }

            return null;
        }

        /// <summary>
        /// Tries to get the spawned object of type <typeparamref name="T"/> with the given object id.
        /// </summary>
        /// <typeparam name="T">The object type to find.</typeparam>
        /// <param name="objectId">The object id to find.</param>
        /// <param name="obj">The found object, or <see langword="null"/>.</param>
        /// <returns><see langword="true"/> if the object was found.</returns>
        public bool TryGetObject<T>(int objectId, out T? obj) where T : ObjectBase
        {
            obj = GetObject<T>(objectId);
            return obj != null;
        }

        /// <summary>
        /// Tries to get the spawned object with the given object id.
        /// </summary>
        /// <param name="objectId">The object id to find.</param>
        /// <param name="obj">The found object, or <see langword="null"/>.</param>
        /// <returns><see langword="true"/> if the object was found.</returns>
        public bool TryGetObject(int objectId, out ObjectBase? obj)
        {
            obj = GetObject(objectId);
            return obj != null;
        }

        /// <summary>
        /// Gets the spawned object with the given netId.
        /// </summary>
        /// <param name="netId">The netId to find.</param>
        /// <returns>The spawned object, or <see langword="null"/> if it was not found.</returns>
        public ObjectBase? GetObjectByNetId(uint netId)
        {
            foreach (SchematicData schematic in GetSchematics())
            {
                if (schematic.GetObjectByNetId(netId) is ObjectBase obj)
                    return obj;
            }

            return null;
        }

        /// <summary>
        /// Gets the spawned object of type <typeparamref name="T"/> with the given netId.
        /// </summary>
        /// <typeparam name="T">The object type to find.</typeparam>
        /// <param name="netId">The netId to find.</param>
        /// <returns>The spawned object, or <see langword="null"/> if it was not found.</returns>
        public T? GetObjectByNetId<T>(uint netId) where T : ObjectBase
        {
            foreach (SchematicData schematic in GetSchematics())
            {
                if (schematic.GetObjectByNetId<T>(netId) is T obj)
                    return obj;
            }

            return null;
        }

        /// <summary>
        /// Gets the first spawned object with the given name.
        /// </summary>
        /// <param name="name">The object name to find.</param>
        /// <returns>The first matching object, or <see langword="null"/> if none was found.</returns>
        public ObjectBase? GetObjectByName(string name)
        {
            foreach (ObjectBase obj in GetObjects())
            {
                if (string.Equals(obj.Name, name, StringComparison.OrdinalIgnoreCase))
                    return obj;
            }

            return null;
        }

        /// <summary>
        /// Gets the first spawned object of type <typeparamref name="T"/> with the given name.
        /// </summary>
        /// <typeparam name="T">The object type to find.</typeparam>
        /// <param name="name">The object name to find.</param>
        /// <returns>The first matching object, or <see langword="null"/> if none was found.</returns>
        public T? GetObjectByName<T>(string name) where T : ObjectBase
        {
            foreach (ObjectBase obj in GetObjects())
            {
                if (obj is T typed && string.Equals(typed.Name, name, StringComparison.OrdinalIgnoreCase))
                    return typed;
            }

            return null;
        }

        /// <summary>
        /// Gets every spawned object with the given name.
        /// </summary>
        /// <param name="name">The object name to find.</param>
        /// <returns>An <see cref="IEnumerable{T}"/> containing the matching objects.</returns>
        public IEnumerable<ObjectBase> GetObjectsByName(string name)
        {
            foreach (ObjectBase obj in GetObjects())
            {
                if (string.Equals(obj.Name, name, StringComparison.OrdinalIgnoreCase))
                    yield return obj;
            }
        }

        /// <summary>
        /// Gets every spawned object of type <typeparamref name="T"/> with the given name.
        /// </summary>
        /// <typeparam name="T">The object type to find.</typeparam>
        /// <param name="name">The object name to find.</param>
        /// <returns>An <see cref="IEnumerable{T}"/> containing the matching objects.</returns>
        public IEnumerable<T> GetObjectsByName<T>(string name) where T : ObjectBase
        {
            foreach (ObjectBase obj in GetObjects())
            {
                if (obj is T typed && string.Equals(typed.Name, name, StringComparison.OrdinalIgnoreCase))
                    yield return typed;
            }
        }

        /// <summary>
        /// Gets the number of spawned objects of type <typeparamref name="T"/> in this map.
        /// </summary>
        /// <typeparam name="T">The object type to count.</typeparam>
        /// <returns>The number of matching objects.</returns>
        public int GetObjectCount<T>() where T : ObjectBase
        {
            int count = 0;

            foreach (ObjectBase obj in GetObjects())
            {
                if (obj is T)
                    count++;
            }

            return count;
        }

        /// <summary>
        /// Gets the number of spawned objects of the given <see cref="ObjectType"/> in this map.
        /// </summary>
        /// <param name="objectType">The object type to count.</param>
        /// <returns>The number of matching objects.</returns>
        public int GetObjectCount(ObjectType objectType)
        {
            int count = 0;

            foreach (ObjectBase obj in GetObjects())
            {
                if (obj.ObjectType == objectType)
                    count++;
            }

            return count;
        }

        /// <summary>
        /// Syncs every schematic of this map with the specified player.
        /// </summary>
        /// <param name="player">The player to sync with.</param>
        public void SyncWithPlayer(Player player)
        {
            foreach (SchematicData schematic in GetSchematics())
                schematic.SyncWithPlayer(player);
        }

        /// <summary>
        /// Syncs every schematic of this map with every ready player.
        /// </summary>
        public void SyncWithAllPlayers()
        {
            foreach (SchematicData schematic in GetSchematics())
                schematic.SyncWithAllPlayers();
        }

        /// <summary>
        /// Destroys this map and every schematic that belongs to it.
        /// </summary>
        public void Destroy() => Loader.DestroyMap(this);
    }
}