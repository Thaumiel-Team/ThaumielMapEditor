// -----------------------------------------------------------------------
// <copyright file="MapHandler.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU Lesser General Public License v3.0 (LGPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using LabApi.Events;
using ThaumielMapEditor.Events.EventArgs.Map;

namespace ThaumielMapEditor.Events.EventArgs.Handlers
{
    public class MapHandler
    {
        public static event LabEventHandler<MapSpawningEventArgs>? MapSpawning;

        public static event LabEventHandler<MapSpawnedEventArgs>? MapSpawned;

        public static event LabEventHandler<MapDestroyingEventArgs>? MapDestroying;

        public static event LabEventHandler<MapDestroyedEventArgs>? MapDestroyed;

        internal static void OnMapSpawning(MapSpawningEventArgs ev) => MapSpawning.InvokeEvent(ev);

        internal static void OnMapSpawned(MapSpawnedEventArgs ev) => MapSpawned.InvokeEvent(ev);

        internal static void OnMapDestroying(MapDestroyingEventArgs ev) => MapDestroying.InvokeEvent(ev);

        internal static void OnMapDestroyed(MapDestroyedEventArgs ev) => MapDestroyed.InvokeEvent(ev);
    }
}