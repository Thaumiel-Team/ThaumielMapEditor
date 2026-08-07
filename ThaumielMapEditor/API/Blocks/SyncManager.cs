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
using ThaumielMapEditor.API.Blocks.ClientSide;
using ThaumielMapEditor.API.Helpers;

namespace ThaumielMapEditor.API.Blocks
{
    public static class SyncManager
    {
        private static readonly HashSet<ClientObject> PendingClientSyncs = [];
        private static readonly HashSet<ServerObject> PendingServerSyncs = [];

        private static bool IsClientSyncScheduled = false;
        private static bool IsServerSyncScheduled = false;

        private const int MaxFlushIterations = 16;

        /// <summary>
        /// Registers a <see cref="ClientObject"/> to be synced at the end of the current frame.
        /// </summary>
        public static void RegisterForSync(ClientObject obj)
        {
            if (obj == null)
                return;

            PendingClientSyncs.Add(obj);
            ScheduleClientSync();
        }

        /// <summary>
        /// Registers a <see cref="ServerObject"/> to be synced at the end of the current frame.
        /// </summary>
        public static void RegisterForSync(ServerObject obj)
        {
            if (obj == null)
                return;

            PendingServerSyncs.Add(obj);
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

            if (!PendingClientSyncs.IsEmpty())
                ScheduleClientSync();
        }

        /// <summary>
        /// Forces an immediate sync of all pending server objects.
        /// </summary>
        public static void FlushServer()
        {
            ProcessPendingServerSyncs();

            if (!PendingServerSyncs.IsEmpty())
                ScheduleServerSync();
        }

        /// <summary>
        /// Clears all pending client syncs without sending.
        /// </summary>
        public static void ClearClientPending()
        {
            PendingClientSyncs.Clear();
            IsClientSyncScheduled = false;
            Timing.KillCoroutines("TME_BatchSync_Client");
        }

        /// <summary>
        /// Clears all pending server syncs without sending.
        /// </summary>
        public static void ClearServerPending()
        {
            PendingServerSyncs.Clear();
            IsServerSyncScheduled = false;
            Timing.KillCoroutines("TME_BatchSync_Server");
        }


        private static IEnumerator<float> EndOfFrameSyncCoroutine()
        {
            yield return Timing.WaitForOneFrame;

            try
            {
                int iteration = 0;
                do
                {
                    ProcessPendingClientSyncs();
                    ProcessPendingServerSyncs();
                    iteration++;
                }
                while (iteration < MaxFlushIterations && (!PendingClientSyncs.IsEmpty() || !PendingServerSyncs.IsEmpty()));
            }
            finally
            {
                IsClientSyncScheduled = false;
                IsServerSyncScheduled = false;
            }

            if (!PendingClientSyncs.IsEmpty())
                ScheduleClientSync();

            if (!PendingServerSyncs.IsEmpty())
                ScheduleServerSync();
        }

        private static void ProcessPendingClientSyncs()
        {
            if (PendingClientSyncs.IsEmpty())
                return;

            List<ClientObject> snapshot = [.. PendingClientSyncs];
            PendingClientSyncs.Clear();

            Dictionary<Player, List<ClientObject>> playerBatches = [];

            foreach (ClientObject obj in snapshot)
            {
                if (!obj.Spawned)
                    continue;

                foreach (Player player in obj.SpawnedPlayers)
                {
                    if (player.IsHost)
                        continue;

                    if (!playerBatches.TryGetValue(player, out var list))
                    {
                        list = new List<ClientObject>(16);
                        playerBatches[player] = list;
                    }

                    list.Add(obj);
                }

                obj.ClearDirtyFlags();
            }

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
                    }
                }
            }

            int objectCount = snapshot.Count;
            LogManager.Debug($"Batch sync completed: {objectCount} ClientObjects synced for {playerBatches.Count} players.");
        }

        private static void ProcessPendingServerSyncs()
        {
            if (PendingServerSyncs.IsEmpty())
                return;

            List<ServerObject> snapshot = [.. PendingServerSyncs];
            PendingServerSyncs.Clear();

            foreach (ServerObject obj in snapshot)
            {
                try
                {
                    NetworkServer.UnSpawn(obj.Object);
                    NetworkServer.Spawn(obj.Object);
                    obj.ClearDirtyFlags();
                }
                catch (Exception ex)
                {
                    LogManager.Error($"Failed to sync object {obj.Name} - {obj.NetId}: {ex.Message}");
                    PendingServerSyncs.Add(obj);
                }
            }

            int objectCount = snapshot.Count;
            LogManager.Debug($"Batch sync completed: {objectCount} ServerObjects synced.");
        }
    }
}