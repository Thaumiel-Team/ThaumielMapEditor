// -----------------------------------------------------------------------
// <copyright file="ConvertFile.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using System;
using CommandSystem;
using ThaumielMapEditor.API.Attributes;
using ThaumielMapEditor.API.Binary;
using ThaumielMapEditor.API.Helpers;
using ThaumielMapEditor.API.Interfaces;

namespace ThaumielMapEditor.Commands.Admin
{
#pragma warning disable CS1591
    [DoNotParse]
    public class ConvertFile : SubCommand
    {
        public override string Name => "convertfile";

        public override string VisibleArgs => "<Schematic|Map> <Name> <Bin|Yaml>";

        public override int RequiredArgsCount => 3;

        public override string Description => "Converts a schematic or map between the binary and YAML formats";

        public override string[] Aliases => ["cf"];

        public override string RequiredPermission => "tme.convertfile";

        public override bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            string directory = arguments.At(0).ToLowerInvariant() switch
            {
                "schematic" or "schematics" or "sc" => "Schematics",
                "map" or "maps" => "Maps",
                _ => string.Empty
            };

            if (string.IsNullOrEmpty(directory))
            {
                response = "The first argument must be 'schematic' or 'map'.";
                return false;
            }

            FileFormat? target = arguments.At(2).ToLowerInvariant() switch
            {
                "bin" or "binary" => FileFormat.Binary,
                "yml" or "yaml" => FileFormat.Yaml,
                _ => null
            };

            if (target == null)
            {
                response = "The third argument must be 'bin' or 'yaml'.";
                return false;
            }

            if (!FormatConverter.TryConvert(directory, arguments.At(1), target.Value, out response))
                return false;

            if (directory == "Schematics")
            {
                Loader.ReloadSchematics();
            }
            else
                Loader.ReloadMaps();

            return true;
        }
    }
}
