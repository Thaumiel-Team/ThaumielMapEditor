// -----------------------------------------------------------------------
// <copyright file="PickupPreview.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using LabApi.Events.Arguments.ServerEvents;
using LabApi.Events.Handlers;
using LabApi.Features.Wrappers;
using ThaumielMapEditor.API.Blocks.ServerObjects;

namespace ThaumielMapEditor.ToolGun.Utilities
{
    [GitBookPage("ToolGun/Utilities")]
    public static class PickupPreview
    {
        private static readonly Dictionary<PickupObject, Pickup> Previews = [];
        private static readonly Dictionary<ToolGunSettings, PickupObject> Owners = [];
        private static bool Registered;

        public static void Register()
        {
            if (Registered)
                return;

            Registered = true;
            ServerEvents.WaitingForPlayers += OnWaitingForPlayers;
            ServerEvents.PickupDestroyed += OnPickupDestroyed;
        }

        public static void Unregister()
        {
            if (!Registered)
                return;

            Registered = false;
            ServerEvents.WaitingForPlayers -= OnWaitingForPlayers;
            ServerEvents.PickupDestroyed -= OnPickupDestroyed;
            ClearAll();
        }

        /// <summary>
        /// Shows a preview for <paramref name="pickup"/> owned by <paramref name="settings"/>.
        /// Does nothing when the pickup already has a live object.
        /// </summary>
        public static void Show(ToolGunSettings settings, PickupObject pickup)
        {
            if (settings == null || pickup == null)
                return;

            Hide(settings);

            if (pickup.Object != null)
                return;

            Owners[settings] = pickup;

            if (Previews.TryGetValue(pickup, out Pickup? existing))
            {
                if (IsValid(existing))
                {
                    Sync(pickup);
                    return;
                }

                Previews.Remove(pickup);
            }

            Pickup? preview = Pickup.Create(pickup.ItemToSpawn, pickup.Position, pickup.Rotation);
            if (preview == null)
                return;

            preview.Spawn();
            preview.IsLocked = true;
            Previews[pickup] = preview;
        }

        /// <summary>
        /// Drops <paramref name="settings"/> ownership of its preview, destroying the preview pickup when no other selection still references it.
        /// </summary>
        public static void Hide(ToolGunSettings settings)
        {
            if (settings == null || !Owners.TryGetValue(settings, out PickupObject? pickup) || pickup == null)
                return;

            Owners.Remove(settings);

            if (Owners.Values.Any(o => ReferenceEquals(o, pickup)))
                return;

            DestroyPreview(pickup);
        }

        /// <summary>
        /// Recreates the preview for <paramref name="pickup"/> (e.g. after its item type changed), or removes it when the pickup spawned a live object.
        /// </summary>
        public static void Refresh(PickupObject pickup)
        {
            if (pickup == null || !Previews.ContainsKey(pickup))
                return;

            DestroyPreview(pickup);

            if (pickup.Object != null || !Owners.Values.Any(o => ReferenceEquals(o, pickup)))
                return;

            Pickup? preview = Pickup.Create(pickup.ItemToSpawn, pickup.Position, pickup.Rotation);
            if (preview == null)
                return;

            preview.Spawn();
            preview.IsLocked = true;
            Previews[pickup] = preview;
        }

        /// <summary>
        /// Moves the preview for <paramref name="pickup"/> to its current transform.
        /// </summary>
        public static void Sync(PickupObject pickup)
        {
            if (pickup == null || !Previews.TryGetValue(pickup, out Pickup? preview) || !IsValid(preview))
                return;

            preview.Position = pickup.Position;
            preview.Rotation = pickup.Rotation;
        }

        public static void ClearAll()
        {
            foreach (PickupObject pickup in Previews.Keys.ToArray())
                DestroyPreview(pickup);

            Owners.Clear();
        }

        private static bool IsValid(Pickup? preview)
        {
            if (preview == null)
                return false;

            return !preview.IsDestroyed && preview.IsSpawned;
        }

        private static void DestroyPreview(PickupObject pickup)
        {
            if (!Previews.TryGetValue(pickup, out Pickup? preview) || preview == null)
                return;

            Previews.Remove(pickup);

            if (!preview.IsDestroyed)
                preview.Destroy();
        }

        private static void OnWaitingForPlayers() => ClearAll();

        private static void OnPickupDestroyed(PickupDestroyedEventArgs ev)
        {
            if (ev.Pickup == null)
                return;

            foreach (KeyValuePair<PickupObject, Pickup> entry in Previews.ToArray())
            {
                if (ReferenceEquals(entry.Value, ev.Pickup))
                    Previews.Remove(entry.Key);
            }
        }
    }
}
