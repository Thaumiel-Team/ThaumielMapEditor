// -----------------------------------------------------------------------
// <copyright file="SchematicData.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using LabApi.Features.Wrappers;
using ThaumielMapEditor.API.Blocks;
using ThaumielMapEditor.API.Blocks.ServerObjects;
using ThaumielMapEditor.API.Blocks.ClientSide;
using LabPrimitive = LabApi.Features.Wrappers.PrimitiveObjectToy;
using System;
using ThaumielMapEditor.API.Animation;
using ThaumielMapEditor.API.Components.Tools;
using ThaumielMapEditor.API.Helpers;
using ThaumielMapEditor.API.Components;
using ThaumielMapEditor.Events.EventArgs.Handlers;
using UnityEngine;
using ThaumielMapEditor.API.Enums;
using Mirror;

namespace ThaumielMapEditor.API.Data
{
    /// <summary>
    /// Transition layer class from PMER.
    /// </summary>
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
        /// Gets the <see cref="GameObject"/> of the base <see cref="PrimitiveObject"/> of this Schematic
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
                if (serverObject is DoorObject && serverObject.Object != null && serverObject.Object.TryGetComponent<DoorLink>(out var link))
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

            foreach (ClientObject clientobj in SpawnedClientObjects.ToArray())
            {
                clientobj.DestroyForAllPlayers();
                SpawnedClientObjects.Remove(clientobj);
            }

            foreach (ServerObject serverobj in SpawnedServerObjects.ToArray())
            {
                DestroyServerObjectInternal(serverobj);
            }

            foreach (Transform transform in ServerSideTransforms.Values)
            {
                if (transform != null)
                    UnityEngine.Object.Destroy(transform.gameObject);
            }

            ServerSideTransforms.Clear();

            if (Primitive != null && !Primitive.IsDestroyed && Primitive.Base != null)
                NetworkServer.Destroy(Primitive.Base.gameObject);

            Executor = null;
            AnimationController.Remove(this);
            ColliderHelper.SchematicColliders.Remove(this);
        }
    }
}