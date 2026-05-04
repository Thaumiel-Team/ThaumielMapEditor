// -----------------------------------------------------------------------
// <copyright file="DisableFlags.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using System;

namespace ThaumielMapEditor.API.Enums
{
    [Flags]
    public enum DisableFlags
    {
        None = 0,
        Used = 1 << 0,
        Decontamination = 1 << 1,
        WarheadDetonated = 1 << 2,
        NTFWaveSpawned = 1 << 3,
        ChaosWaveSpawned = 1 << 4,
        DeadmanSequenceActivated = 1 << 5,
        AnySpawned = NTFWaveSpawned | ChaosWaveSpawned,
    }
}