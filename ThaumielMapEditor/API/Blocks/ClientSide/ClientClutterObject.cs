// -----------------------------------------------------------------------
// <copyright file="ClientClutterObject.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using LabApi.Features.Wrappers;
using Mirror;
using ThaumielMapEditor.API.Blocks.ServerObjects;
using ThaumielMapEditor.API.Data;
using ThaumielMapEditor.API.Enums;
using ThaumielMapEditor.API.Helpers;
using ThaumielMapEditor.Events.EventArgs.Handlers;
using UnityEngine;
using YamlDotNet.Serialization;

namespace ThaumielMapEditor.API.Blocks.ClientSide
{
    [GitBookPage("Blocks/Client/ClutterObject")]
    public class ClientClutterObject : ClientObject
    {
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the <see cref="SchematicData"/> this <see cref="ClientClutterObject"/> is spawned from.
        /// </summary>
        public SchematicData? Schematic { get; set; }

        /// <inheritdoc/>
        public override ObjectType ObjectType => ObjectType.Clutter;

        /// <summary>
        /// Gets or sets the <see cref="ClutterType"/> of this clutter object.
        /// </summary>
        [YamlMember(Alias = "ClutterType")]
        public ClutterType Type
        {
            get;
            set
            {
                if (field == value)
                    return;

                field = value;

                if (!Spawned)
                    return;

                Respawn();
            }
        } = ClutterType.SimpleBoxes;

        /// <summary>
        /// Gets or sets the parent id this <see cref="ClientClutterObject"/> is parented to.
        /// </summary>
        public override int ParentId
        {
            get;
            set
            {
                field = value;

                if (Spawned)
                    RespawnForAllPlayers();
            }
        }

        /// <inheritdoc/>
        public override void SpawnForPlayer(Player player)
        {
            if (player.IsHost)
                return;

            if (!Enum.IsDefined(typeof(ClutterType), Type))
            {
                LogManager.Warn($"Skipping client clutter '{Name}': unknown ClutterType '{Type}'.");
                return;
            }

            GameObject prefab = ClutterObject.GetClutterPrefab(Type);
            uint assetId = PrefabHelper.GetAssetId(prefab);
            if (assetId == 0)
            {
                LogManager.Warn($"Skipping client clutter '{Name}': clutter prefab '{prefab.name}' has no NetworkIdentity.");
                return;
            }

            AssetId = assetId;

            (Vector3 worldPosition, Quaternion worldRotation, Vector3 worldScale) = ResolveWorldTransform();

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
                payload = default,
            });

            ObjectHandler.OnClientObjectSpawned(new(this, player));
            SpawnedPlayers.Add(player);
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

            if (flags.HasFlagFast(SyncFlags.Position) || flags.HasFlagFast(SyncFlags.Rotation) || flags.HasFlagFast(SyncFlags.Scale) || flags.HasFlagFast(SyncFlags.Parent))
                RespawnForPlayer(player);
        }

        /// <summary>
        /// Resolves the world space transform of this <see cref="ClientClutterObject"/>.
        /// </summary>
        /// <returns>The world position, rotation and scale to spawn with.</returns>
        public (Vector3 Position, Quaternion Rotation, Vector3 Scale) ResolveWorldTransform()
        {
            if (Schematic == null)
                return (Position, Rotation, Scale);

            Transform? parent = ColliderHelper.ResolveServerParentTransform(ParentId, Schematic);
            if (parent == null)
                return (Position, Rotation, Scale);

            return (parent.TransformPoint(Position), parent.rotation * Rotation, Vector3.Scale(parent.lossyScale, Scale));
        }

        /// <summary>
        /// Respawns this <see cref="ClientClutterObject"/> for the specified <see cref="Player"/>./>.
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
            RefreshColliders();
            SpawnForPlayer(player);
        }

        /// <summary>
        /// Respawns this <see cref="ClientClutterObject"/> for all players it is spawned for.
        /// </summary>
        public void RespawnForAllPlayers()
        {
            foreach (Player player in SpawnedPlayers.ToArray())
                RespawnForPlayer(player);
        }

        /// <summary>
        /// Respawns this <see cref="ClientClutterObject"/> for all players it is spawned for under a new <see cref="ClientObject.NetId"/>.
        /// </summary>
        /// <remarks>
        /// Used when the prefab itself changes (see <see cref="Type"/>).
        /// </remarks>
        public void Respawn()
        {
            Player[] players = SpawnedPlayers.ToArray();
            uint oldNetId = NetId;
            NetId = NetworkIdentity.GetNextNetworkId();

            foreach (Player player in players)
            {
                if (player.IsHost)
                    continue;

                DrawableLinesHelper.StopDraw(this, player);
                ObjectHandler.OnClientObjectDestroyed(new(this, player));
                player.Connection.Send(new ObjectDestroyMessage { netId = oldNetId });
                SpawnedPlayers.Remove(player);
            }

            RefreshColliders();

            foreach (Player player in players)
            {
                if (player.IsHost)
                    continue;

                SpawnForPlayer(player);
            }
        }

        public void RefreshColliders()
        {
            if (Schematic == null)
            {
                ServerColliders = [];
                return;
            }

            ColliderHelper.DestroyClientObjectColliders(this, Schematic);
            ColliderHelper.CreateClientObjectColliders(this, Schematic);
        }

        /// <summary>
        /// Sets the parent of the <see cref="ClientClutterObject"/> and respawns it.
        /// </summary>
        /// <param name="parentId">The <see cref="NetworkBehaviour.netId"/> of the parent.</param>
        public override void SetParent(int parentId)
        {
            UpdateParentReference(parentId);
            ParentId = parentId;
        }

        /// <summary>
        /// Sets the parent of the <see cref="ClientClutterObject"/> and respawns it.
        /// </summary>
        /// <param name="network">The <see cref="NetworkBehaviour"/> of the parent.</param>
        public override void SetParent(NetworkBehaviour network)
        {
            UpdateParentReference((int)network.netId);
            ParentId = (int)network.netId;
        }

        /// <summary>
        /// Sets the parent of the <see cref="ClientClutterObject"/> and respawns it.
        /// </summary>
        /// <param name="gameObject">The <see cref="GameObject"/> of the parent.</param>
        /// <returns><see langword="true"/> if the <see cref="ClientClutterObject"/> was parented.</returns>
        public override bool SetParent(GameObject gameObject)
        {
            if (!gameObject.TryGetComponent<NetworkBehaviour>(out var network))
                return false;

            UpdateParentReference((int)network.netId);
            ParentId = (int)network.netId;
            return true;
        }

        private void UpdateParentReference(int parentId)
        {
            ParentNetId = (uint)parentId;

            if (TryFindParentGameObject(ParentNetId, out GameObject? found))
            {
                Parent = found;
            }
            else
            {
                Parent = null;
                LogManager.Warn($"Failed to find GameObject with NetId {ParentNetId}!");
            }
        }
    }
}
