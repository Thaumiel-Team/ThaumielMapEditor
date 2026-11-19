// -----------------------------------------------------------------------
// <copyright file="MapSpawnedEventArgs.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU Lesser General Public License v3.0 (LGPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using ThaumielMapEditor.API.Data;

namespace ThaumielMapEditor.Events.EventArgs.Map
{
    /// <summary>
    /// Invoked after a map is registered. Its schematics may still be spawning.
    /// </summary>
    [GitBookPage("Events/EventArgs/MapSpawnedEventArgs")]
    public class MapSpawnedEventArgs : System.EventArgs
    {
        /// <summary>
        /// Gets the map that was spawned.
        /// </summary>
        public MapData Map { get; }

        public MapSpawnedEventArgs(MapData map)
        {
            Map = map;
        }
    }
}