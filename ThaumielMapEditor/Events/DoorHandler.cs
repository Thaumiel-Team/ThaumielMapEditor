// -----------------------------------------------------------------------
// <copyright file="DoorHandler.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using Interactables.Interobjects.DoorUtils;
using LabApi.Events.Arguments.ServerEvents;
using LabApi.Events.Handlers;
using ThaumielMapEditor.API.Components.Tools;
using ThaumielMapEditor.API.Enums;

namespace ThaumielMapEditor.Events
{
    internal static class DoorHandler
    {
        public static void Register()
        {
            ServerEvents.DoorDamaged += OnDoorDamaged;
            ServerEvents.DoorLockChanged += OnDoorLockChanged;
            DoorEvents.OnDoorAction += OnDoorAction;
        }

        public static void Unregister()
        {
            ServerEvents.DoorDamaged -= OnDoorDamaged;
            ServerEvents.DoorLockChanged -= OnDoorLockChanged;
            DoorEvents.OnDoorAction -= OnDoorAction;
        }

        private static void OnDoorDamaged(DoorDamagedEventArgs ev)
        {
            Dispatch(ev.Door?.Base, EventType.OnDoorDamaged);
        }

        private static void OnDoorLockChanged(DoorLockChangedEventArgs ev)
        {
            if (ev.Door?.Base == null)
                return;

            if (ev.PrevLockReason == 0 && ev.LockReason != 0)
            {
                Dispatch(ev.Door.Base, EventType.OnDoorLocked);
            }
            else if (ev.PrevLockReason != 0 && ev.LockReason == 0)
                Dispatch(ev.Door.Base, EventType.OnDoorUnlocked);
        }

        private static void OnDoorAction(DoorVariant variant, DoorAction action, ReferenceHub user)
        {
            EventType? eventType = action switch
            {
                DoorAction.Opened => EventType.OnDoorOpened,
                DoorAction.Closed => EventType.OnDoorClosed,
                DoorAction.Destroyed => EventType.OnDoorDestroyed,
                _ => null
            };

            if (eventType is { } type)
                Dispatch(variant, type);
        }

        private static void Dispatch(DoorVariant? variant, EventType eventType)
        {
            if (variant == null)
                return;

            if (variant.TryGetComponent(out DoorRuntime runtime))
                runtime.ExecuteEvent(eventType);
        }
    }
}
