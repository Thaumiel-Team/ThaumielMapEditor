// -----------------------------------------------------------------------
// <copyright file="WaypointSettings.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using LabApi.Features.Wrappers;
using SecretAPI.Features.UserSettings;
using ThaumielMapEditor.API.Blocks.ServerObjects;
using UnityEngine;

namespace ThaumielMapEditor.ToolGun.Utilities.Settings.Objects
{
    public class WaypointBoundsXSetting : CustomSliderSetting
    {
        public static int SettingId => Main.Instance.Config.WaypointBoundsXSettingId;

        public WaypointBoundsXSetting() : base(SettingId, "Bounds X", 0.1f, 50f)
        {
        }

        public override CustomHeader Header => ToolGunHeaders.Waypoint;

        protected override bool CanView(Player player) => ObjectSettingsHelper.CanViewServer<WaypointObject>(player);

        protected override CustomSetting CreateDuplicate() => new WaypointBoundsXSetting();

        protected override void PersonalizeSetting()
        {
            if (KnownOwner != null && ObjectSettingsHelper.TryGetServer(KnownOwner, out WaypointObject? waypoint) && waypoint != null)
                DefaultValue = ToolGunMenu.Clamp(waypoint.BoundsSize.x, 0.1f, 50f);
        }

        protected override void HandleSettingUpdate()
        {
            if (LastUpdateType == SettingResponseType.Initial)
                return;

            if (KnownOwner != null && ObjectSettingsHelper.TryGetServer(KnownOwner, out WaypointObject? waypoint) && waypoint != null)
            {
                Vector3 bounds = waypoint.BoundsSize;
                bounds.x = SelectedValueFloat;
                waypoint.BoundsSize = bounds;
            }
        }
    }

    public class WaypointBoundsYSetting : CustomSliderSetting
    {
        public static int SettingId => Main.Instance.Config.WaypointBoundsYSettingId;

        public WaypointBoundsYSetting() : base(SettingId, "Bounds Y", 0.1f, 50f)
        {
        }

        public override CustomHeader Header => ToolGunHeaders.Waypoint;

        protected override bool CanView(Player player) => ObjectSettingsHelper.CanViewServer<WaypointObject>(player);

        protected override CustomSetting CreateDuplicate() => new WaypointBoundsYSetting();

        protected override void PersonalizeSetting()
        {
            if (KnownOwner != null && ObjectSettingsHelper.TryGetServer(KnownOwner, out WaypointObject? waypoint) && waypoint != null)
                DefaultValue = ToolGunMenu.Clamp(waypoint.BoundsSize.y, 0.1f, 50f);
        }

        protected override void HandleSettingUpdate()
        {
            if (LastUpdateType == SettingResponseType.Initial)
                return;

            if (KnownOwner != null && ObjectSettingsHelper.TryGetServer(KnownOwner, out WaypointObject? waypoint) && waypoint != null)
            {
                Vector3 bounds = waypoint.BoundsSize;
                bounds.y = SelectedValueFloat;
                waypoint.BoundsSize = bounds;
            }
        }
    }

    public class WaypointBoundsZSetting : CustomSliderSetting
    {
        public static int SettingId => Main.Instance.Config.WaypointBoundsZSettingId;

        public WaypointBoundsZSetting() : base(SettingId, "Bounds Z", 0.1f, 50f)
        {
        }

        public override CustomHeader Header => ToolGunHeaders.Waypoint;

        protected override bool CanView(Player player) => ObjectSettingsHelper.CanViewServer<WaypointObject>(player);

        protected override CustomSetting CreateDuplicate() => new WaypointBoundsZSetting();

        protected override void PersonalizeSetting()
        {
            if (KnownOwner != null && ObjectSettingsHelper.TryGetServer(KnownOwner, out WaypointObject? waypoint) && waypoint != null)
                DefaultValue = ToolGunMenu.Clamp(waypoint.BoundsSize.z, 0.1f, 50f);
        }

        protected override void HandleSettingUpdate()
        {
            if (LastUpdateType == SettingResponseType.Initial)
                return;

            if (KnownOwner != null && ObjectSettingsHelper.TryGetServer(KnownOwner, out WaypointObject? waypoint) && waypoint != null)
            {
                Vector3 bounds = waypoint.BoundsSize;
                bounds.z = SelectedValueFloat;
                waypoint.BoundsSize = bounds;
            }
        }
    }

    public class WaypointPrioritySetting : CustomSliderSetting
    {
        public static int SettingId => Main.Instance.Config.WaypointPrioritySettingId;

        public WaypointPrioritySetting() : base(SettingId, "Priority", 0f, 100f)
        {
        }

        public override CustomHeader Header => ToolGunHeaders.Waypoint;

        protected override bool CanView(Player player) => ObjectSettingsHelper.CanViewServer<WaypointObject>(player);

        protected override CustomSetting CreateDuplicate() => new WaypointPrioritySetting();

        protected override void PersonalizeSetting()
        {
            if (KnownOwner != null && ObjectSettingsHelper.TryGetServer(KnownOwner, out WaypointObject? waypoint) && waypoint != null)
                DefaultValue = ToolGunMenu.Clamp(waypoint.Priority, 0f, 100f);
        }

        protected override void HandleSettingUpdate()
        {
            if (LastUpdateType == SettingResponseType.Initial)
                return;

            if (KnownOwner != null && ObjectSettingsHelper.TryGetServer(KnownOwner, out WaypointObject? waypoint) && waypoint != null)
                waypoint.Priority = SelectedValueFloat;
        }
    }

    public class WaypointVisualizeSetting : CustomTwoButtonSetting
    {
        public static int SettingId => Main.Instance.Config.WaypointVisualizeSettingId;

        public WaypointVisualizeSetting() : base(SettingId, "Visualize Bounds", "Off", "On")
        {
        }

        public override CustomHeader Header => ToolGunHeaders.Waypoint;

        protected override bool CanView(Player player) => ObjectSettingsHelper.CanViewServer<WaypointObject>(player);

        protected override CustomSetting CreateDuplicate() => new WaypointVisualizeSetting();

        protected override void PersonalizeSetting()
        {
            if (KnownOwner != null && ObjectSettingsHelper.TryGetServer(KnownOwner, out WaypointObject? waypoint) && waypoint != null)
                Base.DefaultIsB = waypoint.VisualizeBounds;
        }

        protected override void HandleSettingUpdate()
        {
            if (LastUpdateType == SettingResponseType.Initial)
                return;

            if (KnownOwner != null && ObjectSettingsHelper.TryGetServer(KnownOwner, out WaypointObject? waypoint) && waypoint != null)
                waypoint.VisualizeBounds = IsOptionB;
        }
    }
}
