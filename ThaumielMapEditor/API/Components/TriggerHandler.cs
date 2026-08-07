// -----------------------------------------------------------------------
// <copyright file="TriggerHandler.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using InventorySystem.Items.Pickups;
using LabApi.Features.Wrappers;
using System;
using UnityEngine;

namespace ThaumielMapEditor.API.Components
{
    public class TriggerHandler : MonoBehaviour
    {
        /// <summary>
        /// Gets the box collider associated with the trigger.
        /// </summary>
        public BoxCollider? Collider { get; private set; }

        /// <summary>
        /// Fired when a <see cref="Player"/> enters the bounds of the <see cref="TriggerHandler"/>
        /// </summary>
        public event Action<Player, Collider>? OnPlayerEntered;

        /// <summary>
        /// Fired when a <see cref="Player"/> leaves the bounds of the <see cref="TriggerHandler"/>
        /// </summary>
        public event Action<Player, Collider>? OnPlayerExited;

        /// <summary>
        /// Fired when a <see cref="Pickup"/> enters the bounds of the <see cref="TriggerHandler"/>
        /// </summary>
        public event Action<Pickup, Collider>? OnPickupEntered;

        /// <summary>
        /// Fired when a <see cref="Pickup"/> leaves the bounds of the <see cref="TriggerHandler"/>
        /// </summary>
        public event Action<Pickup, Collider>? OnPickupExited;

        /// <summary>
        /// Fired when a <see cref="Projectile"/> enters the bounds of the <see cref="TriggerHandler"/>
        /// </summary>
        public event Action<Projectile, Collider>? OnProjectileEntered;

        /// <summary>
        /// Fired when a <see cref="Projectile"/> leaves the bounds of the <see cref="TriggerHandler"/>
        /// </summary>
        public event Action<Projectile, Collider>? OnProjectileExited;

        private void Awake()
        {
            if (!TryGetComponent<BoxCollider>(out var collider))
                collider = gameObject.AddComponent<BoxCollider>();

            collider.isTrigger = true;
            Collider = collider;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (Player.TryGet(other.gameObject, out var player))
            {
                OnPlayerEntered?.Invoke(player, other);
                return;
            }

            if (other.gameObject.TryGetComponent<ItemPickupBase>(out var pickupbase) && Pickup.TryGet(pickupbase.Info.Serial, out var pickup))
            {
                if (pickup is Projectile projectile)
                {
                    OnProjectileEntered?.Invoke(projectile, other);
                }
                else
                    OnPickupEntered?.Invoke(pickup, other);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (Player.TryGet(other.gameObject, out var player))
            {
                OnPlayerExited?.Invoke(player, other);
                return;
            }

            if (other.gameObject.TryGetComponent<ItemPickupBase>(out var pickupbase) && Pickup.TryGet(pickupbase.Info.Serial, out var pickup))
            {
                if (pickup is Projectile projectile)
                {
                    OnProjectileExited?.Invoke(projectile, other);
                }
                else
                    OnPickupExited?.Invoke(pickup, other);   
            }
        }
    }
}