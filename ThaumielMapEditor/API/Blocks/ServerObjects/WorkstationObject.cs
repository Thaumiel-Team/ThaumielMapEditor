// -----------------------------------------------------------------------
// <copyright file="WorkstationObject.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using InventorySystem.Items.Firearms.Attachments;
using LabApi.Features.Wrappers;
using MapGeneration.Distributors;
using Mirror;
using PlayerRoles;
using System.Collections.Generic;
using ThaumielMapEditor.API.Data;
using ThaumielMapEditor.API.Enums;
using ThaumielMapEditor.API.Extensions;
using ThaumielMapEditor.API.Helpers;
using ThaumielMapEditor.API.Serialization;
using UnityEngine;
using YamlDotNet.Serialization;

namespace ThaumielMapEditor.API.Blocks.ServerObjects
{
    public class WorkstationObject : ServerObject
    {
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
        public List<RoleTypeId> AllowedRoles { get; set; } = [];

        /// <summary>
        /// Gets or sets whether players can use this <see cref="WorkstationObject"/> instance.
        /// </summary>
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

            ParseValues(serializable);
            WorkstationController workstationPrefab = UnityEngine.Object.Instantiate(PrefabHelper.Workstation);
            NetworkServer.UnSpawn(workstationPrefab.gameObject);
            Base = workstationPrefab;
            Object = Base.gameObject;
            NetId = Base.netId;

            workstationPrefab.NetworkStatus = (byte)(AllowInteractions ? 0 : 4);
            SetWorldTransform(schematic);

            if (workstationPrefab.TryGetComponent(out StructurePositionSync structurePositionSync))
            {
                structurePositionSync.Network_position = workstationPrefab.transform.position;
                structurePositionSync.Network_rotationY = (sbyte)Mathf.RoundToInt(workstationPrefab.transform.rotation.eulerAngles.y / 5.625f);
            }

            NetworkServer.Spawn(workstationPrefab.gameObject);
            WorkstationCache.Add(workstationPrefab, this);
            base.SpawnObject(schematic, serializable);
        }

        public override void DestroyObject(SchematicData schematic)
        {
            WorkstationCache.Remove(Base!);
            base.DestroyObject(schematic);
        }

        public void ParseValues(SerializableObject serializable)
        {
            if (serializable.ObjectType != ObjectType.Workstation)
            {
                LogManager.Warn($"Tried to parse {serializable.ObjectType} as TextToy");
                return;
            }

            if (!serializable.Values.TryConvertValue<List<RoleTypeId>>("AllowedRoles", out var roles))
            {
                LogManager.Warn("Failed to parse AllowedRoles");
            }

            if (!serializable.Values.TryConvertValue<bool>("AllowInteractions", out var allow))
            {
                LogManager.Warn("Failed to parse AllowInteractions");
            }

            AllowedRoles = roles;
            AllowInteractions = allow;
        }
    }
}