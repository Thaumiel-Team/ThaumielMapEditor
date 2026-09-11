// -----------------------------------------------------------------------
// <copyright file="CullingUpdater.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using LabApi.Features.Wrappers;
using ThaumielMapEditor.API.Blocks.ClientSide;
using ThaumielMapEditor.API.Data;
using ThaumielMapEditor.API.Helpers;
using UnityEngine;

namespace ThaumielMapEditor.API.Components
{
    public class CullingUpdater : MonoBehaviour
    {
        private static int _phaseCounter;

        public Player? Player;
        public Player? LastTarget;
        private int _phase;

        public static int UpdateIntervalFrames
        {
            get;
            set => field = Mathf.Max(1, value);
        } = 20;

        public void Init(Player player)
        {
            Player = player;
            LastTarget = null;
            _phase = Mathf.Abs(Interlocked.Increment(ref _phaseCounter) % UpdateIntervalFrames);
            enabled = true;
        }

        private void LateUpdate()
        {
            int interval = UpdateIntervalFrames;
            if (Time.frameCount % interval != _phase % interval)
                return;

            Player? spectator = Player;
            if (spectator == null || spectator.IsDestroyed)
            {
                enabled = false;
                return;
            }

            if (spectator.IsAlive)
            {
                ClearSpectatedState(spectator);
                return;
            }

            Player? current = spectator.CurrentlySpectating;
            if (current == LastTarget)
                return;

            if (!IsSpectatorUsable(spectator))
                return;

            Player? previous = LastTarget;
            LastTarget = current;

            SyncSpectatorLod(previous, current, spectator);
            SyncSpectatorCulling(previous, current, spectator);
        }

        private void ClearSpectatedState(Player spectator)
        {
            Player? stale = LastTarget;
            if (stale == null)
                return;

            LastTarget = null;

            if (stale.IsDestroyed || !IsSpectatorUsable(spectator))
                return;

            if (LODHelper.PlayersInLODZones.TryGetValue(stale, out HashSet<LODZone>? staleZones) && staleZones.Count > 0)
            {
                LODHelper.PlayersInLODZones.TryGetValue(spectator, out HashSet<LODZone>? ownZones);

                HashSet<PrimitiveObject> stalePrimitives = [];
                foreach (LODZone zone in staleZones)
                {
                    if (zone == null || (ownZones != null && ownZones.Contains(zone)))
                        continue;

                    CollectLodPrimitives(zone, stalePrimitives);
                }

                foreach (PrimitiveObject primitive in stalePrimitives)
                {
                    if (primitive.IsSpawnedForPlayer(spectator))
                        primitive.DespawnForPlayer(spectator);
                }
            }

            if (CullingObject.AllInstances.Count == 0)
                return;

            foreach (CullingObject zone in CullingObject.AllInstances.ToArray())
            {
                if (zone.PlayersInside.Contains(stale) && !zone.PlayersInside.Contains(spectator))
                    zone.ToggleVisibility(spectator, false);
            }
        }

        private static void SyncSpectatorCulling(Player? oldTarget, Player? newTarget, Player spectator)
        {
            if (oldTarget == newTarget || CullingObject.AllInstances.Count == 0)
                return;

            foreach (CullingObject zone in CullingObject.AllInstances.ToArray())
            {
                bool wasShown = oldTarget != null && zone.PlayersInside.Contains(oldTarget);
                bool shouldShow = newTarget != null && zone.PlayersInside.Contains(newTarget);

                if (wasShown == shouldShow)
                    continue;

                zone.ToggleVisibility(spectator, shouldShow);
            }
        }

        private static void SyncSpectatorLod(Player? oldTarget, Player? newTarget, Player spectator)
        {
            if (oldTarget == newTarget)
                return;

            HashSet<PrimitiveObject> oldPrimitives = oldTarget != null ? CollectLodPrimitives(oldTarget) : [];
            HashSet<PrimitiveObject> newPrimitives = newTarget != null ? CollectLodPrimitives(newTarget) : [];

            foreach (PrimitiveObject primitive in oldPrimitives)
            {
                if (newPrimitives.Contains(primitive) || !primitive.IsSpawnedForPlayer(spectator))
                    continue;

                primitive.DespawnForPlayer(spectator);
            }

            foreach (PrimitiveObject primitive in newPrimitives)
            {
                if (oldPrimitives.Contains(primitive) || primitive.IsSpawnedForPlayer(spectator))
                    continue;

                primitive.ShowForPlayer(spectator);
            }
        }

        private static HashSet<PrimitiveObject> CollectLodPrimitives(Player target)
        {
            HashSet<PrimitiveObject> result = [];

            if (LODHelper.PlayersInLODZones.TryGetValue(target, out HashSet<LODZone>? zones))
            {
                foreach (LODZone zone in zones)
                    CollectLodPrimitives(zone, result);
            }

            return result;
        }

        private static void CollectLodPrimitives(LODZone zone, HashSet<PrimitiveObject> result)
        {
            if (zone == null)
                return;

            SchematicData? schematic = zone.Schematic;
            if (schematic == null || zone.PrimitivestoUnload.Count == 0)
                return;

            HashSet<PrimitiveType> unload = zone.PrimitivestoUnload.ToHashSet();
            foreach (PrimitiveObject primitive in schematic.GetClientObject<PrimitiveObject>())
            {
                if (unload.Contains(primitive.PrimitiveType))
                    result.Add(primitive);
            }
        }

        internal static void UpdateSpectatorLOD(Player? target, Player spectator, bool isNowVisible)
        {
            if (target == null || target.IsDestroyed || !IsSpectatorUsable(spectator))
                return;

            if (isNowVisible)
            {
                SyncSpectatorLod(null, target, spectator);
            }
            else
                SyncSpectatorLod(target, null, spectator);
        }

        private static bool IsSpectatorUsable(Player spectator) =>
            !spectator.IsHost && !spectator.IsDestroyed && spectator.Connection != null && spectator.Connection.isReady;
    }
}
