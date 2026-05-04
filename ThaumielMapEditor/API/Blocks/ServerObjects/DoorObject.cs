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
using ThaumielMapEditor.API.Extensions;
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
        public DoorType DoorType { get; set; }

        /// <summary>
        /// The keycard permission flags required to interact with this door.
        /// Syncs to the live door object when changed after spawning.
        /// </summary>
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
        /// Silently ignored for non-breakable door types.
        /// Syncs to the live door object when changed after spawning.
        /// </summary>
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
        /// Silently ignored for non-breakable door types.
        /// Syncs to the live door object when changed after spawning.
        /// </summary>
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
            NetId = Base.netId;
            SetWorldTransform(schematic);
            doorPrefab.transform.SetPositionAndRotation(Position, Rotation);
            ApplyProperties(doorPrefab);
            NetworkServer.Spawn(Object);

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
            NetId = Base.netId;
            SetWorldTransform(schematic);
            doorPrefab.transform.SetPositionAndRotation(Position, Rotation);
            ApplyProperties(doorPrefab);
            NetworkServer.Spawn(Object);

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

        /// <summary>
        /// Returns a string representation of this door object's current state.
        /// </summary>
        /// <returns>A formatted string listing all key property values.</returns>
        public override string ToString()
        {
            return $"DoorType: {DoorType}, Permissions: {Permissions}, RequireAllPermissions: {RequireAllPermissions}, Bypass2176: {Bypass2176}, MaxHealth: {MaxHealth}, Health: {Health}, IsOpen: {IsOpen}, IsLocked: {IsLocked}";
        }

        /// <summary>
        /// Reads and parses all door properties from the provided <see cref="SerializableObject"/>.
        /// Logs a warning and returns early if the object type is incorrect or any field fails to parse.
        /// </summary>
        /// <param name="serializable">
        /// The serializable object containing a <c>Values</c> dictionary with door property data.
        /// Expected keys: <c>DoorType</c>, <c>Permissions</c>, <c>RequireAllPermissions</c>,
        /// <c>Bypass2176</c>, <c>MaxHealth</c>, <c>Health</c>, <c>IsOpen</c>, <c>IsLocked</c>.
        /// </param>
        public void ParseValues(SerializableObject serializable)
        {
            if (serializable.ObjectType != ObjectType.Door)
            {
                LogManager.Warn($"Tried to parse {serializable.ObjectType} as Door.");
                return;
            }

            if (!serializable.Values.TryConvertValue<DoorType>("DoorType", out var doorType))
            {
                LogManager.Warn("Failed to parse DoorType");
            }

            if (!serializable.Values.TryConvertValue<DoorPermissionFlags>("Permissions", out var permissions))
            {
                LogManager.Warn("Failed to parse Permissions");
            }

            if (!serializable.Values.TryConvertValue<bool>("RequireAllPermissions", out var requireAllPermissions))
            {
                LogManager.Warn("Failed to parse RequireAllPermissions");
            }

            if (!serializable.Values.TryConvertValue<bool>("Bypass2176", out var bypass2176))
            {
                LogManager.Warn("Failed to parse Bypass2176");
            }

            if (!serializable.Values.TryConvertValue<float>("MaxHealth", out var maxHealth))
            {
                LogManager.Warn("Failed to parse MaxHealth");
            }

            if (!serializable.Values.TryConvertValue<float>("Health", out var health))
            {
                LogManager.Warn("Failed to parse Health");
            }

            if (!serializable.Values.TryConvertValue<bool>("IsOpen", out var isOpen))
            {
                LogManager.Warn("Failed to parse IsOpen");
            }

            if (!serializable.Values.TryConvertValue<bool>("IsLocked", out var isLocked))
            {
                LogManager.Warn("Failed to parse IsLocked");
            }

            DoorType = doorType;
            Permissions = permissions;
            RequireAllPermissions = requireAllPermissions;
            Bypass2176 = bypass2176;
            MaxHealth = maxHealth;
            Health = health;
            IsOpen = isOpen;
            IsLocked = isLocked;
        }
    }
}