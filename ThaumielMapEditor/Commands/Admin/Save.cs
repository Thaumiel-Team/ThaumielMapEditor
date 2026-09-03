// -----------------------------------------------------------------------
// <copyright file="Save.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Linq;
using CommandSystem;
using LabApi.Features.Wrappers;
using ThaumielMapEditor.API.Attributes;
using ThaumielMapEditor.API.Data;
using ThaumielMapEditor.API.Extensions;
using ThaumielMapEditor.API.Helpers;
using ThaumielMapEditor.API.Interfaces;

namespace ThaumielMapEditor.Commands.Admin
{
#pragma warning disable CS1591
    [DoNotParse]
    public class Save : ISubCommand
    {
        public override string Name => "save";

        public override string VisibleArgs => "<Map Name>";

        public override int RequiredArgsCount => 1;

        public override string Description => "Saves the current spawned schematics into a map file";

        public override string[] Aliases => [""];

        public override string RequiredPermission => "tme.save";

        public override bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            if (arguments.Count < 1)
            {
                response = $"Wrong usage! Correct usage: tme {Name} {VisibleArgs}";
                return false;
            }

            MapData map = new();
            if (!Player.TryGet(sender, out var player))
            {
                response = "You must be a player to run this command!";
                return false;
            }
            
            if (player.Room == null)
            {
                response = $"You must be in a room to run this!";
                return false;
            }

            string fileName = arguments.At(0);
            string safeName = string.Concat(fileName.Split(System.IO.Path.GetInvalidFileNameChars()));
            if (string.IsNullOrWhiteSpace(safeName) || safeName.Contains(".."))
            {
                response = "Invalid map name. Avoid path separators, '..'.";
                return false;
            }

            map.Room = player.Room;
            map.FileName = safeName;
            foreach (SchematicData schematic in Loader.SpawnedSchematics.ToArray())
            {
                Vector3 pos = player.Room.LocalPosition(schematic.Position);
                map.Schematics.Add(new() { LocalPosition = pos, SchematicName = schematic.FileName});
            }

            Loader.SaveMap(map);
            response = $"Saved map {safeName}.";
            return true;
        }
    }
}