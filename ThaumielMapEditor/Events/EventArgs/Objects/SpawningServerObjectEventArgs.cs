// -----------------------------------------------------------------------
// <copyright file="SpawningServerObjectEventArgs.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU Lesser General Public License v3.0 (LGPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using ThaumielMapEditor.API.Data;
using ThaumielMapEditor.API.Serialization;

namespace ThaumielMapEditor.Events.EventArgs.Objects
{
    [GitBookPage("Events/EventArgs/SpawningServerObjectEventArgs")]
    public class SpawningServerObjectEventArgs : CancellableEventArgs
    {
        /// <summary>
        /// Gets the schematic the object belongs to.
        /// </summary>
        public SchematicData Schematic { get; }

        /// <summary>
        /// Gets the serialized object that is being spawned.
        /// </summary>
        public SerializableObject SerializedObject { get; }

        public SpawningServerObjectEventArgs(SchematicData schematic, SerializableObject serializedObject)
        {
            Schematic = schematic;
            SerializedObject = serializedObject;
        }
    }
}