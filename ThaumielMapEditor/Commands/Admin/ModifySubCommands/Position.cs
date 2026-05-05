// -----------------------------------------------------------------------
// <copyright file="Position.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using System;
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
                    if (!float.TryParse(arguments.At(1), out float x))
                    {
                        response = "Failed to parse X coordinate. Make sure its a number.";
                        return false;
                    }

                    if (!float.TryParse(arguments.At(2), out float y))
                    {
                        response = "Failed to parse Y coordinate. Make sure its a number.";
                        return false;
                    }

                    if (!float.TryParse(arguments.At(3), out float z))
                    {
                        response = "Failed to parse Z coordinate. Make sure its a number.";
                        return false;
                    }

                    data.Position = new(x, y, z);

                    foreach (ServerObject serverObject in data.SpawnedServerObjects)
                    {
                        serverObject.UpdateObject(data);
                    }
                    
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