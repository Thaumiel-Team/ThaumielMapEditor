// -----------------------------------------------------------------------
// <copyright file="Workstation.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU Lesser General Public License v3.0 (LGPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using InventorySystem.Items.Firearms.Attachments;
using MapGeneration.Distributors;
using Mirror;
using PlayerRoles;
using System.Collections.Generic;
using ThaumielMapEditor.API.Binary.Attributes;
using ThaumielMapEditor.API.Data;
using ThaumielMapEditor.API.Enums;
using ThaumielMapEditor.API.Helpers;
using ThaumielMapEditor.API.Serialization;
using UnityEngine;
using YamlDotNet.Serialization;

namespace ThaumielMapEditor.API.Blocks.Definitions
{
    [GitBookPage("Blocks/Definitions/Workstation")]
    public sealed class Workstation : ServerDefinition<Workstation.Server>
    {
        /// <inheritdoc/>
        public override string Name => "Workstation";

        /// <inheritdoc/>
        public override ObjectType ObjectType => ObjectType.Workstation;

        public sealed class Server : ServerObject
        {
            [YamlIgnore, BinaryIgnore]
            public static Dictionary<WorkstationController, Server> WorkstationCache = [];

            /// <summary>
            /// The instantiated <see cref="WorkstationController"/> that backs this server object.
            /// It will be null until <see cref="SpawnObject"/> successfully instantiates the prefab.
            /// </summary>
            [YamlIgnore]
            public WorkstationController? Base { get; private set; }

            /// <summary>
            /// Gets or sets the <see cref="RoleTypeId"/>s that are allowed to use this <see cref="Server"/> instance.
            /// </summary>
            [YamlMember(Alias = "AllowedRoles")]
            [BinaryProperty(Name = "AllowedRoles")]
            public List<RoleTypeId> AllowedRoles { get; set; } = [];

            /// <summary>
            /// Gets or sets whether players can use this <see cref="Server"/> instance.
            /// </summary>
            [YamlMember(Alias = "AllowInteractions")]
            [BinaryProperty(Name = "AllowInteractions")]
            public bool AllowInteractions
            {
                get;
                set
                {
                    Base?.NetworkStatus = (byte)(value ? 0 : 4);
                    field = value;
                }
            }

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
                Object.transform.localScale = Scale;
                
                if (workstationPrefab.TryGetComponent(out StructurePositionSync structurePositionSync))
                {
                    structurePositionSync.Network_position = workstationPrefab.transform.position;
                    Base.transform.rotation = Quaternion.AngleAxis(Rotation.y, Vector3.up);
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
}