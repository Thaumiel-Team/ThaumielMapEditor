// -----------------------------------------------------------------------
// <copyright file="SyncFlags.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using System;

namespace ThaumielMapEditor.API.Enums
{
    [Flags]
    public enum SyncFlags
    {
        None = 0,
        Position = 1 << 0,
        Scale = 1 << 1,
        Rotation = 1 << 2,
        IsStatic = 1 << 3,
        MovementSmoothing = 1 << 4,
        Parent = 1 << 5,

        // Primitives
        Color = 1 << 6,
        PrimitiveType = 1 << 7,
        PrimitiveFlags = 1 << 8,

        // Lights
        LightIntensity = 1 << 9,
        LightRange = 1 << 10,
        LightColor = 1 << 11,
        Shadows = 1 << 12,
        ShadowStrength = 1 << 13,
        LightType = 1 << 14,
        LightShape = 1 << 15,
        SpotAngle = 1 << 16,
        InnerSpotAngle = 1 << 17,

        // Capybaras
        Collisions = 1 << 18,
    }
}