// -----------------------------------------------------------------------
// <copyright file="SerializableObject.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using ThaumielMapEditor.API.Enums;

namespace ThaumielMapEditor.API.Serialization
{
    /// <summary>
    /// This class is used to read object data from yaml
    /// </summary>

    [GitBookPage("Serialization/SerializableObject")]
    public class SerializableObject
    {
        /// <summary>
        /// Gets or sets the object id for the <see cref="SerializableObject"/> instance.
        /// </summary>
        public int ObjectId { get; set; }

        /// <summary>
        /// Gets or sets the parent id for the <see cref="SerializableObject"/> instance.
        /// </summary>
        public int ParentId { get; set; }

        /// <summary>
        /// Gets or sets the file name for the <see cref="SerializableObject"/> instance.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the position for the <see cref="SerializableObject"/> instance.
        /// </summary>
        public Vector3 Position { get; set; }

        /// <summary>
        /// Gets or sets the scale for the <see cref="SerializableObject"/> instance.
        /// </summary>
        public Vector3 Scale { get; set; }

        /// <summary>
        /// Gets or sets the rotation for the <see cref="SerializableObject"/> instance.
        /// </summary>
        public Quaternion Rotation { get; set; }

        /// <summary>
        /// Gets or sets whether the <see cref="SerializableObject"/> instance is static.
        /// </summary>
        public bool IsStatic { get; set; }

        /// <summary>
        /// Gets or sets the sync interval for the <see cref="SerializableObject"/> instance.
        /// </summary>
        public byte MovementSmoothing { get; set; }

        /// <summary>
        /// Gets or sets the object type for the <see cref="SerializableObject"/> instance.
        /// </summary>
        public ObjectType ObjectType { get; set; }

        /// <summary>
        /// Gets or sets the values for the <see cref="SerializableObject"/> instance.
        /// </summary>
        public Dictionary<string, object> Values { get; set; } = [];

        public List<SerializableTool> Tools { get; set; } = [];

        public SerializableCullingSettings CullingSettings { get; set; } = new();

        public string AnimatorName { get; set; } = string.Empty;
    }
}