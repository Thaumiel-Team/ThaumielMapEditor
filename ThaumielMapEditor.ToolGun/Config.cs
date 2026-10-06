// -----------------------------------------------------------------------
// <copyright file="Config.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using System.ComponentModel;
using UnityEngine;

namespace ThaumielMapEditor.ToolGun
{
    [DoNotParse]
    public class Config
    {
        public Config()
        {
            int id = Random.Range(10000, 65000);
            SubMenuSettingId = id++;
            CycleForwardKeybindId = id++;
            CycleBackwardKeybindId = id++;
            SelectObjectKeybindId = id++;
            UnselectObjectKeybindId = id++;
            NoSelectionInfoId = id++;
            SelectionTitleInfoId = id++;
            NameSettingId = id++;
            PositionXSettingId = id++;
            PositionYSettingId = id++;
            PositionZSettingId = id++;
            RotationXSettingId = id++;
            RotationYSettingId = id++;
            RotationZSettingId = id++;
            ScaleXSettingId = id++;
            ScaleYSettingId = id++;
            ScaleZSettingId = id++;
            StaticSettingId = id++;
            SmoothingSettingId = id++;
            PrimitiveColorRSettingId = id++;
            PrimitiveColorGSettingId = id++;
            PrimitiveColorBSettingId = id++;
            PrimitiveTypeSettingId = id++;
            PrimitiveVisibleSettingId = id++;
            PrimitiveCollidableSettingId = id++;
            LightIntensitySettingId = id++;
            LightRangeSettingId = id++;
            LightColorRSettingId = id++;
            LightColorGSettingId = id++;
            LightColorBSettingId = id++;
            LightShadowsSettingId = id++;
            LightShadowStrengthSettingId = id++;
            LightTypeSettingId = id++;
            LightSpotAngleSettingId = id++;
            LightShapeSettingId = id++;
            LightInnerSpotAngleSettingId = id++;
            TextContentSettingId = id++;
            TextWidthSettingId = id++;
            TextHeightSettingId = id++;
            DoorTypeSettingId = id++;
            DoorOpenSettingId = id++;
            DoorLockedSettingId = id++;
            DoorPermissionsSettingId = id++;
            DoorRequireAllPermsSettingId = id++;
            DoorBypass2176SettingId = id++;
            DoorMaxHealthSettingId = id++;
            DoorHealthSettingId = id++;
            WorkstationInfoId = id++;
            WorkstationAllowInteractionsSettingId = id++;
            InteractShapeSettingId = id++;
            InteractDurationSettingId = id++;
            InteractLockedSettingId = id++;
            InteractPermissionsSettingId = id++;
            ClutterInfoId = id++;
            ClutterTypeSettingId = id++;
            LockerInfoId = id++;
            LockerPopulateSettingId = id++;
            LockerClearSettingId = id++;
            CameraLabelSettingId = id++;
            CameraTypeSettingId = id++;
            CameraRoomSettingId = id++;
            CameraVerticalMinSettingId = id++;
            CameraVerticalMaxSettingId = id++;
            CameraHorizontalMinSettingId = id++;
            CameraHorizontalMaxSettingId = id++;
            CameraZoomMinSettingId = id++;
            CameraZoomMaxSettingId = id++;
            WaypointBoundsXSettingId = id++;
            WaypointBoundsYSettingId = id++;
            WaypointBoundsZSettingId = id++;
            WaypointPrioritySettingId = id++;
            WaypointVisualizeSettingId = id++;
            TargetTypeSettingId = id++;
            PickupInfoId = id++;
            PickupItemToSpawnSettingId = id++;
            PickupSpawnPercentageSettingId = id++;
            PickupMaxAmountSettingId = id++;
            PickupIsInfiniteSettingId = id++;
            CapyCollisionsSettingId = id++;
            RagdollInfoId = id++;
            RagdollRoleSettingId = id++;
            RagdollChanceSettingId = id++;
            RagdollDeathReasonSettingId = id++;
            RagdollNameSettingId = id++;
            TeleporterInfoId = id++;
            TeleporterCooldownSettingId = id++;
            TeleporterPerPlayerSettingId = id++;
            TeleporterFlagsSettingId = id++;
            SpeakerVolumeSettingId = id++;
            SpeakerSpatialSettingId = id++;
            SpeakerMinDistanceSettingId = id++;
            SpeakerMaxDistanceSettingId = id++;
            SpeakerLoopSettingId = id++;
            SpeakerPathSettingId = id++;
            SpawnPointInfoId = id++;
            SpawnChanceSettingId = id++;
            SpawnDisableSettingId = id++;
            SpawnDisabledSettingId = id++;
            SpawnInfoId = id++;
            SpawnDoorTypeSettingId = id++;
            SpawnLockerTypeSettingId = id++;
            SpawnPickupItemSettingId = id++;
            LockerTypeSettingId = id++;
            TeleporterTargetSettingId = id++;
            LockerChamberSettingId = id++;
            LockerChamberPermissionsSettingId = id++;
            LockerChamberItemSettingId = id++;
            LockerChamberChanceSettingId = id++;
            LockerChamberAmountSettingId = id++;
            LockerChamberApplySettingId = id++;
            LockerChamberClearSettingId = id;
        }

        [Description("Server Specific Setting (SSS) IDs used by the ToolGun. Change these if they conflict with another plugin's SSS IDs.")]
        public int SubMenuSettingId { get; set; }

        public int CycleForwardKeybindId { get; set; }
        public int CycleBackwardKeybindId { get; set; }
        public int SelectObjectKeybindId { get; set; }
        public int UnselectObjectKeybindId { get; set; }

        public int NoSelectionInfoId { get; set; }
        public int SelectionTitleInfoId { get; set; }
        public int NameSettingId { get; set; }
        public int PositionXSettingId { get; set; }
        public int PositionYSettingId { get; set; }
        public int PositionZSettingId { get; set; }
        public int RotationXSettingId { get; set; }
        public int RotationYSettingId { get; set; }
        public int RotationZSettingId { get; set; }
        public int ScaleXSettingId { get; set; }
        public int ScaleYSettingId { get; set; }
        public int ScaleZSettingId { get; set; }
        public int StaticSettingId { get; set; }
        public int SmoothingSettingId { get; set; }

        public int PrimitiveColorRSettingId { get; set; }
        public int PrimitiveColorGSettingId { get; set; }
        public int PrimitiveColorBSettingId { get; set; }
        public int PrimitiveTypeSettingId { get; set; }
        public int PrimitiveVisibleSettingId { get; set; }
        public int PrimitiveCollidableSettingId { get; set; }

        public int LightIntensitySettingId { get; set; }
        public int LightRangeSettingId { get; set; }
        public int LightColorRSettingId { get; set; }
        public int LightColorGSettingId { get; set; }
        public int LightColorBSettingId { get; set; }
        public int LightShadowsSettingId { get; set; }
        public int LightShadowStrengthSettingId { get; set; }
        public int LightTypeSettingId { get; set; }
        public int LightSpotAngleSettingId { get; set; }
        public int LightShapeSettingId { get; set; }
        public int LightInnerSpotAngleSettingId { get; set; }

        public int TextContentSettingId { get; set; }
        public int TextWidthSettingId { get; set; }
        public int TextHeightSettingId { get; set; }

        public int DoorTypeSettingId { get; set; }
        public int DoorOpenSettingId { get; set; }
        public int DoorLockedSettingId { get; set; }
        public int DoorPermissionsSettingId { get; set; }
        public int DoorRequireAllPermsSettingId { get; set; }
        public int DoorBypass2176SettingId { get; set; }
        public int DoorMaxHealthSettingId { get; set; }
        public int DoorHealthSettingId { get; set; }

        public int WorkstationInfoId { get; set; }
        public int WorkstationAllowInteractionsSettingId { get; set; }
        public int InteractShapeSettingId { get; set; }
        public int InteractDurationSettingId { get; set; }
        public int InteractLockedSettingId { get; set; }
        public int InteractPermissionsSettingId { get; set; }
        public int ClutterInfoId { get; set; }
        public int ClutterTypeSettingId { get; set; }
        public int LockerInfoId { get; set; }
        public int LockerPopulateSettingId { get; set; }
        public int LockerClearSettingId { get; set; }
        public int CameraLabelSettingId { get; set; }
        public int CameraTypeSettingId { get; set; }
        public int CameraRoomSettingId { get; set; }
        public int CameraVerticalMinSettingId { get; set; }
        public int CameraVerticalMaxSettingId { get; set; }
        public int CameraHorizontalMinSettingId { get; set; }
        public int CameraHorizontalMaxSettingId { get; set; }
        public int CameraZoomMinSettingId { get; set; }
        public int CameraZoomMaxSettingId { get; set; }
        public int WaypointBoundsXSettingId { get; set; }
        public int WaypointBoundsYSettingId { get; set; }
        public int WaypointBoundsZSettingId { get; set; }
        public int WaypointPrioritySettingId { get; set; }
        public int WaypointVisualizeSettingId { get; set; }
        public int TargetTypeSettingId { get; set; }
        public int PickupInfoId { get; set; }
        public int PickupItemToSpawnSettingId { get; set; }
        public int PickupSpawnPercentageSettingId { get; set; }
        public int PickupMaxAmountSettingId { get; set; }
        public int PickupIsInfiniteSettingId { get; set; }
        public int CapyCollisionsSettingId { get; set; }
        public int RagdollInfoId { get; set; }
        public int RagdollRoleSettingId { get; set; }
        public int RagdollChanceSettingId { get; set; }
        public int RagdollDeathReasonSettingId { get; set; }
        public int RagdollNameSettingId { get; set; }
        public int TeleporterInfoId { get; set; }
        public int TeleporterCooldownSettingId { get; set; }
        public int TeleporterPerPlayerSettingId { get; set; }
        public int TeleporterFlagsSettingId { get; set; }
        public int SpeakerVolumeSettingId { get; set; }
        public int SpeakerSpatialSettingId { get; set; }
        public int SpeakerMinDistanceSettingId { get; set; }
        public int SpeakerMaxDistanceSettingId { get; set; }
        public int SpeakerLoopSettingId { get; set; }
        public int SpeakerPathSettingId { get; set; }
        public int SpawnPointInfoId { get; set; }
        public int SpawnChanceSettingId { get; set; }
        public int SpawnDisableSettingId { get; set; }
        public int SpawnDisabledSettingId { get; set; }
        public int SpawnInfoId { get; set; }
        public int SpawnDoorTypeSettingId { get; set; }
        public int SpawnLockerTypeSettingId { get; set; }
        public int SpawnPickupItemSettingId { get; set; }
        public int LockerTypeSettingId { get; set; }
        public int TeleporterTargetSettingId { get; set; }
        public int LockerChamberSettingId { get; set; }
        public int LockerChamberPermissionsSettingId { get; set; }
        public int LockerChamberItemSettingId { get; set; }
        public int LockerChamberChanceSettingId { get; set; }
        public int LockerChamberAmountSettingId { get; set; }
        public int LockerChamberApplySettingId { get; set; }
        public int LockerChamberClearSettingId { get; set; }
    }
}
