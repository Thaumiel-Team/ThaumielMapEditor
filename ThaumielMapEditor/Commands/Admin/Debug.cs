// -----------------------------------------------------------------------
// <copyright file="Debug.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Linq;
using CommandSystem;
using DrawableLine;
using ThaumielMapEditor.API.Attributes;
using ThaumielMapEditor.API.Blocks;
using ThaumielMapEditor.API.Blocks.ServerObjects;
using ThaumielMapEditor.API.Components.Tools;
using ThaumielMapEditor.API.Data;
using ThaumielMapEditor.API.Helpers;
using ThaumielMapEditor.API.Interfaces;

namespace ThaumielMapEditor.Commands.Admin
{
    [DoNotParse]
    public class Debug : ISubCommand
    {
        public override string Name => "debug";
        public override string VisibleArgs => "";
        public override string Description => "Generates drawable lines for each object.";
        public override string[] Aliases => ["deb", "draw"];
        public override string RequiredPermission => "tme.debug";

        public override bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            string arg1 = arguments.At(0);
            if (!string.IsNullOrEmpty(arg1))
            {
                if (uint.TryParse(arg1, out var id) && Loader.TryGetSchematicById(id, out var schematic))
                {
                    DrawableLines.IsDebugModeEnabled = true;
                    ParseSchematic(schematic);
                    response = $"Parsed schematic {schematic.FileName}";
                    return true;
                }

                if (bool.TryParse(arg1, out var result))
                {
                    DrawableLines.IsDebugModeEnabled = result;
                    response = $"Set DrawableLines.IsDebugModeEnabled to {result}";
                    return true;
                }
            }

            foreach (SchematicData schematic in Loader.SpawnedSchematics)
            {
                ParseSchematic(schematic);
            }

            response = "Generated lines";
            return true;
        }

        private void ParseSchematic(SchematicData schematic)
        {
            if (!schematic.LODZones.IsEmpty())
                LODHelper.DrawLines(schematic);

            foreach (TeleporterObject teleporter in schematic.GetServerObject<TeleporterObject>())
            {
                teleporter.DrawLines();
            }

            foreach (ServerObject serverObject in schematic.SpawnedServerObjects)
            {
                if (serverObject is InteractionObject interaction)
                    interaction.DrawLines();

                foreach (ColliderTrigger collider in serverObject.Tools.OfType<ColliderTrigger>().ToArray())
                {
                    collider.DrawLines();
                }
            }
        }
    }
}