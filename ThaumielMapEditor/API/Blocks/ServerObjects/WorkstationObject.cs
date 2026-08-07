// -----------------------------------------------------------------------
// <copyright file="WorkstationObject.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using InventorySystem.Items.Firearms.Attachments;
using MapGeneration.Distributors;
using Mirror;
using PlayerRoles;
using System.Collections.Generic;
using ThaumielMapEditor.API.Data;
using ThaumielMapEditor.API.Enums;
using ThaumielMapEditor.API.Helpers;
using ThaumielMapEditor.API.Serialization;
using UnityEngine;
using YamlDotNet.Serialization;

namespace ThaumielMapEditor.API.Blocks.ServerObjects
{
    public class WorkstationObject : ServerObject
    {
        [YamlIgnore]
        public static Dictionary<WorkstationController, WorkstationObject> WorkstationCache = [];

        /// <summary>
        /// The instantiated <see cref="WorkstationController"/> that backs this server object.
        /// It will be null until <see cref="SpawnObject"/> successfully instantiates the prefab.
        /// </summary>
        [YamlIgnore]
        public WorkstationController? Base { get; private set; }

        /// <summary>
        /// Gets or sets the <see cref="RoleTypeId"/>s that are allowed to use this <see cref="WorkstationObject"/> instance.
        /// </summary>
        [YamlMember(Alias = "AllowedRoles")]
        public List<RoleTypeId> AllowedRoles { get; set; } = [];

        /// <summary>
        /// Gets or sets whether players can use this <see cref="WorkstationObject"/> instance.
        /// </summary>
        [YamlMember(Alias = "AllowInteractions")]
        public bool AllowInteractions { get; set; }

        /// <inheritdoc/>
        public override ObjectType ObjectType { get; set; } = ObjectType.Workstation;

        /// <inheritdoc/>
        public override void SpawnObject(SchematicData schematic, SerializableObject serializable)
        {
            if (PrefabHelper.Workstation == null)
            {
                LogManager.Warn($"Workstation prefab is null!");
                return;
            }

            WorkstationController workstationPrefab = UnityEngine.Object.Instantiate(PrefabHelper.Workstation);
            NetworkServer.UnSpawn(workstationPrefab.gameObject);
            Base = workstationPrefab;
            Object = Base.gameObject;

            workstationPrefab.NetworkStatus = (byte)(AllowInteractions ? 0 : 4);
            SetWorldTransform(schematic);

            if (workstationPrefab.TryGetComponent(out StructurePositionSync structurePositionSync))
            {
                structurePositionSync.Network_position = workstationPrefab.transform.position;
                structurePositionSync.Network_rotationY = (sbyte)Mathf.RoundToInt(workstationPrefab.transform.rotation.eulerAngles.y / 5.625f);
            }

            NetworkServer.Spawn(workstationPrefab.gameObject);
            NetId = Base.netId;
            WorkstationCache.Add(workstationPrefab, this);
            base.SpawnObject(schematic, serializable);
        }

        public override void DestroyObject(SchematicData schematic)
        {
            WorkstationCache.Remove(Base!);
            base.DestroyObject(schematic);
        }
    }
}