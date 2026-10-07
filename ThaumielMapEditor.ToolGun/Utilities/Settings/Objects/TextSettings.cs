// -----------------------------------------------------------------------
// <copyright file="TextSettings.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using LabApi.Features.Wrappers;
using SecretAPI.Features.UserSettings;
using UnityEngine;

namespace ThaumielMapEditor.ToolGun.Utilities.Settings.Objects
{
    public class TextContentSetting : CustomPlainTextSetting
    {
        public static int SettingId => Main.Instance.Config.TextContentSettingId;

        private const int MaxTextLength = 512;

        public TextContentSetting() : base(SettingId, "Text", "text...")
        {
        }

        public override CustomHeader Header => ToolGunHeaders.Text;

        protected override bool CanView(Player player) => ToolGunMenu.IsEquipped(player) && ToolGunMenu.IsModifierMenu(player) && ToolGunMenu.TryGetText(player, out _, out _);

        protected override CustomSetting CreateDuplicate() => new TextContentSetting();

        private static string GetCurrentText(Player player)
        {
            if (!ToolGunMenu.TryGetText(player, out var server, out var client))
                return string.Empty;

            return server != null ? server.Text : client?.Text ?? string.Empty;
        }

        protected override void PersonalizeSetting()
        {
            if (KnownOwner == null)
                return;

            if (!ToolGunMenu.TryGetText(KnownOwner, out var server, out var client))
                return;

            string? current = server != null ? server.Text : client?.Text;
            Base.DefaultText = ToolGunMenu.SanitizeDisplayText(current, MaxTextLength);
        }

        protected override void HandleSettingUpdate()
        {
            if (LastUpdateType == SettingResponseType.Initial)
                return;

            if (KnownOwner == null)
                return;

            if (!ToolGunMenu.TryGetText(KnownOwner, out var server, out var client))
                return;

            if (InputText == ToolGunMenu.SanitizeDisplayText(GetCurrentText(KnownOwner), MaxTextLength))
                return;

            if (server != null)
            {
                server.Text = InputText;
            }
            else
                client?.Text = InputText;
        }
    }

    public class TextWidthSetting : CustomSliderSetting
    {
        public static int SettingId => Main.Instance.Config.TextWidthSettingId;

        public TextWidthSetting() : base(SettingId, "Width", 0.1f, 50f)
        {
        }

        public override CustomHeader Header => ToolGunHeaders.Text;

        protected override bool CanView(Player player) => ToolGunMenu.IsEquipped(player) && ToolGunMenu.IsModifierMenu(player) && ToolGunMenu.TryGetText(player, out _, out _);

        protected override CustomSetting CreateDuplicate() => new TextWidthSetting();

        protected override void PersonalizeSetting()
        {
            if (KnownOwner == null)
                return;

            if (!ToolGunMenu.TryGetText(KnownOwner, out var server, out var client))
                return;

            Vector2 size = server != null ? server.DisplaySize : client?.DisplaySize ?? Vector2.zero;
            DefaultValue = ToolGunMenu.Clamp(size.x, 0.1f, 50f);
        }

        protected override void HandleSettingUpdate()
        {
            if (LastUpdateType == SettingResponseType.Initial)
                return;

            if (KnownOwner == null)
                return;

            if (!ToolGunMenu.TryGetText(KnownOwner, out var server, out var client))
                return;

            if (server != null)
            {
                Vector2 size = server.DisplaySize;
                size.x = SelectedValueFloat;
                server.DisplaySize = size;
            }
            else
            {
                Vector2 size = client?.DisplaySize ?? Vector2.zero;
                size.x = SelectedValueFloat;
                client?.DisplaySize = size;
            }
        }
    }

    public class TextHeightSetting : CustomSliderSetting
    {
        public static int SettingId => Main.Instance.Config.TextHeightSettingId;

        public TextHeightSetting() : base(SettingId, "Height", 0.1f, 50f)
        {
        }

        public override CustomHeader Header => ToolGunHeaders.Text;

        protected override bool CanView(Player player) => ToolGunMenu.IsEquipped(player) && ToolGunMenu.IsModifierMenu(player) && ToolGunMenu.TryGetText(player, out _, out _);

        protected override CustomSetting CreateDuplicate() => new TextHeightSetting();

        protected override void PersonalizeSetting()
        {
            if (KnownOwner == null)
                return;

            if (!ToolGunMenu.TryGetText(KnownOwner, out var server, out var client))
                return;

            Vector2 size = server != null ? server.DisplaySize : client?.DisplaySize ?? Vector2.zero;
            DefaultValue = ToolGunMenu.Clamp(size.y, 0.1f, 50f);
        }

        protected override void HandleSettingUpdate()
        {
            if (LastUpdateType == SettingResponseType.Initial)
                return;

            if (KnownOwner == null)
                return;

            if (!ToolGunMenu.TryGetText(KnownOwner, out var server, out var client))
                return;

            if (server != null)
            {
                Vector2 size = server.DisplaySize;
                size.y = SelectedValueFloat;
                server.DisplaySize = size;
            }
            else
            {
                Vector2 size = client?.DisplaySize ?? Vector2.zero;
                size.y = SelectedValueFloat;
                client?.DisplaySize = size;
            }
        }
    }
}
