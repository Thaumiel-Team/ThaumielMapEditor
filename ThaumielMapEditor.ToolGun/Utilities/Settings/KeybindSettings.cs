// -----------------------------------------------------------------------
// <copyright file="KeybindSettings.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using LabApi.Features.Wrappers;
using SecretAPI.Features.UserSettings;
using ThaumielMapEditor.API.Enums;
using ThaumielMapEditor.API.Extensions;
using ThaumielMapEditor.ToolGun.Extensions;
using UnityEngine;

namespace ThaumielMapEditor.ToolGun.Utilities.Settings
{
    public class CycleForwardKeybind : CustomKeybindSetting
    {
        public static int SettingId => Main.Instance.Config.CycleForwardKeybindId;

        public CycleForwardKeybind() : base(SettingId, "Cycle Object Forward", KeyCode.RightArrow, false, false)
        {
        }

        public override CustomHeader Header => ToolGunHeaders.Keybinds;

        protected override bool CanView(Player player) => ToolGunMenu.IsEquipped(player) && ToolGunMenu.IsKeybindsMenu(player);

        protected override CustomSetting CreateDuplicate() => new CycleForwardKeybind();

        protected override void HandleSettingUpdate()
        {
            if (!IsPressed || KnownOwner == null)
                return;

            if (!KnownOwner.TryGetToolGunSettings(out ToolGunSettings settings))
                return;

            settings.SelectedObjectType = settings.SelectedObjectType.Next(true, ObjectType.None, ObjectType.Schematic);
        }
    }

    public class CycleBackwardKeybind : CustomKeybindSetting
    {
        public static int SettingId => Main.Instance.Config.CycleBackwardKeybindId;

        public CycleBackwardKeybind() : base(SettingId, "Cycle Object Backward", KeyCode.LeftArrow, false, false)
        {
        }

        public override CustomHeader Header => ToolGunHeaders.Keybinds;

        protected override bool CanView(Player player) => ToolGunMenu.IsEquipped(player) && ToolGunMenu.IsKeybindsMenu(player);

        protected override CustomSetting CreateDuplicate() => new CycleBackwardKeybind();

        protected override void HandleSettingUpdate()
        {
            if (!IsPressed || KnownOwner == null)
                return;

            if (!KnownOwner.TryGetToolGunSettings(out ToolGunSettings settings))
                return;

            settings.SelectedObjectType = settings.SelectedObjectType.Previous(true, ObjectType.None, ObjectType.Schematic);
        }
    }

    public class SelectObjectKeybind : CustomKeybindSetting
    {
        public static int SettingId => Main.Instance.Config.SelectObjectKeybindId;

        public SelectObjectKeybind() : base(SettingId, "Select Object", KeyCode.UpArrow, false, false)
        {
        }

        public override CustomHeader Header => ToolGunHeaders.Keybinds;

        protected override bool CanView(Player player) => ToolGunMenu.IsEquipped(player) && ToolGunMenu.IsKeybindsMenu(player);

        protected override CustomSetting CreateDuplicate() => new SelectObjectKeybind();

        protected override void HandleSettingUpdate()
        {
            if (!IsPressed || KnownOwner == null)
                return;

            if (!KnownOwner.TryGetToolGunSettings(out ToolGunSettings settings))
                return;

            ToolGunUtils.TrySelectAtAim(KnownOwner, settings);
        }
    }

    public class UnselectObjectKeybind : CustomKeybindSetting
    {
        public static int SettingId => Main.Instance.Config.UnselectObjectKeybindId;

        public UnselectObjectKeybind() : base(SettingId, "Unselect Object", KeyCode.DownArrow, false, false)
        {
        }

        public override CustomHeader Header => ToolGunHeaders.Keybinds;

        protected override bool CanView(Player player) => ToolGunMenu.IsEquipped(player) && ToolGunMenu.IsKeybindsMenu(player);

        protected override CustomSetting CreateDuplicate() => new UnselectObjectKeybind();

        protected override void HandleSettingUpdate()
        {
            if (!IsPressed || KnownOwner == null)
                return;

            if (!KnownOwner.TryGetToolGunSettings(out ToolGunSettings settings))
                return;

            if (!settings.ObjectSelected)
                return;

            ToolGunUtils.ClearSelection(KnownOwner, settings);
        }
    }
}
