// -----------------------------------------------------------------------
// <copyright file="SubMenuSetting.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using LabApi.Features.Wrappers;
using SecretAPI.Features.UserSettings;
using static UserSettings.ServerSpecific.SSDropdownSetting;

namespace ThaumielMapEditor.ToolGun.Utilities.Settings
{
    public class SubMenuSetting : CustomDropdownSetting
    {
        public static int SettingId => Main.Instance.Config.SubMenuSettingId;

        public SubMenuSetting() : base(SettingId, "SubMenu Select", ["Keybinds", "Object Modifier", "Spawn Settings"], 0, DropdownEntryType.HybridLoop)
        {
        }

        public override CustomHeader Header => ToolGunHeaders.Main;

        protected override bool CanView(Player player) => ToolGunMenu.IsEquipped(player);

        protected override CustomSetting CreateDuplicate() => new SubMenuSetting();

        protected override void PersonalizeSetting()
        {
            Base.DefaultOptionIndex = ValidatedSelectedIndex;
        }

        protected override void HandleSettingUpdate()
        {
            if (KnownOwner == null)
                return;
                
            if (LastUpdateType == SettingResponseType.Initial || !HasValueChanged)
                return;

            bool enteringModifier = ValidatedSelectedIndex == 1;

            ToolGunMenu.Refresh(KnownOwner);
            if (enteringModifier)
                ToolGunMenu.SyncPositionSliders(KnownOwner);
        }
    }
}
