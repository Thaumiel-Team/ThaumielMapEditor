// -----------------------------------------------------------------------
// <copyright file="List.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using CommandSystem;
using ThaumielMapEditor.API.Attributes;
using ThaumielMapEditor.API.Helpers;
using ThaumielMapEditor.API.Interfaces;
using ThaumielMapEditor.API.Serialization;

namespace ThaumielMapEditor.Commands.Admin
{
#pragma warning disable CS1591
    [DoNotParse]
    public class List : ISubCommand
    {
        public override string Name => "list";

        public override string Description => "Lists all schematics";

        public override string[] Aliases => ["li"];

        public override string RequiredPermission => "tme.list";

        public override bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            StringBuilder sb = new();
            sb.AppendLine();
            foreach (KeyValuePair<string, SerializableSchematic> kvp in Loader.LoadedSchematics)
                sb.AppendLine($"- {kvp.Key}");

            response = sb.ToString();
            return true;
        }
    }
}