// -----------------------------------------------------------------------
// <copyright file="ToolGunSettings.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using LabApi.Features.Wrappers;
using System;
using System.Collections.Generic;
using ThaumielMapEditor.API.Blocks;
using ThaumielMapEditor.API.Blocks.ClientSide;
using ThaumielMapEditor.API.Enums;

namespace ThaumielMapEditor.ToolGun.Utilities
{
    [GitBookPage("ToolGun/Settings")]
    public class ToolGunSettings
    {
        public ToolGunSettings(ToolGun item)
        {
            Item = item;
        }

        public ToolGunSettings(ToolGun item, Player? owner)
        {
            Item = item;
            Owner = owner;
        }
        
        public Player? Owner { get; internal set; }
        public ToolGun? Item { get; }

        public bool Deleting { get; set; }
        public ObjectType SelectedObjectType { get; set; }

        public DoorType SpawnDoorType { get; set; } = DoorType.Hcz;
        public LockerType SpawnLockerType { get; set; } = LockerType.Misc;
        public ItemType SpawnPickupItem { get; set; } = ItemType.GunCOM15;

        public bool ObjectSelected => SelectedServer != null || SelectedClient != null;
        public ServerObject? SelectedServer { get; set; }
        public ClientObject? SelectedClient { get; set; }

        /// <summary>
        /// Builds the extra spawn values for <see cref="SelectedObjectType"/>, or <see langword="null"/> when the type needs no extra values.
        /// </summary>
        public Dictionary<string, object>? GetSpawnValues() => SelectedObjectType switch
        {
            ObjectType.Door => new(StringComparer.OrdinalIgnoreCase) { ["DoorType"] = SpawnDoorType },
            ObjectType.Locker => new(StringComparer.OrdinalIgnoreCase) { ["LockerType"] = SpawnLockerType == LockerType.None ? LockerType.Misc : SpawnLockerType },
            ObjectType.Pickup => new(StringComparer.OrdinalIgnoreCase) { ["ItemToSpawn"] = (int)SpawnPickupItem < 0 ? ItemType.GunCOM15 : SpawnPickupItem },
            _ => null,
        };

        /// <summary>
        /// Short label of the spawn variant for <see cref="SelectedObjectType"/>, or <see cref="string.Empty"/> when the type has no variant.
        /// </summary>
        public string SpawnVariantLabel() => SelectedObjectType switch
        {
            ObjectType.Door => $" ({SpawnDoorType})",
            ObjectType.Locker => $" ({SpawnLockerType})",
            ObjectType.Pickup => $" ({SpawnPickupItem})",
            _ => string.Empty,
        };
    }
}