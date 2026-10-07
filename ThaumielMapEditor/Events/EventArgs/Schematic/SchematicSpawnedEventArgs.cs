// -----------------------------------------------------------------------
// <copyright file="SchematicSpawnedEventArgs.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using ThaumielMapEditor.API.Data;

namespace ThaumielMapEditor.Events.EventArgs.Schematic
{
    [GitBookPage("Events/EventArgs/SchematicSpawnedEventArgs")]
    public class SchematicSpawnedEventArgs : System.EventArgs
    {
        public SchematicData Schematic { get; }

        public SchematicSpawnedEventArgs(SchematicData schematic)
        {
            Schematic = schematic;
        }
    }
}