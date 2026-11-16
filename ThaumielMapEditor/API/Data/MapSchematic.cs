// -----------------------------------------------------------------------
// <copyright file="MapSchematic.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU Lesser General Public License v3.0 (LGPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using YamlDotNet.Serialization;
using ThaumielMapEditor.API.Helpers;

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

        /// <summary>
        /// Gets a value indicating whether the schematic of this entry is currently spawned.
        /// </summary>
        /// 
        public bool IsSpawned => GetSchematic() != null;

        /// <summary>
        /// Gets the spawned schematic for this entry.
        /// </summary>
        /// <returns>The schematic, or <see langword="null"/> if it is not spawned.</returns>
        public SchematicData? GetSchematic() => Loader.GetSchematicById(SchematicId);

        /// <summary>
        /// Tries to get the spawned schematic for this entry.
        /// </summary>
        /// <param name="schematic">The spawned schematic, or <see langword="null"/>.</param>
        /// <returns><see langword="true"/> if the schematic was found.</returns>
        public bool TryGetSchematic(out SchematicData? schematic)
        {
            schematic = GetSchematic();
            return schematic != null;
        }
    }
}