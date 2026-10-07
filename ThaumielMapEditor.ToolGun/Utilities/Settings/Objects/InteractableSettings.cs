// -----------------------------------------------------------------------
// <copyright file="InteractableSettings.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using AdminToys;
using Interactables.Interobjects.DoorUtils;
using LabApi.Features.Wrappers;
using SecretAPI.Features.UserSettings;
using ThaumielMapEditor.API.Blocks.ServerObjects;
using static AdminToys.InvisibleInteractableToy;

namespace ThaumielMapEditor.ToolGun.Utilities.Settings.Objects
{
    public class InteractShapeSetting : EnumDropdownSetting<ColliderShape>
    {
        public static int SettingId => Main.Instance.Config.InteractShapeSettingId;

        public InteractShapeSetting() : base(SettingId, "Shape")
        {
        }

        public override CustomHeader Header => ToolGunHeaders.Interactable;

        protected override bool CanView(Player player) => ObjectSettingsHelper.CanViewServer<InteractionObject>(player);

        protected override CustomSetting CreateDuplicate() => new InteractShapeSetting();

        protected override void PersonalizeSetting()
        {
            if (KnownOwner != null && ObjectSettingsHelper.TryGetServer(KnownOwner, out InteractionObject? interactable) && interactable != null)
                Base.DefaultOptionIndex = IndexOf(interactable.Shape);
        }

        protected override void HandleSettingUpdate()
        {
            if (LastUpdateType == SettingResponseType.Initial)
                return;

            if (KnownOwner != null && ObjectSettingsHelper.TryGetServer(KnownOwner, out InteractionObject? interactable) && interactable != null)
                interactable.Shape = Values[ValidatedSelectedIndex];
        }
    }

    public class InteractDurationSetting : CustomSliderSetting
    {
        public static int SettingId => Main.Instance.Config.InteractDurationSettingId;

        public InteractDurationSetting() : base(SettingId, "Duration", 0f, 30f)
        {
        }

        public override CustomHeader Header => ToolGunHeaders.Interactable;

        protected override bool CanView(Player player) => ObjectSettingsHelper.CanViewServer<InteractionObject>(player);

        protected override CustomSetting CreateDuplicate() => new InteractDurationSetting();

        protected override void PersonalizeSetting()
        {
            if (KnownOwner != null && ObjectSettingsHelper.TryGetServer(KnownOwner, out InteractionObject? interactable) && interactable != null)
                DefaultValue = ToolGunMenu.Clamp(interactable.InteractionDuration, 0f, 30f);
        }

        protected override void HandleSettingUpdate()
        {
            if (LastUpdateType == SettingResponseType.Initial)
                return;

            if (KnownOwner != null && ObjectSettingsHelper.TryGetServer(KnownOwner, out InteractionObject? interactable) && interactable != null)
                interactable.InteractionDuration = SelectedValueFloat;
        }
    }

    public class InteractLockedSetting : CustomTwoButtonSetting
    {
        public static int SettingId => Main.Instance.Config.InteractLockedSettingId;

        public InteractLockedSetting() : base(SettingId, "Locked", "Off", "On")
        {
        }

        public override CustomHeader Header => ToolGunHeaders.Interactable;

        protected override bool CanView(Player player) => ObjectSettingsHelper.CanViewServer<InteractionObject>(player);

        protected override CustomSetting CreateDuplicate() => new InteractLockedSetting();

        protected override void PersonalizeSetting()
        {
            if (KnownOwner != null && ObjectSettingsHelper.TryGetServer(KnownOwner, out InteractionObject? interactable) && interactable != null)
                Base.DefaultIsB = interactable.IsLocked;
        }

        protected override void HandleSettingUpdate()
        {
            if (LastUpdateType == SettingResponseType.Initial)
                return;

            if (KnownOwner != null && ObjectSettingsHelper.TryGetServer(KnownOwner, out InteractionObject? interactable) && interactable != null)
                interactable.IsLocked = IsOptionB;
        }
    }

    public class InteractPermissionsSetting : EnumDropdownSetting<DoorPermissionFlags>
    {
        public static int SettingId => Main.Instance.Config.InteractPermissionsSettingId;

        public InteractPermissionsSetting() : base(SettingId, "Permissions")
        {
        }

        public override CustomHeader Header => ToolGunHeaders.Interactable;

        protected override bool CanView(Player player) => ObjectSettingsHelper.CanViewServer<InteractionObject>(player);

        protected override CustomSetting CreateDuplicate() => new InteractPermissionsSetting();

        protected override void PersonalizeSetting()
        {
            if (KnownOwner != null && ObjectSettingsHelper.TryGetServer(KnownOwner, out InteractionObject? interactable) && interactable != null)
                Base.DefaultOptionIndex = IndexOf(interactable.Permissions);
        }

        protected override void HandleSettingUpdate()
        {
            if (LastUpdateType == SettingResponseType.Initial)
                return;

            if (KnownOwner != null && ObjectSettingsHelper.TryGetServer(KnownOwner, out InteractionObject? interactable) && interactable != null)
                interactable.Permissions = Values[ValidatedSelectedIndex];
        }
    }
}
