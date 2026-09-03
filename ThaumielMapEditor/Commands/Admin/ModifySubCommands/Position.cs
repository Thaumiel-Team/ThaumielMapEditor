// -----------------------------------------------------------------------
// <copyright file="Position.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Globalization;
using System.Linq;
using System.Text;
using CommandSystem;
using ThaumielMapEditor.API.Blocks;
using ThaumielMapEditor.API.Data;
using ThaumielMapEditor.API.Interfaces;

namespace ThaumielMapEditor.Commands.Admin.ModifySubCommands
{
    public class Position : ISubCommand
    {
        public override string Name => "position";

        public override string VisibleArgs => "<Get|Set>, [X], [Y], [Z]";

        public override int RequiredArgsCount => 1;

        public override string Description => "Gets or sets the position of a schematic";

        public override string RequiredPermission => "tme.modify.position";

        public override bool SubCommandExecute(ArraySegment<string> arguments, ICommandSender sender, SchematicData data, StringBuilder sb, out string response)
        {
            if (arguments.Count < 1)
            {
                response = $"Wrong usage! Correct usage: tme modify position {VisibleArgs}";
                return false;
            }

            string subCommand = arguments.At(0).ToLower();
            switch (subCommand)
            {
                case "get":
                    sb.AppendLine($"Got Schematic Position:");
                    sb.AppendLine($"- X: {data.Position.x}");
                    sb.AppendLine($"- Y: {data.Position.y}");
                    sb.AppendLine($"- Z: {data.Position.z}");
                    break;

                case "set":
                    if (arguments.Count < 4)
                    {
                        response = $"Wrong usage! Correct usage: tme modify position set <X> <Y> <Z>";
                        return false;
                    }

                    if (!float.TryParse(arguments.At(1), NumberStyles.Float, CultureInfo.InvariantCulture, out float x) || float.IsNaN(x) || float.IsInfinity(x))
                    {
                        response = "Failed to parse X coordinate. Make sure its a number.";
                        return false;
                    }

                    if (!float.TryParse(arguments.At(2), NumberStyles.Float, CultureInfo.InvariantCulture, out float y) || float.IsNaN(y) || float.IsInfinity(y))
                    {
                        response = "Failed to parse Y coordinate. Make sure its a number.";
                        return false;
                    }

                    if (!float.TryParse(arguments.At(3), NumberStyles.Float, CultureInfo.InvariantCulture, out float z) || float.IsNaN(z) || float.IsInfinity(z))
                    {
                        response = "Failed to parse Z coordinate. Make sure its a number.";
                        return false;
                    }

                    data.Position = new(x, y, z);

                    foreach (ServerObject serverObject in data.SpawnedServerObjects.ToArray())
                    {
                        serverObject.UpdateObject(data, false);
                    }

                    SyncManager.FlushServer();
                    
                    sb.AppendLine($"Set Schematic Position:");
                    sb.AppendLine($"- X: {data.Position.x}");
                    sb.AppendLine($"- Y: {data.Position.y}");
                    sb.AppendLine($"- Z: {data.Position.z}");
                    break;

                default:
                    response = "You are required to specify 'Get' or 'Set'";
                    return false;
            }

            response = sb.ToString();
            return true;
        }
    }
}