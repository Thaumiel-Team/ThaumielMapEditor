// -----------------------------------------------------------------------
// <copyright file="SyncManager.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU Lesser General Public License v3.0 (LGPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using AdminToys;
using LabApi.Features.Wrappers;
using MEC;
using Mirror;
using System;
using System.Collections.Generic;
using ThaumielMapEditor.API.Enums;
using ThaumielMapEditor.API.Helpers;

namespace ThaumielMapEditor.API.Blocks
{
    [GitBookPage("Blocks/SyncManager")]
    public static class SyncManager
    {
        private static readonly Queue<ClientObject> PendingClientSyncs = new();
        private static readonly Queue<ServerObject> PendingServerSyncs = new();
        private static readonly Dictionary<ServerObject, int> ServerRetryCounts = [];
        private static readonly List<ClientObject> ClientBatch = new(256);
        private static readonly List<ServerObject> ServerBatch = new(32);

        private static Player[] playerBuffer = new Player[32];

        private static bool _scheduled;
        private static bool _processingClient;
        private static bool _processingServer;

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

            if (obj.PendingSync != 0)
                return;

            obj.PendingSync = 1;
            PendingClientSyncs.Enqueue(obj);
            ScheduleSync();
        }

        /// <summary>
        /// Registers a <see cref="ServerObject"/> to be synced at the end of the current frame.
        /// </summary>
        public static void RegisterForSync(ServerObject obj)
        {
            if (obj == null)
                return;

            if (obj.PendingSync != 0)
                return;

            obj.PendingSync = 1;
            PendingServerSyncs.Enqueue(obj);
            ScheduleSync();
        }

        private static void ScheduleSync()
        {
            if (_scheduled)
                return;

            _scheduled = true;

            try
            {
                Timing.RunCoroutine(EndOfFrameSyncCoroutine(), "TME_BatchSync");
            }
            catch
            {
                _scheduled = false;
                throw;
            }
        }

        /// <summary>
        /// Forces an immediate sync of all pending client objects.
        /// </summary>
        public static void FlushClient()
        {
            ProcessPendingClientSyncs();

            if (PendingClientSyncs.Count > 0)
                ScheduleSync();
        }

        /// <summary>
        /// Forces an immediate sync of all pending server objects.
        /// </summary>
        public static void FlushServer()
        {
            ProcessPendingServerSyncs();

            if (PendingServerSyncs.Count > 0)
                ScheduleSync();
        }

        /// <summary>
        /// Clears all pending client syncs without sending.
        /// </summary>
        public static void ClearClientPending()
        {
            while (PendingClientSyncs.Count > 0)
            {
                ClientObject? obj = PendingClientSyncs.Dequeue();
                obj?.PendingSync = 0;
            }
        }

        /// <summary>
        /// Clears all pending server syncs without sending.
        /// </summary>
        public static void ClearServerPending()
        {
            while (PendingServerSyncs.Count > 0)
            {
                ServerObject? obj = PendingServerSyncs.Dequeue();
                obj?.PendingSync = 0;
            }

            ServerRetryCounts.Clear();
        }

        internal static void RemoveFromPending(ClientObject obj)
        {
            if (obj == null)
                return;

            obj.PendingSync = 0;
        }

        internal static void RemoveFromPending(ServerObject obj)
        {
            if (obj == null)
                return;

            obj.PendingSync = 0;
            ServerRetryCounts.Remove(obj);
        }

        private static IEnumerator<float> EndOfFrameSyncCoroutine()
        {
            yield return Timing.WaitForOneFrame;

            try
            {
                ProcessPendingClientSyncs();
                ProcessPendingServerSyncs();
            }
            finally
            {
                _scheduled = false;
            }

            if (PendingClientSyncs.Count > 0 || PendingServerSyncs.Count > 0)
                ScheduleSync();
        }

        private static void ProcessPendingClientSyncs()
        {
            if (_processingClient || PendingClientSyncs.Count == 0)
                return;

            List<ClientObject> batch = ClientBatch;
            batch.Clear();

            while (PendingClientSyncs.Count > 0)
            {
                ClientObject? obj = PendingClientSyncs.Dequeue();
                if (obj == null)
                    continue;

                obj.PendingSync = 0;
                batch.Add(obj);
            }

            if (batch.Count == 0)
                return;

            _processingClient = true;

            try
            {
                int synced = 0;

                foreach (ClientObject obj in batch)
                {
                    if (obj == null)
                        continue;

                    SyncFlags flags = obj.SyncFlags;

                    if (flags == SyncFlags.None)
                        continue;

                    if (!obj.Spawned)
                    {
                        obj.ClearDirtyFlags(flags);
                        continue;
                    }

                    try
                    {
                        int count = CopySpawnedPlayers(obj);

                        if (!obj.TrySendBatchedDelta(flags, playerBuffer, count, out bool needsRetry))
                        {
                            obj.ClearDirtyFlags(flags);
                            continue;
                        }

                        if (needsRetry)
                        {
                            RegisterForSync(obj);
                            continue;
                        }

                        obj.ClearDirtyFlags(flags);
                        synced++;
                    }
                    catch (Exception ex)
                    {
                        LogManager.Error($"Failed to sync object {obj.NetId}: {ex.Message}");
                        RegisterForSync(obj);
                    }
                }

                LogManager.Debug($"Batch sync completed for {synced} of {batch.Count} ClientObjects.");
            }
            finally
            {
                _processingClient = false;
            }

            if (PendingClientSyncs.Count > 0)
                ScheduleSync();
        }

        private static void ProcessPendingServerSyncs()
        {
            if (_processingServer || PendingServerSyncs.Count == 0)
                return;

            List<ServerObject> batch = ServerBatch;
            batch.Clear();

            while (PendingServerSyncs.Count > 0)
            {
                ServerObject? obj = PendingServerSyncs.Dequeue();
                if (obj == null)
                    continue;

                obj.PendingSync = 0;
                batch.Add(obj);
            }

            if (batch.Count == 0)
                return;

            _processingServer = true;

            try
            {
                int synced = 0;

                foreach (ServerObject obj in batch)
                {
                    if (obj == null)
                        continue;

                    if (obj.Object == null || obj.NetworkIdentity == null)
                    {
                        obj.ClearDirtyFlags();
                        ServerRetryCounts.Remove(obj);
                        continue;
                    }

                    SyncFlags flags = obj.SyncFlags;

                    if (flags == SyncFlags.None)
                        continue;

                    try
                    {
                        if (TryApplyLiveServerDelta(obj, flags))
                        {
                            obj.ClearDirtyFlags(flags);
                            ServerRetryCounts.Remove(obj);
                            synced++;
                            continue;
                        }

                        NetworkServer.UnSpawn(obj.Object);
                        NetworkServer.Spawn(obj.Object);
                        obj.ClearDirtyFlags(flags);
                        ServerRetryCounts.Remove(obj);
                        synced++;
                    }
                    catch (Exception ex)
                    {
                        LogManager.Error($"Failed to sync object {obj.Name} - {obj.NetId}: {ex.Message}");

                        ServerRetryCounts.TryGetValue(obj, out int previous);
                        int count = previous + 1;

                        if (count >= 3)
                        {
                            ServerRetryCounts.Remove(obj);
                            LogManager.Warn($"Dropping server sync for '{obj.Name}' after {count} failures.");
                        }
                        else
                        {
                            ServerRetryCounts[obj] = count;
                            RegisterForSync(obj);
                        }
                    }
                }

                LogManager.Debug($"Batch sync completed for {synced} of {batch.Count} ServerObjects.");
            }
            finally
            {
                _processingServer = false;
            }

            if (PendingServerSyncs.Count > 0)
                ScheduleSync();
        }

        private static bool TryApplyLiveServerDelta(ServerObject obj, SyncFlags flags)
        {
            AdminToyBase? toy = obj.Toy;

            if (toy != null)
            {
                if (flags.HasFlagFast(SyncFlags.MovementSmoothing))
                    toy.NetworkMovementSmoothing = obj.MovementSmoothing != 0 ? (byte)(256 - obj.MovementSmoothing) : (byte)0;

                bool hasTransform = flags.HasFlagFast(SyncFlags.Position) || flags.HasFlagFast(SyncFlags.Rotation) || flags.HasFlagFast(SyncFlags.Scale);

                if (!hasTransform)
                    return true;

                if (toy.IsStatic)
                    return false;

                if (flags.HasFlagFast(SyncFlags.Position))
                    toy.NetworkPosition = toy.CachedTransform.localPosition;

                if (flags.HasFlagFast(SyncFlags.Rotation))
                    toy.NetworkRotation = toy.CachedTransform.localRotation;

                if (flags.HasFlagFast(SyncFlags.Scale))
                    toy.NetworkScale = toy.CachedTransform.localScale;

                return true;
            }

            return obj.PositionSync != null && (flags & ~SyncFlags.Position) == SyncFlags.None;
        }

        private static int CopySpawnedPlayers(ClientObject obj)
        {
            int count = obj.SpawnedPlayers.Count;

            if (count == 0)
                return 0;

            if (playerBuffer.Length < count)
                Array.Resize(ref playerBuffer, Math.Max(count, playerBuffer.Length * 2));

            obj.SpawnedPlayers.CopyTo(playerBuffer);
            return count;
        }
    }
}