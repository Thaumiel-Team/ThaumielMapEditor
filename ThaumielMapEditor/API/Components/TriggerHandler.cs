// -----------------------------------------------------------------------
// <copyright file="TriggerHandler.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using Interactables.Interobjects.DoorUtils;
using InventorySystem.Items.Pickups;
using LabApi.Features.Wrappers;
using UnityEngine;

namespace ThaumielMapEditor.API.Components
{
    public class TriggerHandler : MonoBehaviour
    {
        /// <summary>
        /// Gets the box collider associated with the trigger.
        /// </summary>
        public BoxCollider? Collider { get; private set; }

        private void Awake()
        {
            if (!TryGetComponent<BoxCollider>(out var collider))
                collider = gameObject.AddComponent<BoxCollider>();

            collider.isTrigger = true;
            Collider = collider;
        }

        public virtual void OnPlayerEntered(Player player) { }
        public virtual void OnPlayerExited(Player player) { }
        public virtual void OnDoorEntered(DoorVariant door) { }
        public virtual void OnDoorExited(DoorVariant door) { }
        public virtual void OnPickupEntered(Pickup pickup) { }
        public virtual void OnPickupExited(Pickup pickup) { }
        public virtual void OnObjectEntered(GameObject obj) { }
        public virtual void OnObjectExited(GameObject obj) { }

        private void OnTriggerEnter(Collider other)
        {
            if (Player.TryGet(other.gameObject, out var player))
            {
                OnPlayerEntered(player);
                return;
            }

            if (other.gameObject.TryGetComponent<ItemPickupBase>(out var pickupbase))
            {
                OnPickupEntered(Pickup.Get(pickupbase));
                return;
            }

            if (other.gameObject.GetComponentInParent<DoorVariant>() is { } door)
            {
                OnDoorEntered(door);
                return;
            }

            OnObjectEntered(other.gameObject);
        }

        private void OnTriggerExit(Collider other)
        {
            if (Player.TryGet(other.gameObject, out var player))
            {
                OnPlayerExited(player);
                return;
            }

            if (other.gameObject.TryGetComponent<ItemPickupBase>(out var pickupbase))
            {
                OnPickupExited(Pickup.Get(pickupbase));
                return;
            }

            if (other.gameObject.GetComponentInParent<DoorVariant>() is { } door)
            {
                OnDoorExited(door);
                return;
            }

            OnObjectExited(other.gameObject);
        }
    }
}