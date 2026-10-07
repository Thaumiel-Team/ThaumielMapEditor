// -----------------------------------------------------------------------
// <copyright file="SpeakerSettings.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using LabApi.Features.Wrappers;
using SecretAPI.Features.UserSettings;
using ThaumielMapEditor.API.Blocks.ServerObjects;

namespace ThaumielMapEditor.ToolGun.Utilities.Settings.Objects
{
    public abstract class SpeakerSliderSetting : CustomSliderSetting
    {
        protected SpeakerSliderSetting(int id, string label, float min, float max) : base(id, label, min, max)
        {
        }

        public override CustomHeader Header => ToolGunHeaders.Speaker;

        protected override bool CanView(Player player) => ObjectSettingsHelper.CanViewServer<SpeakerObject>(player);
    }

    public class SpeakerVolumeSetting : SpeakerSliderSetting
    {
        public static int SettingId => Main.Instance.Config.SpeakerVolumeSettingId;

        public SpeakerVolumeSetting() : base(SettingId, "Volume", 0f, 100f)
        {
        }

        protected override CustomSetting CreateDuplicate() => new SpeakerVolumeSetting();

        protected override void PersonalizeSetting()
        {
            if (KnownOwner != null && ObjectSettingsHelper.TryGetServer(KnownOwner, out SpeakerObject? speaker) && speaker != null)
                DefaultValue = ToolGunMenu.Clamp(speaker.Volume, 0f, 100f);
        }

        protected override void HandleSettingUpdate()
        {
            if (LastUpdateType == SettingResponseType.Initial)
                return;

            if (KnownOwner != null && ObjectSettingsHelper.TryGetServer(KnownOwner, out SpeakerObject? speaker) && speaker != null)
                speaker.Volume = SelectedValueFloat;
        }
    }

    public class SpeakerSpatialSetting : CustomTwoButtonSetting
    {
        public static int SettingId => Main.Instance.Config.SpeakerSpatialSettingId;

        public SpeakerSpatialSetting() : base(SettingId, "Spatial", "Off", "On")
        {
        }

        public override CustomHeader Header => ToolGunHeaders.Speaker;

        protected override bool CanView(Player player) => ObjectSettingsHelper.CanViewServer<SpeakerObject>(player);

        protected override CustomSetting CreateDuplicate() => new SpeakerSpatialSetting();

        protected override void PersonalizeSetting()
        {
            if (KnownOwner != null && ObjectSettingsHelper.TryGetServer(KnownOwner, out SpeakerObject? speaker) && speaker != null)
                Base.DefaultIsB = speaker.IsSpatial;
        }

        protected override void HandleSettingUpdate()
        {
            if (LastUpdateType == SettingResponseType.Initial)
                return;

            if (KnownOwner != null && ObjectSettingsHelper.TryGetServer(KnownOwner, out SpeakerObject? speaker) && speaker != null)
                speaker.IsSpatial = IsOptionB;
        }
    }

    public class SpeakerMinDistanceSetting : SpeakerSliderSetting
    {
        public static int SettingId => Main.Instance.Config.SpeakerMinDistanceSettingId;

        public SpeakerMinDistanceSetting() : base(SettingId, "Min Distance", 0f, 100f)
        {
        }

        protected override CustomSetting CreateDuplicate() => new SpeakerMinDistanceSetting();

        protected override void PersonalizeSetting()
        {
            if (KnownOwner != null && ObjectSettingsHelper.TryGetServer(KnownOwner, out SpeakerObject? speaker) && speaker != null)
                DefaultValue = ToolGunMenu.Clamp(speaker.MinDistance, 0f, 100f);
        }

        protected override void HandleSettingUpdate()
        {
            if (LastUpdateType == SettingResponseType.Initial)
                return;

            if (KnownOwner != null && ObjectSettingsHelper.TryGetServer(KnownOwner, out SpeakerObject? speaker) && speaker != null)
                speaker.MinDistance = SelectedValueFloat;
        }
    }

    public class SpeakerMaxDistanceSetting : SpeakerSliderSetting
    {
        public static int SettingId => Main.Instance.Config.SpeakerMaxDistanceSettingId;

        public SpeakerMaxDistanceSetting() : base(SettingId, "Max Distance", 0f, 100f)
        {
        }

        protected override CustomSetting CreateDuplicate() => new SpeakerMaxDistanceSetting();

        protected override void PersonalizeSetting()
        {
            if (KnownOwner != null && ObjectSettingsHelper.TryGetServer(KnownOwner, out SpeakerObject? speaker) && speaker != null)
                DefaultValue = ToolGunMenu.Clamp(speaker.MaxDistance, 0f, 100f);
        }

        protected override void HandleSettingUpdate()
        {
            if (LastUpdateType == SettingResponseType.Initial)
                return;

            if (KnownOwner != null && ObjectSettingsHelper.TryGetServer(KnownOwner, out SpeakerObject? speaker) && speaker != null)
                speaker.MaxDistance = SelectedValueFloat;
        }
    }

    public class SpeakerLoopSetting : CustomTwoButtonSetting
    {
        public static int SettingId => Main.Instance.Config.SpeakerLoopSettingId;

        public SpeakerLoopSetting() : base(SettingId, "Loop", "Off", "On")
        {
        }

        public override CustomHeader Header => ToolGunHeaders.Speaker;

        protected override bool CanView(Player player) => ObjectSettingsHelper.CanViewServer<SpeakerObject>(player);

        protected override CustomSetting CreateDuplicate() => new SpeakerLoopSetting();

        protected override void PersonalizeSetting()
        {
            if (KnownOwner != null && ObjectSettingsHelper.TryGetServer(KnownOwner, out SpeakerObject? speaker) && speaker != null)
                Base.DefaultIsB = speaker.Loop;
        }

        protected override void HandleSettingUpdate()
        {
            if (LastUpdateType == SettingResponseType.Initial)
                return;

            if (KnownOwner != null && ObjectSettingsHelper.TryGetServer(KnownOwner, out SpeakerObject? speaker) && speaker != null)
                speaker.Loop = IsOptionB;
        }
    }

    public class SpeakerPathSetting : CustomPlainTextSetting
    {
        public static int SettingId => Main.Instance.Config.SpeakerPathSettingId;

        private const int MaxPathLength = 256;

        public SpeakerPathSetting() : base(SettingId, "Path", "audio...")
        {
        }

        public override CustomHeader Header => ToolGunHeaders.Speaker;

        protected override bool CanView(Player player) => ObjectSettingsHelper.CanViewServer<SpeakerObject>(player);

        protected override CustomSetting CreateDuplicate() => new SpeakerPathSetting();

        protected override void PersonalizeSetting()
        {
            if (KnownOwner != null && ObjectSettingsHelper.TryGetServer(KnownOwner, out SpeakerObject? speaker) && speaker != null)
                Base.DefaultText = ToolGunMenu.SanitizeDisplayText(speaker.Path, MaxPathLength);
        }

        protected override void HandleSettingUpdate()
        {
            if (LastUpdateType == SettingResponseType.Initial)
                return;

            if (KnownOwner != null && ObjectSettingsHelper.TryGetServer(KnownOwner, out SpeakerObject? speaker) && speaker != null)
            {
                if (InputText == ToolGunMenu.SanitizeDisplayText(speaker.Path, MaxPathLength))
                    return;

                speaker.Path = InputText;
            }
        }
    }
}
