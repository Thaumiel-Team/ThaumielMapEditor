// -----------------------------------------------------------------------
// <copyright file="EventType.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
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
    }
}