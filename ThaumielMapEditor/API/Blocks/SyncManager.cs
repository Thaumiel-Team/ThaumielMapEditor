// -----------------------------------------------------------------------
// <copyright file="SyncManager.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU Lesser General Public License v3.0 (LGPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using LabApi.Features.Wrappers;
using MEC;
using Mirror;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ThaumielMapEditor.API.Enums;
using ThaumielMapEditor.API.Helpers;

namespace ThaumielMapEditor.API.Blocks
{
    [GitBookPage("Blocks/SyncManager")]
    public static class SyncManager
    {
        private static readonly ConcurrentDictionary<ClientObject, byte> PendingClientSyncs = new();
        private static readonly ConcurrentDictionary<ServerObject, byte> PendingServerSyncs = new();

        private static int _clientSyncScheduled;
        private static int _serverSyncScheduled;

        private const int MaxFlushIterations = 16;
        private const int MaxSyncRetries = 3;
        private static readonly ConcurrentDictionary<ServerObject, int> ServerRetryCounts = new();

        extension(SyncFlags flags)
        {
            public bool HasFlagFast(SyncFlags flag) => (flags & flag) != 0;
        }

        /// <summary>
        /// Registers a <see cref="ClientObject"/> to be synced at the end of the current frame.
        /// </summary>
        public static void RegisterForSync(ClientObject obj)
        {
            if (obj == null)
                return;

            PendingClientSyncs.TryAdd(obj, 0);
            ScheduleClientSync();
        }

        /// <summary>
        /// Registers a <see cref="ServerObject"/> to be synced at the end of the current frame.
        /// </summary>
        public static void RegisterForSync(ServerObject obj)
        {
            if (obj == null)
                return;

            PendingServerSyncs.TryAdd(obj, 0);
            ScheduleServerSync();
        }

        private static void ScheduleClientSync()
        {
            if (Interlocked.CompareExchange(ref _clientSyncScheduled, 1, 0) != 0)
                return;

            try
            {
                Timing.RunCoroutine(EndOfFrameSyncCoroutine(), "TME_BatchSync_Client");
            }
            catch
            {
                Interlocked.Exchange(ref _clientSyncScheduled, 0);
                throw;
            }
        }

        private static void ScheduleServerSync()
        {
            if (Interlocked.CompareExchange(ref _serverSyncScheduled, 1, 0) != 0)
                return;

            try
            {
                Timing.RunCoroutine(EndOfFrameSyncCoroutine(), "TME_BatchSync_Server");
            }
            catch
            {
                Interlocked.Exchange(ref _serverSyncScheduled, 0);
                throw;
            }
        }

        /// <summary>
        /// Forces an immediate sync of all pending client objects.
        /// </summary>
        public static void FlushClient()
        {
            ProcessPendingClientSyncs();

            if (!PendingClientSyncs.IsEmpty)
                ScheduleClientSync();
        }

        /// <summary>
        /// Forces an immediate sync of all pending server objects.
        /// </summary>
        public static void FlushServer()
        {
            ProcessPendingServerSyncs();

            if (!PendingServerSyncs.IsEmpty)
                ScheduleServerSync();
        }

        /// <summary>
        /// Clears all pending client syncs without sending.
        /// </summary>
        public static void ClearClientPending()
        {
            PendingClientSyncs.Clear();
            Interlocked.Exchange(ref _clientSyncScheduled, 0);

            Timing.KillCoroutines("TME_BatchSync_Client");
        }

        /// <summary>
        /// Clears all pending server syncs without sending.
        /// </summary>
        public static void ClearServerPending()
        {
            PendingServerSyncs.Clear();
            Interlocked.Exchange(ref _serverSyncScheduled, 0);

            ServerRetryCounts.Clear();

            Timing.KillCoroutines("TME_BatchSync_Server");
        }

        internal static void RemoveFromPending(ClientObject obj)
        {
            if (obj == null)
                return;

            PendingClientSyncs.TryRemove(obj, out _);
        }

        internal static void RemoveFromPending(ServerObject obj)
        {
            if (obj == null)
                return;

            PendingServerSyncs.TryRemove(obj, out _);
            ServerRetryCounts.TryRemove(obj, out _);
        }


        private static IEnumerator<float> EndOfFrameSyncCoroutine()
        {
            yield return Timing.WaitForOneFrame;

            try
            {
                int iteration = 0;
                bool hasPending;
                do
                {
                    ProcessPendingClientSyncs();
                    ProcessPendingServerSyncs();
                    iteration++;
                    hasPending = !PendingClientSyncs.IsEmpty || !PendingServerSyncs.IsEmpty;
                }
                while (iteration < MaxFlushIterations && hasPending);
            }
            finally
            {
                Interlocked.Exchange(ref _clientSyncScheduled, 0);
                Interlocked.Exchange(ref _serverSyncScheduled, 0);
            }

            if (!PendingClientSyncs.IsEmpty)
                ScheduleClientSync();

            if (!PendingServerSyncs.IsEmpty)
                ScheduleServerSync();
        }

        private static void ProcessPendingClientSyncs()
        {
            List<ClientObject> snapshot = [];
            foreach (ClientObject obj in PendingClientSyncs.Keys)
            {
                if (PendingClientSyncs.TryRemove(obj, out _))
                    snapshot.Add(obj);
            }

            if (snapshot.Count == 0)
                return;

            Dictionary<Player, List<ClientObject>> playerBatches = [];
            HashSet<ClientObject> needsRetry = [];

            foreach (ClientObject obj in snapshot)
            {
                if (obj == null || !obj.Spawned)
                    continue;

                Player[] targets = obj.SpawnedPlayers.ToArray();
                foreach (Player player in targets)
                {
                    if (player == null || player.IsHost || player.IsDestroyed)
                        continue;

                    if (player.Connection == null || !player.Connection.isReady)
                    {
                        needsRetry.Add(obj);
                        continue;
                    }

                    if (!playerBatches.TryGetValue(player, out var list))
                    {
                        list = new List<ClientObject>(16);
                        playerBatches[player] = list;
                    }

                    list.Add(obj);
                }
            }

            List<ClientObject> failed = [];
            foreach (KeyValuePair<Player, List<ClientObject>> kvp in playerBatches)
            {
                foreach (ClientObject obj in kvp.Value)
                {
                    try
                    {
                        obj.SyncForPlayer(kvp.Key);
                    }
                    catch (Exception ex)
                    {
                        LogManager.Error($"Failed to sync object {obj.NetId} to {kvp.Key.DisplayName}: {ex.Message}");
                        if (!failed.Contains(obj))
                            failed.Add(obj);
                    }
                }
            }

            foreach (ClientObject obj in snapshot)
            {
                if (!failed.Contains(obj) && !needsRetry.Contains(obj))
                    obj.ClearDirtyFlags();
            }

            if (failed.Count > 0 || needsRetry.Count > 0)
            {
                foreach (ClientObject obj in failed)
                    PendingClientSyncs.TryAdd(obj, 0);

                foreach (ClientObject obj in needsRetry)
                    PendingClientSyncs.TryAdd(obj, 0);
            }

            int objectCount = snapshot.Count;
            LogManager.Debug($"Batch sync completed: {objectCount} ClientObjects synced for {playerBatches.Count} players.");
        }

        private static void ProcessPendingServerSyncs()
        {
            List<ServerObject> snapshot = [];
            foreach (ServerObject obj in PendingServerSyncs.Keys)
            {
                if (PendingServerSyncs.TryRemove(obj, out _))
                    snapshot.Add(obj);
            }

            if (snapshot.Count == 0)
                return;

            foreach (ServerObject obj in snapshot)
            {
                if (obj == null || obj.Object == null)
                    continue;

                if (!obj.Object.TryGetComponent<NetworkIdentity>(out _))
                {
                    obj.ClearDirtyFlags();
                    continue;
                }

                try
                {
                    NetworkServer.UnSpawn(obj.Object);
                    NetworkServer.Spawn(obj.Object);
                    obj.ClearDirtyFlags();
                    ServerRetryCounts.TryRemove(obj, out _);
                }
                catch (Exception ex)
                {
                    LogManager.Error($"Failed to sync object {obj.Name} - {obj.NetId}: {ex.Message}");

                    int count = ServerRetryCounts.AddOrUpdate(obj, 1, static (_, previous) => previous + 1);
                    if (count >= MaxSyncRetries)
                    {
                        ServerRetryCounts.TryRemove(obj, out _);
                        LogManager.Warn($"Dropping server sync for '{obj.Name}' after {count} failures.");
                    }
                    else
                        PendingServerSyncs.TryAdd(obj, 0);
                }
            }

            int objectCount = snapshot.Count;
            LogManager.Debug($"Batch sync completed: {objectCount} ServerObjects synced.");
        }
    }
}