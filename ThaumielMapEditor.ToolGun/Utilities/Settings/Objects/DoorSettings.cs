// -----------------------------------------------------------------------
// <copyright file="DoorSettings.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using LabApi.Features.Wrappers;
using Interactables.Interobjects.DoorUtils;
using SecretAPI.Features.UserSettings;
using ThaumielMapEditor.API.Blocks.ServerObjects;
using ThaumielMapEditor.API.Enums;

namespace ThaumielMapEditor.ToolGun.Utilities.Settings.Objects
{
    public static class DoorSettingsHelper
    {
        public static bool TryGetDoor(Player player, out DoorObject? door)
        {
            door = null;

            if (!ToolGunMenu.TryGetSelection(player, out _, out var server, out _))
                return false;

            door = server as DoorObject;
            return door != null;
        }

        public static bool CanViewDoor(Player player) => ToolGunMenu.IsEquipped(player) && ToolGunMenu.IsModifierMenu(player) && TryGetDoor(player, out _);
    }

    public class DoorTypeSetting : EnumDropdownSetting<DoorType>
    {
        public static int SettingId => Main.Instance.Config.DoorTypeSettingId;

        public DoorTypeSetting() : base(SettingId, "Door Type")
        {
        }

        public override CustomHeader Header => ToolGunHeaders.Door;

        protected override bool CanView(Player player) => DoorSettingsHelper.CanViewDoor(player);

        protected override CustomSetting CreateDuplicate() => new DoorTypeSetting();

        protected override void PersonalizeSetting()
        {
            if (KnownOwner != null && DoorSettingsHelper.TryGetDoor(KnownOwner, out DoorObject? door) && door != null)
                Base.DefaultOptionIndex = IndexOf(door.DoorType);
        }

        protected override void HandleSettingUpdate()
        {
            if (LastUpdateType == SettingResponseType.Initial)
                return;

            if (KnownOwner != null && DoorSettingsHelper.TryGetDoor(KnownOwner, out DoorObject? door) && door != null)
                door.DoorType = Values[ValidatedSelectedIndex];
        }
    }

    public class DoorOpenSetting : CustomTwoButtonSetting
    {
        public static int SettingId => Main.Instance.Config.DoorOpenSettingId;

        public DoorOpenSetting() : base(SettingId, "Open", "Off", "On")
        {
        }

        public override CustomHeader Header => ToolGunHeaders.Door;

        protected override bool CanView(Player player) => DoorSettingsHelper.CanViewDoor(player);

        protected override CustomSetting CreateDuplicate() => new DoorOpenSetting();

        protected override void PersonalizeSetting()
        {
            if (KnownOwner != null && DoorSettingsHelper.TryGetDoor(KnownOwner, out DoorObject? door) && door != null)
                Base.DefaultIsB = door.IsOpen;
        }

        protected override void HandleSettingUpdate()
        {
            if (LastUpdateType == SettingResponseType.Initial)
                return;

            if (KnownOwner != null && DoorSettingsHelper.TryGetDoor(KnownOwner, out DoorObject? door) && door != null)
                door.IsOpen = IsOptionB;
        }
    }

    public class DoorLockedSetting : CustomTwoButtonSetting
    {
        public static int SettingId => Main.Instance.Config.DoorLockedSettingId;

        public DoorLockedSetting() : base(SettingId, "Locked", "Off", "On")
        {
        }

        public override CustomHeader Header => ToolGunHeaders.Door;

        protected override bool CanView(Player player) => DoorSettingsHelper.CanViewDoor(player);

        protected override CustomSetting CreateDuplicate() => new DoorLockedSetting();

        protected override void PersonalizeSetting()
        {
            if (KnownOwner != null && DoorSettingsHelper.TryGetDoor(KnownOwner, out DoorObject? door) && door != null)
                Base.DefaultIsB = door.IsLocked;
        }

        protected override void HandleSettingUpdate()
        {
            if (LastUpdateType == SettingResponseType.Initial)
                return;

            if (KnownOwner != null && DoorSettingsHelper.TryGetDoor(KnownOwner, out DoorObject? door) && door != null)
                door.IsLocked = IsOptionB;
        }
    }

    public class DoorPermissionsSetting : EnumDropdownSetting<DoorPermissionFlags>
    {
        public static int SettingId => Main.Instance.Config.DoorPermissionsSettingId;

        public DoorPermissionsSetting() : base(SettingId, "Permissions")
        {
        }

        public override CustomHeader Header => ToolGunHeaders.Door;

        protected override bool CanView(Player player) => DoorSettingsHelper.CanViewDoor(player);

        protected override CustomSetting CreateDuplicate() => new DoorPermissionsSetting();

        protected override void PersonalizeSetting()
        {
            if (KnownOwner != null && DoorSettingsHelper.TryGetDoor(KnownOwner, out DoorObject? door) && door != null)
                Base.DefaultOptionIndex = IndexOf(door.Permissions);
        }

        protected override void HandleSettingUpdate()
        {
            if (LastUpdateType == SettingResponseType.Initial)
                return;

            if (KnownOwner != null && DoorSettingsHelper.TryGetDoor(KnownOwner, out DoorObject? door) && door != null)
                door.Permissions = Values[ValidatedSelectedIndex];
        }
    }

    public class DoorRequireAllPermsSetting : CustomTwoButtonSetting
    {
        public static int SettingId => Main.Instance.Config.DoorRequireAllPermsSettingId;

        public DoorRequireAllPermsSetting() : base(SettingId, "Require All Perms", "Off", "On")
        {
        }

        public override CustomHeader Header => ToolGunHeaders.Door;

        protected override bool CanView(Player player) => DoorSettingsHelper.CanViewDoor(player);

        protected override CustomSetting CreateDuplicate() => new DoorRequireAllPermsSetting();

        protected override void PersonalizeSetting()
        {
            if (KnownOwner != null && DoorSettingsHelper.TryGetDoor(KnownOwner, out DoorObject? door) && door != null)
                Base.DefaultIsB = door.RequireAllPermissions;
        }

        protected override void HandleSettingUpdate()
        {
            if (LastUpdateType == SettingResponseType.Initial)
                return;

            if (KnownOwner != null && DoorSettingsHelper.TryGetDoor(KnownOwner, out DoorObject? door) && door != null)
                door.RequireAllPermissions = IsOptionB;
        }
    }

    public class DoorBypass2176Setting : CustomTwoButtonSetting
    {
        public static int SettingId => Main.Instance.Config.DoorBypass2176SettingId;

        public DoorBypass2176Setting() : base(SettingId, "Bypass 2176", "Off", "On")
        {
        }

        public override CustomHeader Header => ToolGunHeaders.Door;

        protected override bool CanView(Player player) => DoorSettingsHelper.CanViewDoor(player);

        protected override CustomSetting CreateDuplicate() => new DoorBypass2176Setting();

        protected override void PersonalizeSetting()
        {
            if (KnownOwner != null && DoorSettingsHelper.TryGetDoor(KnownOwner, out DoorObject? door) && door != null)
                Base.DefaultIsB = door.Bypass2176;
        }

        protected override void HandleSettingUpdate()
        {
            if (LastUpdateType == SettingResponseType.Initial)
                return;

            if (KnownOwner != null && DoorSettingsHelper.TryGetDoor(KnownOwner, out DoorObject? door) && door != null)
                door.Bypass2176 = IsOptionB;
        }
    }

    public class DoorMaxHealthSetting : CustomSliderSetting
    {
        public static int SettingId => Main.Instance.Config.DoorMaxHealthSettingId;

        public DoorMaxHealthSetting() : base(SettingId, "Max Health", 0f, 10000f)
        {
        }

        public override CustomHeader Header => ToolGunHeaders.Door;

        protected override bool CanView(Player player) => DoorSettingsHelper.CanViewDoor(player);

        protected override CustomSetting CreateDuplicate() => new DoorMaxHealthSetting();

        protected override void PersonalizeSetting()
        {
            if (KnownOwner != null && DoorSettingsHelper.TryGetDoor(KnownOwner, out DoorObject? door) && door != null)
                DefaultValue = ToolGunMenu.Clamp(door.MaxHealth, 0f, 10000f);
        }

        protected override void HandleSettingUpdate()
        {
            if (LastUpdateType == SettingResponseType.Initial)
                return;

            if (KnownOwner != null && DoorSettingsHelper.TryGetDoor(KnownOwner, out DoorObject? door) && door != null)
                door.MaxHealth = SelectedValueFloat;
        }
    }

    public class DoorHealthSetting : CustomSliderSetting
    {
        public static int SettingId => Main.Instance.Config.DoorHealthSettingId;

        public DoorHealthSetting() : base(SettingId, "Health", 0f, 10000f)
        {
        }

        public override CustomHeader Header => ToolGunHeaders.Door;

        protected override bool CanView(Player player) => DoorSettingsHelper.CanViewDoor(player);

        protected override CustomSetting CreateDuplicate() => new DoorHealthSetting();

        protected override void PersonalizeSetting()
        {
            if (KnownOwner != null && DoorSettingsHelper.TryGetDoor(KnownOwner, out DoorObject? door) && door != null)
                DefaultValue = ToolGunMenu.Clamp(door.Health, 0f, 10000f);
        }

        protected override void HandleSettingUpdate()
        {
            if (LastUpdateType == SettingResponseType.Initial)
                return;

            if (KnownOwner != null && DoorSettingsHelper.TryGetDoor(KnownOwner, out DoorObject? door) && door != null)
                door.Health = SelectedValueFloat;
        }
    }
}
