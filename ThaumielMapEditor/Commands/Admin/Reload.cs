// -----------------------------------------------------------------------
// <copyright file="Reload.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using CommandSystem;
using System;
using ThaumielMapEditor.API.Attributes;
using ThaumielMapEditor.API.Helpers;
using ThaumielMapEditor.API.Interfaces;

namespace ThaumielMapEditor.Commands.Admin
{
#pragma warning disable CS1591
    [DoNotParse]
    public class Reload : ISubCommand
    {
        public override string Name => "reload";

        public override string Description => "Reloads all schematics";

        public override string[] Aliases => ["re"];

        public override string RequiredPermission => "tme.reload";

        public override bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            Loader.ReloadSchematics();
            response = "Reloaded.";
            return true;
        }
    }
}