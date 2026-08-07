// -----------------------------------------------------------------------
// <copyright file="RagdollSpawner.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using LabApi.Features.Wrappers;
using PlayerRoles;
using PlayerStatsSystem;
using ThaumielMapEditor.API.Data;
using ThaumielMapEditor.API.Enums;
using ThaumielMapEditor.API.Serialization;
using UnityEngine;
using YamlDotNet.Serialization;

namespace ThaumielMapEditor.API.Blocks.ServerObjects
{
    public class RagdollSpawner : ServerObject
    {
        [YamlIgnore]
        public Ragdoll? Base { get; internal set; }

        [YamlMember(Alias = "RoleType")]
        public RoleTypeId RoleType { get; set; }

        [YamlMember(Alias = "Chance")]
        public float SpawnChance { get; set; }

        [YamlMember(Alias = "DeathReason")]
        public string DeathReason { get; set; } = string.Empty;

        [YamlMember(Alias = "DollName")]
        public string DollName { get; set; } = string.Empty;

        public override ObjectType ObjectType => ObjectType.RagdollSpawner;

        public override void SpawnObject(SchematicData schematic, SerializableObject serializable)
        {
            SetWorldTransform(schematic);
            if (Random.Range(0f, 100f) > SpawnChance)
                return;

            CustomReasonDamageHandler handler = new(DeathReason);
            Base = Ragdoll.SpawnRagdoll(RoleType, Position, Rotation, handler, Name, Scale);
            Object = Base?.Base.gameObject;
            base.SpawnObject(schematic, serializable);
        }
    }
}