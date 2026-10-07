// -----------------------------------------------------------------------
// <copyright file="TargetSettings.cs" company="Thaumiel Team">
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
    public class TargetTypeSetting : EnumDropdownSetting<TargetType>
    {
        public static int SettingId => Main.Instance.Config.TargetTypeSettingId;

        public TargetTypeSetting() : base(SettingId, "Target Type")
        {
        }

        public override CustomHeader Header => ToolGunHeaders.Target;

        protected override bool CanView(Player player) => ObjectSettingsHelper.CanViewServer<TargetDummyObject>(player);

        protected override CustomSetting CreateDuplicate() => new TargetTypeSetting();

        protected override void PersonalizeSetting()
        {
            if (KnownOwner != null && ObjectSettingsHelper.TryGetServer(KnownOwner, out TargetDummyObject? target) && target != null)
                Base.DefaultOptionIndex = IndexOf(target.Type);
        }

        protected override void HandleSettingUpdate()
        {
            if (LastUpdateType == SettingResponseType.Initial)
                return;

            if (KnownOwner != null && ObjectSettingsHelper.TryGetServer(KnownOwner, out TargetDummyObject? target) && target != null)
                target.Type = Values[ValidatedSelectedIndex];
        }
    }
}
