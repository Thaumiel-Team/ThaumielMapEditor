// -----------------------------------------------------------------------
// <copyright file="Spawn.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Text;
using CommandSystem;
using LabApi.Features.Wrappers;
using ThaumielMapEditor.API.Attributes;
using ThaumielMapEditor.API.Data;
using ThaumielMapEditor.API.Helpers;
using ThaumielMapEditor.API.Interfaces;
using ThaumielMapEditor.API.Serialization;
using UnityEngine;

namespace ThaumielMapEditor.Commands.Admin
{
#pragma warning disable CS1591
    [DoNotParse]
    public class Spawn : ISubCommand
    {
        public static readonly CachedLayerMask RayMask = new("Default", "Door", "CCTV");

        public override string Name => "spawn";

        public override string VisibleArgs => "<Schematic name>, <X>, <Y>, <Z>";

        public override int RequiredArgsCount => 1;

        public override string Description => "Spawns the named Schematic";

        public override string[] Aliases => ["sp", "create", "cr"];

        public override string RequiredPermission => "tme.spawn";

        public override bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            if (!Loader.LoadedSchematics.TryGetValue(arguments.At(0), out SerializableSchematic schematic))
            {
                response = $"Schematic '{arguments.At(0)}' not found.";
                return false;
            }

            Vector3 position = new();

            if (arguments.Count == 4)
            {
                if (!float.TryParse(arguments.At(1), out var x))
                {
                    response = $"Failed to parse X value. Invalid float: {arguments.At(1)}";
                    return false;
                }
                if (!float.TryParse(arguments.At(2), out var y))
                {
                    response = $"Failed to parse Y value. Invalid float: {arguments.At(2)}";
                    return false;
                }
                if (!float.TryParse(arguments.At(3), out var z))
                {
                    response = $"Failed to parse Z value. Invalid float: {arguments.At(3)}";
                    return false;
                }

                position = new(x, y, z);
            }
            else
            {
                if (!Player.TryGet(sender, out var player))
                {
                    response = "Failed to get Player.";
                    return false;
                }

                if (!Physics.Raycast(player.Camera.transform.position + player.Camera.forward, player.Camera.forward, out var hit, 50, RayMask))
                {
                    response = "Failed to get placement position from raycast.";
                    return false;
                }

                position = hit.point;
            }

            SchematicData data = Loader.SpawnSchematic(schematic, position);
            StringBuilder sb = new();
            sb.AppendLine();
            sb.AppendLine($"Spawning schematic '{schematic.FileName}'...");
            sb.AppendLine($"- Id: {data.Id}");
            sb.AppendLine($"- Position: {position}");
            sb.AppendLine($"- Scale: {schematic.Scale}");
            sb.AppendLine($"- Objects queued: {schematic.Objects.Count}");

            response = sb.ToString();
            return true;
        }
    }
}