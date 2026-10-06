// -----------------------------------------------------------------------
// <copyright file="ClutterSettings.cs" company="Thaumiel Team">
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
    public class ClutterInfo : CustomTextAreaSetting
    {
        public static int SettingId => Main.Instance.Config.ClutterInfoId;

        public ClutterInfo() : base(SettingId, string.Empty)
        {
        }

        public override CustomHeader Header => ToolGunHeaders.Clutter;

        protected override bool CanView(Player player) => ObjectSettingsHelper.CanViewServer<ClutterObject>(player);

        protected override CustomSetting CreateDuplicate() => new ClutterInfo();

        protected override void PersonalizeSetting()
        {
            if (KnownOwner != null && ObjectSettingsHelper.TryGetServer(KnownOwner, out ClutterObject? clutter) && clutter != null)
                Content = $"Clutter Settings ({clutter.Type})";
        }

        protected override void HandleSettingUpdate()
        {
        }
    }

    public class ClutterTypeSetting : EnumDropdownSetting<ClutterType>
    {
        public static int SettingId => Main.Instance.Config.ClutterTypeSettingId;

        public ClutterTypeSetting() : base(SettingId, "Clutter Type")
        {
        }

        public override CustomHeader Header => ToolGunHeaders.Clutter;

        protected override bool CanView(Player player) => ObjectSettingsHelper.CanViewServer<ClutterObject>(player);

        protected override CustomSetting CreateDuplicate() => new ClutterTypeSetting();

        protected override void PersonalizeSetting()
        {
            if (KnownOwner != null && ObjectSettingsHelper.TryGetServer(KnownOwner, out ClutterObject? clutter) && clutter != null)
                Base.DefaultOptionIndex = IndexOf(clutter.Type);
        }

        protected override void HandleSettingUpdate()
        {
            if (LastUpdateType == SettingResponseType.Initial)
                return;

            if (KnownOwner != null && ObjectSettingsHelper.TryGetServer(KnownOwner, out ClutterObject? clutter) && clutter != null)
                clutter.Type = Values[ValidatedSelectedIndex];
        }
    }
}
