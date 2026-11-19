// -----------------------------------------------------------------------
// <copyright file="MapSpawningEventArgs.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU Lesser General Public License v3.0 (LGPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using ThaumielMapEditor.API.Serialization;

namespace ThaumielMapEditor.Events.EventArgs.Map
{
    [GitBookPage("Events/EventArgs/MapSpawningEventArgs")]
    public class MapSpawningEventArgs : CancellableEventArgs
    {
        /// <summary>
        /// Gets the serialized map that is being spawned.
        /// </summary>
        public SerializableMap SerializedMap { get; }

        public MapSpawningEventArgs(SerializableMap serializedMap)
        {
            SerializedMap = serializedMap;
        }
    }
}