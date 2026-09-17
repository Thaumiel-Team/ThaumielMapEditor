// -----------------------------------------------------------------------
// <copyright file="CameraType.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using ThaumielMapEditor.API.Attributes;
using ThaumielMapEditor.API.Blocks.ServerObjects;

namespace ThaumielMapEditor.API.Enums
{
    /// <summary>
    /// Defines the types that <see cref="ClutterObject"/> can use.
    /// </summary>
    [GitBookPage("Enums/CameraType")]
    public enum CameraType
    {
        /// <summary>
        /// Light containment zone.
        /// </summary>
        Lcz = 0,

        /// <summary>
        /// Heavy containment zone.
        /// </summary>
        Hcz = 1,

        /// <summary>
        /// Entrance zone.
        /// </summary>
        Ez = 2,

        /// <summary>
        /// Entrance zone but with a arm.
        /// </summary>
        EzArm = 3,

        /// <summary>
        /// Surface zone
        /// </summary>
        Sz = 4
    }
}