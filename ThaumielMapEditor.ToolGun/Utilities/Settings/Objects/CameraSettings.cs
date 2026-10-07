// -----------------------------------------------------------------------
// <copyright file="CameraSettings.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using LabApi.Features.Wrappers;
using MapGeneration;
using SecretAPI.Features.UserSettings;
using ThaumielMapEditor.API.Blocks.ServerObjects;
using UnityEngine;
using CameraType = ThaumielMapEditor.API.Enums.CameraType;

namespace ThaumielMapEditor.ToolGun.Utilities.Settings.Objects
{
    public class CameraLabelSetting : CustomPlainTextSetting
    {
        public static int SettingId => Main.Instance.Config.CameraLabelSettingId;

        private const int MaxLabelLength = 128;

        public CameraLabelSetting() : base(SettingId, "Label", "label...")
        {
        }

        public override CustomHeader Header => ToolGunHeaders.Camera;

        protected override bool CanView(Player player) => ObjectSettingsHelper.CanViewServer<CameraObject>(player);

        protected override CustomSetting CreateDuplicate() => new CameraLabelSetting();

        protected override void PersonalizeSetting()
        {
            if (KnownOwner != null && ObjectSettingsHelper.TryGetServer(KnownOwner, out CameraObject? camera) && camera != null)
                Base.DefaultText = ToolGunMenu.SanitizeDisplayText(camera.Label, MaxLabelLength);
        }

        protected override void HandleSettingUpdate()
        {
            if (LastUpdateType == SettingResponseType.Initial)
                return;

            if (KnownOwner != null && ObjectSettingsHelper.TryGetServer(KnownOwner, out CameraObject? camera) && camera != null)
            {
                if (InputText == ToolGunMenu.SanitizeDisplayText(camera.Label, MaxLabelLength))
                    return;

                camera.Label = InputText;
            }
        }
    }

    public class CameraTypeSetting : EnumDropdownSetting<CameraType>
    {
        public static int SettingId => Main.Instance.Config.CameraTypeSettingId;

        public CameraTypeSetting() : base(SettingId, "Camera Type")
        {
        }

        public override CustomHeader Header => ToolGunHeaders.Camera;

        protected override bool CanView(Player player) => ObjectSettingsHelper.CanViewServer<CameraObject>(player);

        protected override CustomSetting CreateDuplicate() => new CameraTypeSetting();

        protected override void PersonalizeSetting()
        {
            if (KnownOwner != null && ObjectSettingsHelper.TryGetServer(KnownOwner, out CameraObject? camera) && camera != null)
                Base.DefaultOptionIndex = IndexOf(camera.Type);
        }

        protected override void HandleSettingUpdate()
        {
            if (LastUpdateType == SettingResponseType.Initial)
                return;

            if (KnownOwner != null && ObjectSettingsHelper.TryGetServer(KnownOwner, out CameraObject? camera) && camera != null)
                camera.Type = Values[ValidatedSelectedIndex];
        }
    }

    public class CameraRoomSetting : EnumDropdownSetting<RoomName>
    {
        public static int SettingId => Main.Instance.Config.CameraRoomSettingId;

        public CameraRoomSetting() : base(SettingId, "Room")
        {
        }

        public override CustomHeader Header => ToolGunHeaders.Camera;

        protected override bool CanView(Player player) => ObjectSettingsHelper.CanViewServer<CameraObject>(player);

        protected override CustomSetting CreateDuplicate() => new CameraRoomSetting();

        protected override void PersonalizeSetting()
        {
            if (KnownOwner != null && ObjectSettingsHelper.TryGetServer(KnownOwner, out CameraObject? camera) && camera != null)
                Base.DefaultOptionIndex = IndexOf(camera.RoomName);
        }

        protected override void HandleSettingUpdate()
        {
            if (LastUpdateType == SettingResponseType.Initial)
                return;

            if (KnownOwner != null && ObjectSettingsHelper.TryGetServer(KnownOwner, out CameraObject? camera) && camera != null)
                camera.RoomName = Values[ValidatedSelectedIndex];
        }
    }

    public abstract class CameraSliderSetting : CustomSliderSetting
    {
        protected CameraSliderSetting(int id, string label, float min, float max) : base(id, label, min, max)
        {
        }

        public override CustomHeader Header => ToolGunHeaders.Camera;

        protected override bool CanView(Player player) => ObjectSettingsHelper.CanViewServer<CameraObject>(player);
    }

    public class CameraVerticalMinSetting : CameraSliderSetting
    {
        public static int SettingId => Main.Instance.Config.CameraVerticalMinSettingId;

        public CameraVerticalMinSetting() : base(SettingId, "Vertical Min", -180f, 180f)
        {
        }

        protected override CustomSetting CreateDuplicate() => new CameraVerticalMinSetting();

        protected override void PersonalizeSetting()
        {
            if (KnownOwner != null && ObjectSettingsHelper.TryGetServer(KnownOwner, out CameraObject? camera) && camera != null)
                DefaultValue = ToolGunMenu.Clamp(camera.VerticalConstraint.x, -180f, 180f);
        }

        protected override void HandleSettingUpdate()
        {
            if (LastUpdateType == SettingResponseType.Initial)
                return;

            if (KnownOwner != null && ObjectSettingsHelper.TryGetServer(KnownOwner, out CameraObject? camera) && camera != null)
            {
                Vector2 constraint = camera.VerticalConstraint;
                constraint.x = SelectedValueFloat;
                camera.VerticalConstraint = constraint;
            }
        }
    }

    public class CameraVerticalMaxSetting : CameraSliderSetting
    {
        public static int SettingId => Main.Instance.Config.CameraVerticalMaxSettingId;

        public CameraVerticalMaxSetting() : base(SettingId, "Vertical Max", -180f, 180f)
        {
        }

        protected override CustomSetting CreateDuplicate() => new CameraVerticalMaxSetting();

        protected override void PersonalizeSetting()
        {
            if (KnownOwner != null && ObjectSettingsHelper.TryGetServer(KnownOwner, out CameraObject? camera) && camera != null)
                DefaultValue = ToolGunMenu.Clamp(camera.VerticalConstraint.y, -180f, 180f);
        }

        protected override void HandleSettingUpdate()
        {
            if (LastUpdateType == SettingResponseType.Initial)
                return;

            if (KnownOwner != null && ObjectSettingsHelper.TryGetServer(KnownOwner, out CameraObject? camera) && camera != null)
            {
                Vector2 constraint = camera.VerticalConstraint;
                constraint.y = SelectedValueFloat;
                camera.VerticalConstraint = constraint;
            }
        }
    }

    public class CameraHorizontalMinSetting : CameraSliderSetting
    {
        public static int SettingId => Main.Instance.Config.CameraHorizontalMinSettingId;

        public CameraHorizontalMinSetting() : base(SettingId, "Horizontal Min", -180f, 180f)
        {
        }

        protected override CustomSetting CreateDuplicate() => new CameraHorizontalMinSetting();

        protected override void PersonalizeSetting()
        {
            if (KnownOwner != null && ObjectSettingsHelper.TryGetServer(KnownOwner, out CameraObject? camera) && camera != null)
                DefaultValue = ToolGunMenu.Clamp(camera.HorizontalConstraint.x, -180f, 180f);
        }

        protected override void HandleSettingUpdate()
        {
            if (LastUpdateType == SettingResponseType.Initial)
                return;

            if (KnownOwner != null && ObjectSettingsHelper.TryGetServer(KnownOwner, out CameraObject? camera) && camera != null)
            {
                Vector2 constraint = camera.HorizontalConstraint;
                constraint.x = SelectedValueFloat;
                camera.HorizontalConstraint = constraint;
            }
        }
    }

    public class CameraHorizontalMaxSetting : CameraSliderSetting
    {
        public static int SettingId => Main.Instance.Config.CameraHorizontalMaxSettingId;

        public CameraHorizontalMaxSetting() : base(SettingId, "Horizontal Max", -180f, 180f)
        {
        }

        protected override CustomSetting CreateDuplicate() => new CameraHorizontalMaxSetting();

        protected override void PersonalizeSetting()
        {
            if (KnownOwner != null && ObjectSettingsHelper.TryGetServer(KnownOwner, out CameraObject? camera) && camera != null)
                DefaultValue = ToolGunMenu.Clamp(camera.HorizontalConstraint.y, -180f, 180f);
        }

        protected override void HandleSettingUpdate()
        {
            if (LastUpdateType == SettingResponseType.Initial)
                return;

            if (KnownOwner != null && ObjectSettingsHelper.TryGetServer(KnownOwner, out CameraObject? camera) && camera != null)
            {
                Vector2 constraint = camera.HorizontalConstraint;
                constraint.y = SelectedValueFloat;
                camera.HorizontalConstraint = constraint;
            }
        }
    }

    public class CameraZoomMinSetting : CameraSliderSetting
    {
        public static int SettingId => Main.Instance.Config.CameraZoomMinSettingId;

        public CameraZoomMinSetting() : base(SettingId, "Zoom Min", 1f, 120f)
        {
        }

        protected override CustomSetting CreateDuplicate() => new CameraZoomMinSetting();

        protected override void PersonalizeSetting()
        {
            if (KnownOwner != null && ObjectSettingsHelper.TryGetServer(KnownOwner, out CameraObject? camera) && camera != null)
                DefaultValue = ToolGunMenu.Clamp(camera.ZoomConstraint.x, 1f, 120f);
        }

        protected override void HandleSettingUpdate()
        {
            if (LastUpdateType == SettingResponseType.Initial)
                return;

            if (KnownOwner != null && ObjectSettingsHelper.TryGetServer(KnownOwner, out CameraObject? camera) && camera != null)
            {
                Vector2 constraint = camera.ZoomConstraint;
                constraint.x = SelectedValueFloat;
                camera.ZoomConstraint = constraint;
            }
        }
    }

    public class CameraZoomMaxSetting : CameraSliderSetting
    {
        public static int SettingId => Main.Instance.Config.CameraZoomMaxSettingId;

        public CameraZoomMaxSetting() : base(SettingId, "Zoom Max", 1f, 120f)
        {
        }

        protected override CustomSetting CreateDuplicate() => new CameraZoomMaxSetting();

        protected override void PersonalizeSetting()
        {
            if (KnownOwner != null && ObjectSettingsHelper.TryGetServer(KnownOwner, out CameraObject? camera) && camera != null)
                DefaultValue = ToolGunMenu.Clamp(camera.ZoomConstraint.y, 1f, 120f);
        }

        protected override void HandleSettingUpdate()
        {
            if (LastUpdateType == SettingResponseType.Initial)
                return;

            if (KnownOwner != null && ObjectSettingsHelper.TryGetServer(KnownOwner, out CameraObject? camera) && camera != null)
            {
                Vector2 constraint = camera.ZoomConstraint;
                constraint.y = SelectedValueFloat;
                camera.ZoomConstraint = constraint;
            }
        }
    }
}
