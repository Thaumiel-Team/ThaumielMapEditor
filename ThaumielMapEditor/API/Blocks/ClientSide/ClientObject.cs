// -----------------------------------------------------------------------
// <copyright file="ClientObject.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using AdminToys;
using LabApi.Features.Wrappers;
using Mirror;
using ThaumielMapEditor.API.Enums;
using ThaumielMapEditor.API.Extensions;
using ThaumielMapEditor.API.Helpers;
using ThaumielMapEditor.API.Serialization;
using ThaumielMapEditor.Events.EventArgs.Handlers;
using UnityEngine;

namespace ThaumielMapEditor.API.Blocks.ClientSide
{
    public class ClientObject
    {
        internal SyncFlags SyncFlags { get; private set; } = SyncFlags.None;
        
        /// <summary>
        /// True if this object has pending changes that need syncing.
        /// </summary>
        public bool IsDirty => SyncFlags != SyncFlags.None;
        
        /// <summary>
        /// Marks specific properties as needing to be synced and registers for batch sync.
        /// </summary>
        protected void MarkSyncNeeded(SyncFlags flags)
        {
            SyncFlags |= flags;
            SyncManager.RegisterForSync(this);
        }

        internal void ClearDirtyFlags() => SyncFlags = SyncFlags.None;

        public static event Action<Vector3, ClientObject>? PositionUpdated;
        public static event Action<Vector3, ClientObject>? ScaleUpdated;
        public static event Action<Quaternion, ClientObject>? RotationUpdated;

        /// <summary>
        /// All the <see cref="Player"/>s that this <see cref="ClientObject"/> instance has been spawned for.
        /// </summary>
        public HashSet<Player> SpawnedPlayers { get; internal set; } = [];

        /// <summary>
        /// Gets a value indicating whether this <see cref="ClientObject"/> has been spawned for any players.
        /// </summary>
        public bool Spawned => !SpawnedPlayers.IsEmpty();

        /// <summary>
        /// Gets the object id from the <see cref="SerializableObject"/> this <see cref="ClientObject"/> instance was generated from.
        /// </summary>
        public int ObjectId { get; internal set; }

        /// <summary>
        /// Gets the parent id from the <see cref="SerializableObject"/> this <see cref="ClientObject"/> instance was generated from.
        /// </summary>
        public int ParentId
        {
            get;
            set
            {
                field = value;

                if (Spawned)
                    SetParent(value);
            }
        }

        /// <summary>
        /// Gets or sets the position of the <see cref="ClientObject"/> instance.
        /// </summary>
        /// <remarks>
        /// This will be automatically synced to players.
        /// </remarks>
        public Vector3 Position
        {
            get;
            set
            {
                field = value;
                MarkSyncNeeded(SyncFlags.Position);
                PositionUpdated?.Invoke(value, this);
            }
        }

        /// <summary>
        /// Gets or sets the scale of the <see cref="ClientObject"/> instance.
        /// </summary>
        /// <remarks>
        /// This will be automatically synced to players.
        /// </remarks>
        public Vector3 Scale
        {
            get;
            set
            {
                field = value;
                MarkSyncNeeded(SyncFlags.Scale);
                ScaleUpdated?.Invoke(value, this);
            }
        }

        /// <summary>
        /// Gets or sets the rotation of the <see cref="ClientObject"/> instance.
        /// </summary>
        /// <remarks>
        /// This will be automatically synced to players.
        /// </remarks>
        public Quaternion Rotation
        {
            get;
            set
            {
                field = value;
                MarkSyncNeeded(SyncFlags.Rotation);
                RotationUpdated?.Invoke(value, this);
            }
        }

        /// <summary>
        /// Gets or sets whether or not this <see cref="ClientObject"/> is static.
        /// </summary>
        public bool IsStatic
        {
            get;
            set
            {
                if (field == value)
                    return;

                field = value;
                MarkSyncNeeded(SyncFlags.IsStatic);
            }
        }

        /// <summary>
        /// Gets or sets the sync interval of the <see cref="ClientObject"/>.
        /// </summary>
        public byte MovementSmoothing
        {
            get;
            set
            {
                field = value;
                MarkSyncNeeded(SyncFlags.MovementSmoothing);
            }
        }

        /// <summary>
        /// Gets or sets the world position of the <see cref="ClientObject"/> instance.
        /// </summary>
        public Vector3 WorldPosition { get; set; }

        /// <summary>
        /// Gets or sets the world rotation of the <see cref="ClientObject"/> instance.
        /// </summary>
        public Quaternion WorldRotation { get; set; }

        /// <summary>
        /// Gets or sets the colliders of the <see cref="ClientObject"/> instance.
        /// </summary>
        public Collider[] ServerColliders { get; set; } = [];

        /// <summary>
        /// Gets or sets the <see cref="GameObject"/> the <see cref="ClientObject"/> is parented to on the client.
        /// </summary>
        public GameObject? Parent { get; internal set; }

        /// <summary>
        /// Gets or sets the netId of the <see cref="GameObject"/> this <see cref="ClientObject"/> is parented to.
        /// </summary>
        public uint ParentNetId { get; internal set; }

        /// <summary>
        /// Gets or sets the netid of the <see cref="ClientObject"/> instance.
        /// </summary>
        public uint NetId { get; internal set; }

        /// <summary>
        /// Gets or sets the object type of the <see cref="ClientObject"/> instance.
        /// </summary>
        public virtual ObjectType ObjectType { get; internal set; }

        /// <summary>
        /// Gets or sets the asset id of the <see cref="ClientObject"/> instance.
        /// </summary>
        public uint AssetId { get; internal set; }

        /// <summary>
        /// Spawns the <see cref="ClientObject"/> instance for the specified player.
        /// </summary>
        /// <param name="player">The player to spawn the <see cref="ClientObject"/> instance to.</param>
        public virtual void SpawnForPlayer(Player player) { }

        /// <summary>
        /// Destroys this <see cref="ClientObject"/> instance for the specified <see cref="Player"/>
        /// </summary>
        /// <param name="player">The <see cref="Player"/> to destroy this object on.</param>
        public void DestroyForPlayer(Player player)
        {
            player.Connection.Send(new ObjectDestroyMessage { netId = NetId });
            SpawnedPlayers.Remove(player);
        }

        /// <summary>
        /// Destroys this <see cref="ClientObject"/> instance for all <see cref="Player"/>s
        /// </summary>
        public void DestroyForAllPlayers()
        {
            foreach (Player player in Player.ReadyList)
            {
                if (player.IsHost)
                    continue;

                DestroyForPlayer(player);
            }
        }

        /// <summary>
        /// Sets the parent of the 
        /// </summary>
        /// <param name="player">The <see cref="Player"/> that will recive the parent message=</param>
        /// <param name="parentId">The <see cref="NetworkBehaviour.netId"/> of the parent</param>
        public void SetParent(Player player, int parentId)
        {
            player.SendFakeRPC(NetId, typeof(AdminToyBase), nameof(AdminToyBase.RpcChangeParent), 0, parentId);

            GameObject? go = NetworkServer.spawned.TryGetValue(ParentNetId, out NetworkIdentity identity) ? identity.gameObject : null;
            if (go != null)
            {
                Parent = go;
            }
            else
                LogManager.Warn($"Failed to find GameObject with NetId {ParentNetId}!");
        }

        /// <summary>
        /// Sets the parent of the <see cref="ClientObject"/>
        /// </summary>
        /// <param name="parentId">The <see cref="NetworkBehaviour.netId"/> of the parent</param>
        public void SetParent(int parentId)
        {
            foreach (Player player in SpawnedPlayers)
            {
                player.SendFakeRPC(NetId, typeof(AdminToyBase), nameof(AdminToyBase.RpcChangeParent), 0, parentId);

                GameObject? go = NetworkServer.spawned.TryGetValue(ParentNetId, out NetworkIdentity identity) ? identity.gameObject : null;
                if (go != null)
                {
                    Parent = go;
                }
                else
                    LogManager.Warn($"Failed to find GameObject with NetId {ParentNetId}!");
            }
        }

        /// <summary>
        /// Gets a value from a <see cref="SerializableObject"/> by key, converting it to the specified type.
        /// </summary>
        /// <typeparam name="T">The type to convert the value to.</typeparam>
        /// <param name="serializable">The serializable object to retrieve the value from.</param>
        /// <param name="key">The key of the value to retrieve.</param>
        /// <returns>The value associated with the key, converted to <typeparamref name="T"/>.</returns>
        public T GetValue<T>(SerializableObject serializable, string key) =>
            serializable.Values.GetConvertValue<T>(key);

        /// <summary>
        /// Checks if this object is spawned for the specified <see cref="Player"/>.
        /// </summary>
        /// <param name="player">The <see cref="Player"/> to check</param>
        /// <returns><see langword="true"/> if <see cref="SpawnedPlayers"/> contains the player else returns false.</returns>
        public bool IsSpawnedForPlayer(Player player)
            => SpawnedPlayers.Contains(player);

        /// <summary>
        /// Hides this object for the specified player.
        /// </summary>
        /// <param name="player">The player to hide this object for.</param>
        public void HideForPlayer(Player player)
        {
            if (player.IsHost)
                return;

            player.Connection.Send(new ObjectHideMessage { netId = NetId });
        }

        /// <summary>
        /// Shows this object for the specified player.
        /// </summary>
        /// <param name="player">The player to show this object for.</param>
        public void ShowForPlayer(Player player)
        {
            if (player.IsHost)
                return;

            SpawnForPlayer(player);
        }

        /// <summary>
        /// Destroys this object for the specified player.
        /// </summary>
        /// <param name="player">The player to despawn this object for.</param>
        public void DespawnForPlayer(Player player)
        {
            if (player.IsHost)
                return;

            ObjectHandler.OnClientObjectDestroyed(new (this, player));
            player.Connection.Send(new ObjectDestroyMessage { netId = NetId });
            SpawnedPlayers.Remove(player);
        }

        /// <summary>
        /// Destroys this object for all ready players.
        /// </summary>
        /// <returns>The number of players this object was despawned for.</returns>
        public uint DespawnForAllPlayers()
        {
            uint count = 0;
            foreach (Player player in Player.ReadyList)
            {
                if (player.IsHost)
                    continue;

                count++;
                DespawnForPlayer(player);
            }

            return count;
        }

        /// <summary>
        /// Syncs the server values to all <see cref="Player"/>s this <see cref="ClientObject"/> is spawned for
        /// </summary>
        public void SyncToPlayers()
        {
            if (!Spawned)
                return;

            foreach (Player player in SpawnedPlayers)
            {
                if (player.IsHost)
                    continue;

                LogManager.Debug($"Syncing object with id {NetId} to {player.DisplayName}");
                SpawnForPlayer(player);
            }

            ClearDirtyFlags();
        }

        /// <summary>
        /// Syncs the server values to the specified <see cref="Player"/>
        /// </summary>
        /// <param name="player">The <see cref="Player"/> to be synced.</param>
        public void SyncToPlayer(Player player)
        {
            if (!Spawned)
                return;

            if (player.IsHost)
                return;

            LogManager.Debug($"Syncing object with id {NetId} to {player.DisplayName}");
            SpawnForPlayer(player);
            ClearDirtyFlags();
        }

        /// <summary>
        /// Syncs the server values to the players that match the filter.
        /// </summary>
        /// <param name="filter">The filter.</param>
        public void SyncToPlayer(Func<Player, bool> filter)
        {
            if (!Spawned)
                return;

            foreach (Player player in SpawnedPlayers)
            {
                if (player.IsHost)
                    continue;

                if (!filter(player))
                    continue;

                LogManager.Debug($"Syncing object with id {NetId} to {player.DisplayName}");
                SpawnForPlayer(player);
            }

            ClearDirtyFlags();
        }

        internal void PerformBatchedSync()
        {
            if (!Spawned || SyncFlags == SyncFlags.None)
                return;

            foreach (Player player in SpawnedPlayers)
            {
                if (player.IsHost)
                    continue;

                SpawnForPlayer(player);
            }
            
            ClearDirtyFlags();
            LogManager.Debug($"Batched sync completed for object {NetId} ({SyncFlags})");
        }

        /// <summary>
        /// Spawns a <see cref="GameObject"/> to the specified <see cref="Player"/>.
        /// </summary>
        /// <param name="obj">The <see cref="GameObject"/> to be spawned.</param>
        /// <param name="player">The <see cref="Player"/> to be spawned for.</param>
        /// <returns><see langword="true"/> if it is successfully spawned otherwise returns <see langword="false"/></returns>
        public virtual bool SpawnObjectOnPlayers(GameObject obj, Player player)
        {
            if (obj == null)
            {
                LogManager.Error("object is null.");
                return false;
            }

            if (!obj.TryGetComponent<NetworkIdentity>(out NetworkIdentity identity))
            {
                LogManager.Error($"{obj.name} has no NetworkIdentity component.");
                return false;
            }

            if (NetworkServer.spawned.ContainsKey(identity.netId))
            {
                LogManager.Warn($"netId {identity.netId} is already in the spawned dictionary.");
                return false;
            }
            
            identity.isLocalPlayer = false;
            identity.isClient = true;
            identity.isServer = false;
            identity.netId = NetworkIdentity.GetNextNetworkId();
            NetworkServer.spawned[identity.netId] = identity;
            LogManager.Info($"Registered {obj.name} with netId={identity.netId}.");
            identity.OnStartServer();
            SendCustomSpawnMessage(identity, player);

            return true;
        }

        private static void SendCustomSpawnMessage(NetworkIdentity identity, Player player)
        {
            if (!player.Connection.isReady)
            {
                LogManager.Warn($"Player is not ready. Message not sent.");
                return;
            }

            using NetworkWriterPooled ownerWriter = NetworkWriterPool.Get();
            using NetworkWriterPooled observersWriter = NetworkWriterPool.Get();
            ArraySegment<byte> payload = BuildSpawnPayload(identity, ownerWriter, observersWriter);
            SpawnMessage message = new()
            {
                netId = identity.netId,
                isLocalPlayer = false,
                isOwner = false,
                sceneId = 0,
                assetId = identity.assetId,
                position = identity.transform.localPosition,
                rotation = identity.transform.localRotation,
                scale = identity.transform.localScale,
                payload = payload
            };

            player.Connection.Send(message);
            LogManager.Debug($"Sent SpawnMessage for {identity.name} (netId={identity.netId}) to player {player.DisplayName}.");
        }

        private static ArraySegment<byte> BuildSpawnPayload(NetworkIdentity identity, NetworkWriterPooled ownerWriter, NetworkWriterPooled observersWriter)
        {
            if (identity.NetworkBehaviours.Length == 0)
                return default;

            identity.SerializeServer(initialState: true, ownerWriter, observersWriter);
            return observersWriter.ToArraySegment();
        }
    }
}