// -----------------------------------------------------------------------
// <copyright file="TMELogs.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using System;
using CommandSystem;
using ThaumielMapEditor.API.Helpers;
using ThaumielMapEditor.API.Helpers.Networking;

namespace ThaumielMapEditor.Commands.Console
{
    [CommandHandler(typeof(GameConsoleCommandHandler))]
    public class TMELogs : ParentCommand
    {
        public override string Command => "tmelogs";

        public override string[] Aliases => ["tmelogsupload", "tmeupload"];

        public override string Description => "Uploads your logs to the TME API.";

        public override void LoadGeneratedCommands() { }

        protected override bool ExecuteParent(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            LogUploadRequest request = new();
            request.SendRequest();

            response = "Please wait for the log.";
            return true;
        }
    }
}