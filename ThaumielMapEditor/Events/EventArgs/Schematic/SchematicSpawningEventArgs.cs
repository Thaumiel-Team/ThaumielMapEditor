// -----------------------------------------------------------------------
// <copyright file="SchematicSpawningEventArgs.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU Lesser General Public License v3.0 (LGPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using ThaumielMapEditor.API.Data;
using ThaumielMapEditor.API.Serialization;

namespace ThaumielMapEditor.Events.EventArgs.Schematic
{
    [GitBookPage("Events/EventArgs/SchematicSpawningEventArgs")]
    public class SchematicSpawningEventArgs : CancellableEventArgs
    {
        /// <summary>
        /// Gets the serialized schematic that is being spawned.
        /// </summary>
        public SerializableSchematic SerializedSchematic { get; }

        /// <summary>
        /// Gets the schematic being spawned.
        /// </summary>
        public SchematicData Schematic { get; }

        public SchematicSpawningEventArgs(SerializableSchematic serializedSchematic, SchematicData schematic)
        {
            SerializedSchematic = serializedSchematic;
            Schematic = schematic;
        }
    }
}