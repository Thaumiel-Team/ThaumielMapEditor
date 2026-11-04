// -----------------------------------------------------------------------
// <copyright file="CullingParent.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU Lesser General Public License v3.0 (LGPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Linq;
using LabApi.Features.Wrappers;
using Mirror;
using ThaumielMapEditor.API.Data;
using ThaumielMapEditor.API.Enums;
using ThaumielMapEditor.API.Helpers;
using ThaumielMapEditor.API.Serialization;
using ThaumielMapEditor.Events.EventArgs.Handlers;
using UnityEngine;
using YamlDotNet.Serialization;
using SpawnableCullingParent = AdminToys.SpawnableCullingParent;

namespace ThaumielMapEditor.API.Blocks.Definitions
{
    [GitBookPage("Blocks/Definitions/CullingParent")]
    public sealed class CullingParent : ObjectDefinition<CullingParent>
    {
        /// <summary>
        /// Gets or sets the world space bounds size.
        /// </summary>
        [YamlMember(Alias = "BoundsSize")]
        public Vector3 BoundsSize { get; set; } = new(10f, 10f, 10f);

        /// <inheritdoc/>
        public override string Name => "CullingParent";

        /// <inheritdoc/>
        public override ObjectType ObjectType => ObjectType.CullingParent;

        /// <inheritdoc/>
        public override ServerObject? ServerObject(ObjectBase data)
        {
            if (PrefabHelper.CullingParent == null)
            {
                LogManager.Warn($"Skipping culling parent '{data.Name}': culling parent prefab not registered.");
                return null;
            }

            if (BoundsSize == Vector3.zero)
            {
                LogManager.Warn($"Skipping culling parent '{data.Name}': BoundsSize is zero.");
                return null;
            }

            return new Server(data, this);
        }

        /// <inheritdoc/>
        public override ClientObject? ClientObject(ObjectBase data)
        {
            if (PrefabHelper.CullingParent?.netIdentity == null)
            {
                LogManager.Warn($"Skipping client culling parent '{data.Name}': culling parent prefab not registered.");
                return null;
            }

            if (BoundsSize == Vector3.zero)
            {
                LogManager.Warn($"Skipping client culling parent '{data.Name}': BoundsSize is zero.");
                return null;
            }

            return new Client(data, this)
            {
                AssetId = PrefabHelper.CullingParent.netIdentity.assetId,
                Scale = Vector3.one,
            };
        }

        [GitBookPage("Blocks/Definitions/CullingParent/Server")]
        public sealed class Server : ServerObject
        {
            private readonly CullingParent definition;

            /// <summary>
            /// Initializes a new instance of the <see cref="Server"/> class.
            /// </summary>
            /// <param name="data">The shared object state.</param>
            /// <param name="definition">The definition that owns the serialized values.</param>
            public Server(ObjectBase data, CullingParent definition)
            {
                CopyStateFrom(data);
                this.definition = definition;
            }

            /// <summary>
            /// Gets the underlying <see cref="SpawnableCullingParent"/>.
            /// </summary>
            public SpawnableCullingParent? Base { get; private set; }

            /// <summary>
            /// Gets or sets the world space bounds size.
            /// </summary>
            public Vector3 BoundsSize
            {
                get => Base != null ? Base.BoundsSize : definition.BoundsSize;
                set
                {
                    definition.BoundsSize = value;
                    Base?.NetworkBoundsSize = value;
                }
            }

            /// <summary>
            /// Gets or sets the world position.
            /// </summary>
            public override Vector3 Position
            {
                get => Base != null ? Base.BoundsPosition : base.Position;
                set
                {
                    base.Position = value;
                    Base?.NetworkBoundsPosition = value;
                    Object?.transform.position = value;
                }
            }

            /// <summary>
            /// Gets or sets the scale.
            /// </summary>
            public override Vector3 Scale
            {
                get => base.Scale;
                set
                {
                    base.Scale = value;
                    Object?.transform.localScale = Vector3.one;
                }
            }

            /// <inheritdoc/>
            public override void SpawnObject(SchematicData schematic, SerializableObject serializable)
            {
                if (PrefabHelper.CullingParent == null)
                {
                    LogManager.Warn($"Skipping culling parent '{Name}': culling parent prefab not registered.");
                    return;
                }

                SpawnableCullingParent instance = UnityEngine.Object.Instantiate(PrefabHelper.CullingParent);
                Object = instance.gameObject;

                SetWorldTransform(schematic);
                Object.transform.localScale = Vector3.one;

                Base = instance;
                Base.NetworkBoundsPosition = base.Position;
                Base.NetworkBoundsSize = definition.BoundsSize;

                Object.transform.SetPositionAndRotation(base.Position, base.Rotation);
                Object.transform.localScale = Vector3.one;

                NetworkServer.Spawn(Object);

                if (Object.TryGetComponent<NetworkIdentity>(out var network))
                    NetId = network.netId;

                base.SpawnObject(schematic, serializable);
                ClearDirtyFlags();
            }

            /// <inheritdoc/>
            public override void OnSpawned(SchematicData schematic)
            {
                if (Object == null)
                    return;

                schematic.ServerSideTransforms[ObjectId] = Object.transform;
                LogManager.Debug($"Spawned server culling parent '{Name}' with NetId {NetId}.");
            }
        }

        [GitBookPage("Blocks/Definitions/CullingParent/Client")]
        public sealed class Client : ClientObject
        {
            private readonly CullingParent definition;

            /// <summary>
            /// Initializes a new instance of the <see cref="Client"/> class.
            /// </summary>
            /// <param name="data">The shared object state.</param>
            /// <param name="definition">The definition that owns the serialized values.</param>
            public Client(ObjectBase data, CullingParent definition)
            {
                CopyStateFrom(data);
                this.definition = definition;
            }

            /// <summary>
            /// Gets or sets the world space bounds size.
            /// </summary>
            public Vector3 BoundsSize
            {
                get => definition.BoundsSize;
                set
                {
                    if (definition.BoundsSize == value)
                        return;

                    definition.BoundsSize = value;
                    MarkSyncNeeded(SyncFlags.BoundsSize);
                }
            }

            /// <inheritdoc/>
            public override ObjectType ObjectType => ObjectType.CullingParent;

            /// <inheritdoc/>
            public override void SpawnForPlayer(Player player)
            {
                if (player == null || player.IsDestroyed || player.IsHost || player.IsDummy)
                    return;

                if (IsSpawnedForPlayer(player))
                    return;

                if (player.Connection == null || !player.Connection.isReady)
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
                LogManager.Debug($"Spawned client culling parent '{Name}' with NetId {NetId}.");
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
            /// Respawns this culling parent for the specified player.
            /// </summary>
            /// <param name="player">The player to respawn for.</param>
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
            /// Respawns this culling parent for all players it is spawned for.
            /// </summary>
            public void RespawnForAllPlayers()
            {
                foreach (Player player in SpawnedPlayers.ToArray())
                    RespawnForPlayer(player);
            }

            /// <summary>
            /// Resolves the world space transform of this culling parent.
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

            /// <summary>
            /// Resolves the world space scale of this culling parent.
            /// </summary>
            /// <returns>The world scale to spawn with.</returns>
            public Vector3 ResolveWorldScale()
            {
                if (Schematic == null)
                    return Vector3.one;

                Transform? parent = ColliderHelper.ResolveServerParentTransform(ParentId, Schematic);
                return parent == null ? Vector3.one : parent.lossyScale;
            }

            /// <inheritdoc/>
            public override void OnAddedToSchematic(SchematicData schematic)
            {
                if (Serialization != null)
                    Loader.EnsureGameObjectNode(Serialization, schematic);

                ClearDirtyFlags();
                SyncManager.RemoveFromPending(this);
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
}