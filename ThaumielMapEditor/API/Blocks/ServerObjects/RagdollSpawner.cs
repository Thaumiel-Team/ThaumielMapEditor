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
using ThaumielMapEditor.API.Extensions;
using ThaumielMapEditor.API.Helpers;
using ThaumielMapEditor.API.Serialization;
using UnityEngine;

namespace ThaumielMapEditor.API.Blocks.ServerObjects
{
    public class RagdollSpawner : ServerObject
    {
        /// <summary>
        /// Gets or sets the base <see cref="Ragdoll"/> instance associated with this spawner.
        /// </summary>
        public Ragdoll? Base { get; internal set; }

        /// <summary>
        /// Gets or sets the <see cref="RoleTypeId"/> used to determine the ragdoll's appearance.
        /// </summary>
        public RoleTypeId RoleType { get; set; }

        /// <summary>
        /// Gets or sets the chance (0-100) that this ragdoll will spawn.
        /// </summary>
        public float SpawnChance { get; set; }

        /// <summary>
        /// Gets or sets the death reason displayed on the ragdoll.
        /// </summary>
        public string DeathReason { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the name displayed on the ragdoll.
        /// </summary>
        public string DollName { get; set; } = string.Empty;

        /// <inheritdoc/>
        public override ObjectType ObjectType => ObjectType.RagdollSpawner;

        /// <inheritdoc/>
        public override void SpawnObject(SchematicData schematic, SerializableObject serializable)
        {
            ParseValues(serializable);
            SetWorldTransform(schematic);
            if (Random.Range(0f, 100f) > SpawnChance)
                return;

            CustomReasonDamageHandler handler = new(DeathReason);
            Base = Ragdoll.SpawnRagdoll(RoleType, Position, Rotation, handler, Name, Scale);
            Object = Base?.Base.gameObject;
            base.SpawnObject(schematic, serializable);
        }

        private void ParseValues(SerializableObject serializable)
        {
            if (serializable.ObjectType != ObjectType)
            {
                LogManager.Warn($"Tried to parse {serializable.ObjectType} as Ragdoll Spawner");
                return;
            }

            if (!serializable.Values.TryConvertValue<float>("Chance", out var chance))
            {
                LogManager.Warn("Failed to parse SpawnChance");
            }
            if (!serializable.Values.TryConvertValue<RoleTypeId>("RoleType", out var role))
            {
                LogManager.Warn("Failed to parse RoleType");
            }
            if (!serializable.Values.TryConvertValue<string>("DeathReason", out var reason))
            {
                LogManager.Warn("Failed to parse DeathReason");
            }
            if (!serializable.Values.TryConvertValue<string>("DollName", out var name))
            {
                LogManager.Warn("Failed to parse DollName");
            }

            RoleType = role;
            SpawnChance = chance;
            DeathReason = reason;
            DollName = name;
        }
    }
}