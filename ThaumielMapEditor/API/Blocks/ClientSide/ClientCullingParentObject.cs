// -----------------------------------------------------------------------
// <copyright file="ClientCullingParentObject.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Linq;
using LabApi.Features.Wrappers;
using Mirror;
using ThaumielMapEditor.API.Data;
using ThaumielMapEditor.API.Enums;
using ThaumielMapEditor.API.Helpers;
using ThaumielMapEditor.Events.EventArgs.Handlers;
using UnityEngine;
using YamlDotNet.Serialization;

namespace ThaumielMapEditor.API.Blocks.ClientSide
{
    [GitBookPage("Blocks/Client/ClientCullingParentObject")]
    public class ClientCullingParentObject : ClientObject
    {
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the <see cref="SchematicData"/> this <see cref="ClientCullingParentObject"/> is spawned from.
        /// </summary>
        public SchematicData? Schematic { get; set; }

        /// <inheritdoc/>
        public override ObjectType ObjectType => ObjectType.CullingParent;

        /// <summary>
        /// Gets or sets the size.
        /// </summary>
        [YamlMember(Alias = "BoundsSize")]
        public Vector3 BoundsSize
        {
            get;
            set
            {
                if (field == value)
                    return;

                field = value;
                MarkSyncNeeded(SyncFlags.BoundsSize);
            }
        } = new(10f, 10f, 10f);

        /// <inheritdoc/>
        public override void SpawnForPlayer(Player player)
        {
            if (player.IsHost)
                return;

            if (PrefabHelper.CullingParent?.netIdentity == null)
            {
                LogManager.Warn($"Skipping client culling parent '{Name}': culling parent prefab not registered.");
                return;
            }

            uint assetId = PrefabHelper.CullingParent.netIdentity.assetId;
            AssetId = assetId;

            (Vector3 worldPosition, Quaternion worldRotation) = ResolveWorldTransform();
            Vector3 worldScale = ResolveWorldScale();

            ArraySegment<byte> payload = BuildSpawnPayload(worldPosition);

            player.Connection.Send(new SpawnMessage
            {
                netId = NetId,
                isLocalPlayer = false,
                isOwner = false,
                sceneId = 0,
                assetId = assetId,
                position = worldPosition,
                rotation = worldRotation,
                scale = worldScale,
                payload = payload,
            });

            SendBoundsDelta(player, worldPosition);
            ObjectHandler.OnClientObjectSpawned(new(this, player));
            SpawnedPlayers.Add(player);
        }

        private void SendBoundsDelta(Player player, Vector3 worldPosition)
        {
            using NetworkWriterPooled writer = NetworkWriterPool.Get();

            writer.WriteByte(1);
            int sizePos = writer.Position;
            writer.WriteByte(0);
            int dataStart = writer.Position;

            writer.WriteULong(0UL);
            writer.WriteULong(3UL);
            writer.WriteVector3(worldPosition);
            writer.WriteVector3(BoundsSize);

            int dataEnd = writer.Position;
            writer.Position = sizePos;
            writer.WriteByte((byte)(dataEnd - dataStart));
            writer.Position = dataEnd;

            player.Connection.Send(new EntityStateMessage { netId = NetId, payload = writer.ToArraySegment() });
        }

        /// <inheritdoc/>
        public override void SyncForPlayer(Player player)
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

        /// <inheritdoc/>
        public override void SendDeltaUpdate(Player player, SyncFlags flags)
        {
            if (player.IsHost)
                return;

            if (player.Connection == null || !player.Connection.isReady)
                return;

            if (flags != SyncFlags.None)
                RespawnForPlayer(player);
        }

        /// <summary>
        /// Respawns this <see cref="ClientCullingParentObject"/> for the specified <see cref="Player"/>.
        /// </summary>
        /// <param name="player">The <see cref="Player"/> to respawn for.</param>
        public void RespawnForPlayer(Player player)
        {
            if (player.IsHost || !IsSpawnedForPlayer(player))
                return;

            if (player.Connection == null || !player.Connection.isReady)
                return;

            DrawableLinesHelper.StopDraw(this, player);
            ObjectHandler.OnClientObjectDestroyed(new(this, player));
            player.Connection.Send(new ObjectDestroyMessage { netId = NetId });
            SpawnForPlayer(player);
        }

        /// <summary>
        /// Respawns this <see cref="ClientCullingParentObject"/> for all players it is spawned for.
        /// </summary>
        public void RespawnForAllPlayers()
        {
            foreach (Player player in SpawnedPlayers.ToArray())
                RespawnForPlayer(player);
        }

        /// <summary>
        /// Resolves the world space transform of this <see cref="ClientCullingParentObject"/>.
        /// </summary>
        /// <returns>The world position and rotation to spawn with.</returns>
        public (Vector3 Position, Quaternion Rotation) ResolveWorldTransform()
        {
            try
            {
                if (Schematic == null)
                {
                    LogManager.Warn($"Cannot resolve world transform for client culling parent '{Name}': schematic reference is missing, falling back to local position {Position}.");
                    return (Position, Rotation);
                }

                Transform? parent = ColliderHelper.ResolveServerParentTransform(ParentId, Schematic);
                if (parent == null)
                {
                    LogManager.Warn($"Cannot resolve world transform for client culling parent '{Name}': parent transform for id {ParentId} is missing, falling back to local position {Position}.");
                    return (Position, Rotation);
                }

                return (parent.TransformPoint(Position), parent.rotation * Rotation);
            }
            catch (Exception ex)
            {
                LogManager.Warn($"Failed to resolve world transform for client culling parent '{Name}': {ex.Message}. Falling back to local position {Position}.");
                return (Position, Rotation);
            }
        }

        public Vector3 ResolveWorldScale()
        {
            if (Schematic == null)
                return Vector3.one;

            Transform? parent = ColliderHelper.ResolveServerParentTransform(ParentId, Schematic);
            return parent == null ? Vector3.one : parent.lossyScale;
        }

        private ArraySegment<byte> BuildSpawnPayload(Vector3 worldPosition)
        {
            using NetworkWriterPooled writer = NetworkWriterPool.Get();
            Compression.CompressVarUInt(writer, 1UL);
            writer.WriteByte(24);
            writer.WriteVector3(worldPosition);
            writer.WriteVector3(BoundsSize);
            return writer.ToArraySegment();
        }
    }
}
