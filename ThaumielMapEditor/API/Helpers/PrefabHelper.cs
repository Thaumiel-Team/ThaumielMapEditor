// -----------------------------------------------------------------------
// <copyright file="PrefabHelper.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using AdminToys;
using Interactables.Interobjects.DoorUtils;
using InventorySystem.Items.Firearms.Attachments;
using MapGeneration.Distributors;
using MapGeneration.RoomConnectors;
using Mirror;
using ThaumielMapEditor.API.Data;
using UnityEngine;

namespace ThaumielMapEditor.API.Helpers
{
    public class PrefabHelper
    {
        /// <summary>
        /// A list of all registered prefabs and their associated collider data.
        /// </summary>
        public static List<PrefabCollidersData> PrefabColliders = [];

        /// <summary>
        /// Indicates whether <see cref="RegisterPrefabs"/> has been called.
        /// </summary>
        public static bool RanRegister = false;

        public static PrimitiveObjectToy? PrimitiveObject { get; private set; }
        public static LightSourceToy? LightSource { get; private set; }
        public static DoorVariant? DoorLcz { get; private set; }
        public static DoorVariant? DoorHcz { get; private set; }
        public static DoorVariant? DoorEz { get; private set; }
        public static DoorVariant? DoorHeavyBulk { get; private set; }
        public static DoorVariant? DoorGate { get; private set; }
        public static WorkstationController? Workstation { get; private set; }
        public static InvisibleInteractableToy? Interactable { get; private set; }
        public static CapybaraToy? Capybara { get; private set; }
        public static TextToy? TextToy { get; private set; }
        public static WaypointToy? WaypointToy { get; private set; }
        public static ShootingTarget? ShootingTargetSport { get; private set; }
        public static ShootingTarget? ShootingTargetDBoy { get; private set; }
        public static ShootingTarget? ShootingTargetBinary { get; private set; }
        public static Locker? LockerLargeGun { get; private set; }
        public static Locker? LockerRifleRack { get; private set; }
        public static Locker? LockerMisc { get; private set; }
        public static Locker? LockerRegularMedkit { get; private set; }
        public static Locker? LockerAdrenalineMedkit { get; private set; }
        public static Locker? LockerExperimentalWeapon { get; private set; }
        public static Locker? Pedestal { get; private set; }
        public static Scp079CameraToy? CameraLcz { get; private set; }
        public static Scp079CameraToy? CameraHcz { get; private set; }
        public static Scp079CameraToy? CameraSz { get; private set; }
        public static Scp079CameraToy? CameraEzArm { get; private set; }
        public static Scp079CameraToy? CameraEz { get; private set; }
        public static GameObject? SimpleBoxes { get; private set; }
        public static GameObject? PipesShort { get; private set; }
        public static GameObject? BoxesLadder { get; private set; }
        public static GameObject? TankSupportedShelf { get; private set; }
        public static GameObject? AngledFences { get; private set; }
        public static GameObject? HugeOrangePipes { get; private set; }
        public static GameObject? PipesLong { get; private set; }
        public static GameObject? BrokenElectricalBox { get; private set; }

        public static void RegisterPrefabs()
        {
            if (RanRegister)
                return;

            foreach (GameObject prefab in NetworkClient.prefabs.Values)
            {
                string prefabName = prefab.name;
                PrefabColliders.Add(new PrefabCollidersData
                {
                    Prefab = prefab,
                    Colliders = ColliderData.ParseObjectColliders(prefab)
                });

                if (prefab.TryGetComponent<PrimitiveObjectToy>(out var primitiveObject))
                {
                    PrimitiveObject = primitiveObject;
                    continue;
                }

                if (prefab.TryGetComponent<LightSourceToy>(out var lightSource))
                {
                    LightSource = lightSource;
                    continue;
                }

                if (prefab.TryGetComponent<DoorVariant>(out var doorVariant))
                {
                    switch (prefabName)
                    {
                        case "LCZ BreakableDoor":
                            DoorLcz = doorVariant;
                            continue;
                            
                        case "HCZ BreakableDoor":
                            DoorHcz = doorVariant;
                            continue;

                        case "EZ BreakableDoor":
                            DoorEz = doorVariant;
                            continue;

                        case "HCZ BulkDoor":
                            DoorHeavyBulk = doorVariant;
                            continue;

                        case "Spawnable Unsecured Pryable GateDoor":
                            DoorGate = doorVariant;
                            continue;
                    }
                }

                if (prefab.TryGetComponent<WorkstationController>(out var workstation))
                {
                    Workstation = workstation;
                    continue;
                }

                if (prefab.TryGetComponent<InvisibleInteractableToy>(out var interactable))
                {
                    Interactable = interactable;
                    continue;
                }

                if (prefab.TryGetComponent<CapybaraToy>(out var capybara))
                {
                    Capybara = capybara;
                    continue;
                }

                if (prefab.TryGetComponent<TextToy>(out var texttoy))
                {
                    TextToy = texttoy;
                    continue;
                }

                if (prefab.GetComponent<SpawnableClutterConnector>())
                {
                    switch (prefabName)
                    {
                        case "Simple Boxes Open Connector":
                            SimpleBoxes = prefab;
                            continue;

                        case "Pipes Short Open Connector":
                            PipesShort = prefab;
                            continue;

                        case "Boxes Ladder Open Connector":
                            BoxesLadder = prefab;
                            continue;

                        case "Tank-Supported Shelf Open Connector":
                            TankSupportedShelf = prefab;
                            continue;

                        case "Angled Fences Open Connector":
                            AngledFences = prefab;
                            continue;

                        case "Huge Orange Pipes Open Connector":
                            HugeOrangePipes = prefab;
                            continue;

                        case "Pipes Long Open Connector":
                            PipesLong = prefab;
                            continue;
                    }
                }

                if (prefabName == "Broken Electrical Box Open Connector")
                {
                    BrokenElectricalBox = prefab;
                    continue;
                }

                if (prefab.TryGetComponent(out Scp079CameraToy cameraToy))
                {
                    switch (prefabName)
                    {
                        case "LczCameraToy":
                            CameraLcz = cameraToy;
                            continue;

                        case "HczCameraToy":
                            CameraHcz = cameraToy;
                            continue;

                        case "SzCameraToy":
                            CameraSz = cameraToy;
                            continue;

                        case "EzArmCameraToy":
                            CameraEzArm = cameraToy;
                            continue;

                        case "EzCameraToy":
                            CameraEz = cameraToy;
                            continue;
                    }
                }

                if (prefab.TryGetComponent(out WaypointToy waypointToy))
                {
                    WaypointToy = waypointToy;
                    continue;
                }

                if (prefab.TryGetComponent(out Locker locker))
                {
                    switch (prefabName)
                    {
                        case "LargeGunLockerStructure":
                            LockerLargeGun = locker;
                            continue;

                        case "RifleRackStructure":
                            LockerRifleRack = locker;
                            continue;

                        case "MiscLocker":
                            LockerMisc = locker;
                            continue;

                        case "RegularMedkitStructure":
                            LockerRegularMedkit = locker;
                            continue;
                            
                        case "AdrenalineMedkitStructure":
                            LockerAdrenalineMedkit = locker;
                            continue;

                        case "Scp500PedestalStructure Variant":
                            Pedestal = locker;
                            continue;

                        case "Experimental Weapon Locker":
                            LockerExperimentalWeapon = locker;
                            continue;
                    }
                }

                if (prefab.TryGetComponent(out ShootingTarget shootingTarget))
                {
                    switch (prefabName)
                    {
                        case "sportTargetPrefab":
                            ShootingTargetSport = shootingTarget;
                            continue;

                        case "dboyTargetPrefab":
                            ShootingTargetDBoy = shootingTarget;
                            continue;

                        case "binaryTargetPrefab":
                            ShootingTargetBinary = shootingTarget;
                            continue;
                    }
                }
            }

            RanRegister = true;
        }

        /// <summary>
        /// Gets the asset id of the specified <see cref="GameObject"/>.
        /// </summary>
        /// <param name="obj">The <see cref="GameObject"/> to get the AssetId from.</param>
        /// <returns>0 if no AssetId was found otherwise returns the AssetId.</returns>
        public static uint GetAssetId(GameObject obj)
        {
            if (!obj.TryGetComponent<NetworkBehaviour>(out var net))
            {
                LogManager.Warn($"Failed to get AssetId. Obj has no NetworkBehaviour component");
                return 0;
            }

            return net.netIdentity.assetId;
        }
    }
}