// -----------------------------------------------------------------------
// <copyright file="RagdollSettings.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using LabApi.Features.Wrappers;
using PlayerRoles;
using SecretAPI.Features.UserSettings;
using ThaumielMapEditor.API.Blocks.ServerObjects;

namespace ThaumielMapEditor.ToolGun.Utilities.Settings.Objects
{
    public class RagdollInfo : CustomTextAreaSetting
    {
        public static int SettingId => Main.Instance.Config.RagdollInfoId;

        public RagdollInfo() : base(SettingId, string.Empty)
        {
        }

        public override CustomHeader Header => ToolGunHeaders.Ragdoll;

        protected override bool CanView(Player player) => ObjectSettingsHelper.CanViewServer<RagdollSpawner>(player);

        protected override CustomSetting CreateDuplicate() => new RagdollInfo();

        protected override void PersonalizeSetting()
        {
            if (KnownOwner != null && ObjectSettingsHelper.TryGetServer(KnownOwner, out RagdollSpawner? ragdoll) && ragdoll != null)
                Content = $"Ragdoll Spawner ({ragdoll.RoleType}, {ragdoll.SpawnChance}%)";
        }

        protected override void HandleSettingUpdate()
        {
        }
    }

    public class RagdollRoleSetting : EnumDropdownSetting<RoleTypeId>
    {
        public static int SettingId => Main.Instance.Config.RagdollRoleSettingId;

        public RagdollRoleSetting() : base(SettingId, "Role Type")
        {
        }

        public override CustomHeader Header => ToolGunHeaders.Ragdoll;

        protected override bool CanView(Player player) => ObjectSettingsHelper.CanViewServer<RagdollSpawner>(player);

        protected override CustomSetting CreateDuplicate() => new RagdollRoleSetting();

        protected override void PersonalizeSetting()
        {
            if (KnownOwner != null && ObjectSettingsHelper.TryGetServer(KnownOwner, out RagdollSpawner? ragdoll) && ragdoll != null)
                Base.DefaultOptionIndex = IndexOf(ragdoll.RoleType);
        }

        protected override void HandleSettingUpdate()
        {
            if (LastUpdateType == SettingResponseType.Initial)
                return;

            if (KnownOwner != null && ObjectSettingsHelper.TryGetServer(KnownOwner, out RagdollSpawner? ragdoll) && ragdoll != null)
                ragdoll.RoleType = Values[ValidatedSelectedIndex];
        }
    }

    public class RagdollChanceSetting : CustomSliderSetting
    {
        public static int SettingId => Main.Instance.Config.RagdollChanceSettingId;

        public RagdollChanceSetting() : base(SettingId, "Spawn Chance", 0f, 100f)
        {
        }

        public override CustomHeader Header => ToolGunHeaders.Ragdoll;

        protected override bool CanView(Player player) => ObjectSettingsHelper.CanViewServer<RagdollSpawner>(player);

        protected override CustomSetting CreateDuplicate() => new RagdollChanceSetting();

        protected override void PersonalizeSetting()
        {
            if (KnownOwner != null && ObjectSettingsHelper.TryGetServer(KnownOwner, out RagdollSpawner? ragdoll) && ragdoll != null)
                DefaultValue = ToolGunMenu.Clamp(ragdoll.SpawnChance, 0f, 100f);
        }

        protected override void HandleSettingUpdate()
        {
            if (LastUpdateType == SettingResponseType.Initial)
                return;

            if (KnownOwner != null && ObjectSettingsHelper.TryGetServer(KnownOwner, out RagdollSpawner? ragdoll) && ragdoll != null)
                ragdoll.SpawnChance = SelectedValueFloat;
        }
    }

    public class RagdollDeathReasonSetting : CustomPlainTextSetting
    {
        public static int SettingId => Main.Instance.Config.RagdollDeathReasonSettingId;

        private const int MaxReasonLength = 128;

        public RagdollDeathReasonSetting() : base(SettingId, "Death Reason", "reason...")
        {
        }

        public override CustomHeader Header => ToolGunHeaders.Ragdoll;

        protected override bool CanView(Player player) => ObjectSettingsHelper.CanViewServer<RagdollSpawner>(player);

        protected override CustomSetting CreateDuplicate() => new RagdollDeathReasonSetting();

        protected override void PersonalizeSetting()
        {
            if (KnownOwner != null && ObjectSettingsHelper.TryGetServer(KnownOwner, out RagdollSpawner? ragdoll) && ragdoll != null)
                Base.DefaultText = ToolGunMenu.SanitizeDisplayText(ragdoll.DeathReason, MaxReasonLength);
        }

        protected override void HandleSettingUpdate()
        {
            if (LastUpdateType == SettingResponseType.Initial)
                return;

            if (KnownOwner != null && ObjectSettingsHelper.TryGetServer(KnownOwner, out RagdollSpawner? ragdoll) && ragdoll != null)
            {
                if (InputText == ToolGunMenu.SanitizeDisplayText(ragdoll.DeathReason, MaxReasonLength))
                    return;

                ragdoll.DeathReason = InputText;
            }
        }
    }

    public class RagdollNameSetting : CustomPlainTextSetting
    {
        public static int SettingId => Main.Instance.Config.RagdollNameSettingId;

        private const int MaxDollNameLength = 64;

        public RagdollNameSetting() : base(SettingId, "Doll Name", "name...")
        {
        }

        public override CustomHeader Header => ToolGunHeaders.Ragdoll;

        protected override bool CanView(Player player) => ObjectSettingsHelper.CanViewServer<RagdollSpawner>(player);

        protected override CustomSetting CreateDuplicate() => new RagdollNameSetting();

        protected override void PersonalizeSetting()
        {
            if (KnownOwner != null && ObjectSettingsHelper.TryGetServer(KnownOwner, out RagdollSpawner? ragdoll) && ragdoll != null)
                Base.DefaultText = ToolGunMenu.SanitizeDisplayText(ragdoll.DollName, MaxDollNameLength);
        }

        protected override void HandleSettingUpdate()
        {
            if (LastUpdateType == SettingResponseType.Initial)
                return;

            if (KnownOwner != null && ObjectSettingsHelper.TryGetServer(KnownOwner, out RagdollSpawner? ragdoll) && ragdoll != null)
            {
                if (InputText == ToolGunMenu.SanitizeDisplayText(ragdoll.DollName, MaxDollNameLength))
                    return;

                ragdoll.DollName = InputText;
            }
        }
    }
}
