// -----------------------------------------------------------------------
// <copyright file="LockerObject.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using MapGeneration.Distributors;
using Mirror;
using ThaumielMapEditor.API.Data;
using ThaumielMapEditor.API.Enums;
using ThaumielMapEditor.API.Extensions;
using ThaumielMapEditor.API.Helpers;
using ThaumielMapEditor.API.Serialization;
using UnityEngine;
using YamlDotNet.Serialization;
using LabLocker = LabApi.Features.Wrappers.Locker;
using MEC;

namespace ThaumielMapEditor.API.Blocks.ServerObjects.Lockers
{
    [GitBookPage("Blocks/Server/LockerObject")]
    public class LockerObject : ServerObject
    {
        private static readonly ItemType[] AllItemTypes = (ItemType[])Enum.GetValues(typeof(ItemType));

        /// <summary>
        /// The instantiated <see cref="Locker"/> component in the scene.
        /// </summary>
#pragma warning disable CS8618
        [YamlIgnore]
        public Locker Base { get; private set; }
#pragma warning restore CS8618

        /// <summary>
        /// Serialized chamber definitions that describe what items and permissions each chamber should have.
        /// </summary>
        public List<LockerChamber> Chambers { get; private set; } = [];

        /// <inheritdoc/>
        public override ObjectType ObjectType { get; set; } = ObjectType.Locker;

        /// <summary>
        /// The <see cref="LockerType"/> variant for this locker (e.g., Medkit, RifleRack, Misc).
        /// Setting this on a spawned locker respawns it in place with the new prefab
        /// </summary>
        [YamlMember(Alias = "LockerType")]
        public LockerType Type
        {
            get;
            set
            {
                if (field == value)
                    return;

                if (value == LockerType.None)
                    return;

                field = value;

                if (Base == null || Object == null)
                    return;

                NetworkServer.Destroy(Object);
                Respawn();
            }
        }

        /// <summary>
        /// Maps a <see cref="LockerType"/> value to the corresponding locker prefab from <see cref="PrefabHelper"/>.
        /// </summary>
        /// <param name="type">The locker type to get a prefab for.</param>
        /// <returns>The matching <see cref="Locker"/> prefab, or throws for unknown types.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when an unsupported <see cref="LockerType"/> is supplied.</exception>
        public Locker? GetPrefabFromType(LockerType type)
        {
            return type switch
            {
                LockerType.None => null,
                LockerType.Adrenaline => PrefabHelper.LockerAdrenalineMedkit,
                LockerType.ExperimentalWeapon => PrefabHelper.LockerExperimentalWeapon,
                LockerType.LargeGun => PrefabHelper.LockerLargeGun,
                LockerType.Medkit => PrefabHelper.LockerRegularMedkit,
                LockerType.Misc => PrefabHelper.LockerMisc,
                LockerType.Pedestal => PrefabHelper.Pedestal,
                LockerType.RifleRack => PrefabHelper.LockerRifleRack,
                _ => throw new ArgumentOutOfRangeException(nameof(type), $"Unknown LockerType: {type}")
            };
        }

        /// <inheritdoc/>
        public override void SpawnObject(SchematicData schematic, SerializableObject serializable)
        {
            Locker? prefab = GetPrefabFromType(Type);
            if (prefab is null)
            {
                LogManager.Warn($"Failed to get locker prefab.");
                return;
            }

            GameObject lockerobj = UnityEngine.Object.Instantiate(prefab.gameObject);
            if (!lockerobj.TryGetComponent<Locker>(out var locker))
            {
                LogManager.Warn($"Failed to get Locker component from prefab.");
                return;
            }

            Base = locker;
            Object = locker.gameObject;
            NetworkServer.UnSpawn(locker.gameObject);
            SetWorldTransform(schematic);
            Object.transform.localScale = Scale;
            locker.gameObject.name += " [TME Locker]";
            if (locker.TryGetComponent<StructurePositionSync>(out var posSync))
            {
                posSync.Network_position = Position;
                
                Base.transform.rotation = Quaternion.AngleAxis(Rotation.y, Vector3.up);
                posSync.Network_rotationY = (sbyte)Mathf.RoundToInt(Rotation.eulerAngles.y / 5.625f);
            }

            locker.ParentRoom = RoomExtensions.GetClosestRoomToPosition(Position)?.Base ?? null;
            NetworkServer.Spawn(locker.gameObject);
            Timing.CallDelayed(Timing.WaitForOneFrame, () =>
            {
                LabLocker labLocker = LabLocker.Get(locker);
                labLocker.ClearAllChambers();
                labLocker.ClearLockerLoot();
                locker._serverChambersFilled = true;
                PopulateChambers();
            });

            base.SpawnObject(schematic, serializable);
        }

        /// <summary>
        /// Respawns this locker in place with the current <see cref="Type"/> prefab.
        /// Called automatically when <see cref="Type"/> changes on an already spawned locker.
        /// </summary>
        public void Respawn()
        {
            Locker? prefab = GetPrefabFromType(Type);
            if (prefab is null)
            {
                LogManager.Warn($"Failed to get locker prefab for respawn (LockerType: {Type}).");
                return;
            }

            GameObject lockerobj = UnityEngine.Object.Instantiate(prefab.gameObject);
            if (!lockerobj.TryGetComponent<Locker>(out var locker))
            {
                LogManager.Warn($"Failed to get Locker component from prefab on respawn.");
                UnityEngine.Object.Destroy(lockerobj);
                return;
            }

            Base = locker;
            Object = locker.gameObject;
            NetworkServer.UnSpawn(locker.gameObject);
            Object.transform.SetPositionAndRotation(Position, Rotation);
            Object.transform.localScale = Scale;
            locker.gameObject.name += " [TME Locker]";
            if (locker.TryGetComponent<StructurePositionSync>(out var posSync))
            {
                posSync.Network_position = Position;
                Base.transform.rotation = Quaternion.AngleAxis(Rotation.y, Vector3.up);
                posSync.Network_rotationY = (sbyte)Mathf.RoundToInt(Rotation.eulerAngles.y / 5.625f);
            }

            locker.ParentRoom = RoomExtensions.GetClosestRoomToPosition(Position)?.Base ?? null;
            NetworkServer.Spawn(locker.gameObject);
            NetId = locker.netId;
            Timing.CallDelayed(Timing.WaitForOneFrame, () =>
            {
                if (Base == null)
                    return;

                LabLocker labLocker = LabLocker.Get(Base);
                labLocker.ClearAllChambers();
                labLocker.ClearLockerLoot();
                Base._serverChambersFilled = true;
                PopulateChambers();
            });
        }

        /// <summary>
        /// Populates the runtime locker chambers based on the serialized <see cref="Chambers"/> data.
        /// </summary>
        public void PopulateChambers()
        {
            if (Base == null || Base.Chambers == null)
                return;

            for (int i = 0; i < Base.Chambers.Length; i++)
            {
                MapGeneration.Distributors.LockerChamber chamber = Base.Chambers[i];
                chamber.AcceptableItems = AllItemTypes;

                foreach (LockerChamber lockerChamber in Chambers)
                {
                    if (lockerChamber.Index != i)
                        continue;

                    chamber.RequiredPermissions = lockerChamber.Permissions;
                    foreach (ChamberData chamberData in lockerChamber.Data)
                    {
                        if (UnityEngine.Random.Range(0f, 100f) > chamberData.SpawnPercent)
                            continue;

                        chamber.SpawnItem(chamberData.ItemType, chamberData.AmountToSpawn);
                        LogManager.Debug($"Spawned item in Chamber {i}");
                    }
                }
            }
        }

        /// <summary>
        /// Removes all items currently spawned in the locker chambers.
        /// </summary>
        public void ClearChambers()
        {
            if (Base == null)
                return;

            LabLocker labLocker = LabLocker.Get(Base);
            labLocker.ClearAllChambers();
            labLocker.ClearLockerLoot();
        }

        /// <summary>
        /// Gets the number of runtime chambers on the spawned locker prefab, or 0 when not spawned.
        /// </summary>
        public int ChamberCount => Base?.Chambers?.Length ?? 0;

        /// <summary>
        /// Gets the serialized chamber definition for the given index, or <see langword="null"/> when not configured.
        /// </summary>
        public LockerChamber? GetChamber(int index)
        {
            foreach (LockerChamber chamber in Chambers)
            {
                if (chamber.Index == (uint)index)
                    return chamber;
            }

            return null;
        }

        /// <summary>
        /// Gets the serialized chamber definition for the given index, creating it with defaults when missing.
        /// </summary>
        public LockerChamber GetOrCreateChamber(int index)
        {
            LockerChamber? existing = GetChamber(index);
            if (existing != null)
                return existing;

            LockerChamber created = new() { Index = (uint)index };
            Chambers.Add(created);
            return created;
        }

        /// <summary>
        /// Removes the serialized chamber definition for the given index.
        /// </summary>
        /// <returns><see langword="true"/> when an entry was removed.</returns>
        public bool RemoveChamber(int index) => Chambers.RemoveAll(c => c.Index == (uint)index) > 0;

        /// <summary>
        /// Clears runtime chambers and repopulates them from the serialized <see cref="Chambers"/> data.
        /// </summary>
        public void RefreshChambers()
        {
            ClearChambers();
            PopulateChambers();
        }
    }
}