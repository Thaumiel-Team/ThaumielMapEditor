// -----------------------------------------------------------------------
// <copyright file="ClutterDefinition.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Linq;
using LabApi.Features.Wrappers;
using MapGeneration.RoomConnectors;
using Mirror;
using ThaumielMapEditor.API.Blocks.ClientSide;
using ThaumielMapEditor.API.Data;
using ThaumielMapEditor.API.Enums;
using ThaumielMapEditor.API.Helpers;
using ThaumielMapEditor.API.Serialization;
using ThaumielMapEditor.Events.EventArgs.Handlers;
using UnityEngine;
using YamlDotNet.Serialization;

namespace ThaumielMapEditor.API.Blocks.Definitions
{
    /// <summary>
    /// The definition of a clutter object, shared by the client and server runtime representations.
    /// </summary>
    [GitBookPage("Blocks/Definitions/Clutter")]
    public sealed class ClutterDefinition : ObjectDefinition<ClutterDefinition>
    {
        /// <summary>
        /// Gets or sets the <see cref="ClutterType"/> of this clutter object.
        /// </summary>
        [YamlMember(Alias = "ClutterType")]
        public ClutterType Type { get; set; } = ClutterType.SimpleBoxes;

        /// <inheritdoc/>
        public override string Name => "Clutter";

        /// <inheritdoc/>
        public override ObjectType ObjectType => ObjectType.Clutter;

        /// <summary>
        /// Gets the <see cref="GameObject"/> prefab associated with the given <see cref="ClutterType"/>.
        /// </summary>
        /// <param name="type">The clutter type.</param>
        /// <returns>The matching prefab.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the type is unknown.</exception>
        public static GameObject GetClutterPrefab(ClutterType type)
        {
#pragma warning disable CS8603 // Possible null reference return.
            return type switch
            {
                ClutterType.SimpleBoxes => PrefabHelper.SimpleBoxes,
                ClutterType.PipesShort => PrefabHelper.PipesShort,
                ClutterType.BoxesLadder => PrefabHelper.BoxesLadder,
                ClutterType.TankSupportedShelf => PrefabHelper.TankSupportedShelf,
                ClutterType.AngledFences => PrefabHelper.AngledFences,
                ClutterType.HugeOrangePipes => PrefabHelper.HugeOrangePipes,
                ClutterType.PipesLongOpen => PrefabHelper.PipesLong,
                ClutterType.BrokenElectricalBox => PrefabHelper.BrokenElectricalBox,
                _ => throw new ArgumentOutOfRangeException(nameof(type), $"Unknown ClutterType: {type}")
            };
#pragma warning restore CS8603 // Possible null reference return.
        }

        /// <inheritdoc/>
        public override global::ThaumielMapEditor.API.Blocks.ServerObject? ServerObject(ObjectBase data)
            => new Server(data, this);

        /// <inheritdoc/>
        public override ClientObject? ClientObject(ObjectBase data)
        {
            GameObject? prefab;
            try
            {
                prefab = GetClutterPrefab(Type);
            }
            catch (ArgumentOutOfRangeException ex)
            {
                LogManager.Warn($"Skipping client clutter '{data.Name}': {ex.Message}");
                return null;
            }

            if (prefab == null)
            {
                LogManager.Warn($"Skipping client clutter '{data.Name}': clutter prefab for type '{Type}' is not registered.");
                return null;
            }

            uint assetId = PrefabHelper.GetAssetId(prefab);
            if (assetId == 0)
            {
                LogManager.Warn($"Skipping client clutter '{data.Name}': clutter prefab '{prefab.name}' has no NetworkIdentity.");
                return null;
            }

            return new Client(data, this)
            {
                AssetId = assetId,
            };
        }

        /// <summary>
        /// The server-side representation of a clutter object.
        /// </summary>
        public sealed class Server : global::ThaumielMapEditor.API.Blocks.ServerObject
        {
            private readonly ClutterDefinition definition;

            /// <summary>
            /// Initializes a new instance of the <see cref="Server"/> class.
            /// </summary>
            /// <param name="data">The shared object state.</param>
            /// <param name="definition">The definition that owns the serialized values.</param>
            public Server(ObjectBase data, ClutterDefinition definition)
            {
                CopyStateFrom(data);
                this.definition = definition;
            }

            /// <summary>
            /// Gets or sets the clutter type.
            /// </summary>
            public ClutterType Type
            {
                get => definition.Type;
                set
                {
                    if (definition.Type == value)
                        return;

                    definition.Type = value;
                    if (ClutterGameObject == null || Object == null)
                        return;

                    NetworkServer.Destroy(Object);
                    Respawn();
                }
            }

            /// <summary>
            /// Gets the underlying clutter <see cref="GameObject"/>.
            /// </summary>
            public GameObject? ClutterGameObject { get; private set; }

            /// <summary>
            /// Gets the <see cref="SpawnableClutterConnector"/> component, if the clutter type has one.
            /// </summary>
            public SpawnableClutterConnector? Base { get; private set; }

            /// <inheritdoc/>
            public override void SpawnObject(SchematicData schematic, SerializableObject serializable)
            {
                GameObject clutterPrefab = UnityEngine.Object.Instantiate(GetClutterPrefab(Type));
                NetworkServer.UnSpawn(clutterPrefab);
                ClutterGameObject = clutterPrefab;
                Object = ClutterGameObject;
                if (ClutterGameObject.TryGetComponent<SpawnableClutterConnector>(out var connector))
                    Base = connector;

                SetWorldTransform(schematic);
                clutterPrefab.transform.localScale = Scale;
                NetworkServer.Spawn(clutterPrefab);

                if (clutterPrefab.TryGetComponent<NetworkBehaviour>(out var network))
                    NetId = network.netId;

                base.SpawnObject(schematic, serializable);
            }

            /// <summary>
            /// Respawns this clutter in place with the current <see cref="Type"/> prefab.
            /// </summary>
            public void Respawn()
            {
                GameObject clutterPrefab = UnityEngine.Object.Instantiate(GetClutterPrefab(Type));
                NetworkServer.UnSpawn(clutterPrefab);
                ClutterGameObject = clutterPrefab;
                Object = clutterPrefab;
                Base = clutterPrefab.TryGetComponent<SpawnableClutterConnector>(out var connector) ? connector : null;
                clutterPrefab.transform.SetPositionAndRotation(Position, Rotation);
                clutterPrefab.transform.localScale = Scale;
                NetworkServer.Spawn(clutterPrefab);

                if (clutterPrefab.TryGetComponent<NetworkBehaviour>(out var network))
                    NetId = network.netId;
            }
        }

        /// <summary>
        /// The client-side representation of a clutter object.
        /// </summary>
        public sealed class Client : ClientObject
        {
            private readonly ClutterDefinition definition;

            /// <summary>
            /// Initializes a new instance of the <see cref="Client"/> class.
            /// </summary>
            /// <param name="data">The shared object state.</param>
            /// <param name="definition">The definition that owns the serialized values.</param>
            public Client(ObjectBase data, ClutterDefinition definition)
            {
                CopyStateFrom(data);
                this.definition = definition;
            }

            /// <summary>
            /// Gets or sets the clutter type.
            /// </summary>
            public ClutterType Type
            {
                get => definition.Type;
                set
                {
                    if (definition.Type == value)
                        return;

                    definition.Type = value;

                    if (Spawned)
                        Respawn();
                }
            }

            /// <inheritdoc/>
            public override int ParentId
            {
                get => base.ParentId;
                set
                {
                    if (base.ParentId == value)
                        return;

                    base.ParentId = value;

                    if (Spawned)
                        RespawnForAllPlayers();
                }
            }

            /// <inheritdoc/>
            public override ObjectType ObjectType => ObjectType.Clutter;

            /// <inheritdoc/>
            public override void SpawnForPlayer(Player player)
            {
                if (player == null || player.IsDestroyed || player.IsHost || player.IsDummy)
                    return;

                if (IsSpawnedForPlayer(player))
                    return;

                if (player.Connection == null || !player.Connection.isReady)
                    return;

                if (!Enum.IsDefined(typeof(ClutterType), Type))
                {
                    LogManager.Warn($"Skipping client clutter '{Name}': unknown ClutterType '{Type}'.");
                    return;
                }

                GameObject? prefab;
                try
                {
                    prefab = GetClutterPrefab(Type);
                }
                catch (ArgumentOutOfRangeException ex)
                {
                    LogManager.Warn($"Skipping client clutter '{Name}': {ex.Message}");
                    return;
                }

                if (prefab == null)
                {
                    LogManager.Warn($"Skipping client clutter '{Name}': clutter prefab for type '{Type}' is not registered.");
                    return;
                }

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
            /// Resolves the world space transform of this clutter object.
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
            /// Respawns this clutter object for the specified player.
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
                RefreshColliders();
                SpawnForPlayer(player);
            }

            /// <summary>
            /// Respawns this clutter object for all players it is spawned for.
            /// </summary>
            public void RespawnForAllPlayers()
            {
                foreach (Player player in SpawnedPlayers.ToArray())
                    RespawnForPlayer(player);
            }

            /// <summary>
            /// Respawns this clutter object for all players it is spawned for under a new netId.
            /// </summary>
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

            /// <summary>
            /// Recreates the server-side colliders of this clutter object.
            /// </summary>
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

            /// <inheritdoc/>
            public override void SetParent(int parentId)
            {
                UpdateParentReference(parentId);
                ParentId = parentId;
            }

            /// <inheritdoc/>
            public override void SetParent(NetworkBehaviour network)
            {
                UpdateParentReference((int)network.netId);
                ParentId = (int)network.netId;
            }

            /// <inheritdoc/>
            public override bool SetParent(GameObject gameObject)
            {
                if (!gameObject.TryGetComponent(out NetworkBehaviour network))
                    return false;

                UpdateParentReference((int)network.netId);
                ParentId = (int)network.netId;
                return true;
            }

            /// <inheritdoc/>
            public override void OnAddedToSchematic(SchematicData schematic)
            {
                ColliderHelper.CreateClientObjectColliders(this, schematic);
                ClearDirtyFlags();
                SyncManager.RemoveFromPending(this);
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
}
