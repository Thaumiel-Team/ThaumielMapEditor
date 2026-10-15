// -----------------------------------------------------------------------
// <copyright file="Destroy.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CommandSystem;
using ThaumielMapEditor.API.Data;
using ThaumielMapEditor.API.Helpers;
using ThaumielMapEditor.API.Interfaces;

namespace ThaumielMapEditor.Commands.Admin
{
#pragma warning disable CS1591
    [DoNotParse]
    public class Destroy : SubCommand
    {
        public override string Name => "destroy";

        public override string VisibleArgs => "<Schematic | Map> <Map Name | Schematic Id>";

        public override int RequiredArgsCount => 2;

        public override string Description => "Destroys the specified schematic or map";

        public override string[] Aliases => ["de", "delete", "remove", "del"];

        public override string RequiredPermission => "tme.destroy";

        public override bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            string type = arguments.At(0);
            string idstr = arguments.At(1);

            if (arguments.Count < 2)
            {
                response = "Invalid ID. Run 'tme spawned' to get all spawned schematics.";
                return false;
            }

            if (type.Equals("schematic", StringComparison.InvariantCultureIgnoreCase))
            {
                if (!uint.TryParse(idstr, out var id))
                {
                    response = $"Failed to parse {idstr} to a integer.";
                    return false;
                }

                if (!Loader.SchematicsById.TryGetValue(id, out var data))
                {
                    StringBuilder sb = new();
                    sb.AppendLine();
                    sb.AppendLine($"No schematic with id {arguments.At(0)} was found.");
                    sb.AppendLine($"Available schematics:");
                    foreach (KeyValuePair<uint, SchematicData> kvp in Loader.SchematicsById.ToArray())
                        sb.AppendLine($"- [{kvp.Key}]: {kvp.Value.FileName}");

                    response = sb.ToString();
                    return false;
                }

                Loader.DestroySchematic(data);
            }
            else if (type.Equals("map", StringComparison.InvariantCultureIgnoreCase))
            {
                MapData? map = Loader.SpawnedMaps.FirstOrDefault(m => m.FileName.Equals(idstr, StringComparison.InvariantCultureIgnoreCase));
                if (map == null)
                {
                    StringBuilder sb = new();
                    sb.AppendLine();
                    sb.AppendLine($"Failed to find map with name {idstr}.");
                    sb.AppendLine($"Available maps:");
                    foreach (MapData data in Loader.SpawnedMaps.ToArray())
                        sb.AppendLine($"- [{data.Id}]: {data.FileName}");

                    response = sb.ToString();
                    response = $"Failed to find map with name {idstr}";
                    return false;
                }

                Loader.DestroyMap(map);
                response = $"Destroyed map with name {idstr}";
                return true;
            }

            response = $"Please specify type to destroy. Use 'Map' or 'Schematic'";
            return true;
        }
    }
}