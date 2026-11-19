// -----------------------------------------------------------------------
// <copyright file="MapDestroyedEventArgs.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU Lesser General Public License v3.0 (LGPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using ThaumielMapEditor.API.Data;

namespace ThaumielMapEditor.Events.EventArgs.Map
{
    [GitBookPage("Events/EventArgs/MapDestroyedEventArgs")]
    public class MapDestroyedEventArgs : System.EventArgs
    {
        /// <summary>
        /// Gets the map that was destroyed.
        /// </summary>
        public MapData Map { get; }

        public MapDestroyedEventArgs(MapData map)
        {
            Map = map;
        }
    }
}