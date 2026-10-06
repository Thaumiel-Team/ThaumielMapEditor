// -----------------------------------------------------------------------
// <copyright file="TransformSettings.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using LabApi.Features.Wrappers;
using SecretAPI.Features.UserSettings;
using UnityEngine;

namespace ThaumielMapEditor.ToolGun.Utilities.Settings
{
    public class NoSelectionInfo : CustomTextAreaSetting
    {
        public static int SettingId => Main.Instance.Config.NoSelectionInfoId;

        public NoSelectionInfo() : base(SettingId, "No Block Selected.")
        {
        }

        public override CustomHeader Header => ToolGunHeaders.Modifier;

        protected override bool CanView(Player player) => ToolGunMenu.IsEquipped(player) && ToolGunMenu.IsModifierMenu(player) && !ToolGunMenu.HasSelection(player);

        protected override CustomSetting CreateDuplicate() => new NoSelectionInfo();

        protected override void HandleSettingUpdate()
        {
        }
    }

    public class SelectionTitleInfo : CustomTextAreaSetting
    {
        public static int SettingId => Main.Instance.Config.SelectionTitleInfoId;

        public SelectionTitleInfo() : base(SettingId, string.Empty)
        {
        }

        public override CustomHeader Header => ToolGunHeaders.Modifier;

        protected override bool CanView(Player player) => ToolGunMenu.IsEquipped(player) && ToolGunMenu.IsModifierMenu(player) && ToolGunMenu.HasSelection(player);

        protected override CustomSetting CreateDuplicate() => new SelectionTitleInfo();

        protected override void PersonalizeSetting()
        {
            if (KnownOwner == null)
                return;

            if (!ToolGunMenu.TryGetSelection(KnownOwner, out _, out var server, out var client))
                return;

            Content = server != null ? $"{server.ObjectType} (Server)" : $"{client?.ObjectType} (Client)";
        }

        protected override void HandleSettingUpdate()
        {
        }
    }

    public class NameSetting : CustomPlainTextSetting
    {
        public static int SettingId => Main.Instance.Config.NameSettingId;

        private const int MaxNameLength = 64;

        public NameSetting() : base(SettingId, "Name", "...")
        {
        }

        public override CustomHeader Header => ToolGunHeaders.Modifier;

        protected override bool CanView(Player player) => ToolGunMenu.IsEquipped(player) && ToolGunMenu.IsModifierMenu(player) && ToolGunMenu.HasName(player);

        protected override CustomSetting CreateDuplicate() => new NameSetting();

        protected override void PersonalizeSetting()
        {
            if (KnownOwner != null)
                Base.DefaultText = ToolGunMenu.SanitizeDisplayText(ToolGunMenu.GetName(KnownOwner), MaxNameLength);
        }

        protected override void HandleSettingUpdate()
        {
            if (LastUpdateType == SettingResponseType.Initial)
                return;

            if (KnownOwner == null)
                return;

            if (InputText == ToolGunMenu.SanitizeDisplayText(ToolGunMenu.GetName(KnownOwner), MaxNameLength))
                return;

            ToolGunMenu.SetName(KnownOwner, InputText);
        }
    }

    public abstract class TransformSliderSetting : CustomSliderSetting
    {
        protected TransformSliderSetting(int id, string label, float min, float max) : base(id, label, min, max)
        {
            IsServerSetting = true;
        }

        public override CustomHeader Header => ToolGunHeaders.Transform;

        protected override bool CanView(Player player) => ToolGunMenu.IsEquipped(player) && ToolGunMenu.IsModifierMenu(player) && ToolGunMenu.HasSelection(player);
    }

    public class PositionXSetting : TransformSliderSetting
    {
        public static int SettingId => Main.Instance.Config.PositionXSettingId;

        public PositionXSetting() : base(SettingId, "Position X", -1000f, 1000f)
        {
        }

        protected override CustomSetting CreateDuplicate() => new PositionXSetting();

        protected override void PersonalizeSetting()
        {
            if (KnownOwner != null)
                DefaultValue = ToolGunMenu.Clamp(ToolGunMenu.GetPosition(KnownOwner).x, -1000f, 1000f);
        }

        protected override void HandleSettingUpdate()
        {
            if (LastUpdateType == SettingResponseType.Initial)
                return;

            if (KnownOwner == null)
                return;

            Vector3 value = ToolGunMenu.GetPosition(KnownOwner);
            value.x = SelectedValueFloat;
            ToolGunMenu.SetPosition(KnownOwner, value);
        }
    }

    public class PositionYSetting : TransformSliderSetting
    {
        public static int SettingId => Main.Instance.Config.PositionYSettingId;

        public PositionYSetting() : base(SettingId, "Position Y", -1000f, 1000f)
        {
        }

        protected override CustomSetting CreateDuplicate() => new PositionYSetting();

        protected override void PersonalizeSetting()
        {
            if (KnownOwner != null)
                DefaultValue = ToolGunMenu.Clamp(ToolGunMenu.GetPosition(KnownOwner).y, -1000f, 1000f);
        }

        protected override void HandleSettingUpdate()
        {
            if (LastUpdateType == SettingResponseType.Initial)
                return;

            if (KnownOwner == null)
                return;

            Vector3 value = ToolGunMenu.GetPosition(KnownOwner);
            value.y = SelectedValueFloat;
            ToolGunMenu.SetPosition(KnownOwner, value);
        }
    }

    public class PositionZSetting : TransformSliderSetting
    {
        public static int SettingId => Main.Instance.Config.PositionZSettingId;

        public PositionZSetting() : base(SettingId, "Position Z", -1000f, 1000f)
        {
        }

        protected override CustomSetting CreateDuplicate() => new PositionZSetting();

        protected override void PersonalizeSetting()
        {
            if (KnownOwner != null)
                DefaultValue = ToolGunMenu.Clamp(ToolGunMenu.GetPosition(KnownOwner).z, -1000f, 1000f);
        }

        protected override void HandleSettingUpdate()
        {
            if (LastUpdateType == SettingResponseType.Initial)
                return;

            if (KnownOwner == null)
                return;

            Vector3 value = ToolGunMenu.GetPosition(KnownOwner);
            value.z = SelectedValueFloat;
            ToolGunMenu.SetPosition(KnownOwner, value);
        }
    }

    public class RotationXSetting : TransformSliderSetting
    {
        public static int SettingId => Main.Instance.Config.RotationXSettingId;

        public RotationXSetting() : base(SettingId, "Rotation X", 0f, 360f)
        {
        }

        protected override CustomSetting CreateDuplicate() => new RotationXSetting();

        protected override void PersonalizeSetting()
        {
            if (KnownOwner != null)
                DefaultValue = ToolGunMenu.Clamp(ToolGunMenu.GetRotation(KnownOwner).eulerAngles.x, 0f, 360f);
        }

        protected override void HandleSettingUpdate()
        {
            if (LastUpdateType == SettingResponseType.Initial)
                return;

            if (KnownOwner == null)
                return;

            Vector3 euler = ToolGunMenu.GetRotation(KnownOwner).eulerAngles;
            euler.x = SelectedValueFloat;
            ToolGunMenu.SetRotation(KnownOwner, Quaternion.Euler(euler));
        }
    }

    public class RotationYSetting : TransformSliderSetting
    {
        public static int SettingId => Main.Instance.Config.RotationYSettingId;

        public RotationYSetting() : base(SettingId, "Rotation Y", 0f, 360f)
        {
        }

        protected override CustomSetting CreateDuplicate() => new RotationYSetting();

        protected override void PersonalizeSetting()
        {
            if (KnownOwner != null)
                DefaultValue = ToolGunMenu.Clamp(ToolGunMenu.GetRotation(KnownOwner).eulerAngles.y, 0f, 360f);
        }

        protected override void HandleSettingUpdate()
        {
            if (LastUpdateType == SettingResponseType.Initial)
                return;

            if (KnownOwner == null)
                return;

            Vector3 euler = ToolGunMenu.GetRotation(KnownOwner).eulerAngles;
            euler.y = SelectedValueFloat;
            ToolGunMenu.SetRotation(KnownOwner, Quaternion.Euler(euler));
        }
    }

    public class RotationZSetting : TransformSliderSetting
    {
        public static int SettingId => Main.Instance.Config.RotationZSettingId;

        public RotationZSetting() : base(SettingId, "Rotation Z", 0f, 360f)
        {
        }

        protected override CustomSetting CreateDuplicate() => new RotationZSetting();

        protected override void PersonalizeSetting()
        {
            if (KnownOwner != null)
                DefaultValue = ToolGunMenu.Clamp(ToolGunMenu.GetRotation(KnownOwner).eulerAngles.z, 0f, 360f);
        }

        protected override void HandleSettingUpdate()
        {
            if (LastUpdateType == SettingResponseType.Initial)
                return;

            if (KnownOwner == null)
                return;

            Vector3 euler = ToolGunMenu.GetRotation(KnownOwner).eulerAngles;
            euler.z = SelectedValueFloat;
            ToolGunMenu.SetRotation(KnownOwner, Quaternion.Euler(euler));
        }
    }

    public class ScaleXSetting : TransformSliderSetting
    {
        public static int SettingId => Main.Instance.Config.ScaleXSettingId;

        public ScaleXSetting() : base(SettingId, "Scale X", 0.01f, 50f)
        {
        }

        protected override CustomSetting CreateDuplicate() => new ScaleXSetting();

        protected override void PersonalizeSetting()
        {
            if (KnownOwner != null)
                DefaultValue = ToolGunMenu.Clamp(ToolGunMenu.GetScale(KnownOwner).x, 0.01f, 50f);
        }

        protected override void HandleSettingUpdate()
        {
            if (LastUpdateType == SettingResponseType.Initial)
                return;

            if (KnownOwner == null)
                return;

            Vector3 value = ToolGunMenu.GetScale(KnownOwner);
            value.x = SelectedValueFloat;
            ToolGunMenu.SetScale(KnownOwner, value);
        }
    }

    public class ScaleYSetting : TransformSliderSetting
    {
        public static int SettingId => Main.Instance.Config.ScaleYSettingId;

        public ScaleYSetting() : base(SettingId, "Scale Y", 0.01f, 50f)
        {
        }

        protected override CustomSetting CreateDuplicate() => new ScaleYSetting();

        protected override void PersonalizeSetting()
        {
            if (KnownOwner != null)
                DefaultValue = ToolGunMenu.Clamp(ToolGunMenu.GetScale(KnownOwner).y, 0.01f, 50f);
        }

        protected override void HandleSettingUpdate()
        {
            if (LastUpdateType == SettingResponseType.Initial)
                return;

            if (KnownOwner == null)
                return;

            Vector3 value = ToolGunMenu.GetScale(KnownOwner);
            value.y = SelectedValueFloat;
            ToolGunMenu.SetScale(KnownOwner, value);
        }
    }

    public class ScaleZSetting : TransformSliderSetting
    {
        public static int SettingId => Main.Instance.Config.ScaleZSettingId;

        public ScaleZSetting() : base(SettingId, "Scale Z", 0.01f, 50f)
        {
        }

        protected override CustomSetting CreateDuplicate() => new ScaleZSetting();

        protected override void PersonalizeSetting()
        {
            if (KnownOwner != null)
                DefaultValue = ToolGunMenu.Clamp(ToolGunMenu.GetScale(KnownOwner).z, 0.01f, 50f);
        }

        protected override void HandleSettingUpdate()
        {
            if (LastUpdateType == SettingResponseType.Initial)
                return;

            if (KnownOwner == null)
                return;

            Vector3 value = ToolGunMenu.GetScale(KnownOwner);
            value.z = SelectedValueFloat;
            ToolGunMenu.SetScale(KnownOwner, value);
        }
    }

    public class StaticSetting : CustomTwoButtonSetting
    {
        public static int SettingId => Main.Instance.Config.StaticSettingId;

        public StaticSetting() : base(SettingId, "Static", "Off", "On")
        {
        }

        public override CustomHeader Header => ToolGunHeaders.Transform;

        protected override bool CanView(Player player) => ToolGunMenu.IsEquipped(player) && ToolGunMenu.IsModifierMenu(player) && ToolGunMenu.HasSelection(player);

        protected override CustomSetting CreateDuplicate() => new StaticSetting();

        protected override void PersonalizeSetting()
        {
            if (KnownOwner != null)
                Base.DefaultIsB = ToolGunMenu.GetStatic(KnownOwner);
        }

        protected override void HandleSettingUpdate()
        {
            if (LastUpdateType == SettingResponseType.Initial)
                return;

            if (KnownOwner != null)
                ToolGunMenu.SetStatic(KnownOwner, IsOptionB);
        }
    }

    public class SmoothingSetting : CustomSliderSetting
    {
        public static int SettingId => Main.Instance.Config.SmoothingSettingId;

        public SmoothingSetting() : base(SettingId, "Smoothing", 0f, 60, 0f, true)
        {
        }

        public override CustomHeader Header => ToolGunHeaders.Transform;

        protected override bool CanView(Player player) => ToolGunMenu.IsEquipped(player) && ToolGunMenu.IsModifierMenu(player) && ToolGunMenu.HasSelection(player);

        protected override CustomSetting CreateDuplicate() => new SmoothingSetting();

        protected override void PersonalizeSetting()
        {
            if (KnownOwner != null)
                DefaultValue = ToolGunMenu.GetSmoothing(KnownOwner);
        }

        protected override void HandleSettingUpdate()
        {
            if (LastUpdateType == SettingResponseType.Initial)
                return;

            if (KnownOwner != null)
                ToolGunMenu.SetSmoothing(KnownOwner, (byte)SelectedValueInt);
        }
    }
}
