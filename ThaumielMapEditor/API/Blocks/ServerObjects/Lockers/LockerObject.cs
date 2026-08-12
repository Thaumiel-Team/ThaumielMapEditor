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
        /// </summary>
        [YamlMember(Alias = "LockerType")]
        public LockerType Type { get; private set; }

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
            locker.gameObject.name += " [TME Locker]";
            if (locker.TryGetComponent<StructurePositionSync>(out var posSync))
            {
                posSync.Network_position = Position;
                
                Base.transform.rotation = Quaternion.AngleAxis(Rotation.y, Vector3.up)
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
        /// Populates the runtime locker chambers based on the serialized <see cref="Chambers"/> data.
        /// For each serialized chamber:
        /// - Matches it to the corresponding runtime chamber by index.
        /// - Applies required permissions.
        /// - Iterates configured <see cref="ChamberData"/> entries and spawns items based on their spawn chance and amounts.
        /// Ensures each serialized chamber only spawns once per population pass.
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
    }
}