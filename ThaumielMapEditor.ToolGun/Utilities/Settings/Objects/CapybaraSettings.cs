// -----------------------------------------------------------------------
// <copyright file="CapybaraSettings.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using LabApi.Features.Wrappers;
using SecretAPI.Features.UserSettings;

namespace ThaumielMapEditor.ToolGun.Utilities.Settings.Objects
{
    public class CapyCollisionsSetting : CustomTwoButtonSetting
    {
        public static int SettingId => Main.Instance.Config.CapyCollisionsSettingId;

        public CapyCollisionsSetting() : base(SettingId, "Collisions", "Off", "On")
        {
        }

        public override CustomHeader Header => ToolGunHeaders.Capybara;

        protected override bool CanView(Player player) => ToolGunMenu.IsEquipped(player) && ToolGunMenu.IsModifierMenu(player) && ToolGunMenu.TryGetCapy(player, out _, out _);

        protected override CustomSetting CreateDuplicate() => new CapyCollisionsSetting();

        protected override void PersonalizeSetting()
        {
            if (KnownOwner == null)
                return;

            if (!ToolGunMenu.TryGetCapy(KnownOwner, out var server, out var client))
                return;

            Base.DefaultIsB = server != null ? server.CollisionsEnabled : client?.CollisionsEnabled ?? false;
        }

        protected override void HandleSettingUpdate()
        {
            if (LastUpdateType == SettingResponseType.Initial)
                return;

            if (KnownOwner == null)
                return;

            if (!ToolGunMenu.TryGetCapy(KnownOwner, out var server, out var client))
                return;

            if (server != null)
            {
                server.CollisionsEnabled = IsOptionB;
            }
            else
                client?.CollisionsEnabled = IsOptionB;
        }
    }
}
