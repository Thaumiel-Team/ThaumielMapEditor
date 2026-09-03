// -----------------------------------------------------------------------
// <copyright file="Scale.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Globalization;
using System.Linq;
using System.Text;
using CommandSystem;
using ThaumielMapEditor.API.Blocks;
using ThaumielMapEditor.API.Data;
using ThaumielMapEditor.API.Interfaces;

namespace ThaumielMapEditor.Commands.Admin.ModifySubCommands
{
    public class Scale : ISubCommand
    {
        public override string Name => "scale";

        public override string RequiredPermission => "tme.modify.scale";

        public override string Description => "Scales the specified schematic by the specified values";

        public override int RequiredArgsCount => 3;

        public override string VisibleArgs => "<X>, <Y>, <Z>";

        public override bool SubCommandExecute(ArraySegment<string> arguments, ICommandSender sender, SchematicData schematic, StringBuilder sb, out string response)
        {
            if (arguments.Count < 3)
            {
                response = $"Wrong usage! Correct usage: tme modify {Name} {VisibleArgs}";
                return false;
            }

            if (!float.TryParse(arguments.At(0), NumberStyles.Float, CultureInfo.InvariantCulture, out var x) || float.IsNaN(x) || float.IsInfinity(x) || x == 0f)
            {
                response = "Failed to parse X coordinate. Make sure its a non-zero number";
                return false;
            }

            if (!float.TryParse(arguments.At(1), NumberStyles.Float, CultureInfo.InvariantCulture, out var y) || float.IsNaN(y) || float.IsInfinity(y) || y == 0f)
            {
                response = "Failed to parse Y coordinate. Make sure its a non-zero number";
                return false;
            }

            if (!float.TryParse(arguments.At(2), NumberStyles.Float, CultureInfo.InvariantCulture, out var z) || float.IsNaN(z) || float.IsInfinity(z) || z == 0f)
            {
                response = "Failed to parse Z coordinate. Make sure its a non-zero number";
                return false;
            }

            Vector3 scale = new(x, y, z);
            Vector3 prevscale = schematic.Scale;
            schematic.Scale = scale;

            foreach (ServerObject serverObject in schematic.SpawnedServerObjects.ToArray())
            {
                serverObject.UpdateObject(schematic, false);
            }

            SyncManager.FlushServer();

            response = $"Scaled schematic {schematic.FileName} to {scale} from {prevscale}";
            return true;
        }
    }
}