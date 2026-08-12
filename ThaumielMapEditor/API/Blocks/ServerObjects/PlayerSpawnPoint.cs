// -----------------------------------------------------------------------
// <copyright file="PlayerSpawnPoint.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using LabApi.Events.Arguments.PlayerEvents;
using MEC;
using PlayerRoles;
using ThaumielMapEditor.API.Data;
using ThaumielMapEditor.API.Enums;
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
            List<PlayerSpawnPoint> validSpawns = [];
            float totalWeight = 0;
            foreach (PlayerSpawnPoint spawn in Instances)
            {
                if (spawn.Disabled || !spawn.AllowedRoles.Contains(ev.Role.RoleTypeId))
                    continue;

                validSpawns.Add(spawn);
                totalWeight += spawn.Chance;
            }

            if (validSpawns.Count == 0 || totalWeight <= 0)
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

        public bool HasFlagFast(DisableFlags flag) => (Disable & flag) != 0;
    }
}