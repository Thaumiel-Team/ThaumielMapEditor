// -----------------------------------------------------------------------
// <copyright file="Coroutines.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using CommandSystem;
using MEC;
using ThaumielMapEditor.API.Helpers;
using ThaumielMapEditor.API.Interfaces;

namespace ThaumielMapEditor.Commands.Admin
{
    public class Coroutines : ISubCommand
    {
        public override string Name => "coroutines";

        public override string Description => "Lists all the coroutines running or ran";

        public override string[] Aliases => ["coro", "cor"];

        public override string RequiredPermission => "tme.coroutines";

        public override bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            StringBuilder sb = new();
            sb.AppendLine();
            sb.AppendLine($"Thaumiel Map Editor Coroutines:");
            foreach (KeyValuePair<string, CoroutineHandle> kvp in MECHelper.handles)
            {
                sb.AppendLine($"\t Id: '{kvp.Key}' - Running: '{kvp.Value.IsRunning}'");
            }

            response = sb.ToString();
            return true;
        }
    }
}