// -----------------------------------------------------------------------
// <copyright file="Update.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU Lesser General Public License v3.0 (LGPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using System;
using CommandSystem;
using ThaumielMapEditor.API.Helpers;
using ThaumielMapEditor.API.Helpers.Networking;

namespace ThaumielMapEditor.Commands.Console
{
    [CommandHandler(typeof(GameConsoleCommandHandler))]
    public class Update : ParentCommand
    {
        public override string Command => "tmeupdate";

        public override string[] Aliases => [];

        public override string Description => "Updates the Thaumiel Map Editor plugin to the latest version.";

        public override void LoadGeneratedCommands() { }

        protected override bool ExecuteParent(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            bool Prerelease = false;
            bool Force = false;

            if (arguments.Count > 0)
            {
                if (bool.TryParse(arguments.At(0), out var force))
                {
                    Force = force;
                }

                if (arguments.Count > 1 && bool.TryParse(arguments.At(1), out var prerelease))
                {
                    Prerelease = prerelease;
                }
            }

            MECHelper.TryRunCoroutine(Updater.UpdatePluginCoroutine(Force, Prerelease));
            response = "Request ran";
            return true;
        }
    }
}