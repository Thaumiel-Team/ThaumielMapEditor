// -----------------------------------------------------------------------
// <copyright file="SyncManager.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using LabApi.Features.Wrappers;
using MEC;
using Mirror;
using System;
using System.Collections.Generic;
using System.Linq;
using ThaumielMapEditor.API.Blocks.ClientSide;
using ThaumielMapEditor.API.Helpers;

namespace ThaumielMapEditor.API.Blocks
{
    public static class SyncManager
    {
        private static readonly HashSet<ClientObject> PendingClientSyncs = [];
        private static readonly HashSet<ServerObject> PendingServerSyncs = [];
        private static readonly object ClientLock = new();
        private static readonly object ServerLock = new();

        private static volatile bool IsClientSyncScheduled = false;
        private static volatile bool IsServerSyncScheduled = false;

        private const int MaxFlushIterations = 16;
        private const int MaxSyncRetries = 3;
        private static readonly Dictionary<ServerObject, int> ServerRetryCounts = [];

        /// <summary>
        /// Registers a <see cref="ClientObject"/> to be synced at the end of the current frame.
        /// </summary>
        public static void RegisterForSync(ClientObject obj)
        {
            if (obj == null)
                return;

            lock (ClientLock)
            {
                PendingClientSyncs.Add(obj);
            }

            ScheduleClientSync();
        }

        /// <summary>
        /// Registers a <see cref="ServerObject"/> to be synced at the end of the current frame.
        /// </summary>
        public static void RegisterForSync(ServerObject obj)
        {
            if (obj == null)
                return;

            lock (ServerLock)
            {
                PendingServerSyncs.Add(obj);
            }

            ScheduleServerSync();
        }

        private static void ScheduleClientSync()
        {
            if (IsClientSyncScheduled)
                return;

            IsClientSyncScheduled = true;
            Timing.RunCoroutine(EndOfFrameSyncCoroutine(), "TME_BatchSync_Client");
        }

        private static void ScheduleServerSync()
        {
            if (IsServerSyncScheduled)
                return;

            IsServerSyncScheduled = true;
            Timing.RunCoroutine(EndOfFrameSyncCoroutine(), "TME_BatchSync_Server");
        }

        /// <summary>
        /// Forces an immediate sync of all pending client objects.
        /// </summary>
        public static void FlushClient()
        {
            ProcessPendingClientSyncs();

            lock (ClientLock)
            {
                if (PendingClientSyncs.Count > 0)
                    ScheduleClientSync();
            }
        }

        /// <summary>
        /// Forces an immediate sync of all pending server objects.
        /// </summary>
        public static void FlushServer()
        {
            ProcessPendingServerSyncs();

            lock (ServerLock)
            {
                if (PendingServerSyncs.Count > 0)
                    ScheduleServerSync();
            }
        }

        /// <summary>
        /// Clears all pending client syncs without sending.
        /// </summary>
        public static void ClearClientPending()
        {
            lock (ClientLock)
            {
                PendingClientSyncs.Clear();
                IsClientSyncScheduled = false;
            }

            Timing.KillCoroutines("TME_BatchSync_Client");
        }

        /// <summary>
        /// Clears all pending server syncs without sending.
        /// </summary>
        public static void ClearServerPending()
        {
            lock (ServerLock)
            {
                PendingServerSyncs.Clear();
                IsServerSyncScheduled = false;
            }

            lock (ServerRetryCounts)
            {
                ServerRetryCounts.Clear();
            }

            Timing.KillCoroutines("TME_BatchSync_Server");
        }

        internal static void RemoveFromPending(ClientObject obj)
        {
            if (obj == null)
                return;

            lock (ClientLock)
            {
                PendingClientSyncs.Remove(obj);
            }
        }

        internal static void RemoveFromPending(ServerObject obj)
        {
            if (obj == null)
                return;

            lock (ServerLock)
            {
                PendingServerSyncs.Remove(obj);
            }

            lock (ServerRetryCounts)
            {
                ServerRetryCounts.Remove(obj);
            }
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
                    lock (ClientLock)
                    lock (ServerLock)
                    {
                        hasPending = PendingClientSyncs.Count > 0 || PendingServerSyncs.Count > 0;
                    }
                }
                while (iteration < MaxFlushIterations && hasPending);
            }
            finally
            {
                IsClientSyncScheduled = false;
                IsServerSyncScheduled = false;
            }

            lock (ClientLock)
            {
                if (PendingClientSyncs.Count > 0)
                    ScheduleClientSync();
            }

            lock (ServerLock)
            {
                if (PendingServerSyncs.Count > 0)
                    ScheduleServerSync();
            }
        }

        private static void ProcessPendingClientSyncs()
        {
            List<ClientObject> snapshot;
            lock (ClientLock)
            {
                if (PendingClientSyncs.Count == 0)
                    return;

                snapshot = [.. PendingClientSyncs];
                PendingClientSyncs.Clear();
            }

            Dictionary<Player, List<ClientObject>> playerBatches = [];

            foreach (ClientObject obj in snapshot)
            {
                if (obj == null || !obj.Spawned)
                    continue;

                Player[] targets = obj.SpawnedPlayers.ToArray();
                foreach (Player player in targets)
                {
                    if (player == null || player.IsHost || player.IsDestroyed)
                        continue;

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
                        obj.SpawnForPlayer(kvp.Key);
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
                if (!failed.Contains(obj))
                    obj.ClearDirtyFlags();
            }

            if (failed.Count > 0)
            {
                lock (ClientLock)
                {
                    foreach (ClientObject obj in failed)
                        PendingClientSyncs.Add(obj);
                }
            }

            int objectCount = snapshot.Count;
            LogManager.Debug($"Batch sync completed: {objectCount} ClientObjects synced for {playerBatches.Count} players.");
        }

        private static void ProcessPendingServerSyncs()
        {
            List<ServerObject> snapshot;
            lock (ServerLock)
            {
                if (PendingServerSyncs.Count == 0)
                    return;

                snapshot = [.. PendingServerSyncs];
                PendingServerSyncs.Clear();
            }

            foreach (ServerObject obj in snapshot)
            {
                if (obj == null || obj.Object == null)
                    continue;

                try
                {
                    NetworkServer.UnSpawn(obj.Object);
                    NetworkServer.Spawn(obj.Object);
                    obj.ClearDirtyFlags();
                    lock (ServerRetryCounts)
                    {
                        ServerRetryCounts.Remove(obj);
                    }
                }
                catch (Exception ex)
                {
                    LogManager.Error($"Failed to sync object {obj.Name} - {obj.NetId}: {ex.Message}");
                    bool retry = true;
                    lock (ServerRetryCounts)
                    {
                        ServerRetryCounts.TryGetValue(obj, out int count);
                        count++;
                        if (count >= MaxSyncRetries)
                        {
                            ServerRetryCounts.Remove(obj);
                            retry = false;
                            LogManager.Warn($"Dropping server sync for '{obj.Name}' after {count} failures.");
                        }
                        else
                            ServerRetryCounts[obj] = count;
                    }

                    if (retry)
                    {
                        lock (ServerLock)
                        {
                            PendingServerSyncs.Add(obj);
                        }
                    }
                }
            }

            int objectCount = snapshot.Count;
            LogManager.Debug($"Batch sync completed: {objectCount} ServerObjects synced.");
        }
    }
}