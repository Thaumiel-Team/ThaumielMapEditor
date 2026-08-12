// -----------------------------------------------------------------------
// <copyright file="ServerHandler.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using LabApi.Events.Arguments.ServerEvents;
using LabApi.Events.Arguments.WarheadEvents;
using LabApi.Events.Handlers;
using PlayerRoles;
using System.Collections.Generic;
using System.Linq;
using ThaumielMapEditor.API.Blocks;
using ThaumielMapEditor.API.Blocks.ClientSide;
using ThaumielMapEditor.API.Blocks.ServerObjects;
using ThaumielMapEditor.API.Data;
using ThaumielMapEditor.API.Enums;
using ThaumielMapEditor.API.Helpers;
using ThaumielMapEditor.API.Helpers.Networking;

namespace ThaumielMapEditor.Events
{
    internal class ServerHandler
    {
        public static bool RanUpdateCheck { get; private set; }

        public static void Register()
        {
            ServerEvents.WaitingForPlayers += OnWaitingForPlayers;
            ServerEvents.RoundStarted += OnRoundStart;
            ServerEvents.LczDecontaminationStarted += OnDecom;
            ServerEvents.WaveRespawned += OnWaveSpawned;
            WarheadEvents.Started += OnWarheadStarting;
            WarheadEvents.Detonated += OnWarheadDetonated;
            ServerEvents.RoomLightChanged += OnRoomLightChanged;
            ServerEvents.DeadmanSequenceActivating += OnDMSActived;
        }

        public static void Unregister()
        {
            ServerEvents.WaitingForPlayers -= OnWaitingForPlayers;
            ServerEvents.RoundStarted -= OnRoundStart;
            ServerEvents.LczDecontaminationStarted -= OnDecom; 
            ServerEvents.WaveRespawned -= OnWaveSpawned;
            WarheadEvents.Started -= OnWarheadStarting;
            WarheadEvents.Detonated -= OnWarheadDetonated;
            ServerEvents.RoomLightChanged -= OnRoomLightChanged;
            ServerEvents.DeadmanSequenceActivating -= OnDMSActived;
        }

        private static void OnDMSActived(DeadmanSequenceActivatingEventArgs ev)
        {
            foreach (PlayerSpawnPoint point in PlayerSpawnPoint.Instances.Where(p => p.HasFlagFast(DisableFlags.DeadmanSequenceActivated)))
            {
                point.Disabled = true;
            }
        }

        private static void OnWaveSpawned(WaveRespawnedEventArgs ev)
        {
            switch (ev.Wave.Faction)
            {
                case Faction.FoundationStaff:
                    foreach (PlayerSpawnPoint point in PlayerSpawnPoint.Instances.Where(p => p.HasFlagFast(DisableFlags.NTFWaveSpawned)))
                    {
                        point.Disabled = true;
                    }
                    break;

                case Faction.FoundationEnemy:
                    foreach (PlayerSpawnPoint point in PlayerSpawnPoint.Instances.Where(p => p.HasFlagFast(DisableFlags.ChaosWaveSpawned)))
                    {
                        point.Disabled = true;
                    }
                    break;
            }
        }

        // TODO Test.
        private static void OnRoomLightChanged(RoomLightChangedEventArgs ev)
        {
            foreach (SchematicData schematic in Loader.SchematicsById.Values)
            {
                if (schematic.Room == null || schematic.Room != ev.Room)
                    continue;

                foreach (ServerObject obj in schematic.SpawnedServerObjects)
                {
                    if (obj is LightObjectServer serverLight)
                        serverLight.Intensity = ev.NewState ? serverLight.Intensity : 0f;
                }

                foreach (ClientObject obj in schematic.SpawnedClientObjects)
                {
                    if (obj is LightObject light)
                        light.Intensity = ev.NewState ? light.Intensity : 0f;
                }
            }
        }

        private static void OnWaitingForPlayers()
        {
            PrefabHelper.RegisterPrefabs();
            Loader.Cleanup();

            if (!RanUpdateCheck)
            {
                RanUpdateCheck = true;
                MECHelper.TryRunCoroutine(Updater.CheckForUpdatesCoroutine(false), "WaitingForPlayers - Update Check");
            }

            foreach (string name in Main.Instance.Config!.WaitingForPlayers)
            {
                MapParser.ParseInput(name);
            }
        }

        private static void OnRoundStart()
        {
            foreach (string name in Main.Instance.Config!.RoundStarted)
            {
                MapParser.ParseInput(name);
            }
        }

        private static void OnDecom()
        {
            foreach (string name in Main.Instance.Config!.DecontaminationStarted)
            {
                MapParser.ParseInput(name);
            }

            foreach (PlayerSpawnPoint point in PlayerSpawnPoint.Instances.Where(p => p.HasFlagFast(DisableFlags.Decontamination)))
            {
                point.Disabled = true;
            }
        }

        private static void OnWarheadStarting(WarheadStartedEventArgs ev)
        {
            foreach (string name in Main.Instance.Config!.WarheadStarted)
            {
                MapParser.ParseInput(name);
            }
        }

        private static void OnWarheadDetonated(WarheadDetonatedEventArgs ev)
        {
            foreach (string name in Main.Instance.Config!.WarheadDetonated)
            {
                MapParser.ParseInput(name);
            }

            foreach (PlayerSpawnPoint point in PlayerSpawnPoint.Instances.Where(p => p.HasFlagFast(DisableFlags.WarheadDetonated)))
            {
                point.Disabled = true;
            }
        }
    }
}