// -----------------------------------------------------------------------
// <copyright file="SpawnPointSettings.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using LabApi.Features.Wrappers;
using SecretAPI.Features.UserSettings;
using ThaumielMapEditor.API.Blocks.ServerObjects;
using ThaumielMapEditor.API.Enums;

namespace ThaumielMapEditor.ToolGun.Utilities.Settings.Objects
{
    public class SpawnPointInfo : CustomTextAreaSetting
    {
        public static int SettingId => Main.Instance.Config.SpawnPointInfoId;

        public SpawnPointInfo() : base(SettingId, string.Empty)
        {
        }

        public override CustomHeader Header => ToolGunHeaders.SpawnPoint;

        protected override bool CanView(Player player) => ObjectSettingsHelper.CanViewServer<PlayerSpawnPoint>(player);

        protected override CustomSetting CreateDuplicate() => new SpawnPointInfo();

        protected override void PersonalizeSetting()
        {
            if (KnownOwner != null && ObjectSettingsHelper.TryGetServer(KnownOwner, out PlayerSpawnPoint? spawn) && spawn != null)
                Content = $"Spawn Point (Chance: {spawn.Chance}%, Roles: {spawn.AllowedRoles.Count})";
        }

        protected override void HandleSettingUpdate()
        {
        }
    }

    public class SpawnChanceSetting : CustomSliderSetting
    {
        public static int SettingId => Main.Instance.Config.SpawnChanceSettingId;

        public SpawnChanceSetting() : base(SettingId, "Chance", 0f, 100f)
        {
        }

        public override CustomHeader Header => ToolGunHeaders.SpawnPoint;

        protected override bool CanView(Player player) => ObjectSettingsHelper.CanViewServer<PlayerSpawnPoint>(player);

        protected override CustomSetting CreateDuplicate() => new SpawnChanceSetting();

        protected override void PersonalizeSetting()
        {
            if (KnownOwner != null && ObjectSettingsHelper.TryGetServer(KnownOwner, out PlayerSpawnPoint? spawn) && spawn != null)
                DefaultValue = ToolGunMenu.Clamp(spawn.Chance, 0f, 100f);
        }

        protected override void HandleSettingUpdate()
        {
            if (LastUpdateType == SettingResponseType.Initial)
                return;

            if (KnownOwner != null && ObjectSettingsHelper.TryGetServer(KnownOwner, out PlayerSpawnPoint? spawn) && spawn != null)
                spawn.Chance = SelectedValueFloat;
        }
    }

    public class SpawnDisableSetting : EnumDropdownSetting<DisableFlags>
    {
        public static int SettingId => Main.Instance.Config.SpawnDisableSettingId;

        public SpawnDisableSetting() : base(SettingId, "Disable")
        {
        }

        public override CustomHeader Header => ToolGunHeaders.SpawnPoint;

        protected override bool CanView(Player player) => ObjectSettingsHelper.CanViewServer<PlayerSpawnPoint>(player);

        protected override CustomSetting CreateDuplicate() => new SpawnDisableSetting();

        protected override void PersonalizeSetting()
        {
            if (KnownOwner != null && ObjectSettingsHelper.TryGetServer(KnownOwner, out PlayerSpawnPoint? spawn) && spawn != null)
                Base.DefaultOptionIndex = IndexOf(spawn.Disable);
        }

        protected override void HandleSettingUpdate()
        {
            if (LastUpdateType == SettingResponseType.Initial)
                return;

            if (KnownOwner != null && ObjectSettingsHelper.TryGetServer(KnownOwner, out PlayerSpawnPoint? spawn) && spawn != null)
                spawn.Disable = Values[ValidatedSelectedIndex];
        }
    }

    public class SpawnDisabledSetting : CustomTwoButtonSetting
    {
        public static int SettingId => Main.Instance.Config.SpawnDisabledSettingId;

        public SpawnDisabledSetting() : base(SettingId, "Disabled", "Off", "On")
        {
        }

        public override CustomHeader Header => ToolGunHeaders.SpawnPoint;

        protected override bool CanView(Player player) => ObjectSettingsHelper.CanViewServer<PlayerSpawnPoint>(player);

        protected override CustomSetting CreateDuplicate() => new SpawnDisabledSetting();

        protected override void PersonalizeSetting()
        {
            if (KnownOwner != null && ObjectSettingsHelper.TryGetServer(KnownOwner, out PlayerSpawnPoint? spawn) && spawn != null)
                Base.DefaultIsB = spawn.Disabled;
        }

        protected override void HandleSettingUpdate()
        {
            if (LastUpdateType == SettingResponseType.Initial)
                return;

            if (KnownOwner != null && ObjectSettingsHelper.TryGetServer(KnownOwner, out PlayerSpawnPoint? spawn) && spawn != null)
                spawn.Disabled = IsOptionB;
        }
    }
}
