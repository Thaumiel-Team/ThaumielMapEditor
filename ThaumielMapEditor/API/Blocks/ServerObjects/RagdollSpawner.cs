// -----------------------------------------------------------------------
// <copyright file="RagdollSpawner.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using LabApi.Features.Wrappers;
using Mirror;
using PlayerRoles;
using PlayerStatsSystem;
using ThaumielMapEditor.API.Binary.Attributes;
using ThaumielMapEditor.API.Data;
using ThaumielMapEditor.API.Enums;
using ThaumielMapEditor.API.Helpers;
using ThaumielMapEditor.API.Serialization;
using UnityEngine;
using YamlDotNet.Serialization;

namespace ThaumielMapEditor.API.Blocks.ServerObjects
{
    [GitBookPage("Blocks/Server/RagdollSpawner")]
    public class RagdollSpawner : ServerObject
    {
        [YamlIgnore]
        public Ragdoll? Base { get; internal set; }

        [YamlMember(Alias = "RoleType")]
        [BinaryProperty(Name = "RoleType")]
        public RoleTypeId RoleType { get; set; }

        [YamlMember(Alias = "Chance")]
        [BinaryProperty(Name = "Chance")]
        public float SpawnChance { get; set; }

        [YamlMember(Alias = "DeathReason")]
        [BinaryProperty(Name = "DeathReason")]
        public string DeathReason { get; set; } = string.Empty;

        [YamlMember(Alias = "DollName")]
        [BinaryProperty(Name = "DollName")]
        public string DollName { get; set; } = string.Empty;

        public override ObjectType ObjectType => ObjectType.RagdollSpawner;

        public override void SpawnObject(SchematicData schematic, SerializableObject serializable)
        {
            SetWorldTransform(schematic);
            if (Random.Range(0f, 100f) > SpawnChance)
                return;

            CustomReasonDamageHandler handler = new(DeathReason);
            Base = Ragdoll.SpawnRagdoll(RoleType, Position, Rotation, handler, Name, Scale);
            Object = new();
            Object.transform.SetPositionAndRotation(Position, Rotation);
            Object.transform.localScale = Scale;
            SelectionProxyHelper.AddTrigger(Object);
            base.SpawnObject(schematic, serializable);
        }

        /// <inheritdoc/>
        public override void DestroyObject(SchematicData schematic)
        {
            Base?.Destroy();
            SelectionProxyHelper.DestroyProxy(this);
            base.DestroyObject(schematic);
        }
    }
}