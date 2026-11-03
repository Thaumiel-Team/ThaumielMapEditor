// -----------------------------------------------------------------------
// <copyright file="PMERBlock.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU Lesser General Public License v3.0 (LGPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;

namespace ThaumielMapEditor.API.Conversion
{
    public class PMERBlock
    {
        /// <summary>
        /// Gets or sets the name of the PMER schematic block
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the object id of the PMER schematic block
        /// </summary>
        public int ObjectId { get; set; }

        /// <summary>
        /// Gets or sets the parent id of the PMER schematic block
        /// </summary>
        public int ParentId { get; set; }

        /// <summary>
        /// Gets or sets the position of the PMER schematic block
        /// </summary>
        public Vector3 Position { get; set; }

        /// <summary>
        /// Gets or sets the rotation of the PMER schematic block
        /// </summary>
        public Vector3 Rotation { get; set; }

        /// <summary>
        /// Gets or sets the scale of the PMER schematic block
        /// </summary>
        public Vector3 Scale { get; set; }

        /// <summary>
        /// Gets or sets the blocktype of the PMER schematic block
        /// </summary>
        public int BlockType { get; set; }

        /// <summary>
        /// Gets or sets the properties of the PMER schematic block
        /// </summary>
        public Dictionary<string, object> Properties { get; set; } = [];
    }
}