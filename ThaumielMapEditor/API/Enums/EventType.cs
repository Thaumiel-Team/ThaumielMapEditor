// -----------------------------------------------------------------------
// <copyright file="EventType.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU Lesser General Public License v3.0 (LGPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

namespace ThaumielMapEditor.API.Enums
{
    [GitBookPage("Enums/EventType")]
    public enum EventType
    {
        OnSpawned,
        OnDestroyed,
        OnTriggerEntered,
        OnTriggerExited,
        OnInteraction,
        OnInteractionDenied,
        OnDoorDestroyed,
        OnDoorOpened,
        OnDoorClosed,
        OnDoorLocked,
        OnDoorDamaged,
        OnDoorUnlocked,
        OnAnimationKey,
    }
}