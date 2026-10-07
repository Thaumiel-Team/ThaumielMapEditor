// -----------------------------------------------------------------------
// <copyright file="ObjectType.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

namespace ThaumielMapEditor.API.Enums
{
    /// <summary>
    /// Defines the types of objects that can be loaded.
    /// </summary>
    [GitBookPage("Enums/ObjectType")]
    public enum ObjectType
    {
        None = 0,
        Primitive = 1,
        Light = 2,
        Door = 3,
        Workstation = 4,
        Interactable = 5,
        TextToy = 6,
        Capybara = 7,
        Pickup = 8,
        Clutter = 9,
        Camera = 10,
        Waypoint = 11,
        Locker = 12,
        Target = 13,
        Schematic = 14,
        Teleporter = 15,
        GameObject = 16,
        Speaker = 17,
        PlayerSpawnPoint = 18,
        RagdollSpawner = 19,
    }
}