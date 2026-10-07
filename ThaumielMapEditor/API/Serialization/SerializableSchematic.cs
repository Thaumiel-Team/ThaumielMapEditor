// -----------------------------------------------------------------------
// <copyright file="SerializableSchematic.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;

namespace ThaumielMapEditor.API.Serialization
{
    /// <summary>
    /// This class is used to read schematic data from yaml
    /// </summary>
    [GitBookPage("Serialization/SerializableSchematic")]
    public class SerializableSchematic
    {
        /// <summary>
        /// Gets or sets the root object id for the <see cref="SerializableSchematic"/> instance.
        /// </summary>
        public int RootObjectId { get; set; }

        /// <summary>
        /// Gets or sets file name for the <see cref="SerializableSchematic"/> instance.
        /// </summary>
        public string FileName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the rotation for the <see cref="SerializableSchematic"/> instance.
        /// </summary>
        public Quaternion Rotation { get; set; }

        /// <summary>
        /// Gets or sets the scale for the <see cref="SerializableSchematic"/> instance.
        /// </summary>
        public Vector3 Scale { get; set; }

        /// <summary>
        /// Gets or sets the objects for the <see cref="SerializableSchematic"/> instance.
        /// </summary>
        public List<SerializableObject> Objects {get; set; } = [];

        /// <summary>
        /// Gets or sets the server-side objects for the <see cref="SerializableSchematic"/> instance.
        /// </summary>
        public List<SerializableObject> ServerSideObjects { get; set; } = [];
        
        /// <summary>
        /// Gets or sets the LOD Zones for the <see cref="SerializableSchematic"/> instance.
        /// </summary>
        public List<SerializableLOD> LOD { get; set; } = [];
    }
}