// -----------------------------------------------------------------------
// <copyright file="DoorObject.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using Interactables.Interobjects;
using Interactables.Interobjects.DoorUtils;
using MapGeneration.RoomConnectors;
using MEC;
using Mirror;
using System;
using ThaumielMapEditor.API.Data;
using ThaumielMapEditor.API.Enums;
using ThaumielMapEditor.API.Helpers;
using ThaumielMapEditor.API.Serialization;
using ThaumielMapEditor.Events.EventArgs.Handlers;
using UnityEngine;
using YamlDotNet.Serialization;

namespace ThaumielMapEditor.API.Blocks.ServerObjects
{
    public class DoorObject : ServerObject
    {
        /// <summary>
        /// Returns the <see cref="DoorVariant"/> prefab that corresponds to the given <see cref="DoorType"/>.
        /// </summary>
        /// <param name="type">The door type to look up.</param>
        /// <returns>The matching <see cref="DoorVariant"/> prefab, or <c>null</c> if not found.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="type"/> does not match any known door type.
        /// </exception>
        public DoorVariant? GetDoorFromType(DoorType type)
        {
            return type switch
            {
                DoorType.Lcz => PrefabHelper.DoorLcz,
                DoorType.Hcz => PrefabHelper.DoorHcz,
                DoorType.Ez => PrefabHelper.DoorEz,
                DoorType.Gate => PrefabHelper.DoorGate,
                DoorType.BulkHead => PrefabHelper.DoorHeavyBulk,
                _ => throw new ArgumentOutOfRangeException(nameof(type), $"Unknown door type: {type}")
            };
        }

        /// <summary>
        /// Reference to the underlying game <see cref="DoorVariant"/> object.
        /// </summary>
        [YamlIgnore]
        public DoorVariant? Base { get; internal set; }

        /// <summary>
        /// The visual and functional type of this door.
        /// </summary>
        [YamlMember(Alias = "DoorType")]
        public DoorType DoorType { get; set; }

        /// <summary>
        /// The keycard permission flags required to interact with this door.
        /// Syncs to the live door object when changed after spawning.
        /// </summary>
        [YamlMember(Alias = "Permissions")]
        public DoorPermissionFlags Permissions
        {
            get;
            set
            {
                if (field == value)
                    return;

                Base?.RequiredPermissions = new(value, RequireAllPermissions, Bypass2176);
                field = value;
            }
        }

        /// <summary>
        /// If <see langword="true"/>, the player must hold <b>all</b> listed permissions rather than just one.
        /// Syncs to the live door object when changed after spawning.
        /// </summary>
        [YamlMember(Alias = "RequireAllPermissions")]
        public bool RequireAllPermissions
        {
            get;
            set
            {
                if (field == value)
                    return;

                Base?.RequiredPermissions = new(Permissions, value, Bypass2176);
                field = value;
            }
        }

        /// <summary>
        /// If <see langword="true"/>, SCP-2176 can bypass this door's permissions.
        /// Syncs to the live door object when changed after spawning.
        /// </summary>
        [YamlMember(Alias = "Bypass2176")]
        public bool Bypass2176
        {
            get;
            set
            {
                if (field == value)
                    return;

                Base?.RequiredPermissions = new(Permissions, RequireAllPermissions, value);
                field = value;
            }
        }

        /// <summary>
        /// The maximum health of this door. Only applies to doors that extend <see cref="BreakableDoor"/>.
        /// Ignored for non-breakable door types.
        /// Syncs to the live door object when changed after spawning.
        /// </summary>
        [YamlMember(Alias = "MaxHealth")]
        public float MaxHealth
        {
            get;
            set
            {
                if (field == value || Base == null || Base is not BreakableDoor breakable)
                    return;

                breakable.MaxHealth = value;
                field = value;
            }
        }

        /// <summary>
        /// The current remaining health of this door. Only applies to doors that extend <see cref="BreakableDoor"/>.
        /// Ignored for non-breakable door types.
        /// Syncs to the live door object when changed after spawning.
        /// </summary>
        [YamlMember(Alias = "Health")]
        public float Health
        {
            get;
            set
            {
                if (field == value || Base == null || Base is not BreakableDoor breakable)
                    return;

                breakable.RemainingHealth = value;
                field = value;
            }
        }

        /// <summary>
        /// Whether the door is currently open.
        /// </summary>
        [YamlMember(Alias = "IsOpen")]
        public bool IsOpen
        {
            get;
            set
            {
                if (field == value)
                    return;

                Base?.NetworkTargetState = value;
                field = value;
            }
        }

        /// <summary>
        /// Whether the door is locked via admin command.
        /// Syncs to the live door object when changed after spawning.
        /// </summary>
        [YamlMember(Alias = "IsLocked")]
        public bool IsLocked
        {
            get;
            set
            {
                if (field == value)
                    return;

                Base?.ServerChangeLock(DoorLockReason.SpecialDoorFeature, value);
                field = value;
            }
        }

        /// <summary>
        /// The object type identifier for this server object. Always <see cref="ObjectType.Door"/>.
        /// </summary>
        public override ObjectType ObjectType { get; set; } = ObjectType.Door;

        /// <inheritdoc/>
        public override void SpawnObject(SchematicData schematic, SerializableObject serializable)
        {
            Base = GetDoorFromType(DoorType);
            if (Base == null)
            {
                LogManager.Warn($"Failed to get DoorVariant from {schematic.FileName}, DoorType: {DoorType}");
                return;
            }

            DoorVariant doorPrefab = UnityEngine.Object.Instantiate(Base);
            NetworkServer.UnSpawn(doorPrefab.gameObject);
            if (doorPrefab.TryGetComponent<WallableSmallNodeRoomConnector>(out var con) && DoorType == DoorType.Hcz)
                con.Network_syncBitmask = 3;

            if (doorPrefab.TryGetComponent(out DoorRandomInitialStateExtension doorRandomInitialStateExtension))
                UnityEngine.Object.Destroy(doorRandomInitialStateExtension);

            Object = doorPrefab.gameObject;
            SetWorldTransform(schematic);
            ApplyProperties(doorPrefab);
            NetworkServer.Spawn(Object);
            NetId = doorPrefab.netId;

            base.SpawnObject(schematic, serializable);
        }

        public void SpawnObject(SchematicData schematic)
        {
            Base = GetDoorFromType(DoorType);
            if (Base == null)
            {
                LogManager.Warn($"Failed to get DoorVariant from {schematic.FileName}, DoorType: {DoorType}");
                return;
            }

            DoorVariant doorPrefab = UnityEngine.Object.Instantiate(Base);
            NetworkServer.UnSpawn(doorPrefab.gameObject);
            if (doorPrefab.TryGetComponent<WallableSmallNodeRoomConnector>(out var con) && DoorType == DoorType.Hcz)
                con.Network_syncBitmask = 3;

            if (doorPrefab.TryGetComponent(out DoorRandomInitialStateExtension doorRandomInitialStateExtension))
                UnityEngine.Object.Destroy(doorRandomInitialStateExtension);

            Object = doorPrefab.gameObject;
            SetWorldTransform(schematic);
            ApplyProperties(doorPrefab);
            NetworkServer.Spawn(Object);
            NetId = doorPrefab.netId;

            ObjectHandler.OnServerObjectSpawned(new(this));
            SpawnedObjects.Add(this);
            schematic.SpawnedServerObjects.Add(this);
        }

        /// <summary>
        /// Applies all current property values to the given door prefab <see cref="GameObject"/>.
        /// This includes health, lock state, open state, and permissions.
        /// Called internally during <see cref="SpawnObject(SchematicData, SerializableObject)"/>.
        /// </summary>
        /// <param name="door">The <see cref="DoorVariant"/> to apply properties to.</param>
        public void ApplyProperties(DoorVariant door)
        {
            if (door is BreakableDoor breakable)
            {
                breakable.MaxHealth = MaxHealth;
                breakable.RemainingHealth = Health;
            }

            door.NetworkTargetState = IsOpen;
            door.ServerChangeLock(DoorLockReason.SpecialDoorFeature, IsLocked);
            door.RequiredPermissions = new(Permissions, RequireAllPermissions, Bypass2176);

            Timing.CallDelayed(Timing.WaitForOneFrame, () =>
            {
                IsOpen = !IsOpen;
                Timing.CallDelayed(Timing.WaitForOneFrame, () =>
                {
                    IsOpen = !IsOpen;
                });
            });
        }
    }
}