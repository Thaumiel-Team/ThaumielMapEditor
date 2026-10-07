// -----------------------------------------------------------------------
// <copyright file="SpawnSettings.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using LabApi.Features.Wrappers;
using SecretAPI.Features.UserSettings;
using ThaumielMapEditor.API.Enums;
using ThaumielMapEditor.ToolGun.Extensions;

namespace ThaumielMapEditor.ToolGun.Utilities.Settings
{
    public static class SpawnSettingsHelper
    {
        public static bool CanViewSpawn(Player player, ObjectType type)
        {
            if (!ToolGunMenu.IsEquipped(player) || !ToolGunMenu.IsSpawnMenu(player))
                return false;

            ToolGunSettings? settings = player.GetToolGunSettings();
            return settings != null && settings.SelectedObjectType == type;
        }
    }

    public class SpawnInfo : CustomTextAreaSetting
    {
        public static int SettingId => Main.Instance.Config.SpawnInfoId;

        public SpawnInfo() : base(SettingId, string.Empty)
        {
        }

        public override CustomHeader Header => ToolGunHeaders.Spawn;

        protected override bool CanView(Player player) => ToolGunMenu.IsEquipped(player) && ToolGunMenu.IsSpawnMenu(player);

        protected override CustomSetting CreateDuplicate() => new SpawnInfo();

        protected override void PersonalizeSetting()
        {
            if (KnownOwner == null)
                return;

            ToolGunSettings? settings = KnownOwner.GetToolGunSettings();
            if (settings == null)
                return;

            Content = $"Spawning: {settings.SelectedObjectType}{settings.SpawnVariantLabel()}";
        }

        protected override void HandleSettingUpdate()
        {
        }
    }

    public class SpawnDoorTypeSetting : EnumDropdownSetting<DoorType>
    {
        public static int SettingId => Main.Instance.Config.SpawnDoorTypeSettingId;

        public SpawnDoorTypeSetting() : base(SettingId, "Door Type")
        {
        }

        public override CustomHeader Header => ToolGunHeaders.Spawn;

        protected override bool CanView(Player player) => SpawnSettingsHelper.CanViewSpawn(player, ObjectType.Door);

        protected override CustomSetting CreateDuplicate() => new SpawnDoorTypeSetting();

        protected override void PersonalizeSetting()
        {
            if (KnownOwner != null && KnownOwner.TryGetToolGunSettings(out ToolGunSettings settings))
                Base.DefaultOptionIndex = IndexOf(settings.SpawnDoorType);
        }

        protected override void HandleSettingUpdate()
        {
            if (LastUpdateType == SettingResponseType.Initial)
                return;

            if (KnownOwner != null && KnownOwner.TryGetToolGunSettings(out ToolGunSettings settings))
                settings.SpawnDoorType = Values[ValidatedSelectedIndex];
        }
    }

    public class SpawnLockerTypeSetting : EnumDropdownSetting<LockerType>
    {
        public static int SettingId => Main.Instance.Config.SpawnLockerTypeSettingId;

        public SpawnLockerTypeSetting() : base(SettingId, "Locker Type")
        {
        }

        public override CustomHeader Header => ToolGunHeaders.Spawn;

        protected override bool CanView(Player player) => SpawnSettingsHelper.CanViewSpawn(player, ObjectType.Locker);

        protected override CustomSetting CreateDuplicate() => new SpawnLockerTypeSetting();

        protected override void PersonalizeSetting()
        {
            if (KnownOwner != null && KnownOwner.TryGetToolGunSettings(out ToolGunSettings settings))
                Base.DefaultOptionIndex = IndexOf(settings.SpawnLockerType);
        }

        protected override void HandleSettingUpdate()
        {
            if (LastUpdateType == SettingResponseType.Initial)
                return;

            if (KnownOwner != null && KnownOwner.TryGetToolGunSettings(out ToolGunSettings settings))
                settings.SpawnLockerType = Values[ValidatedSelectedIndex];
        }
    }

    public class SpawnPickupItemSetting : EnumDropdownSetting<ItemType>
    {
        public static int SettingId => Main.Instance.Config.SpawnPickupItemSettingId;

        public SpawnPickupItemSetting() : base(SettingId, "Pickup Item")
        {
        }

        public override CustomHeader Header => ToolGunHeaders.Spawn;

        protected override bool CanView(Player player) => SpawnSettingsHelper.CanViewSpawn(player, ObjectType.Pickup);

        protected override CustomSetting CreateDuplicate() => new SpawnPickupItemSetting();

        protected override void PersonalizeSetting()
        {
            if (KnownOwner != null && KnownOwner.TryGetToolGunSettings(out ToolGunSettings settings))
                Base.DefaultOptionIndex = IndexOf(settings.SpawnPickupItem);
        }

        protected override void HandleSettingUpdate()
        {
            if (LastUpdateType == SettingResponseType.Initial)
                return;

            if (KnownOwner != null && KnownOwner.TryGetToolGunSettings(out ToolGunSettings settings))
                settings.SpawnPickupItem = Values[ValidatedSelectedIndex];
        }
    }
}
