// -----------------------------------------------------------------------
// <copyright file="MapDestroyingEventArgs.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU Lesser General Public License v3.0 (LGPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using ThaumielMapEditor.API.Data;

namespace ThaumielMapEditor.Events.EventArgs.Map
{
    [GitBookPage("Events/EventArgs/MapDestroyingEventArgs")]
    public class MapDestroyingEventArgs : CancellableEventArgs
    {
        /// <summary>
        /// Gets the map being destroyed.
        /// </summary>
        public MapData Map { get; }

        public MapDestroyingEventArgs(MapData map)
        {
            Map = map;
        }
    }
}