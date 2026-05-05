// -----------------------------------------------------------------------
// <copyright file="Parent.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using CommandSystem;
using LabApi.Features.Permissions;
using ThaumielMapEditor.API.Attributes;
using ThaumielMapEditor.API.Helpers;
using ThaumielMapEditor.API.Interfaces;
using ThaumielMapEditor.Commands.Admin;

namespace ThaumielMapEditor.Commands
{
#pragma warning disable CS1591
    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    [DoNotParse]
    public class Parent : ParentCommand
    {
        public Parent() => LoadGeneratedCommands();

        public override string Command => "thaumielmapeditor";

        public override string[] Aliases => ["tme"];

        public override string Description => "Manage the features of Thaumiel Map Editor";

        public override void LoadGeneratedCommands()
        {
            Subcommands.Add(new Save());
            Subcommands.Add(new Modify());
            Subcommands.Add(new Spawned());
            Subcommands.Add(new Destroy());
            Subcommands.Add(new Spawn());
            Subcommands.Add(new List());
            Subcommands.Add(new Reload());
            Subcommands.Add(new Grab());
            Subcommands.Add(new Admin.Convert());
            Subcommands.Add(new Coroutines());
        }

        private List<ISubCommand> Subcommands { get; } = [];

        protected override bool ExecuteParent(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            try
            {
                if (arguments.Count == 0)
                {
                    response = $"Thaumiel Map Editor v{Main.Instance.Version} by Mr. Baguetter\n\nAvailable commands:";
                    foreach (ISubCommand command in Subcommands)
                        response += $"\n- tme {command.Name}{(command.VisibleArgs != string.Empty ? $" {command.VisibleArgs}" : "")} - {command.Description}";

                    return true;
                }

                ISubCommand cmd = Subcommands.FirstOrDefault(cmd => cmd.Name == arguments.At(0));
                cmd ??= Subcommands.FirstOrDefault(cmd => cmd.Aliases.Contains(arguments.At(0)));

                if (cmd == null)
                {
                    response = "Command not found!";
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

                ArraySegment<string> args = new(arguments.Array!, arguments.Offset + 1, arguments.Count - 1);
                return cmd.Execute(args, sender, out response);
            }
            catch (Exception ex)
            {
                LogManager.Error($"Error when running a command: {ex}");
            }

            response = "An error has occured please check your console.";
            return false;
        }
    }
}