// -----------------------------------------------------------------------
// <copyright file="PickupSettings.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using LabApi.Features.Wrappers;
using SecretAPI.Features.UserSettings;
using ThaumielMapEditor.API.Blocks.ServerObjects;

namespace ThaumielMapEditor.ToolGun.Utilities.Settings.Objects
{
    public class PickupInfo : CustomTextAreaSetting
    {
        public static int SettingId => Main.Instance.Config.PickupInfoId;

        public PickupInfo() : base(SettingId, string.Empty)
        {
        }

        public override CustomHeader Header => ToolGunHeaders.Pickup;

        protected override bool CanView(Player player) => ObjectSettingsHelper.CanViewServer<PickupObject>(player);

        protected override CustomSetting CreateDuplicate() => new PickupInfo();

        protected override void PersonalizeSetting()
        {
            if (KnownOwner != null && ObjectSettingsHelper.TryGetServer(KnownOwner, out PickupObject? pickup) && pickup != null)
                Content = $"Pickup Settings ({pickup.ItemToSpawn})";
        }

        protected override void HandleSettingUpdate()
        {
        }
    }

    public class PickupItemToSpawnSetting : EnumDropdownSetting<ItemType>
    {
        public static int SettingId => Main.Instance.Config.PickupItemToSpawnSettingId;

        public PickupItemToSpawnSetting() : base(SettingId, "Item")
        {
        }

        public override CustomHeader Header => ToolGunHeaders.Pickup;

        protected override bool CanView(Player player) => ObjectSettingsHelper.CanViewServer<PickupObject>(player);

        protected override CustomSetting CreateDuplicate() => new PickupItemToSpawnSetting();

        protected override void PersonalizeSetting()
        {
            if (KnownOwner != null && ObjectSettingsHelper.TryGetServer(KnownOwner, out PickupObject? pickup) && pickup != null)
                Base.DefaultOptionIndex = IndexOf(pickup.ItemToSpawn);
        }

        protected override void HandleSettingUpdate()
        {
            if (LastUpdateType == SettingResponseType.Initial)
                return;

            if (KnownOwner != null && ObjectSettingsHelper.TryGetServer(KnownOwner, out PickupObject? pickup) && pickup != null)
            {
                pickup.ItemToSpawn = Values[ValidatedSelectedIndex];
                PickupPreview.Refresh(pickup);
            }
        }
    }

    public class PickupSpawnPercentageSetting : CustomSliderSetting
    {
        public static int SettingId => Main.Instance.Config.PickupSpawnPercentageSettingId;

        public PickupSpawnPercentageSetting() : base(SettingId, "Spawn Chance", 0f, 100f)
        {
        }

        public override CustomHeader Header => ToolGunHeaders.Pickup;

        protected override bool CanView(Player player) => ObjectSettingsHelper.CanViewServer<PickupObject>(player);

        protected override CustomSetting CreateDuplicate() => new PickupSpawnPercentageSetting();

        protected override void PersonalizeSetting()
        {
            if (KnownOwner != null && ObjectSettingsHelper.TryGetServer(KnownOwner, out PickupObject? pickup) && pickup != null)
                DefaultValue = ToolGunMenu.Clamp(pickup.SpawnPercentage, 0f, 100f);
        }

        protected override void HandleSettingUpdate()
        {
            if (LastUpdateType == SettingResponseType.Initial)
                return;

            if (KnownOwner != null && ObjectSettingsHelper.TryGetServer(KnownOwner, out PickupObject? pickup) && pickup != null)
                pickup.SpawnPercentage = SelectedValueFloat;
        }
    }

    public class PickupMaxAmountSetting : CustomSliderSetting
    {
        public static int SettingId => Main.Instance.Config.PickupMaxAmountSettingId;

        public PickupMaxAmountSetting() : base(SettingId, "Max Amount", 0f, 99f, 0f, true)
        {
        }

        public override CustomHeader Header => ToolGunHeaders.Pickup;

        protected override bool CanView(Player player) => ObjectSettingsHelper.CanViewServer<PickupObject>(player);

        protected override CustomSetting CreateDuplicate() => new PickupMaxAmountSetting();

        protected override void PersonalizeSetting()
        {
            if (KnownOwner != null && ObjectSettingsHelper.TryGetServer(KnownOwner, out PickupObject? pickup) && pickup != null)
                DefaultValue = ToolGunMenu.Clamp(pickup.MaxAmount, 0f, 99f);
        }

        protected override void HandleSettingUpdate()
        {
            if (LastUpdateType == SettingResponseType.Initial)
                return;

            if (KnownOwner != null && ObjectSettingsHelper.TryGetServer(KnownOwner, out PickupObject? pickup) && pickup != null)
                pickup.MaxAmount = (uint)SelectedValueInt;
        }
    }

    public class PickupIsInfiniteSetting : CustomTwoButtonSetting
    {
        public static int SettingId => Main.Instance.Config.PickupIsInfiniteSettingId;

        public PickupIsInfiniteSetting() : base(SettingId, "Infinite", "Off", "On")
        {
        }

        public override CustomHeader Header => ToolGunHeaders.Pickup;

        protected override bool CanView(Player player) => ObjectSettingsHelper.CanViewServer<PickupObject>(player);

        protected override CustomSetting CreateDuplicate() => new PickupIsInfiniteSetting();

        protected override void PersonalizeSetting()
        {
            if (KnownOwner != null && ObjectSettingsHelper.TryGetServer(KnownOwner, out PickupObject? pickup) && pickup != null)
                Base.DefaultIsB = pickup.IsInfinite;
        }

        protected override void HandleSettingUpdate()
        {
            if (LastUpdateType == SettingResponseType.Initial)
                return;

            if (KnownOwner != null && ObjectSettingsHelper.TryGetServer(KnownOwner, out PickupObject? pickup) && pickup != null)
                pickup.IsInfinite = IsOptionB;
        }
    }
}
