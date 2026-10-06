// -----------------------------------------------------------------------
// <copyright file="ToolGun.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using CustomItemsAPI.Items;
using InventorySystem.Items.Firearms.Attachments;
using InventorySystem.Items.Firearms.Modules;
using LabApi.Features.Wrappers;
using LabApiExtensions.Configs;
using LabApiExtensions.Enums;
using LabApiExtensions.Helpers;
using System.Collections.Generic;
using ThaumielMapEditor.API.Enums;
using ThaumielMapEditor.API.Extensions;
using ThaumielMapEditor.API.Helpers;
using ThaumielMapEditor.ToolGun.Utilities;
using ThaumielMapEditor.ToolGun.Utilities.Settings;
using UnityEngine;

namespace ThaumielMapEditor.ToolGun
{
    public class ToolGun : CustomFirearmBase
    {
        public override string DisplayName { get; set; } = "TME ToolGun";
        public override string CustomItemName { get; set; } = "tmetoolgun";
        public override string Description { get; set; } = "The ToolGun for TME.";
        public override ItemType Type { get; } = ItemType.GunCOM18;
        public override List<AttachmentName> AttachmentNames => [AttachmentName.Flashlight, AttachmentName.DotSight];
        public override MathValueFloat Damage { get; } = new(MathOption.Set, 0);

        public static Dictionary<ushort, ToolGunSettings> SettingsBySerial = [];
        public static Dictionary<ushort, LinearAdsModule> ADSCache = [];

        public override void Parse(Item item)
        {
            if (!SettingsBySerial.TryGetValue(item.Serial, out ToolGunSettings settings))
            {
                settings = new ToolGunSettings(this, item.CurrentOwner);
                SettingsBySerial[item.Serial] = settings;
            }

            base.Parse(item);
        }

        public override void Parse(Pickup pickup)
        {
            if (!SettingsBySerial.TryGetValue(pickup.Serial, out ToolGunSettings settings))
            {
                settings = new ToolGunSettings(this);
                SettingsBySerial[pickup.Serial] = settings;
            }

            base.Parse(pickup);
        }

        public override void OnChanged(Player player, Item? oldItem, Item? newItem, bool changedToThisItem)
        {
            if (!changedToThisItem)
            {
                ToolGunUtils.ClearAllSelectionsForPlayer(player);
                ToolGunMenu.Unequip(player);

                if (player.GameObject == null)
                    return;

                if (player.GameObject.TryGetComponent<ToolGunUI>(out var oldui))
                    oldui.enabled = false;

                return;
            }

            ToolGunMenu.Equip(player);

            if (newItem == null || player.GameObject == null)
                return;

            if (!SettingsBySerial.TryGetValue(newItem.Serial, out ToolGunSettings settings))
            {
                settings = new ToolGunSettings(this, newItem.CurrentOwner);
                SettingsBySerial[newItem.Serial] = settings;
            }

            if (!player.GameObject.TryGetComponent<ToolGunUI>(out var ui))
            {
                ui = player.GameObject.AddComponent<ToolGunUI>();
                ui.Init(player, this, settings);
            }
            else
                ui.Init(player, this, settings);

            if (!ui.enabled)
                ui.enabled = true;

            base.OnChanged(player, oldItem, newItem, changedToThisItem);
        }

        public override void OnPicked(Player player, Item item)
        {
            if (!SettingsBySerial.TryGetValue(item.Serial, out ToolGunSettings settings))
            {
                settings = new ToolGunSettings(this, item.CurrentOwner);
            }

            settings.Owner = player;
            SettingsBySerial[item.Serial] = settings;
            base.OnPicked(player, item);
        }

        public override void OnDropped(Player player, Pickup pickup)
        {
            if (!SettingsBySerial.TryGetValue(pickup.Serial, out ToolGunSettings settings))
            {
                settings = new ToolGunSettings(this);
            }

            settings.Owner = null;
            SettingsBySerial[pickup.Serial] = settings;
        }

        public override void OnDropping(Player player, Item item, TypeWrapper<bool> isThrow, TypeWrapper<bool> isAllowed)
        {
            isAllowed.Value = false;
            if (SettingsBySerial.TryGetValue(item.Serial, out ToolGunSettings settings))
            {
                settings.SelectedObjectType = settings.SelectedObjectType.Next(true, ObjectType.None, ObjectType.Schematic);
                SettingsBySerial[item.Serial] = settings;
            }
        }

        public override void OnShooting(Player player, FirearmItem weapon, TypeWrapper<bool> isAllowedHelper)
        {
            isAllowedHelper.Value = false;

            if (!SettingsBySerial.TryGetValue(weapon.Serial, out ToolGunSettings settings))
                return;

            if (!ADSCache.TryGetValue(weapon.Serial, out LinearAdsModule? ads))
            {
                if (weapon.Base.TryGetModule<LinearAdsModule>(out LinearAdsModule? ads1))
                {
                    ADSCache[weapon.Serial] = ads1;
                    ads = ads1;
                }
                else
                {
                    LogManager.Warn($"Failed to get ADS module for ToolGun {weapon.Serial}");
                    return;
                }
            }

            if (ads.AdsTarget)
            {
                ToolGunUtils.TrySelectAtAim(player, settings);
                return;
            }

            if (ToolGunUtils.TrySendRayCast(player, out RaycastHit hit))
            {
                Loader.SpawnSingleObject(settings.SelectedObjectType, hit.point, player.Rotation, values: settings.GetSpawnValues());
            }
        }

        public override void OnAim(Player player, FirearmItem weapon, bool aiming)
        {
            if (SettingsBySerial.TryGetValue(weapon.Serial, out ToolGunSettings settings))
            {
                settings.Deleting = !settings.Deleting;
                SettingsBySerial[weapon.Serial] = settings;
            }
        }
    }
}
