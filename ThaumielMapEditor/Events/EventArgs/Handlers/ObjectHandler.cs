// -----------------------------------------------------------------------
// <copyright file="ObjectHandler.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU Lesser General Public License v3.0 (LGPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using LabApi.Events;
using ThaumielMapEditor.Events.EventArgs.Objects;

namespace ThaumielMapEditor.Events.EventArgs.Handlers
{
    public class ObjectHandler
    {
        public static event LabEventHandler<DestroyedClientObjectEventArgs>? ClientObjectDestroyed;

        public static event LabEventHandler<SpawnedClientObjectEventArgs>? ClientObjectSpawned;

        public static event LabEventHandler<DestroyedServerObjectEventArgs>? ServerObjectDestroyed;

        public static event LabEventHandler<SpawnedServerObjectEventArgs>? ServerObjectSpawned;

        internal static void OnClientObjectDestroyed(DestroyedClientObjectEventArgs ev) => ClientObjectDestroyed.InvokeEvent(ev);

        internal static void OnClientObjectSpawned(SpawnedClientObjectEventArgs ev) => ClientObjectSpawned.InvokeEvent(ev);

        internal static void OnServerObjectDestroyed(DestroyedServerObjectEventArgs ev) => ServerObjectDestroyed.InvokeEvent(ev);

        internal static void OnServerObjectSpawned(SpawnedServerObjectEventArgs ev) => ServerObjectSpawned.InvokeEvent(ev);
    }
}