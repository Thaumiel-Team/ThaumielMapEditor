// -----------------------------------------------------------------------
// <copyright file="SchematicData.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU Lesser General Public License v3.0 (LGPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using LabApi.Features.Wrappers;
using ThaumielMapEditor.API.Blocks;
using DoorDef = ThaumielMapEditor.API.Blocks.Definitions.Door;
using LabPrimitive = LabApi.Features.Wrappers.PrimitiveObjectToy;
using System;
using ThaumielMapEditor.API.Animation;
using ThaumielMapEditor.API.Components.Tools;
using ThaumielMapEditor.API.Helpers;
using ThaumielMapEditor.API.Components;
using ThaumielMapEditor.Events.EventArgs.Handlers;
using UnityEngine;
using ThaumielMapEditor.API.Enums;
using ThaumielMapEditor.API.Serialization;
using Mirror;

namespace ThaumielMapEditor.API.Data
{
    /// <summary>
    /// Transition layer class from PMER.
    /// </summary>
    [Obsolete("Use SchematicData instead of this.")]
    public class SchematicObject : SchematicData;

    [GitBookPage("Data/SchematicData")]
    public class SchematicData
    {
        /// <summary>
        /// Fired when the <see cref="Position"/> is set;
        /// </summary>
        public static event Action<SchematicData>? SchematicPositionUpdated;

        /// <summary>
        /// Fired when the <see cref="Rotation"/> is set;
        /// </summary>
        public static event Action<SchematicData>? SchematicRotationUpdated;

        /// <summary>
        /// Gets or sets the file name.
        /// </summary>
        public string FileName { get; internal set; } = string.Empty;

        /// <summary>
        /// Gets the root object id.
        /// </summary>
        public int RootObjectId { get; internal set; }

        /// <summary>
        /// Gets or sets the id
        /// </summary>
        public uint Id { get; set; }

        /// <summary>
        /// Gets or sets the position of this <see cref="SchematicData"/> instance.
        /// </summary>
        public Vector3 Position
        {
            get;
            set
            {
                field = value;
                Primitive?.Position = value; 
                SchematicPositionUpdated?.Invoke(this);
            }
        }

        /// <summary>
        /// Gets or sets the rotation of this <see cref="SchematicData"/> instance.
        /// </summary>
        public Quaternion Rotation
        {
            get;
            set
            {
                field = value;
                Primitive?.Rotation = value;
                SchematicRotationUpdated?.Invoke(this);
            }
        }

        /// <summary>
        /// Gets or sets the global euler angles of this <see cref="SchematicData"/> instance.
        /// </summary>
        public Vector3 EulerAngles
        {
            get => Rotation.eulerAngles;
            set => Rotation = Quaternion.Euler(value);
        }

        /// <summary>
        /// Gets or sets the scale of this <see cref="SchematicData"/> instance.
        /// </summary>
        public Vector3 Scale
        {
            get => Primitive?.Scale ?? Vector3.one;
            set
            {
                Primitive?.Scale = value;
            }
        }

        /// <summary>
        /// Gets the room this <see cref="SchematicData"/> instance was spawned in.
        /// </summary>
        public Room? Room { get; internal set; }

        public AnimationController AnimationController => AnimationController.Get(this);

        public Dictionary<int, Transform> ServerSideTransforms = [];

        /// <summary>
        /// Gets the base <see cref="LabPrimitive"/> that all client primtives will be parented to.
        /// </summary>
        public LabPrimitive? Primitive { get; internal set; }

        /// <summary>
        /// A list of the spawned <see cref="LODData"/> instances
        /// </summary>
        public List<LODData> LODZones { get; internal set; } = [];

        /// <summary>
        /// A list of all spawned <see cref="ClientObject"/> instances.
        /// </summary>
        public List<ClientObject> SpawnedClientObjects = [];

        /// <summary>
        /// A list of all spawned <see cref="ServerObject"/> instances.
        /// </summary>
        public List<ServerObject> SpawnedServerObjects = [];

        /// <summary>
        /// The <see cref="BlockExecutor"/> of this schematic.
        /// </summary>
        public BlockExecutor? Executor { get; internal set; }

        /// <summary>
        /// Gets the <see cref="UnityEngine.GameObject"/> of the base <see cref="LabPrimitive"/> of this Schematic
        /// </summary>
        public GameObject? GameObject => Primitive?.GameObject;

        /// <summary>
        /// Retrieves all spawned <see cref="ClientObject"/>s of the specified type.
        /// </summary>
        /// <typeparam name="T">The type of <see cref="ClientObject"/> to filter.</typeparam>
        /// <returns>An <see cref="IEnumerable{T}"/> containing all <see cref="ClientObject"/>s that match type <typeparamref name="T"/>.</returns>
        public IEnumerable<T> GetClientObject<T>() where T : ClientObject => SpawnedClientObjects.OfType<T>();

        /// <summary>
        /// Retrieves all spawned <see cref="ServerObject"/>s of the specified type.
        /// </summary>
        /// <typeparam name="T">The type of <see cref="ServerObject"/> to filter.</typeparam>
        /// <returns>An <see cref="IEnumerable{T}"/> containing all <see cref="ServerObject"/>s that match type <typeparamref name="T"/>.</returns>
        public IEnumerable<T> GetServerObject<T>() where T : ServerObject => SpawnedServerObjects.OfType<T>();

        /// <summary>
        /// Gets all of the <see cref="SpawnedServerObjects"/> that have a <see cref="NetworkIdentity"/> as a component.
        /// </summary>
        public IReadOnlyList<NetworkIdentity> ServerNetworkIdentities => SpawnedServerObjects.Where(o => o.Object != null).Select(o => o.Object!.GetComponent<NetworkIdentity>()).ToList();

        /// <summary>
        /// Gets the total number of spawned objects in this schematic.
        /// </summary>
        public int ObjectCount =>  SpawnedServerObjects.Count + SpawnedClientObjects.Count;

        /// <summary>
        /// Gets the number of spawned objects of type <typeparamref name="T"/> in this schematic.
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
        /// Gets every spawned object in this schematic.
        /// </summary>
        /// <returns>An <see cref="IEnumerable{T}"/> containing the spawned objects.</returns>
        public IEnumerable<ObjectBase> GetObjects()
        {
            foreach (ServerObject server in SpawnedServerObjects)
                yield return server;

            foreach (ClientObject client in SpawnedClientObjects)
                yield return client;
        }

        /// <summary>
        /// Gets every spawned object of type <typeparamref name="T"/> in this schematic.
        /// </summary>
        /// <typeparam name="T">The object type to return.</typeparam>
        /// <returns>An <see cref="IEnumerable{T}"/> containing the matching objects.</returns>
        public IEnumerable<T> GetObjects<T>() where T : ObjectBase
        {
            foreach (ObjectBase obj in GetObjects())
            {
                if (obj is T typed)
                    yield return typed;
            }
        }

        /// <summary>
        /// Gets the first spawned object of type <typeparamref name="T"/> in this schematic.
        /// </summary>
        /// <typeparam name="T">The object type to find.</typeparam>
        /// <returns>The first matching object, or <see langword="null"/> if none was found.</returns>
        public T? GetObject<T>() where T : ObjectBase
        {
            foreach (ObjectBase obj in GetObjects())
            {
                if (obj is T typed)
                    return typed;
            }

            return null;
        }

        /// <summary>
        /// Gets the <see cref="LODZone"/> components attached to the schematic root.
        /// </summary>
        /// <returns>The LOD zones, or an empty array when the root has none.</returns>
        public LODZone[] GetLODZones()
        {
            GameObject? root = Primitive?.GameObject;
            return root != null ? root.GetComponents<LODZone>() : [];
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
        /// Gets the spawned object with the given object id.
        /// </summary>
        /// <param name="objectId">The object id to find.</param>
        /// <returns>The spawned object, or <see langword="null"/> if it was not found.</returns>
        public ObjectBase? GetObject(int objectId)
        {
            foreach (ServerObject server in SpawnedServerObjects)
            {
                if (server.ObjectId == objectId)
                    return server;
            }

            foreach (ClientObject client in SpawnedClientObjects)
            {
                if (client.ObjectId == objectId)
                    return client;
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
            foreach (ServerObject server in SpawnedServerObjects)
            {
                if (server.ObjectId == objectId && server is T typedServer)
                    return typedServer;
            }

            foreach (ClientObject client in SpawnedClientObjects)
            {
                if (client.ObjectId == objectId && client is T typedClient)
                    return typedClient;
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
        /// Determines whether the given object belongs to this schematic.
        /// </summary>
        /// <param name="obj">The object to check.</param>
        /// <returns><see langword="true"/> if the object belongs to this schematic.</returns>
        public bool Contains(ObjectBase obj)
        {
            if (obj == null)
                return false;

            foreach (ServerObject server in SpawnedServerObjects)
            {
                if (ReferenceEquals(server, obj))
                    return true;
            }

            foreach (ClientObject client in SpawnedClientObjects)
            {
                if (ReferenceEquals(client, obj))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Gets every spawned object that is parented directly to the schematic root.
        /// </summary>
        /// <returns>An <see cref="IEnumerable{T}"/> containing the root objects.</returns>
        public IEnumerable<ObjectBase> GetRootObjects()
        {
            foreach (ObjectBase obj in GetObjects())
            {
                if (obj.ParentId == RootObjectId)
                    yield return obj;
            }
        }

        /// <summary>
        /// Gets every spawned object of type <typeparamref name="T"/> that is parented directly to the schematic root.
        /// </summary>
        /// <typeparam name="T">The object type to return.</typeparam>
        /// <returns>An <see cref="IEnumerable{T}"/> containing the root objects.</returns>
        public IEnumerable<T> GetRootObjects<T>() where T : ObjectBase
        {
            foreach (ObjectBase obj in GetObjects())
            {
                if (obj.ParentId == RootObjectId && obj is T typed)
                    yield return typed;
            }
        }

        /// <summary>
        /// Gets every spawned object that is parented to the given object.
        /// </summary>
        /// <param name="parent">The parent object.</param>
        /// <returns>An <see cref="IEnumerable{T}"/> containing the child objects.</returns>
        public IEnumerable<ObjectBase> GetChildren(ObjectBase parent) =>
            parent == null ? [] : GetChildren(parent.ObjectId);

        /// <summary>
        /// Gets every spawned object that is parented to the given object id.
        /// </summary>
        /// <param name="parentId">The parent object id.</param>
        /// <returns>An <see cref="IEnumerable{T}"/> containing the child objects.</returns>
        public IEnumerable<ObjectBase> GetChildren(int parentId)
        {
            foreach (ObjectBase obj in GetObjects())
            {
                if (obj.ParentId == parentId)
                    yield return obj;
            }
        }

        /// <summary>
        /// Gets the parent of the given object.
        /// </summary>
        /// <param name="child">The child object.</param>
        /// <returns>The parent object, or <see langword="null"/> if it is parented to the schematic root.</returns>
        public ObjectBase? GetParent(ObjectBase child) => 
            child == null ? null : GetObject(child.ParentId);

        /// <summary>
        /// Creates a new empty schematic at the given position.
        /// </summary>
        /// <param name="fileName">The file name of the schematic.</param>
        /// <param name="position">The world position at which to place the schematic.</param>
        /// <returns>The created schematic.</returns>
        public static SchematicData Create(string fileName, Vector3 position) =>
            Loader.CreateSchematic(fileName, position);

        /// <summary>
        /// Creates a new empty schematic at the given position and rotation.
        /// </summary>
        /// <param name="fileName">The file name of the schematic.</param>
        /// <param name="position">The world position at which to place the schematic.</param>
        /// <param name="rotation">The rotation to apply to the schematic.</param>
        /// <returns>The created schematic.</returns>
        public static SchematicData Create(string fileName, Vector3 position, Quaternion rotation) =>
            Loader.CreateSchematic(fileName, position, rotation);

        /// <summary>
        /// Creates a new empty schematic at the given position, rotation, and scale.
        /// </summary>
        /// <param name="fileName">The file name of the schematic.</param>
        /// <param name="position">The world position at which to place the schematic.</param>
        /// <param name="rotation">The rotation to apply to the schematic.</param>
        /// <param name="scale">The scale to apply to the schematic.</param>
        /// <returns>The created schematic.</returns>
        public static SchematicData Create(string fileName, Vector3 position, Quaternion rotation, Vector3 scale) =>
            Loader.CreateSchematic(fileName, position, rotation, scale);

        /// <summary>
        /// Creates and spawns a new object of type <typeparamref name="T"/> in this schematic.
        /// </summary>
        /// <typeparam name="T">The runtime object type to create.</typeparam>
        /// <param name="position">Optional local position of the object.</param>
        /// <param name="rotation">Optional rotation of the object.</param>
        /// <param name="scale">Optional scale of the object.</param>
        /// <param name="name">Optional name of the object.</param>
        /// <param name="parentId">Optional object id of the parent.</param>
        /// <returns>The spawned object.</returns>
        /// <exception cref="InvalidOperationException">Thrown when <typeparamref name="T"/> is not a registered object type.</exception>
        public T AddObject<T>(Vector3? position = null, Quaternion? rotation = null, Vector3? scale = null, string? name = null, int? parentId = null) where T : ObjectBase
        {
            ObjectType? objectType = ObjectRegistry.GetObjectType(typeof(T)) ?? throw new InvalidOperationException($"'{typeof(T).Name}' is not a registered runtime object type.");
            ObjectDefinition? definition = ObjectRegistry.Create(objectType.Value) ?? throw new InvalidOperationException($"No definition is registered for '{objectType}'.");

            SerializableObject serializable = new()
            {
                ObjectId = GetNextObjectId(),
                ParentId = parentId ?? RootObjectId,
                Name = name ?? definition.Name,
                ObjectType = objectType.Value,
                Position = position ?? Vector3.zero,
                Rotation = rotation ?? Quaternion.identity,
                Scale = scale ?? Vector3.one,
            };

            definition.Load(serializable);

            ObjectBase data = new();
            data.LoadFrom(serializable, this);

            if (typeof(ServerObject).IsAssignableFrom(typeof(T)))
            {
                if (!definition.SupportsServer)
                    throw new InvalidOperationException($"'{objectType}' does not support server objects.");

                ServerObject? server = definition.ServerObject(data);
                if (server is not T typedServer)
                    throw new InvalidOperationException($"'{objectType}' did not create a '{typeof(T).Name}'.");

                server.SpawnObject(this, serializable);

                if (server.Object == null)
                {
                    LogManager.Warn($"Failed to spawn '{definition.Name}' in '{FileName}'.");
                    return typedServer;
                }

                if (server.ParentToSpawnTransform)
                    server.Object.transform.SetParent(ResolveServerParentTransform(serializable.ParentId), false);

                server.OnSpawned(this);
                return typedServer;
            }

            if (!typeof(ClientObject).IsAssignableFrom(typeof(T)))
                throw new InvalidOperationException($"'{typeof(T).Name}' is not a server or client object.");

            if (!definition.SupportsClient)
                throw new InvalidOperationException($"'{objectType}' does not support client objects.");

            ClientObject? client = definition.ClientObject(data);
            if (client is not T typedClient)
                throw new InvalidOperationException($"'{objectType}' did not create a '{typeof(T).Name}'.");

            client.ParentNetId = ResolveParentNetId(serializable.ParentId);
            client.NetId = NetworkIdentity.GetNextNetworkId();
            client.ObjectId = serializable.ObjectId;
            client.ParentId = serializable.ParentId;

            SpawnedClientObjects.Add(client);
            client.OnAddedToSchematic(this);

            LODZone[] lodZones = GetLODZones();

            foreach (Player player in Player.ReadyList)
            {
                if (client.ShouldSpawnFor(player, lodZones))
                    client.SpawnForPlayer(player);
            }

            return typedClient;
        }

        /// <summary>
        /// Creates and spawns a new named object of type <typeparamref name="T"/> in this schematic.
        /// </summary>
        /// <typeparam name="T">The runtime object type to create.</typeparam>
        /// <param name="name">The name of the object.</param>
        /// <returns>The spawned object.</returns>
        public T AddObject<T>(string name) where T : ObjectBase => 
            AddObject<T>(name: name);

        /// <summary>
        /// Creates and spawns a new object of type <typeparamref name="T"/> parented to the given object.
        /// </summary>
        /// <typeparam name="T">The runtime object type to create.</typeparam>
        /// <param name="parent">The object to parent the new object to.</param>
        /// <returns>The spawned object.</returns>
        public T AddObject<T>(ObjectBase parent) where T : ObjectBase => 
            AddObject<T>(parentId: parent?.ObjectId ?? RootObjectId);

        /// <summary>
        /// Creates and spawns a new object of type <typeparamref name="T"/> parented to the given object id.
        /// </summary>
        /// <typeparam name="T">The runtime object type to create.</typeparam>
        /// <param name="parentId">The object id of the parent.</param>
        /// <returns>The spawned object.</returns>
        public T AddObject<T>(int parentId) where T : ObjectBase => 
            AddObject<T>(parentId: parentId);

        private int GetNextObjectId()
        {
            int id = RootObjectId;

            foreach (ServerObject server in SpawnedServerObjects)
                id = Math.Max(id, server.ObjectId);

            foreach (ClientObject client in SpawnedClientObjects)
                id = Math.Max(id, client.ObjectId);

            return id + 1;
        }

        private Transform? ResolveServerParentTransform(int parentId)
        {
            foreach (ServerObject server in SpawnedServerObjects)
            {
                if (server.ObjectId == parentId && server.Object != null)
                    return server.Object.transform;
            }

            return Primitive?.Transform;
        }

        private uint ResolveParentNetId(int parentId)
        {
            foreach (ServerObject server in SpawnedServerObjects)
            {
                if (server.ObjectId == parentId && server.NetId != 0)
                    return server.NetId;
            }

            foreach (ClientObject client in SpawnedClientObjects)
            {
                if (client.ObjectId == parentId && client.NetId != 0)
                    return client.NetId;
            }

            return Primitive?.Base != null ? Primitive.Base.netId : 0;
        }

        /// <summary>
        /// Syncs the <see cref="ClientObject"/> of this <see cref="SchematicData"/> with the specified <see cref="Player"/>.
        /// </summary>
        /// <param name="player">The <see cref="Player"/> to sync with.</param>
        public void SyncWithPlayer(Player player)
        {
            if (player == null || player.IsDestroyed || player.IsDummy || player.IsHost)
                return;

            if (player.Connection == null || !player.Connection.isReady)
                return;

            foreach (ClientObject objects in SpawnedClientObjects.ToArray())
            {
                if (objects == null)
                    continue;
                    
                if (objects.IsSpawnedForPlayer(player))
                    continue;

                objects.SpawnForPlayer(player);
            }

            foreach (ServerObject server in SpawnedServerObjects.ToArray())
            {
                if (server?.Object == null)
                    continue;

                server.ShowForPlayer(player);
            }
        }

        /// <summary>
        /// Syncs this schematic with every ready player.
        /// </summary>
        public void SyncWithAllPlayers()
        {
            foreach (Player player in Player.ReadyList)
                SyncWithPlayer(player);
        }

        /// <summary>
        /// Destroys a spawned Object of this <see cref="SchematicData"/>.
        /// </summary>
        /// <param name="obj">The object to destroy.</param>
        /// <returns><see langword="true"/> if the object belongs to this schematic and was destroyed.</returns>
        public bool DestroyObject(ObjectBase obj)
        {
            if (!Contains(obj))
                return false;

            DestroyObjectTree(obj.ObjectId);
            return true;
        }

        /// <summary>
        /// Destroys the spawned object with the given object id.
        /// </summary>
        /// <param name="objectId">The object id to destroy.</param>
        /// <returns><see langword="true"/> if the object belonged to this schematic and was destroyed.</returns>
        public bool DestroyObject(int objectId)
        {
            if (GetObject(objectId) == null)
                return false;

            DestroyObjectTree(objectId);
            return true;
        }

        /// <summary>
        /// Destroys a single spawned <see cref="ServerObject"/> of this <see cref="SchematicData"/>.
        /// </summary>
        /// <param name="serverObject">The spawned object to destroy.</param>
        /// <returns><see langword="true"/> if the object belongs to this schematic and was destroyed.</returns>
        public bool DestroyServerObject(ServerObject serverObject)
        {
            if (serverObject == null || !SpawnedServerObjects.Contains(serverObject))
                return false;

            DestroyObjectTree(serverObject.ObjectId);
            return true;
        }

        /// <summary>
        /// Destroys a single spawned <see cref="ClientObject"/> of this <see cref="SchematicData"/>.
        /// </summary>
        /// <param name="clientObject">The spawned object to destroy.</param>
        /// <returns><see langword="true"/> if the object belongs to this schematic and was destroyed.</returns>
        public bool DestroyClientObject(ClientObject clientObject)
        {
            if (clientObject == null || !SpawnedClientObjects.Contains(clientObject))
                return false;

            DestroyObjectTree(clientObject.ObjectId);
            return true;
        }

        /// <summary>
        /// Destroys every spawned object in this schematic without destroying the schematic itself.
        /// </summary>
        public void DestroyAllObjects()
        {
            for (int i = SpawnedClientObjects.Count - 1; i >= 0; i--)
                DestroyClientObjectInternal(SpawnedClientObjects[i]);

            for (int i = SpawnedServerObjects.Count - 1; i >= 0; i--)
                DestroyServerObjectInternal(SpawnedServerObjects[i]);

            foreach (Transform transform in ServerSideTransforms.Values)
            {
                if (transform != null)
                    UnityEngine.Object.Destroy(transform.gameObject);
            }

            ServerSideTransforms.Clear();
        }

        private void DestroyObjectTree(int objectId)
        {
            HashSet<int> ids = [objectId];

            bool addedChild;

            do
            {
                addedChild = false;

                foreach (ClientObject client in SpawnedClientObjects.ToArray())
                {
                    if (ids.Contains(client.ParentId))
                        addedChild |= ids.Add(client.ObjectId);
                }

                foreach (ServerObject server in SpawnedServerObjects.ToArray())
                {
                    if (ids.Contains(server.ParentId))
                        addedChild |= ids.Add(server.ObjectId);
                }
            }
            while (addedChild);

            foreach (ClientObject client in SpawnedClientObjects.ToArray())
            {
                if (ids.Contains(client.ObjectId))
                    DestroyClientObjectInternal(client);
            }

            foreach (ServerObject server in SpawnedServerObjects.ToArray())
            {
                if (ids.Contains(server.ObjectId))
                    DestroyServerObjectInternal(server);
            }

            foreach (int id in ids)
            {
                if (!ServerSideTransforms.TryGetValue(id, out Transform? transform))
                    continue;

                ServerSideTransforms.Remove(id);

                if (transform != null)
                    UnityEngine.Object.Destroy(transform.gameObject);
            }
        }

        private void DestroyClientObjectInternal(ClientObject clientObject)
        {
            ColliderHelper.DestroyClientObjectColliders(clientObject, this);
            clientObject.DestroyForAllPlayers();
            SpawnedClientObjects.Remove(clientObject);
        }

        private void DestroyServerObjectInternal(ServerObject serverObject)
        {
            try
            {
                if (serverObject is DoorDef.Server && serverObject.Object != null && serverObject.Object.TryGetComponent<DoorLink>(out var link))
                    link.Unregister();

                if (serverObject.Object != null && serverObject.Object.TryGetComponent<BlockyRuntime>(out var blocky) && blocky.Blocky != null && !string.IsNullOrEmpty(blocky.Blocky.Code))
                    Executor?.Execute(ArgumentsParser.Load(blocky.Blocky), null!, EventType.OnDestroyed);
            }
            catch (Exception ex)
            {
                LogManager.Error($"Schematic destroy cleanup failed for '{serverObject.Name}': {ex.Message}");
            }

            serverObject.DestroyObject(this);
        }

        /// <summary>
        /// Destroys this <see cref="SchematicData"/> instance.
        /// </summary>
        public void Destroy()
        {
            SchematicHandler.OnSchematicDestroyed(new(this));

            foreach (KeyValuePair<LODZone, SchematicData> kvp in Loader.SchematicLODZones.Where(s => s.Value == this).ToArray())
            {
                if (kvp.Key == null)
                    continue;

                Loader.SchematicLODZones.Remove(kvp.Key);
                try
                {
                    if (kvp.Key != null && kvp.Key.gameObject != null)
                        UnityEngine.Object.Destroy(kvp.Key.gameObject);
                }
                catch { }
            }

            DestroyAllObjects();

            if (Primitive != null && !Primitive.IsDestroyed && Primitive.Base != null)
                NetworkServer.Destroy(Primitive.Base.gameObject);

            Executor = null;
            AnimationController.Remove(this);
            ColliderHelper.SchematicColliders.Remove(this);
        }
    }
}