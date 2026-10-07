// -----------------------------------------------------------------------
// <copyright file="LightSettings.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using LabApi.Features.Wrappers;
using SecretAPI.Features.UserSettings;
using UnityEngine;

namespace ThaumielMapEditor.ToolGun.Utilities.Settings.Objects
{
    public abstract class LightSharedSetting : CustomSliderSetting
    {
        protected LightSharedSetting(int id, string label, float min, float max) : base(id, label, min, max)
        {
        }

        public override CustomHeader Header => ToolGunHeaders.Light;

        protected override bool CanView(Player player) => ToolGunMenu.IsEquipped(player) && ToolGunMenu.IsModifierMenu(player) && ToolGunMenu.TryGetLight(player, out _, out _);
    }

    public class LightIntensitySetting : LightSharedSetting
    {
        public static int SettingId => Main.Instance.Config.LightIntensitySettingId;

        public LightIntensitySetting() : base(SettingId, "Intensity", 0f, 8f)
        {
        }

        protected override CustomSetting CreateDuplicate() => new LightIntensitySetting();

        protected override void PersonalizeSetting()
        {
            if (KnownOwner == null)
                return;

            if (!ToolGunMenu.TryGetLight(KnownOwner, out var server, out var client))
                return;

            DefaultValue = ToolGunMenu.Clamp(server != null ? server.Intensity : client?.Intensity ?? 0, 0f, 8f);
        }

        protected override void HandleSettingUpdate()
        {
            if (LastUpdateType == SettingResponseType.Initial)
                return;

            if (KnownOwner == null)
                return;

            if (!ToolGunMenu.TryGetLight(KnownOwner, out var server, out var client))
                return;

            if (server != null)
            {
                server.Intensity = SelectedValueFloat;
            }
            else
                client?.Intensity = SelectedValueFloat;
        }
    }

    public class LightRangeSetting : LightSharedSetting
    {
        public static int SettingId => Main.Instance.Config.LightRangeSettingId;

        public LightRangeSetting() : base(SettingId, "Range", 0f, 60f)
        {
        }

        protected override CustomSetting CreateDuplicate() => new LightRangeSetting();

        protected override void PersonalizeSetting()
        {
            if (KnownOwner == null)
                return;

            if (!ToolGunMenu.TryGetLight(KnownOwner, out var server, out var client))
                return;

            DefaultValue = ToolGunMenu.Clamp(server != null ? server.Range : client?.Range ?? 0, 0f, 60f);
        }

        protected override void HandleSettingUpdate()
        {
            if (LastUpdateType == SettingResponseType.Initial)
                return;

            if (KnownOwner == null)
                return;

            if (!ToolGunMenu.TryGetLight(KnownOwner, out var server, out var client))
                return;

            if (server != null)
            {
                server.Range = SelectedValueFloat;
            }
            else
                client?.Range = SelectedValueFloat;
        }
    }

    public class LightColorRSetting : LightSharedSetting
    {
        public static int SettingId => Main.Instance.Config.LightColorRSettingId;

        public LightColorRSetting() : base(SettingId, "Color R", 0f, 1f)
        {
        }

        protected override CustomSetting CreateDuplicate() => new LightColorRSetting();

        protected override void PersonalizeSetting()
        {
            if (KnownOwner != null)
                DefaultValue = ToolGunMenu.Clamp01(ToolGunMenu.GetColor(KnownOwner).r);
        }

        protected override void HandleSettingUpdate()
        {
            if (LastUpdateType == SettingResponseType.Initial)
                return;

            if (KnownOwner == null)
                return;

            Color value = ToolGunMenu.GetColor(KnownOwner);
            value.r = SelectedValueFloat;
            ToolGunMenu.SetColor(KnownOwner, value);
        }
    }

    public class LightColorGSetting : LightSharedSetting
    {
        public static int SettingId => Main.Instance.Config.LightColorGSettingId;

        public LightColorGSetting() : base(SettingId, "Color G", 0f, 1f)
        {
        }

        protected override CustomSetting CreateDuplicate() => new LightColorGSetting();

        protected override void PersonalizeSetting()
        {
            if (KnownOwner != null)
                DefaultValue = ToolGunMenu.Clamp01(ToolGunMenu.GetColor(KnownOwner).g);
        }

        protected override void HandleSettingUpdate()
        {
            if (LastUpdateType == SettingResponseType.Initial)
                return;

            if (KnownOwner == null)
                return;

            Color value = ToolGunMenu.GetColor(KnownOwner);
            value.g = SelectedValueFloat;
            ToolGunMenu.SetColor(KnownOwner, value);
        }
    }

    public class LightColorBSetting : LightSharedSetting
    {
        public static int SettingId => Main.Instance.Config.LightColorBSettingId;

        public LightColorBSetting() : base(SettingId, "Color B", 0f, 1f)
        {
        }

        protected override CustomSetting CreateDuplicate() => new LightColorBSetting();

        protected override void PersonalizeSetting()
        {
            if (KnownOwner != null)
                DefaultValue = ToolGunMenu.Clamp01(ToolGunMenu.GetColor(KnownOwner).b);
        }

        protected override void HandleSettingUpdate()
        {
            if (LastUpdateType == SettingResponseType.Initial)
                return;

            if (KnownOwner == null)
                return;

            Color value = ToolGunMenu.GetColor(KnownOwner);
            value.b = SelectedValueFloat;
            ToolGunMenu.SetColor(KnownOwner, value);
        }
    }

    public class LightShadowsSetting : EnumDropdownSetting<LightShadows>
    {
        public static int SettingId => Main.Instance.Config.LightShadowsSettingId;

        public LightShadowsSetting() : base(SettingId, "Shadows")
        {
        }

        public override CustomHeader Header => ToolGunHeaders.Light;

        protected override bool CanView(Player player) => ToolGunMenu.IsEquipped(player) && ToolGunMenu.IsModifierMenu(player) && ToolGunMenu.TryGetLight(player, out _, out _);

        protected override CustomSetting CreateDuplicate() => new LightShadowsSetting();

        protected override void PersonalizeSetting()
        {
            if (KnownOwner == null)
                return;

            if (!ToolGunMenu.TryGetLight(KnownOwner, out var server, out var client))
                return;

            Base.DefaultOptionIndex = IndexOf(server != null ? server.Shadows : client?.Shadows ?? LightShadows.None);
        }

        protected override void HandleSettingUpdate()
        {
            if (LastUpdateType == SettingResponseType.Initial)
                return;

            if (KnownOwner == null)
                return;

            if (!ToolGunMenu.TryGetLight(KnownOwner, out var server, out var client))
                return;

            LightShadows value = Values[ValidatedSelectedIndex];
            if (server != null)
            {
                server.Shadows = value;
            }
            else
                client?.Shadows = value;
        }
    }

    public class LightShadowStrengthSetting : LightSharedSetting
    {
        public static int SettingId => Main.Instance.Config.LightShadowStrengthSettingId;

        public LightShadowStrengthSetting() : base(SettingId, "Shadow Strength", 0f, 1f)
        {
        }

        protected override CustomSetting CreateDuplicate() => new LightShadowStrengthSetting();

        protected override void PersonalizeSetting()
        {
            if (KnownOwner == null)
                return;

            if (!ToolGunMenu.TryGetLight(KnownOwner, out var server, out var client))
                return;

            DefaultValue = ToolGunMenu.Clamp(server != null ? server.ShadowStrength : client?.ShadowStrength ?? 0, 0f, 1f);
        }

        protected override void HandleSettingUpdate()
        {
            if (LastUpdateType == SettingResponseType.Initial)
                return;

            if (KnownOwner == null)
                return;

            if (!ToolGunMenu.TryGetLight(KnownOwner, out var server, out var client))
                return;

            if (server != null)
            {
                server.ShadowStrength = SelectedValueFloat;
            }
            else
                client?.ShadowStrength = SelectedValueFloat;
        }
    }

    public class LightTypeSetting : EnumDropdownSetting<LightType>
    {
        public static int SettingId => Main.Instance.Config.LightTypeSettingId;

        public LightTypeSetting() : base(SettingId, "Light Type")
        {
        }

        public override CustomHeader Header => ToolGunHeaders.Light;

        protected override bool CanView(Player player) => ToolGunMenu.IsEquipped(player) && ToolGunMenu.IsModifierMenu(player) && ToolGunMenu.TryGetLight(player, out _, out _);

        protected override CustomSetting CreateDuplicate() => new LightTypeSetting();

        protected override void PersonalizeSetting()
        {
            if (KnownOwner == null)
                return;

            if (!ToolGunMenu.TryGetLight(KnownOwner, out var server, out var client))
                return;

            Base.DefaultOptionIndex = IndexOf(server != null ? server.Type : client?.Type ?? LightType.Point);
        }

        protected override void HandleSettingUpdate()
        {
            if (LastUpdateType == SettingResponseType.Initial)
                return;

            if (KnownOwner == null)
                return;

            if (!ToolGunMenu.TryGetLight(KnownOwner, out var server, out var client))
                return;

            LightType value = Values[ValidatedSelectedIndex];
            if (server != null)
            {
                server.Type = value;
            }
            else
                client?.Type = value;
        }
    }

    public class LightSpotAngleSetting : LightSharedSetting
    {
        public static int SettingId => Main.Instance.Config.LightSpotAngleSettingId;

        public LightSpotAngleSetting() : base(SettingId, "Spot Angle", 1f, 179f)
        {
        }

        protected override CustomSetting CreateDuplicate() => new LightSpotAngleSetting();

        protected override void PersonalizeSetting()
        {
            if (KnownOwner == null)
                return;

            if (!ToolGunMenu.TryGetLight(KnownOwner, out var server, out var client))
                return;

            DefaultValue = ToolGunMenu.Clamp(server != null ? server.SpotAngle : client?.SpotAngle ?? 0, 1f, 179f);
        }

        protected override void HandleSettingUpdate()
        {
            if (LastUpdateType == SettingResponseType.Initial)
                return;

            if (KnownOwner == null)
                return;

            if (!ToolGunMenu.TryGetLight(KnownOwner, out var server, out var client))
                return;

            if (server != null)
            {
                server.SpotAngle = SelectedValueFloat;
            }
            else
                client?.SpotAngle = SelectedValueFloat;
        }
    }

#pragma warning disable CS0618 // Type or member is obsolete

    public class LightShapeSetting : EnumDropdownSetting<LightShape>
#pragma warning restore CS0618 // Type or member is obsolete
    {
        public static int SettingId => Main.Instance.Config.LightShapeSettingId;

        public LightShapeSetting() : base(SettingId, "Light Shape")
        {
        }

        public override CustomHeader Header => ToolGunHeaders.Light;

        protected override bool CanView(Player player) => ToolGunMenu.IsEquipped(player) && ToolGunMenu.IsModifierMenu(player) && ToolGunMenu.TryGetLight(player, out _, out var client) && client != null;

        protected override CustomSetting CreateDuplicate() => new LightShapeSetting();

        protected override void PersonalizeSetting()
        {
            if (KnownOwner == null)
                return;

            if (!ToolGunMenu.TryGetLight(KnownOwner, out _, out var client) || client == null)
                return;

            Base.DefaultOptionIndex = IndexOf(client.Shape);
        }

        protected override void HandleSettingUpdate()
        {
            if (LastUpdateType == SettingResponseType.Initial)
                return;

            if (KnownOwner == null)
                return;

            if (!ToolGunMenu.TryGetLight(KnownOwner, out _, out var client) || client == null)
                return;

            client.Shape = Values[ValidatedSelectedIndex];
        }
    }

    public class LightInnerSpotAngleSetting : CustomSliderSetting
    {
        public static int SettingId => Main.Instance.Config.LightInnerSpotAngleSettingId;

        public LightInnerSpotAngleSetting() : base(SettingId, "Inner Spot Angle", 0f, 179f)
        {
        }

        public override CustomHeader Header => ToolGunHeaders.Light;

        protected override bool CanView(Player player) => ToolGunMenu.IsEquipped(player) && ToolGunMenu.IsModifierMenu(player) && ToolGunMenu.TryGetLight(player, out _, out var client) && client != null;

        protected override CustomSetting CreateDuplicate() => new LightInnerSpotAngleSetting();

        protected override void PersonalizeSetting()
        {
            if (KnownOwner == null)
                return;

            if (!ToolGunMenu.TryGetLight(KnownOwner, out _, out var client) || client == null)
                return;

            DefaultValue = ToolGunMenu.Clamp(client.InnerSpotAngle, 0f, 179f);
        }

        protected override void HandleSettingUpdate()
        {
            if (LastUpdateType == SettingResponseType.Initial)
                return;

            if (KnownOwner == null)
                return;

            if (!ToolGunMenu.TryGetLight(KnownOwner, out _, out var client) || client == null)
                return;

            client.InnerSpotAngle = SelectedValueFloat;
        }
    }
}
