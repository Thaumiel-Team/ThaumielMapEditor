// -----------------------------------------------------------------------
// <copyright file="SchematicDestroyingEventArgs.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU Lesser General Public License v3.0 (LGPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using ThaumielMapEditor.API.Data;

namespace ThaumielMapEditor.Events.EventArgs.Schematic
{
    [GitBookPage("Events/EventArgs/SchematicDestroyingEventArgs")]
    public class SchematicDestroyingEventArgs : CancellableEventArgs
    {
        /// <summary>
        /// Gets the schematic being destroyed.
        /// </summary>
        public SchematicData Schematic { get; }

        public SchematicDestroyingEventArgs(SchematicData schematic)
        {
            Schematic = schematic;
        }
    }
}