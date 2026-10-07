// -----------------------------------------------------------------------
// <copyright file="PrimitiveSettings.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using AdminToys;
using LabApi.Features.Wrappers;
using SecretAPI.Features.UserSettings;
using UnityEngine;

namespace ThaumielMapEditor.ToolGun.Utilities.Settings.Objects
{
    public abstract class PrimitiveSetting : CustomSliderSetting
    {
        protected PrimitiveSetting(int id, string label, float min, float max) : base(id, label, min, max)
        {
        }

        public override CustomHeader Header => ToolGunHeaders.Primitive;

        protected override bool CanView(Player player) => ToolGunMenu.IsEquipped(player) && ToolGunMenu.IsModifierMenu(player) && ToolGunMenu.TryGetPrimitive(player, out _, out _);
    }

    public class PrimitiveColorRSetting : PrimitiveSetting
    {
        public static int SettingId => Main.Instance.Config.PrimitiveColorRSettingId;

        public PrimitiveColorRSetting() : base(SettingId, "Color R", 0f, 1f)
        {
        }

        protected override CustomSetting CreateDuplicate() => new PrimitiveColorRSetting();

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

    public class PrimitiveColorGSetting : PrimitiveSetting
    {
        public static int SettingId => Main.Instance.Config.PrimitiveColorGSettingId;

        public PrimitiveColorGSetting() : base(SettingId, "Color G", 0f, 1f)
        {
        }

        protected override CustomSetting CreateDuplicate() => new PrimitiveColorGSetting();

        protected override void PersonalizeSetting()
        {
            if (KnownOwner != null)
                DefaultValue = ToolGunMenu.Clamp01(ToolGunMenu.GetColor(KnownOwner).g);
        }

        protected override void HandleSettingUpdate()
        {
            if (KnownOwner == null)
                return;

            if (LastUpdateType == SettingResponseType.Initial)
                return;

            Color value = ToolGunMenu.GetColor(KnownOwner);
            value.g = SelectedValueFloat;
            ToolGunMenu.SetColor(KnownOwner, value);
        }
    }

    public class PrimitiveColorBSetting : PrimitiveSetting
    {
        public static int SettingId => Main.Instance.Config.PrimitiveColorBSettingId;

        public PrimitiveColorBSetting() : base(SettingId, "Color B", 0f, 1f)
        {
        }

        protected override CustomSetting CreateDuplicate() => new PrimitiveColorBSetting();

        protected override void PersonalizeSetting()
        {
            if (KnownOwner != null)
                DefaultValue = ToolGunMenu.Clamp01(ToolGunMenu.GetColor(KnownOwner).b);
        }

        protected override void HandleSettingUpdate()
        {
            if (KnownOwner == null)
                return;

            if (LastUpdateType == SettingResponseType.Initial)
                return;

            Color value = ToolGunMenu.GetColor(KnownOwner);
            value.b = SelectedValueFloat;
            ToolGunMenu.SetColor(KnownOwner, value);
        }
    }

    public class PrimitiveTypeSetting : EnumDropdownSetting<PrimitiveType>
    {
        public static int SettingId => Main.Instance.Config.PrimitiveTypeSettingId;

        public PrimitiveTypeSetting() : base(SettingId, "Primitive Type")
        {
        }

        public override CustomHeader Header => ToolGunHeaders.Primitive;

        protected override bool CanView(Player player) => ToolGunMenu.IsEquipped(player) && ToolGunMenu.IsModifierMenu(player) && ToolGunMenu.TryGetPrimitive(player, out _, out _);

        protected override CustomSetting CreateDuplicate() => new PrimitiveTypeSetting();

        protected override void PersonalizeSetting()
        {
            if (KnownOwner == null)
                return;

            if (!ToolGunMenu.TryGetPrimitive(KnownOwner, out var server, out var client))
                return;

            Base.DefaultOptionIndex = IndexOf(server != null ? server.PrimitiveType : client?.PrimitiveType ?? PrimitiveType.Cube);
        }

        protected override void HandleSettingUpdate()
        {
            if (LastUpdateType == SettingResponseType.Initial)
                return;

            if (KnownOwner == null)
                return;

            if (!ToolGunMenu.TryGetPrimitive(KnownOwner, out var server, out var client))
                return;

            PrimitiveType value = Values[ValidatedSelectedIndex];
            if (server != null)
            {
                server.PrimitiveType = value;
            }
            else
                client?.PrimitiveType = value;
        }
    }

    public class PrimitiveVisibleSetting : CustomTwoButtonSetting
    {
        public static int SettingId => Main.Instance.Config.PrimitiveVisibleSettingId;

        public PrimitiveVisibleSetting() : base(SettingId, "Visible", "Off", "On")
        {
        }

        public override CustomHeader Header => ToolGunHeaders.Primitive;

        protected override bool CanView(Player player) => ToolGunMenu.IsEquipped(player) && ToolGunMenu.IsModifierMenu(player) && ToolGunMenu.TryGetPrimitive(player, out _, out _);

        protected override CustomSetting CreateDuplicate() => new PrimitiveVisibleSetting();

        protected override void PersonalizeSetting()
        {
            if (KnownOwner == null)
                return;

            if (!ToolGunMenu.TryGetPrimitive(KnownOwner, out var server, out var client))
                return;

            PrimitiveFlags flags = server != null ? server.PrimitiveFlags : client?.PrimitiveFlags ?? PrimitiveFlags.None;
            Base.DefaultIsB = (flags & PrimitiveFlags.Visible) != 0;
        }

        protected override void HandleSettingUpdate()
        {
            if (LastUpdateType == SettingResponseType.Initial)
                return;

            if (KnownOwner == null)
                return;

            if (!ToolGunMenu.TryGetPrimitive(KnownOwner, out var server, out var client))
                return;

            if (server != null)
            {
                PrimitiveFlags flags = server.PrimitiveFlags;
                flags = IsOptionB ? flags | PrimitiveFlags.Visible : flags & ~PrimitiveFlags.Visible;
                server.PrimitiveFlags = flags;
            }
            else
            {
                PrimitiveFlags flags = client?.PrimitiveFlags ?? PrimitiveFlags.None;
                flags = IsOptionB ? flags | PrimitiveFlags.Visible : flags & ~PrimitiveFlags.Visible;
                client?.PrimitiveFlags = flags;
            }
        }
    }

    public class PrimitiveCollidableSetting : CustomTwoButtonSetting
    {
        public static int SettingId => Main.Instance.Config.PrimitiveCollidableSettingId;

        public PrimitiveCollidableSetting() : base(SettingId, "Collidable", "Off", "On")
        {
        }

        public override CustomHeader Header => ToolGunHeaders.Primitive;

        protected override bool CanView(Player player) => ToolGunMenu.IsEquipped(player) && ToolGunMenu.IsModifierMenu(player) && ToolGunMenu.TryGetPrimitive(player, out _, out _);

        protected override CustomSetting CreateDuplicate() => new PrimitiveCollidableSetting();

        protected override void PersonalizeSetting()
        {
            if (KnownOwner == null)
                return;

            if (!ToolGunMenu.TryGetPrimitive(KnownOwner, out var server, out var client))
                return;

            PrimitiveFlags flags = server != null ? server.PrimitiveFlags : client?.PrimitiveFlags ?? PrimitiveFlags.None;
            Base.DefaultIsB = (flags & PrimitiveFlags.Collidable) != 0;
        }

        protected override void HandleSettingUpdate()
        {
            if (LastUpdateType == SettingResponseType.Initial)
                return;

            if (KnownOwner == null)
                return;

            if (!ToolGunMenu.TryGetPrimitive(KnownOwner, out var server, out var client))
                return;

            if (server != null)
            {
                PrimitiveFlags flags = server.PrimitiveFlags;
                flags = IsOptionB ? flags | PrimitiveFlags.Collidable : flags & ~PrimitiveFlags.Collidable;
                server.PrimitiveFlags = flags;
            }
            else
            {
                PrimitiveFlags flags = client?.PrimitiveFlags ?? PrimitiveFlags.None;
                flags = IsOptionB ? flags | PrimitiveFlags.Collidable : flags & ~PrimitiveFlags.Collidable;
                client?.PrimitiveFlags = flags;
            }
        }
    }
}
