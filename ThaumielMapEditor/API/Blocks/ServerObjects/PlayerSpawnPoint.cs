// -----------------------------------------------------------------------
// <copyright file="PlayerSpawnPoint.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using LabApi.Events.Arguments.PlayerEvents;
using MEC;
using PlayerRoles;
using ThaumielMapEditor.API.Data;
using ThaumielMapEditor.API.Enums;
using ThaumielMapEditor.API.Extensions;
using ThaumielMapEditor.API.Helpers;
using ThaumielMapEditor.API.Serialization;
using UnityEngine;

namespace ThaumielMapEditor.API.Blocks.ServerObjects
{
    public class PlayerSpawnPoint : ServerObject
    {
        public static List<PlayerSpawnPoint> Instances { get; private set; } = [];

        /// <summary>
        /// The collection that defines the list of <see cref="RoleTypeId"/>s allowed to spawn here.
        /// </summary>
        public List<RoleTypeId> AllowedRoles { get; set; } = [];

        /// <summary>
        /// The percent chance of spawning here. (0 - 100)
        /// </summary>
        public float Chance { get; set; }

        /// <summary>
        /// The flags that specify if this will be disabled.
        /// </summary>
        public DisableFlags Disable { get; set; }

        public bool Disabled { get; set; }

#pragma warning disable CS8618
        /// <summary>
        /// The <see cref="SchematicData"/> this <see cref="PlayerSpawnPoint"/> was spawned from.
        /// </summary>
        public SchematicData Schematic;
#pragma warning restore CS8618
        /// <inheritdoc/>
        public override ObjectType ObjectType => ObjectType.PlayerSpawnPoint;

        /// <inheritdoc/>
        public override void SpawnObject(SchematicData schematic, SerializableObject serializable)
        {
            base.SpawnObject(schematic, serializable);
            SetWorldTransform(schematic);
            Schematic = schematic;
            NetId = 0;
            Instances.Add(this);
        }

        /// <inheritdoc/>
        public override void DestroyObject(SchematicData schematic)
        {
            Instances.Remove(this);
            base.DestroyObject(schematic);
        }

        internal static void OnPlayerSpawned(PlayerSpawnedEventArgs ev)
        {
            IEnumerable<PlayerSpawnPoint> validSpawns = Instances.Where(p => p.AllowedRoles.Contains(ev.Role.RoleTypeId) && !p.Disabled);
            if (validSpawns.IsEmpty())
                return;

            float totalWeight = 0;
            foreach (PlayerSpawnPoint spawn in validSpawns)
            {
                totalWeight += spawn.Chance;
            }

            if (totalWeight <= 0)
                return;

            float roll = Random.Range(0f, totalWeight);
            float cumulativeSearch = 0;

            foreach (PlayerSpawnPoint spawn in validSpawns)
            {
                cumulativeSearch += spawn.Chance;
                if (roll <= cumulativeSearch)
                {
                    Timing.CallDelayed(Timing.WaitForOneFrame, () => ev.Player.Position = spawn.Position);
                    LogManager.Debug($"Spawned {ev.Player.Nickname} at a weighted point. Roll: {roll}/{totalWeight}");
                    if (spawn.HasFlagFast(DisableFlags.Used))
                        spawn.Disabled = true;
                        
                    return;
                }
            }
        }

        private void ParseValues(SerializableObject serializable)
        {
            if (serializable.ObjectType != ObjectType.PlayerSpawnPoint)
            {
                LogManager.Warn($"Tried to parse {serializable.ObjectType} as Player Spawn Point");
                return;
            }

            if (serializable.Values.TryConvertValue<List<RoleTypeId>>("AllowedRoles", out var roles))
                AllowedRoles = roles;

            if (serializable.Values.TryConvertValue<float>("Chance", out var chance))
                Chance = chance;

            if (serializable.Values.TryConvertValue<DisableFlags>("DisableFlags", out var flags))
                Disable = flags;
        }

        public bool HasFlagFast(DisableFlags flag) => (Disable & flag) != 0;
    }
}