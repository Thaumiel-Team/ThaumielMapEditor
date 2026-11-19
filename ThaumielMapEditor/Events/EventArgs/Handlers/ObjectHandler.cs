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
        public static event LabEventHandler<SpawningClientObjectEventArgs>? ClientObjectSpawning;

        public static event LabEventHandler<SpawnedClientObjectEventArgs>? ClientObjectSpawned;

        public static event LabEventHandler<DestroyedClientObjectEventArgs>? ClientObjectDestroyed;

        public static event LabEventHandler<ClientObjectPositionUpdatedEventArgs>? ClientObjectPositionUpdated;

        public static event LabEventHandler<ClientObjectRotationUpdatedEventArgs>? ClientObjectRotationUpdated;

        public static event LabEventHandler<ClientObjectScaleUpdatedEventArgs>? ClientObjectScaleUpdated;

        public static event LabEventHandler<SpawningServerObjectEventArgs>? ServerObjectSpawning;

        public static event LabEventHandler<SpawnedServerObjectEventArgs>? ServerObjectSpawned;

        public static event LabEventHandler<ServerObjectUpdatedEventArgs>? ServerObjectUpdated;

        public static event LabEventHandler<DestroyedServerObjectEventArgs>? ServerObjectDestroyed;

        public static event LabEventHandler<ObjectValueChangedEventArgs>? ObjectValueChanged;

        internal static void OnClientObjectSpawning(SpawningClientObjectEventArgs ev) => ClientObjectSpawning.InvokeEvent(ev);

        internal static void OnClientObjectSpawned(SpawnedClientObjectEventArgs ev) => ClientObjectSpawned.InvokeEvent(ev);

        internal static void OnClientObjectDestroyed(DestroyedClientObjectEventArgs ev) => ClientObjectDestroyed.InvokeEvent(ev);

        internal static void OnClientObjectPositionUpdated(ClientObjectPositionUpdatedEventArgs ev) => ClientObjectPositionUpdated.InvokeEvent(ev);

        internal static void OnClientObjectRotationUpdated(ClientObjectRotationUpdatedEventArgs ev) => ClientObjectRotationUpdated.InvokeEvent(ev);

        internal static void OnClientObjectScaleUpdated(ClientObjectScaleUpdatedEventArgs ev) => ClientObjectScaleUpdated.InvokeEvent(ev);

        internal static void OnServerObjectSpawning(SpawningServerObjectEventArgs ev) => ServerObjectSpawning.InvokeEvent(ev);

        internal static void OnServerObjectSpawned(SpawnedServerObjectEventArgs ev) => ServerObjectSpawned.InvokeEvent(ev);

        internal static void OnServerObjectUpdated(ServerObjectUpdatedEventArgs ev) => ServerObjectUpdated.InvokeEvent(ev);

        internal static void OnServerObjectDestroyed(DestroyedServerObjectEventArgs ev) => ServerObjectDestroyed.InvokeEvent(ev);

        internal static void OnObjectValueChanged(ObjectValueChangedEventArgs ev) => ObjectValueChanged.InvokeEvent(ev);
    }
}