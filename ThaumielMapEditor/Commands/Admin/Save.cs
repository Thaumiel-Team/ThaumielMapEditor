// -----------------------------------------------------------------------
// <copyright file="Save.cs" company="Thaumiel Team">
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
            StringBuilder sb = new();
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

            map.Room = player.Room;
            map.FileName = arguments.At(0);
            foreach (SchematicData schematic in Loader.SpawnedSchematics)
            {
                Vector3 pos = player.Room.LocalPosition(schematic.Position);
                map.Schematics.Add(new() { LocalPosition = pos, SchematicName = schematic.FileName});
            }

            Loader.SaveMap(map);
            response = $"Saved map {arguments.At(0)}.";
            return true;
        }
    }
}