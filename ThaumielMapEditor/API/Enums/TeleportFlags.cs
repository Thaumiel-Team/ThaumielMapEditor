// -----------------------------------------------------------------------
// <copyright file="TeleportFlags.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using System;

namespace ThaumielMapEditor.API.Enums
{
    [Flags]
    public enum TeleporterFlags
    {
        None = 0,
        AllowPickups = 1 << 0,
        AllowPlayers = 1 << 1,
        AllowProjectiles = 1 << 2
    }
}