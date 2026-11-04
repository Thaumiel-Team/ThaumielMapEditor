// -----------------------------------------------------------------------
// <copyright file="PlayerSpawnPoint.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU Lesser General Public License v3.0 (LGPL-3.0).
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

namespace ThaumielMapEditor.API.Blocks.Definitions
{
    [GitBookPage("Blocks/Definitions/PlayerSpawnPoint")]
    public sealed class PlayerSpawnPoint : ServerDefinition<PlayerSpawnPoint.Server>
    {
        /// <inheritdoc/>
        public override string Name => "PlayerSpawnPoint";

        /// <inheritdoc/>
        public override ObjectType ObjectType => ObjectType.PlayerSpawnPoint;

        [GitBookPage("Blocks/Definitions/PlayerSpawnPoint/Server")]
        public sealed class Server : ServerObject
        {
            public static List<Server> Instances { get; private set; } = [];

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

            /// <inheritdoc/>
            public override ObjectType ObjectType => ObjectType.PlayerSpawnPoint;

            /// <inheritdoc/>
            public override void SpawnObject(SchematicData schematic, SerializableObject serializable)
            {
                base.SpawnObject(schematic, serializable);
                SetWorldTransform(schematic);
                Object = new();
                Object.transform.SetPositionAndRotation(Position, Rotation);
                Object.transform.localScale = Scale;
                SelectionProxyHelper.AddTrigger(Object);
                Schematic = schematic;
                NetId = 0;
                Instances.Add(this);
            }

            /// <inheritdoc/>
            public override void DestroyObject(SchematicData schematic)
            {
                Instances.Remove(this);
                SelectionProxyHelper.DestroyProxy(this);
                base.DestroyObject(schematic);
            }

            internal static void OnPlayerSpawned(PlayerSpawnedEventArgs ev)
            {
                List<Server> validSpawns = [];
                float totalWeight = 0;
                foreach (Server spawn in Instances)
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

                foreach (Server spawn in validSpawns)
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
}