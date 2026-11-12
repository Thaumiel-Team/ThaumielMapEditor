// -----------------------------------------------------------------------
// <copyright file="ClientObject.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU Lesser General Public License v3.0 (LGPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using AdminToys;
using LabApi.Features.Wrappers;
using Mirror;
using ThaumielMapEditor.API.Components;
using ThaumielMapEditor.API.Enums;
using ThaumielMapEditor.API.Extensions;
using ThaumielMapEditor.API.Helpers;
using ThaumielMapEditor.API.Data;
using ThaumielMapEditor.Events.EventArgs.Handlers;
using UnityEngine;

namespace ThaumielMapEditor.API.Blocks
{
    [GitBookPage("Blocks/ClientObject")]
    public abstract class ClientObject : ObjectBase
    {
        public static event Action<Vector3, ClientObject>? PositionUpdated;
        public static event Action<Vector3, ClientObject>? ScaleUpdated;
        public static event Action<Quaternion, ClientObject>? RotationUpdated;

        /// <summary>
        /// Gets all <see cref="Player"/>s this object has been spawned for.
        /// </summary>
        public HashSet<Player> SpawnedPlayers { get; internal set; } = [];

        /// <summary>
        /// Gets a value indicating whether this object has been spawned for any players.
        /// </summary>
        public bool Spawned => !SpawnedPlayers.IsEmpty();

        /// <summary>
        /// Gets or sets the parent object id.
        /// </summary>
        public override int ParentId
        {
            get => base.ParentId;
            set
            {
                if (base.ParentId == value)
                    return;

                base.ParentId = value;

                if (Spawned)
                    SetParent(value);
            }
        }

        /// <summary>
        /// Gets or sets the position of this object.
        /// </summary>
        public override Vector3 Position
        {
            get => base.Position;
            set
            {
                if (base.Position == value)
                    return;

                base.Position = value;
                MarkSyncNeeded(SyncFlags.Position);
                PositionUpdated?.Invoke(value, this);
            }
        }

        /// <summary>
        /// Gets or sets the scale of this object.
        /// </summary>
        public override Vector3 Scale
        {
            get => base.Scale;
            set
            {
                if (base.Scale == value)
                    return;

                base.Scale = value;
                MarkSyncNeeded(SyncFlags.Scale);
                ScaleUpdated?.Invoke(value, this);
            }
        }

        /// <summary>
        /// Gets or sets the rotation of this object.
        /// </summary>
        public override Quaternion Rotation
        {
            get => base.Rotation;
            set
            {
                if (base.Rotation == value)
                    return;

                base.Rotation = value;
                MarkSyncNeeded(SyncFlags.Rotation);
                RotationUpdated?.Invoke(value, this);
            }
        }

        /// <summary>
        /// Gets or sets whether this object is static.
        /// </summary>
        public override bool IsStatic
        {
            get => base.IsStatic;
            set
            {
                if (base.IsStatic == value)
                    return;

                base.IsStatic = value;
                MarkSyncNeeded(SyncFlags.IsStatic);
            }
        }

        /// <summary>
        /// Gets or sets the movement smoothing value.
        /// </summary>
        public override byte MovementSmoothing
        {
            get => base.MovementSmoothing;
            set
            {
                if (base.MovementSmoothing == value)
                    return;

                base.MovementSmoothing = value;
                MarkSyncNeeded(SyncFlags.MovementSmoothing);
            }
        }

        /// <summary>
        /// Gets or sets the world position reported to the client.
        /// </summary>
        public Vector3 WorldPosition { get; set; }

        /// <summary>
        /// Gets or sets the world rotation reported to the client.
        /// </summary>
        public Quaternion WorldRotation { get; set; }

        /// <summary>
        /// Gets or sets the server side colliders belonging to this object.
        /// </summary>
        public Collider[] ServerColliders { get; set; } = [];

        /// <summary>
        /// Gets or sets the <see cref="GameObject"/> this object is parented to on the client.
        /// </summary>
        public GameObject? Parent { get; internal set; }

        /// <summary>
        /// Gets or sets the netId of the <see cref="GameObject"/> this object is parented to.
        /// </summary>
        public uint ParentNetId { get; internal set; }

        /// <summary>
        /// Gets or sets the asset id used when spawning this object on a client.
        /// </summary>
        public uint AssetId { get; internal set; }

        /// <inheritdoc/>
        protected override void RegisterForSync() => SyncManager.RegisterForSync(this);

        /// <summary>
        /// Called after this object has been added to a <see cref="SchematicData"/> and before it is spawned for players.
        /// </summary>
        /// <param name="schematic">The schematic this object belongs to.</param>
        public virtual void OnAddedToSchematic(SchematicData schematic)
        {
        }

        /// <summary>
        /// Gets a value indicating whether this object should be spawned for the given player when
        /// <see cref="LODZone"/>s are present.
        /// </summary>
        /// <param name="player">The player to check.</param>
        /// <param name="zones">The LOD zones of the schematic.</param>
        public virtual bool ShouldSpawnFor(Player player, LODZone[] zones) => true;

        /// <summary>
        /// Spawns this object for the specified player.
        /// </summary>
        /// <param name="player">The player to spawn the object for.</param>
        public virtual void SpawnForPlayer(Player player)
        {
            if (player == null || player.IsDestroyed || player.IsHost || player.IsDummy)
                return;

            if (IsSpawnedForPlayer(player))
                return;

            if (player.Connection == null || !player.Connection.isReady)
                return;

            using NetworkWriterPooled payloadWriter = NetworkWriterPool.Get();

            payloadWriter.WriteByte(1);

            int sizePos = payloadWriter.Position;
            payloadWriter.WriteByte(0);
            int dataStart = payloadWriter.Position;

            WriteSyncObjects(payloadWriter);
            WriteSyncVars(payloadWriter);

            payloadWriter.WriteUInt(ParentNetId);

            int dataEnd = payloadWriter.Position;
            payloadWriter.Position = sizePos;
            payloadWriter.WriteByte((byte)(dataEnd - dataStart));
            payloadWriter.Position = dataEnd;

            ArraySegment<byte> payload = payloadWriter.ToArraySegment();

            player.Connection.Send(new SpawnMessage
            {
                netId = NetId,
                isLocalPlayer = false,
                isOwner = false,
                sceneId = 0,
                assetId = AssetId,
                position = Position,
                rotation = Rotation,
                scale = Scale,
                payload = payload
            });

            ObjectHandler.OnClientObjectSpawned(new(this, player));
            SpawnedPlayers.Add(player);
        }

        /// <summary>
        /// Destroys this object for the specified player.
        /// </summary>
        /// <param name="player">The player to destroy the object for.</param>
        public void DestroyForPlayer(Player player)
        {
            DrawableLinesHelper.StopDraw(this, player);
            player.Connection.Send(new ObjectDestroyMessage { netId = NetId });
            SpawnedPlayers.Remove(player);
        }

        /// <summary>
        /// Destroys this object for all players.
        /// </summary>
        public void DestroyForAllPlayers()
        {
            DrawableLinesHelper.StopDraw(this, null);
            foreach (Player player in Player.ReadyList)
            {
                if (player.IsHost || player.IsDummy)
                    continue;

                DestroyForPlayer(player);
            }
        }

        /// <summary>
        /// Sets the parent of this object.
        /// </summary>
        /// <param name="player">The player that will receive the parent message.</param>
        /// <param name="parentId">The netId of the parent.</param>
        public virtual void SetParent(Player player, int parentId)
        {
            player.SendFakeRPC(NetId, typeof(AdminToyBase), nameof(AdminToyBase.RpcChangeParent), 0, parentId);

            ParentNetId = (uint)parentId;
            if (TryFindParentGameObject(ParentNetId, out GameObject? found))
            {
                Parent = found;
            }
            else
                LogManager.Warn($"Failed to find GameObject with NetId {ParentNetId}!");
        }

        /// <summary>
        /// Sets the parent of this object.
        /// </summary>
        /// <param name="player">The player that will receive the parent message.</param>
        /// <param name="network">The parent behaviour.</param>
        public virtual void SetParent(Player player, NetworkBehaviour network)
        {
            player.SendFakeRPC(NetId, typeof(AdminToyBase), nameof(AdminToyBase.RpcChangeParent), 0, network.netId);

            ParentNetId = network.netId;
            Parent = network.gameObject;
        }

        /// <summary>
        /// Sets the parent of this object.
        /// </summary>
        /// <param name="player">The player that will receive the parent message.</param>
        /// <param name="gameObject">The parent object.</param>
        /// <returns><see langword="true"/> if the object was parented.</returns>
        public virtual bool SetParent(Player player, GameObject gameObject)
        {
            if (!gameObject.TryGetComponent<NetworkBehaviour>(out var network))
                return false;

            player.SendFakeRPC(NetId, typeof(AdminToyBase), nameof(AdminToyBase.RpcChangeParent), 0, network.netId);
            ParentNetId = network.netId;
            Parent = gameObject;
            return true;
        }

        /// <summary>
        /// Sets the parent of this object for every player it is spawned for.
        /// </summary>
        /// <param name="parentId">The netId of the parent.</param>
        public virtual void SetParent(int parentId)
        {
            foreach (Player player in SpawnedPlayers)
            {
                player.SendFakeRPC(NetId, typeof(AdminToyBase), nameof(AdminToyBase.RpcChangeParent), 0, parentId);
            }

            ParentNetId = (uint)parentId;
            if (TryFindParentGameObject(ParentNetId, out GameObject? found))
            {
                Parent = found;
            }
            else
                LogManager.Warn($"Failed to find GameObject with NetId {ParentNetId}!");
        }

        /// <summary>
        /// Sets the parent of this object for every player it is spawned for.
        /// </summary>
        /// <param name="network">The parent behaviour.</param>
        public virtual void SetParent(NetworkBehaviour network)
        {
            foreach (Player player in SpawnedPlayers)
            {
                player.SendFakeRPC(NetId, typeof(AdminToyBase), nameof(AdminToyBase.RpcChangeParent), 0, network.netId);
            }

            ParentNetId = network.netId;
            Parent = network.gameObject;
        }

        /// <summary>
        /// Sets the parent of this object for every player it is spawned for.
        /// </summary>
        /// <param name="gameObject">The parent object.</param>
        /// <returns><see langword="true"/> if the object was parented.</returns>
        public virtual bool SetParent(GameObject gameObject)
        {
            if (!gameObject.TryGetComponent<NetworkBehaviour>(out var network))
                return false;

            foreach (Player player in SpawnedPlayers)
            {
                player.SendFakeRPC(NetId, typeof(AdminToyBase), nameof(AdminToyBase.RpcChangeParent), 0, network.netId);
            }

            ParentNetId = network.netId;
            Parent = gameObject;
            return true;
        }

        protected static bool TryFindParentGameObject(uint netId, out GameObject? gameObject)
        {
            if (NetworkServer.spawned.TryGetValue(netId, out NetworkIdentity identity) && identity?.gameObject != null)
            {
                gameObject = identity.gameObject;
                return true;
            }

            foreach (SchematicData schematic in Loader.SpawnedSchematics)
            {
                foreach (ClientObject client in schematic.SpawnedClientObjects)
                {
                    if (client.NetId == netId)
                    {
                        gameObject = null;
                        return true;
                    }
                }
            }

            gameObject = null;
            return false;
        }

        /// <summary>
        /// Checks if this object is spawned for the specified player.
        /// </summary>
        /// <param name="player">The player to check.</param>
        public bool IsSpawnedForPlayer(Player player)
            => SpawnedPlayers.Contains(player);

        /// <summary>
        /// Hides this object for the specified player.
        /// </summary>
        /// <param name="player">The player to hide the object for.</param>
        public void HideForPlayer(Player player)
        {
            if (player.IsHost)
                return;

            player.Connection.Send(new ObjectHideMessage { netId = NetId });
            SpawnedPlayers.Remove(player);
        }

        /// <summary>
        /// Shows this object for the specified player.
        /// </summary>
        /// <param name="player">The player to show the object for.</param>
        public void ShowForPlayer(Player player)
        {
            if (player.IsHost)
                return;

            SpawnForPlayer(player);
        }

        /// <summary>
        /// Spawns this object for every ready player that should see it.
        /// </summary>
        public void SpawnForAllPlayers()
        {
            LODZone[] zones = Schematic != null ? Schematic.GetLODZones() : [];

            foreach (Player player in Player.ReadyList)
            {
                if (ShouldSpawnFor(player, zones))
                    SpawnForPlayer(player);
            }
        }

        /// <summary>
        /// Hides this object for every ready player.
        /// </summary>
        public void HideForAllPlayers()
        {
            foreach (Player player in Player.ReadyList)
                HideForPlayer(player);
        }

        /// <summary>
        /// Shows this object for every ready player.
        /// </summary>
        public void ShowForAllPlayers()
        {
            foreach (Player player in Player.ReadyList)
                ShowForPlayer(player);
        }

        /// <summary>
        /// Despawns this object for the specified player.
        /// </summary>
        /// <param name="player">The player to despawn the object for.</param>
        public void DespawnForPlayer(Player player)
        {
            if (player.IsHost)
                return;

            DrawableLinesHelper.StopDraw(this, player);
            ObjectHandler.OnClientObjectDestroyed(new(this, player));
            player.Connection.Send(new ObjectDestroyMessage { netId = NetId });
            SpawnedPlayers.Remove(player);
        }

        /// <summary>
        /// Despawns this object for all ready players.
        /// </summary>
        /// <returns>The number of players the object was despawned for.</returns>
        public uint DespawnForAllPlayers()
        {
            DrawableLinesHelper.StopDraw(this, null);
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
        /// Syncs pending changes for the specified player without resending a spawn message.
        /// </summary>
        /// <param name="player">The player to sync to.</param>
        public virtual void SyncForPlayer(Player player)
        {
            if (player.IsHost)
                return;

            if (!IsSpawnedForPlayer(player))
            {
                SpawnForPlayer(player);
                return;
            }

            SendDeltaUpdate(player, SyncFlags);
        }

        /// <summary>
        /// Syncs pending changes for every player this object is spawned for.
        /// </summary>
        public void SyncForAllPlayers()
        {
            foreach (Player player in SpawnedPlayers)
                SyncForPlayer(player);
        }

        /// <summary>
        /// Sends the given <paramref name="flags"/> as a Mirror entity state message.
        /// </summary>
        /// <param name="player">The player to send the message to.</param>
        /// <param name="flags">The changes to include in the message.</param>
        public virtual void SendDeltaUpdate(Player player, SyncFlags flags)
        {
            if (player.IsHost)
                return;

            if (player.Connection == null || !player.Connection.isReady)
                return;

            if (flags.HasFlagFast(SyncFlags.Parent))
                player.SendFakeRPC(NetId, typeof(AdminToyBase), nameof(AdminToyBase.RpcChangeParent), 0, ParentNetId);

            ulong baseMask = 0UL;
            if (flags.HasFlagFast(SyncFlags.Position))
                baseMask |= 1UL;

            if (flags.HasFlagFast(SyncFlags.Rotation))
                baseMask |= 2UL;

            if (flags.HasFlagFast(SyncFlags.Scale))
                baseMask |= 4UL;

            if (flags.HasFlagFast(SyncFlags.MovementSmoothing))
                baseMask |= 8UL;

            if (flags.HasFlagFast(SyncFlags.IsStatic))
                baseMask |= 16UL;

            ulong combinedMask = baseMask | GetDerivedDirtyBits(flags);
            if (combinedMask == 0UL)
                return;

            using NetworkWriterPooled writer = NetworkWriterPool.Get();

            writer.WriteByte(1);

            int sizePos = writer.Position;
            writer.WriteByte(0);
            int dataStart = writer.Position;

            writer.WriteULong(0UL);

            writer.WriteULong(combinedMask);
            if (flags.HasFlagFast(SyncFlags.Position))
                writer.WriteVector3(Position);

            if (flags.HasFlagFast(SyncFlags.Rotation))
                writer.WriteQuaternion(Rotation);

            if (flags.HasFlagFast(SyncFlags.Scale))
                writer.WriteVector3(Scale);

            if (flags.HasFlagFast(SyncFlags.MovementSmoothing))
                writer.WriteByte(MovementSmoothing);

            if (flags.HasFlagFast(SyncFlags.IsStatic))
                writer.WriteBool(IsStatic);

            writer.WriteULong(combinedMask);
            WriteDerivedSyncVars(writer, flags);

            int dataEnd = writer.Position;
            writer.Position = sizePos;
            writer.WriteByte((byte)(dataEnd - dataStart));
            writer.Position = dataEnd;

            player.Connection.Send(new EntityStateMessage { netId = NetId, payload = writer.ToArraySegment() });
        }

        /// <summary>
        /// Gets the derived dirty bits for the given <paramref name="flags"/>.
        /// </summary>
        /// <param name="flags">The changes to map to dirty bits.</param>
        protected virtual ulong GetDerivedDirtyBits(SyncFlags flags) => 0UL;

        /// <summary>
        /// Writes derived SyncVar values in ascending dirty bit order.
        /// </summary>
        /// <param name="writer">The writer to write to.</param>
        /// <param name="flags">The changes to write.</param>
        protected virtual void WriteDerivedSyncVars(NetworkWriter writer, SyncFlags flags)
        {
        }

        /// <summary>
        /// Syncs the server values to all players this object is spawned for.
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
                SyncForPlayer(player);
            }

            ClearDirtyFlags();
        }

        /// <summary>
        /// Syncs the server values to the specified player.
        /// </summary>
        /// <param name="player">The player to sync to.</param>
        public void SyncToPlayer(Player player)
        {
            if (!Spawned || player.IsHost)
                return;

            LogManager.Debug($"Syncing object with id {NetId} to {player.DisplayName}");
            SyncForPlayer(player);
            ClearDirtyFlags();
        }

        /// <summary>
        /// Syncs the server values to the players that match the filter.
        /// </summary>
        /// <param name="filter">The player filter.</param>
        public void SyncToPlayer(Func<Player, bool> filter)
        {
            if (!Spawned)
                return;

            foreach (Player player in SpawnedPlayers)
            {
                if (player.IsHost || !filter(player))
                    continue;

                LogManager.Debug($"Syncing object with id {NetId} to {player.DisplayName}");
                SyncForPlayer(player);
            }

            ClearDirtyFlags();
        }

        internal void PerformBatchedSync()
        {
            if (!Spawned || SyncFlags == SyncFlags.None)
                return;

            SyncFlags flags = SyncFlags;
            foreach (Player player in SpawnedPlayers)
            {
                if (player.IsHost)
                    continue;

                SyncForPlayer(player);
            }

            ClearDirtyFlags();
            LogManager.Debug($"Batched sync completed for object {NetId} ({flags})");
        }

        /// <summary>
        /// Spawns a <see cref="GameObject"/> to the specified player.
        /// </summary>
        /// <param name="obj">The object to spawn.</param>
        /// <param name="player">The player to spawn it for.</param>
        /// <returns><see langword="true"/> if the object was spawned.</returns>
        public virtual bool SpawnObjectOnPlayers(GameObject obj, Player player)
        {
            if (obj == null)
            {
                LogManager.Error("object is null.");
                return false;
            }

            if (!obj.TryGetComponent(out NetworkIdentity identity))
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

        /// <summary>
        /// Writes the shared sync variables to the specified writer.
        /// </summary>
        /// <param name="payloadWriter">The writer to write to.</param>
        protected virtual void WriteSyncVars(NetworkWriter payloadWriter)
        {
            payloadWriter.WriteVector3(Position);
            payloadWriter.WriteQuaternion(Rotation);
            payloadWriter.WriteVector3(Scale);
            payloadWriter.WriteByte(MovementSmoothing);
            payloadWriter.WriteBool(IsStatic);
        }

        protected virtual void WriteSyncObjects(NetworkWriterPooled payloadWriter)
        {
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