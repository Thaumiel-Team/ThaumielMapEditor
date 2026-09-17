// -----------------------------------------------------------------------
// <copyright file="MapData.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using LabApi.Features.Wrappers;

namespace ThaumielMapEditor.API.Data
{
    [GitBookPage("Data/MapData")]
    public class MapData
    {
        /// <summary>
        /// Gets or sets the file name of this <see cref="MapData"/> instance.
        /// </summary>
        public string FileName = string.Empty;

        /// <summary>
        /// Gets or sets the <see cref="Guid"/> id of this <see cref="MapData"/> instance.
        /// </summary>
        public Guid Id;

        /// <summary>
        /// Gets or sets the position of this <see cref="MapData"/> instance.
        /// </summary>
        public Vector3 Position { get; set; }

        /// <summary>
        /// Gets or sets the room of this <see cref="MapData"/> instance.
        /// </summary>
        public Room? Room { get; set; }

        /// <summary>
        /// Gets or sets the file name of this <see cref="MapData"/> instance.
        /// </summary>
        public List<MapSchematicData> Schematics { get; set; } = [];
    }
}