// -----------------------------------------------------------------------
// <copyright file="SerializableMap.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using MapGeneration;

namespace ThaumielMapEditor.API.Serialization
{
    /// <summary>
    /// This class is used to read map schematics from yaml
    /// </summary>
    [GitBookPage("Serialization/SerializedMapSchematic")]
    public class SerializedMapSchematic
    {
        /// <summary>
        /// Gets or sets the position for the <see cref="SerializedMapSchematic"/> instance.
        /// </summary>
        public Vector3 Position { get; set; }

        /// <summary>
        /// Gets or sets the schematic name of the parent schematic for the <see cref="SerializedMapSchematic"/> instance.
        /// </summary>
        public string SchematicName { get; set; } = string.Empty;
    }

    /// <summary>
    /// This class is used to read map data from yaml
    /// </summary>
    [GitBookPage("Serialization/SerializableMap")]
    public class SerializableMap
    {
        /// <summary>
        /// Gets or sets the file name for the <see cref="SerializableMap"/> instance.
        /// </summary>
        public string FileName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the Guid id for the <see cref="SerializableMap"/> instance.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the room by its name for the <see cref="SerializableMap"/> instance.
        /// </summary>
        public RoomName Room { get; set; }

        /// <summary>
        /// Gets or sets the local position of the room for the <see cref="SerializableMap"/> instance.
        /// </summary>
        public Vector3 LocalPosition { get; set; }

        /// <summary>
        /// Gets or sets the schematics for the <see cref="SerializableMap"/> instance.
        /// </summary>
        public List<SerializedMapSchematic> Schematics { get; set; } = [];
    }
}