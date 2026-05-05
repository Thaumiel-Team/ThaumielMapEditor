// -----------------------------------------------------------------------
// <copyright file="Destroy.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using CommandSystem;
using ThaumielMapEditor.API.Attributes;
using ThaumielMapEditor.API.Data;
using ThaumielMapEditor.API.Helpers;
using ThaumielMapEditor.API.Interfaces;

namespace ThaumielMapEditor.Commands.Admin
{
#pragma warning disable CS1591
    [DoNotParse]
    public class Destroy : ISubCommand
    {
        public override string Name => "destroy";

        public override string VisibleArgs => "<Schematic Id>";

        public override int RequiredArgsCount => 1;

        public override string Description => "Destroys the specified schematic";

        public override string[] Aliases => ["de", "delete", "remove", "del"];

        public override string RequiredPermission => "tme.destroy";

        public override bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            uint count = 0;
            if (!uint.TryParse(arguments.At(0), out var id))
            {
                response = "Invalid uint.";
                return false;
            }

            if (!Loader.SchematicsById.TryGetValue(id, out var data))
            {
                StringBuilder sb = new();
                sb.AppendLine();
                sb.AppendLine($"No schematic with id {arguments.At(0)} was found.");
                sb.AppendLine($"Available schematics:");
                foreach (KeyValuePair<uint, SchematicData> kvp in Loader.SchematicsById)
                    sb.AppendLine($"- [{kvp.Key}]: {kvp.Value.FileName}");

                response = sb.ToString();
                return false;
            }

            Loader.DestroySchematic(data);
            response = $"Destroyed schematic {arguments.At(0)} for {count} players";
            return true;
        }
    }
}