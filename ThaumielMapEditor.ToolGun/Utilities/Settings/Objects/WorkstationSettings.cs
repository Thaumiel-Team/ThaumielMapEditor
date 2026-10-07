// -----------------------------------------------------------------------
// <copyright file="WorkstationSettings.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using LabApi.Features.Wrappers;
using SecretAPI.Features.UserSettings;
using ThaumielMapEditor.API.Blocks.ServerObjects;

namespace ThaumielMapEditor.ToolGun.Utilities.Settings.Objects
{
    public class WorkstationInfo : CustomTextAreaSetting
    {
        public static int SettingId => Main.Instance.Config.WorkstationInfoId;

        public WorkstationInfo() : base(SettingId, string.Empty)
        {
        }

        public override CustomHeader Header => ToolGunHeaders.Workstation;

        protected override bool CanView(Player player) => ObjectSettingsHelper.CanViewServer<WorkstationObject>(player);

        protected override CustomSetting CreateDuplicate() => new WorkstationInfo();

        protected override void PersonalizeSetting()
        {
            if (KnownOwner != null && ObjectSettingsHelper.TryGetServer(KnownOwner, out WorkstationObject? workstation) && workstation != null)
                Content = $"Workstation Settings (Interactions: {(workstation.AllowInteractions ? "Allowed" : "Blocked")})";
        }

        protected override void HandleSettingUpdate()
        {
        }
    }

    public class WorkstationAllowInteractionsSetting : CustomTwoButtonSetting
    {
        public static int SettingId => Main.Instance.Config.WorkstationAllowInteractionsSettingId;

        public WorkstationAllowInteractionsSetting() : base(SettingId, "Allow Interactions", "Off", "On")
        {
        }

        public override CustomHeader Header => ToolGunHeaders.Workstation;

        protected override bool CanView(Player player) => ObjectSettingsHelper.CanViewServer<WorkstationObject>(player);

        protected override CustomSetting CreateDuplicate() => new WorkstationAllowInteractionsSetting();

        protected override void PersonalizeSetting()
        {
            if (KnownOwner != null && ObjectSettingsHelper.TryGetServer(KnownOwner, out WorkstationObject? workstation) && workstation != null)
                Base.DefaultIsB = workstation.AllowInteractions;
        }

        protected override void HandleSettingUpdate()
        {
            if (LastUpdateType == SettingResponseType.Initial)
                return;

            if (KnownOwner != null && ObjectSettingsHelper.TryGetServer(KnownOwner, out WorkstationObject? workstation) && workstation != null)
                workstation.AllowInteractions = IsOptionB;
        }
    }
}
