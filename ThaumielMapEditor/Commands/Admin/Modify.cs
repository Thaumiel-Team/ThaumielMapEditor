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
            SubCommands.Clear();
            SubCommands.Add(new Position());
            SubCommands.Add(new Rotation());
            SubCommands.Add(new Scale());
        }

        public override bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            StringBuilder sb = new();
            sb.AppendLine();
            SchematicData? data;

            if (arguments.Count < 1)
            {
                response = $"Wrong usage! Valid subcommands: {string.Join(", ", SubCommands.Select(c => c.Name))}";
                return false;
            }

            string invoked = arguments.At(0);
            ISubCommand? cmd = SubCommands.FirstOrDefault(c => string.Equals(c.Name, invoked, StringComparison.OrdinalIgnoreCase)) ?? SubCommands.FirstOrDefault(c => c.Aliases.Any(a => string.Equals(a, invoked, StringComparison.OrdinalIgnoreCase)));
            if (cmd == null)
            {
                response = $"SubCommand not found! Valid SubCommands: {string.Join("\n", SubCommands)} - {SubCommands.Count}";
                return false;
            }

            if (!sender.HasPermissions(cmd.RequiredPermission))
            {
                response = $"You don't have permission to access that command! Required permission: {cmd.RequiredPermission}";
                return false;
            }

            if (arguments.Count - 1 < cmd.RequiredArgsCount)
            {
                response = $"Wrong usage! Correct usage: tme modify {cmd.Name} {cmd.VisibleArgs}";
                return false;
            }

            uint id = 0;
            bool hasId = arguments.Count >= 2 && uint.TryParse(arguments.At(1), out id);

            if (hasId)
            {
                if (!Loader.SchematicsById.TryGetValue(id, out data) || data.Primitive == null)
                {
                    sb.AppendLine($"No schematic with id {id} was found.");
                    sb.AppendLine($"Available schematics:");

                    foreach (KeyValuePair<uint, SchematicData> kvp in Loader.SchematicsById.ToArray())
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

            int offset = hasId ? 2 : 1;
            ArraySegment<string> args = offset >= arguments.Count ? new ArraySegment<string>([], 0, 0) : new(arguments.Array!, arguments.Offset + offset, arguments.Count - offset);
            return cmd.SubCommandExecute(args, sender, data, sb, out response);
        }
    }
}