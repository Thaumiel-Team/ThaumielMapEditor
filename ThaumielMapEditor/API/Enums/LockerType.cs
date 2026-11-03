// -----------------------------------------------------------------------
// <copyright file="LockerType.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU Lesser General Public License v3.0 (LGPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using ThaumielMapEditor.API.Blocks.Definitions;

namespace ThaumielMapEditor.API.Enums
{
    /// <summary>
    /// Defines the types that <see cref="Locker.Server"/> can use.
    /// </summary>
    [GitBookPage("Enums/LockerType")]
    public enum LockerType
    {
        None = 0,
        Pedestal = 1,
        LargeGun = 2,
        RifleRack = 3,
        Misc = 4,
        Medkit = 5,
        Adrenaline = 6,
        ExperimentalWeapon = 7,
    }
}