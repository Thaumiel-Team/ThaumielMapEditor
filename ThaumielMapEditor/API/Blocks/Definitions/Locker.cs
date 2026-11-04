// -----------------------------------------------------------------------
// <copyright file="Locker.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU Lesser General Public License v3.0 (LGPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using MapGeneration.Distributors;
using Mirror;
using ThaumielMapEditor.API.Binary.Attributes;
using ThaumielMapEditor.API.Data;
using ThaumielMapEditor.API.Enums;
using ThaumielMapEditor.API.Extensions;
using ThaumielMapEditor.API.Helpers;
using ThaumielMapEditor.API.Serialization;
using UnityEngine;
using YamlDotNet.Serialization;
using LabLocker = LabApi.Features.Wrappers.Locker;
using MEC;
using GameLocker = MapGeneration.Distributors.Locker;
using ConfigLockerChamber = ThaumielMapEditor.API.Blocks.Lockers.LockerChamber;
using ThaumielMapEditor.API.Blocks.Lockers;

namespace ThaumielMapEditor.API.Blocks.Definitions
{
    [GitBookPage("Blocks/Definitions/Locker")]
    public sealed class Locker : ServerDefinition<Locker.Server>
    {
        /// <inheritdoc/>
        public override string Name => "Locker";

        /// <inheritdoc/>
        public override ObjectType ObjectType => ObjectType.Locker;

        [GitBookPage("Blocks/Definitions/Locker/Server")]
        public sealed class Server : ServerObject
        {
            private static readonly ItemType[] AllItemTypes = (ItemType[])Enum.GetValues(typeof(ItemType));

            /// <summary>
            /// The instantiated <see cref="GameLocker"/> component in the scene.
            /// </summary>
            #pragma warning disable CS8618
            [YamlIgnore]
            public GameLocker Base { get; private set; }
            #pragma warning restore CS8618

            /// <summary>
            /// Serialized chamber definitions that describe what items and permissions each chamber should have.
            /// </summary>
            public List<ConfigLockerChamber> Chambers { get; private set; } = [];

            /// <inheritdoc/>
            public override ObjectType ObjectType { get; set; } = ObjectType.Locker;

            /// <summary>
            /// The <see cref="LockerType"/> variant for this locker.
            /// Setting this on a spawned locker respawns it in place with the new prefab
            /// </summary>
            [YamlMember(Alias = "LockerType")]
            [BinaryProperty(Name = "LockerType")]
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
            /// <returns>The matching <see cref="GameLocker"/> prefab, or throws for unknown types.</returns>
            /// <exception cref="ArgumentOutOfRangeException">Thrown when an unsupported <see cref="LockerType"/> is supplied.</exception>
            public GameLocker? GetPrefabFromType(LockerType type)
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

            public bool TryGetChamber(Chambers value, out ConfigLockerChamber? chamber)
            {
                chamber = GetChamber(value);
                return chamber != null;
            }

            public ConfigLockerChamber? GetChamber(Chambers value)
            {
                foreach (ConfigLockerChamber chamber in Chambers)
                {
                    if (chamber.Index != (int)value)
                        continue;

                    return chamber;
                }
                
                return null;
            }

            private void ParseChambers()
            {
                Chambers.Clear();
                uint index = 0;
                foreach (MapGeneration.Distributors.LockerChamber basechamber in Base.Chambers)
                {
                    ConfigLockerChamber chamber = new()
                    {
                        Base = basechamber,
                        Index = index
                    };
                
                    index++;
                    Chambers.Add(chamber);
                }
            }

            /// <inheritdoc/>
            public override void SpawnObject(SchematicData schematic, SerializableObject serializable)
            {
                GameLocker? prefab = GetPrefabFromType(Type);
                if (prefab is null)
                {
                    LogManager.Warn($"Failed to get locker prefab.");
                    return;
                }

                GameLocker lockerobj = UnityEngine.Object.Instantiate(prefab);

                Base = lockerobj;
                Object = lockerobj.gameObject;
                NetworkServer.UnSpawn(lockerobj.gameObject);
                SetWorldTransform(schematic);
                Object.transform.localScale = Scale;
                lockerobj.gameObject.name += " [TME Locker]";

                if (lockerobj.TryGetComponent<StructurePositionSync>(out var posSync))
                {
                    posSync.Network_position = Position;
                    Base.transform.rotation = Quaternion.AngleAxis(Rotation.y, Vector3.up);
                    posSync.Network_rotationY = (sbyte)Mathf.RoundToInt(Rotation.eulerAngles.y / 5.625f);
                }

                lockerobj.ParentRoom = RoomExtensions.GetClosestRoomToPosition(Position)?.Base ?? null;
                ParseChambers();
                NetworkServer.Spawn(lockerobj.gameObject);
                Timing.CallDelayed(Timing.WaitForOneFrame, () =>
                {
                    LabLocker labLocker = LabLocker.Get(lockerobj);
                    labLocker.ClearAllChambers();
                    labLocker.ClearLockerLoot();
                    lockerobj._serverChambersFilled = true;
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
                GameLocker? prefab = GetPrefabFromType(Type);
                if (prefab is null)
                {
                    LogManager.Warn($"Failed to get locker prefab for respawn (LockerType: {Type}).");
                    return;
                }

                GameLocker lockerobj = UnityEngine.Object.Instantiate(prefab);
                Base = lockerobj;
                Object = lockerobj.gameObject;
                NetworkServer.UnSpawn(lockerobj.gameObject);
                Object.transform.SetPositionAndRotation(Position, Rotation);
                Object.transform.localScale = Scale;
                lockerobj.gameObject.name += " [TME Locker]";

                if (lockerobj.TryGetComponent<StructurePositionSync>(out var posSync))
                {
                    posSync.Network_position = Position;
                    Base.transform.rotation = Quaternion.AngleAxis(Rotation.y, Vector3.up);
                    posSync.Network_rotationY = (sbyte)Mathf.RoundToInt(Rotation.eulerAngles.y / 5.625f);
                }

                lockerobj.ParentRoom = RoomExtensions.GetClosestRoomToPosition(Position)?.Base ?? null;
                ParseChambers();
                NetworkServer.Spawn(lockerobj.gameObject);
                NetId = lockerobj.netId;
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
                    foreach (ConfigLockerChamber lockerChamber in Chambers)
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
            /// Removes the serialized chamber definition for the given index.
            /// </summary>
            /// <returns><see langword="true"/> when an entry was removed.</returns>
            public bool RemoveChamber(int index) =>
                Chambers.RemoveAll(c => c.Index == (uint)index) > 0;

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
}