// -----------------------------------------------------------------------
// <copyright file="SchematicDestroyedEventArgs.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using ThaumielMapEditor.API.Data;

namespace ThaumielMapEditor.Events.EventArgs.Schematic
{
    [GitBookPage("Events/EventArgs/SchematicDestroyedEventArgs")]
    public class SchematicDestroyedEventArgs : System.EventArgs
    {
        public SchematicData Schematic { get; }

        public SchematicDestroyedEventArgs(SchematicData schematic)
        {
            Schematic = schematic;
        }
    }
}