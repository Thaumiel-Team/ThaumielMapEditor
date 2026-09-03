// -----------------------------------------------------------------------
// <copyright file="CullingObject.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using LabApi.Features.Wrappers;
using Mirror;
using ThaumielMapEditor.API.Blocks;
using ThaumielMapEditor.API.Blocks.ClientSide;
using ThaumielMapEditor.API.Helpers;
using UnityEngine;

namespace ThaumielMapEditor.API.Components
{
    public class CullingObject : TriggerHandler
    {
        public static readonly HashSet<CullingObject> AllInstances = [];
        public readonly HashSet<Player> PlayersInside = [];

        public NetworkIdentity? NetworkIdentity;
        public ClientObject? ClientObject;
        public ServerObject? ServerObject;
        public Vector3 Bounds;

        public void Init(ClientObject client, Vector3 bounds)
        {
            ClientObject = client;
            Bounds = bounds;
        }

        public void Init(ServerObject server, Vector3 bounds)
        {
            ServerObject = server;
            Bounds = bounds;

            if (server.Object != null)
                NetworkIdentity = server.Object.GetComponent<NetworkIdentity>();
        }

        public void Setup()
        {
            if (Collider == null)
            {
                LogManager.Warn($"CullingObject setup skipped: collider is null for '{ClientObject?.GetType().Name ?? ServerObject?.Name ?? "unknown"}'.");
                return;
            }

            Collider.size = Bounds;
            AllInstances.Add(this);
        }

        private void OnDestroy()
        {
            AllInstances.Remove(this);
        }

        public override void OnPlayerEntered(Player player)
        {
            if (!PlayersInside.Add(player))
                return;

            ToggleVisibility(player, true);
            foreach (Player spectator in player.CurrentSpectators)
            {
                ToggleVisibility(spectator, true);
            }
        }

        public override void OnPlayerExited(Player player)
        {
            if (!PlayersInside.Remove(player))
                return;

            ToggleVisibility(player, false);
            foreach (Player spectator in player.CurrentSpectators)
            {
                ToggleVisibility(spectator, false);
            }
        }

        public bool IsInsideCollider(Vector3 pos)
        {
            if (Collider == null)
                return false;

            return Collider.bounds.Contains(pos);
        }

        public void ToggleVisibility(Player player, bool show)
        {
            if (player == null || player.IsDestroyed)
                return;

            if (player.Connection == null || !player.Connection.isReady)
                return;

            try
            {
                if (ClientObject != null)
                {
                    if (show)
                    {
                        ClientObject.SpawnForPlayer(player);
                    }
                    else
                        ClientObject.DespawnForPlayer(player);
                }
                else if (ServerObject != null)
                {
                    if (NetworkIdentity == null)
                        return;

                    if (show)
                    {
                        NetworkServer.ShowForConnection(NetworkIdentity, player.Connection);
                    }
                    else
                        NetworkServer.HideForConnection(NetworkIdentity, player.Connection);
                }
            }
            catch (Exception ex)
            {
                LogManager.Error($"CullingObject ToggleVisibility failed: {ex.Message}");
            }
        }

        internal static void RemovePlayer(Player player)
        {
            foreach (CullingObject instance in AllInstances.ToArray())
            {
                instance.PlayersInside.Remove(player);
            }
        }
    }
}