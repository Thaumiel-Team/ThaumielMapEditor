// -----------------------------------------------------------------------
// <copyright file="Rotation.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Text;
using CommandSystem;
using ThaumielMapEditor.API.Attributes;
using ThaumielMapEditor.API.Blocks;
using ThaumielMapEditor.API.Data;
using ThaumielMapEditor.API.Interfaces;

namespace ThaumielMapEditor.Commands.Admin.ModifySubCommands
{
#pragma warning disable CS1591
    [DoNotParse]
    public class Rotation : ISubCommand
    {
        public override string Name => "rotate";

        public override string VisibleArgs => "<X>, <Y>, <Z>";

        public override int RequiredArgsCount => 3;

        public override string Description => "Rotates the specified schematic by the specified value.";

        public override string[] Aliases => ["rot", "rotate"];

        public override string RequiredPermission => "tme.modify.rotate";

        public override bool SubCommandExecute(ArraySegment<string> arguments, ICommandSender sender, SchematicData schematic, StringBuilder sb, out string response)
        {
            if (!float.TryParse(arguments.At(0), out var x))
            {
                response = "Failed to parse X coordinate. Make sure its a number";
                return false;
            }

            if (!float.TryParse(arguments.At(1), out var y))
            {
                response = "Failed to parse Y coordinate. Make sure its a number";
                return false;
            }

            if (!float.TryParse(arguments.At(2), out var z))
            {
                response = "Failed to parse Z coordinate. Make sure its a number";
                return false;
            }

            Quaternion rotation = Quaternion.Euler(new(x, y, z));
            Quaternion prevrot = schematic.Primitive!.Rotation;
            schematic.Rotation = rotation;

            foreach (ServerObject serverObject in schematic.SpawnedServerObjects)
            {
                serverObject.UpdateObject(schematic);
            }

            response = $"Rotated schematic {schematic.FileName} to {rotation} from {prevrot}";
            return true;
        }
    }
}