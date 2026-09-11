// -----------------------------------------------------------------------
// <copyright file="PlayerHandler.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Arguments.Scp079Events;
using LabApi.Events.Handlers;
using LabApi.Features.Wrappers;
using MEC;
using System.Collections.Generic;
using System.Linq;
using ThaumielMapEditor.API.Blocks;
using ThaumielMapEditor.API.Blocks.ClientSide;
using ThaumielMapEditor.API.Blocks.ServerObjects;
using ThaumielMapEditor.API.Components;
using ThaumielMapEditor.API.Data;
using ThaumielMapEditor.API.Extensions;
using ThaumielMapEditor.API.Helpers;
using ThaumielMapEditor.Commands.Admin;

namespace ThaumielMapEditor.Events
{
    internal class PlayerHandler
    {

        public static void Register()
        {
            PlayerEvents.Joined += OnPlayerJoined;
            PlayerEvents.Spawned += PlayerSpawnPoint.OnPlayerSpawned;
            Scp079Events.ChangedCamera += OnScp079ChangedCamera;
            ReferenceHub.OnBeforePlayerDestroyed += OnPlayerLeft;
        }

        public static void Unregister()
        {
            PlayerEvents.Joined -= OnPlayerJoined;
            PlayerEvents.Spawned -= PlayerSpawnPoint.OnPlayerSpawned;
            Scp079Events.ChangedCamera -= OnScp079ChangedCamera;
            ReferenceHub.OnBeforePlayerDestroyed -= OnPlayerLeft;
        }

        private static void OnScp079ChangedCamera(Scp079ChangedCameraEventArgs ev)
        {
            if (ev.Player == null || ev.Camera == null)
                return;

            foreach (CullingObject cullingZone in CullingObject.AllInstances.ToArray())
            {
                cullingZone.ToggleVisibility(ev.Player, cullingZone.IsInsideCollider(ev.Camera.Position));
            }
        }

        private static void OnPlayerLeft(ReferenceHub hub)
        {
            if (!Player.TryGet(hub.gameObject, out var player))
            {
                LogManager.Warn($"Failed to get leaving player.");
                return;
            }

            if (player.IsHost)
                return;

            foreach (SchematicData data in Loader.SpawnedSchematics.ToArray())
            {
                foreach (ClientObject clientobj in data.SpawnedClientObjects.ToArray())
                {
                    if (!clientobj.SpawnedPlayers.Contains(player))
                        continue;

                    clientobj.SpawnedPlayers.Remove(player);
                    SyncManager.RemoveFromPending(clientobj);
                }
            }

            CullingObject.RemovePlayer(player);
            PlayerExtensions.EffectCache.Remove(player);
            LODHelper.PlayersInLODZones.Remove(player);
            Grab.ReleasePlayer(player);
        }

        private static void OnPlayerJoined(PlayerJoinedEventArgs ev)
        {
            if (ev.Player == null || ev.Player.IsHost || ev.Player.IsDummy)
            {
                LogManager.Warn($"Player was null when joined.");
                return;
            }

            Timing.RunCoroutine(SyncPlayerWhenReady(ev.Player, ev.Player.DisplayName));
            if (!ev.Player.IsDestroyed && ev.Player.GameObject != null)
                ev.Player.GameObject.AddComponent<CullingUpdater>().Init(ev.Player);
        }

        private static IEnumerator<float> SyncPlayerWhenReady(Player player, string name)
        {
            float timeout = 30f;
            while (!player.IsReady && timeout > 0f)
            {
                if (player.IsDestroyed)
                    yield break;

                timeout -= Timing.DeltaTime;
                yield return Timing.WaitForOneFrame;
            }

            if (player.IsDestroyed)
            {
                LogManager.Warn($"Player with name {name} was destroyed before sync could run.");
                yield break;
            }

            if (!player.IsReady)
            {
                LogManager.Warn($"Timed out waiting for player {name} to be ready; skipping schematic sync.");
                yield break;
            }

            foreach (SchematicData data in Loader.SchematicsById.Values.ToArray())
            {
                LogManager.Debug($"Syncing {data.FileName} to {name}");
                data.SyncWithPlayer(player);
            }

            CreditHelper.SetTag(player);
        }
    }
}