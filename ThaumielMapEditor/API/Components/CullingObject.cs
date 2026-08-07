// -----------------------------------------------------------------------
// <copyright file="CullingObject.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using LabApi.Features.Wrappers;
using Mirror;
using ThaumielMapEditor.API.Blocks;
using ThaumielMapEditor.API.Blocks.ClientSide;
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

            NetworkIdentity = ServerObject.Object!.GetComponent<NetworkIdentity>();
        }

        public void Setup()
        {
            if (Collider == null)
                return;

            Collider.size = Bounds;
            AllInstances.Add(this);

            OnPlayerEntered += PlayerEntered;
            OnPlayerExited += PlayerExited;
        }

        private void OnDestroy()
        {
            AllInstances.Remove(this);
            
            OnPlayerEntered -= PlayerEntered;
            OnPlayerExited -= PlayerExited;
        }

        private void PlayerEntered(Player player, Collider _)
        {
            if (!PlayersInside.Add(player))
                return;

            ToggleVisibility(player, true);
            foreach (Player spectator in player.CurrentSpectators)
            {
                ToggleVisibility(spectator, true);
            }
        }

        private void PlayerExited(Player player, Collider _)
        {
            if (!PlayersInside.Remove(player)) return;

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
    }
}