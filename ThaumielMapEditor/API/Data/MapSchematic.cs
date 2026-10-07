// -----------------------------------------------------------------------
// <copyright file="MapSchematic.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using YamlDotNet.Serialization;

namespace ThaumielMapEditor.API.Data
{
    [GitBookPage("Data/MapSchematicData")]
    public class MapSchematicData
    {
        /// <summary>
        /// Gets or sets the local position relative to the <see cref="MapData"/> instance this <see cref="MapSchematicData"/> instance is tied to.
        /// </summary>
        public Vector3 LocalPosition { get; set; }
        
        /// <summary>
        /// Gets or sets the schematic name to load of this <see cref="MapSchematicData"/> instance.
        /// </summary>
        public string SchematicName { get; set; } = string.Empty;

        [YamlIgnore]
        public uint SchematicId { get; set; }
    }
}