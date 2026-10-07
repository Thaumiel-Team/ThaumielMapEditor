// -----------------------------------------------------------------------
// <copyright file="LockerChamber.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using Interactables.Interobjects.DoorUtils;

namespace ThaumielMapEditor.API.Blocks.ServerObjects.Lockers
{
    /// <summary>
    /// Represents a single chamber configuration for a locker.
    /// A locker may contain multiple chambers, each with permissions and item spawn definitions.
    /// </summary>
    [GitBookPage("Blocks/Server/LockerObject/LockerChamber")]
    public class LockerChamber
    {
        /// <summary>
        /// Gets or sets the zero-based index of this chamber within the parent locker.
        /// </summary>
        public uint Index { get; set; }

        /// <summary>
        /// Gets or sets the Permissions that control which players or roles can open or interact with this chamber.
        /// </summary>
        public DoorPermissionFlags Permissions { get; set; }

        /// <summary>
        /// Gets or sets the Item spawn definitions for this chamber.
        /// Each <see cref="ChamberData"/> entry describes an item type, its spawn probability, and the quantity to spawn.
        /// </summary>
        public List<ChamberData> Data { get; set; } = [];
    }

    /// <summary>
    /// Describes a single item entry that can be spawned inside a locker chamber.
    /// </summary>
    [GitBookPage("Blocks/Server/LockerObject/ChamberData")]
    public class ChamberData
    {
        /// <summary>
        /// Gets or sets the type of item to spawn.
        /// </summary>
        public ItemType ItemType { get; set; }

        /// <summary>
        /// Gets or sets the chance this entry is selected when populating the chamber, expressed as a percentage (e.g., 50.0 = 50%).
        /// </summary>
        public float SpawnPercent { get; set; }

        /// <summary>
        /// Gets or sets the Number of instances to spawn when this entry is selected.
        /// </summary>
        public int AmountToSpawn { get; set; }
    }
}