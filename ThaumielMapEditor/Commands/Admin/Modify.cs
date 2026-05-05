// -----------------------------------------------------------------------
// <copyright file="Modify.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CommandSystem;
using LabApi.Features.Permissions;
using LabApi.Features.Wrappers;
using ThaumielMapEditor.API.Data;
using ThaumielMapEditor.API.Helpers;
using ThaumielMapEditor.API.Interfaces;
using ThaumielMapEditor.Commands.Admin.ModifySubCommands;

namespace ThaumielMapEditor.Commands.Admin
{
    public class Modify : ISubCommand
    {
        public Modify() => PopulateSubCommands();

        public override string Name => "modify";

        public override string RequiredPermission => "tme.modify";

        public override string Description => "Modifies the specified values in the specified schematic";

        public override string VisibleArgs => "";

        public override int RequiredArgsCount => 1;

        public override string[] Aliases => ["mod"];

        public override void PopulateSubCommands()
        {
            SubCommands.Add(new Position());
            SubCommands.Add(new Rotation());
            SubCommands.Add(new Scale());
        }

        public override bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            StringBuilder sb = new();
            sb.AppendLine();
            SchematicData? data;
            bool hasId = uint.TryParse(arguments.At(1), out uint id);

            ISubCommand? cmd = SubCommands.FirstOrDefault(cmd => cmd.Name == arguments.At(0)) ?? SubCommands.FirstOrDefault(cmd => cmd.Aliases.Contains(arguments.At(0)));
            if (cmd == null)
            {
                response = $"SubCommand not found! Valid SubCommands: {string.Join("\n", SubCommands)} - {SubCommands.Count}";
                return false;
            }

            if (!sender.HasPermissions(cmd.RequiredPermission))
            {
                response = $"You don't have permission to access that command! Requited permission: {cmd.RequiredPermission}";
                return false;
            }

            if (arguments.Count < cmd.RequiredArgsCount)
            {
                response = $"Wrong usage! Correct usage: tme {cmd.Name} {cmd.VisibleArgs}";
                return false;
            }

            if (hasId)
            {
                if (!Loader.SchematicsById.TryGetValue(id, out data) || data.Primitive == null)
                {
                    sb.AppendLine($"No schematic with id {id} was found.");
                    sb.AppendLine($"Available schematics:");

                    foreach (KeyValuePair<uint, SchematicData> kvp in Loader.SchematicsById)
                    {
                        sb.AppendLine($"- [{kvp.Key}]: {kvp.Value.FileName}");
                    }

                    response = sb.ToString();
                    return false;
                }
            }
            else
            {
                if (!Player.TryGet(sender, out var player))
                {
                    response = "Failed to parse player. Use the version with a Schematic ID instead.";
                    return false;
                }

                data = CommandHelper.GetSchematic(player);
                if (data == null)
                {
                    response = "Failed to find schematic via raycast. Make sure you are looking at one.";
                    return false;
                }
            }

            ArraySegment<string> args = new(arguments.Array!, arguments.Offset + 2, arguments.Count - 2);
            return cmd.SubCommandExecute(args, sender, data, sb, out response);
        }
    }
}