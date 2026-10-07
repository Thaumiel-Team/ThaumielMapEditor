// -----------------------------------------------------------------------
// <copyright file="ClutterType.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using ThaumielMapEditor.API.Blocks.ServerObjects;

namespace ThaumielMapEditor.API.Enums
{
    /// <summary>
    /// Defines the types that <see cref="ClutterObject"/> can use.
    /// </summary>
    [GitBookPage("Enums/ClutterType")]
    public enum ClutterType
    {
        SimpleBoxes = 0,
        PipesShort,
        BoxesLadder,
        TankSupportedShelf,
        AngledFences,
        HugeOrangePipes,
        PipesLongOpen,
        BrokenElectricalBox
    }
}