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
using System.Linq;
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
            foreach (SchematicData schematic in Loader.SpawnedSchematics.Where(s => s.Room != null && s.Room == ev.Room))
            {
                if (schematic.GetClientObject<LightObject>().IsEmpty() && schematic.GetServerObject<LightObjectServer>().IsEmpty())
                    continue;

                foreach (LightObjectServer serverLight in schematic.GetServerObject<LightObjectServer>())
                {
                    float Intensity = 0;
                    Intensity = serverLight.Intensity;

                    if (!ev.NewState)
                    {
                        serverLight.Intensity = 0;
                    }
                    else
                        serverLight.Intensity = Intensity;
                }

                foreach (LightObject light in schematic.GetClientObject<LightObject>())
                {
                    float Intensity = 0;
                    Intensity = light.Intensity;

                    if (!ev.NewState)
                    {
                        light.Intensity = 0;
                    }
                    else
                        light.Intensity = Intensity;
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