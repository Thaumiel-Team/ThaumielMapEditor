// -----------------------------------------------------------------------
// <copyright file="PMERRoot.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU Lesser General Public License v3.0 (LGPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ThaumielMapEditor.API.Conversion
{
    public class PMERRoot
    {
        /// <summary>
        /// The name of the PMER Schematic
        /// </summary>
        [JsonIgnore]
        public string Name = string.Empty;

        /// <summary>
        /// Gets or sets the root id of the PMER schematic root
        /// </summary>
        [JsonPropertyName("RootObjectId")]
        public int RootObjectId { get; set; }
        
        /// <summary>
        /// Gets or sets the blocks of the PMER schematic root
        /// </summary>
        [JsonPropertyName("Blocks")]
        public List<PMERBlock> Blocks { get; set; } = [];
    }

}