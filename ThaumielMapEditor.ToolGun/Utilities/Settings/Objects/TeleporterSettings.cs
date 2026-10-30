// -----------------------------------------------------------------------
// <copyright file="TeleporterSettings.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using LabApi.Features.Wrappers;
using SecretAPI.Features.UserSettings;
using ThaumielMapEditor.API.Blocks.Definitions;
using ThaumielMapEditor.API.Data;
using ThaumielMapEditor.API.Enums;
using ThaumielMapEditor.API.Helpers;
using static UserSettings.ServerSpecific.SSDropdownSetting;

namespace ThaumielMapEditor.ToolGun.Utilities.Settings.Objects
{
    public class TeleporterInfo : CustomTextAreaSetting
    {
        public static int SettingId => Main.Instance.Config.TeleporterInfoId;

        public TeleporterInfo() : base(SettingId, string.Empty)
        {
        }

        public override CustomHeader Header => ToolGunHeaders.Teleporter;

        protected override bool CanView(Player player) => ObjectSettingsHelper.CanViewServer<Teleporter.Server>(player);

        protected override CustomSetting CreateDuplicate() => new TeleporterInfo();

        protected override void PersonalizeSetting()
        {
            if (KnownOwner != null && ObjectSettingsHelper.TryGetServer(KnownOwner, out Teleporter.Server? teleporter) && teleporter != null)
                Content = $"Teleporter Settings ({teleporter.Id}, Targets: {teleporter.Targets.Count})";
        }

        protected override void HandleSettingUpdate()
        {
        }
    }

    public class TeleporterCooldownSetting : CustomSliderSetting
    {
        public static int SettingId => Main.Instance.Config.TeleporterCooldownSettingId;

        public TeleporterCooldownSetting() : base(SettingId, "Cooldown", 0f, 120f)
        {
        }

        public override CustomHeader Header => ToolGunHeaders.Teleporter;

        protected override bool CanView(Player player) => ObjectSettingsHelper.CanViewServer<Teleporter.Server>(player);

        protected override CustomSetting CreateDuplicate() => new TeleporterCooldownSetting();

        protected override void PersonalizeSetting()
        {
            if (KnownOwner != null && ObjectSettingsHelper.TryGetServer(KnownOwner, out Teleporter.Server? teleporter) && teleporter != null)
                DefaultValue = ToolGunMenu.Clamp(teleporter.CoolDown, 0f, 120f);
        }

        protected override void HandleSettingUpdate()
        {
            if (LastUpdateType == SettingResponseType.Initial)
                return;

            if (KnownOwner != null && ObjectSettingsHelper.TryGetServer(KnownOwner, out Teleporter.Server? teleporter) && teleporter != null)
                teleporter.CoolDown = SelectedValueFloat;
        }
    }

    public class TeleporterPerPlayerSetting : CustomTwoButtonSetting
    {
        public static int SettingId => Main.Instance.Config.TeleporterPerPlayerSettingId;

        public TeleporterPerPlayerSetting() : base(SettingId, "Per Player Cooldown", "Off", "On")
        {
        }

        public override CustomHeader Header => ToolGunHeaders.Teleporter;

        protected override bool CanView(Player player) => ObjectSettingsHelper.CanViewServer<Teleporter.Server>(player);

        protected override CustomSetting CreateDuplicate() => new TeleporterPerPlayerSetting();

        protected override void PersonalizeSetting()
        {
            if (KnownOwner != null && ObjectSettingsHelper.TryGetServer(KnownOwner, out Teleporter.Server? teleporter) && teleporter != null)
                Base.DefaultIsB = teleporter.PerPlayerCooldown;
        }

        protected override void HandleSettingUpdate()
        {
            if (LastUpdateType == SettingResponseType.Initial)
                return;

            if (KnownOwner != null && ObjectSettingsHelper.TryGetServer(KnownOwner, out Teleporter.Server? teleporter) && teleporter != null)
                teleporter.PerPlayerCooldown = IsOptionB;
        }
    }

    public class TeleporterFlagsSetting : EnumDropdownSetting<TeleporterFlags>
    {
        public static int SettingId => Main.Instance.Config.TeleporterFlagsSettingId;

        public TeleporterFlagsSetting() : base(SettingId, "Flags")
        {
        }

        public override CustomHeader Header => ToolGunHeaders.Teleporter;

        protected override bool CanView(Player player) => ObjectSettingsHelper.CanViewServer<Teleporter.Server>(player);

        protected override CustomSetting CreateDuplicate() => new TeleporterFlagsSetting();

        protected override void PersonalizeSetting()
        {
            if (KnownOwner != null && ObjectSettingsHelper.TryGetServer(KnownOwner, out Teleporter.Server? teleporter) && teleporter != null)
                Base.DefaultOptionIndex = IndexOf(teleporter.Flags);
        }

        protected override void HandleSettingUpdate()
        {
            if (LastUpdateType == SettingResponseType.Initial)
                return;

            if (KnownOwner != null && ObjectSettingsHelper.TryGetServer(KnownOwner, out Teleporter.Server? teleporter) && teleporter != null)
                teleporter.Flags = Values[ValidatedSelectedIndex];
        }
    }

    public class TeleporterTargetSetting : CustomDropdownSetting
    {
        public static int SettingId => Main.Instance.Config.TeleporterTargetSettingId;

        public TeleporterTargetSetting() : base(SettingId, "Target Teleporter", ["None"], 0, DropdownEntryType.HybridLoop)
        {
        }

        public override CustomHeader Header => ToolGunHeaders.Teleporter;

        protected override bool CanView(Player player) => ObjectSettingsHelper.CanViewServer<Teleporter.Server>(player);

        protected override CustomSetting CreateDuplicate() => new TeleporterTargetSetting();

        protected override void PersonalizeSetting()
        {
            if (KnownOwner == null)
                return;

            if (!ObjectSettingsHelper.TryGetServer(KnownOwner, out Teleporter.Server? teleporter) || teleporter == null)
                return;

            List<Teleporter.Server> ordered = GetOrderedTargets(teleporter);
            Options = ["None", .. ordered.Select(LabelOf)];

            Guid current = teleporter.Targets.FirstOrDefault();
            int index = current == Guid.Empty ? 0 : ordered.FindIndex(t => t.Id == current) + 1;
            Base.DefaultOptionIndex = Math.Max(index, 0);
        }

        protected override void HandleSettingUpdate()
        {
            if (LastUpdateType == SettingResponseType.Initial)
                return;

            if (KnownOwner == null)
                return;

            if (!ObjectSettingsHelper.TryGetServer(KnownOwner, out Teleporter.Server? teleporter) || teleporter == null)
                return;

            List<Teleporter.Server> ordered = GetOrderedTargets(teleporter);
            int selected = ValidatedSelectedIndex;

            teleporter.Targets.Clear();

            if (selected > 0 && selected <= ordered.Count)
                teleporter.Targets.Add(ordered[selected - 1].Id);
        }

        private static List<Teleporter.Server> GetOrderedTargets(Teleporter.Server self) =>
            Teleporter.Server.Teleporters.Values
                .Where(t => t != null && t.Id != self.Id)
                .OrderBy(LabelOf, StringComparer.OrdinalIgnoreCase)
                .ThenBy(t => t.Id)
                .ToList();

        private static string LabelOf(Teleporter.Server teleporter)
        {
            string name = string.IsNullOrWhiteSpace(teleporter.Name) ? teleporter.Id.ToString("N").Substring(0, 8) : teleporter.Name;
            return $"{ToolGunMenu.SanitizeDisplayText(name, 64)}-{ToolGunMenu.SanitizeDisplayText(GetSchematicName(teleporter), 64)}";
        }

        private static string GetSchematicName(Teleporter.Server teleporter)
        {
            foreach (SchematicData schematic in Loader.SpawnedSchematics.ToArray())
            {
                if (schematic.SpawnedServerObjects.Contains(teleporter))
                    return string.IsNullOrEmpty(schematic.FileName) ? "?" : schematic.FileName;
            }

            return "?";
        }
    }
}
