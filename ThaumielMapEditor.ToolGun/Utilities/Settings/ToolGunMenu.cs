// -----------------------------------------------------------------------
// <copyright file="ToolGunMenu.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;
using LabApi.Features.Wrappers;
using SecretAPI.Features.UserSettings;
using ThaumielMapEditor.API.Blocks;
using ThaumielMapEditor.API.Blocks.ClientSide;
using ThaumielMapEditor.API.Blocks.ServerObjects;
using ThaumielMapEditor.API.Data;
using ThaumielMapEditor.ToolGun.Extensions;
using ThaumielMapEditor.ToolGun.Utilities.Settings.Objects;
using UnityEngine;
using UserSettings.ServerSpecific;

namespace ThaumielMapEditor.ToolGun.Utilities.Settings
{
    public static class ToolGunMenu
    {
        private static readonly HashSet<Player> Equipped = [];

        private static List<CustomSetting> All = [
            new SubMenuSetting(),

            new CycleForwardKeybind(),
            new CycleBackwardKeybind(),
            new SelectObjectKeybind(),
            new UnselectObjectKeybind(),

            new NoSelectionInfo(),
            new SelectionTitleInfo(),
            new NameSetting(),

            new PositionXSetting(),
            new PositionYSetting(),
            new PositionZSetting(),
            new RotationXSetting(),
            new RotationYSetting(),
            new RotationZSetting(),
            new ScaleXSetting(),
            new ScaleYSetting(),
            new ScaleZSetting(),
            new StaticSetting(),
            new SmoothingSetting(),

            new PrimitiveColorRSetting(),
            new PrimitiveColorGSetting(),
            new PrimitiveColorBSetting(),
            new PrimitiveTypeSetting(),
            new PrimitiveVisibleSetting(),
            new PrimitiveCollidableSetting(),

            new LightIntensitySetting(),
            new LightRangeSetting(),
            new LightColorRSetting(),
            new LightColorGSetting(),
            new LightColorBSetting(),
            new LightShadowsSetting(),
            new LightShadowStrengthSetting(),
            new LightTypeSetting(),
            new LightSpotAngleSetting(),
            new LightShapeSetting(),
            new LightInnerSpotAngleSetting(),

            new TextContentSetting(),
            new TextWidthSetting(),
            new TextHeightSetting(),

            new DoorTypeSetting(),
            new DoorOpenSetting(),
            new DoorLockedSetting(),
            new DoorPermissionsSetting(),
            new DoorRequireAllPermsSetting(),
            new DoorBypass2176Setting(),
            new DoorMaxHealthSetting(),
            new DoorHealthSetting(),

            new WorkstationInfo(),
            new WorkstationAllowInteractionsSetting(),
            new InteractShapeSetting(),
            new InteractDurationSetting(),
            new InteractLockedSetting(),
            new InteractPermissionsSetting(),
            new ClutterInfo(),
            new ClutterTypeSetting(),
            new LockerInfo(),
            new LockerTypeSetting(),
            new LockerChamberSetting(),
            new LockerChamberPermissionsSetting(),
            new LockerChamberItemSetting(),
            new LockerChamberChanceSetting(),
            new LockerChamberAmountSetting(),
            new LockerChamberApplySetting(),
            new LockerChamberClearSetting(),
            new LockerPopulateSetting(),
            new LockerClearSetting(),
            new CameraTypeSetting(),
            new CameraLabelSetting(),
            new CameraRoomSetting(),
            new CameraVerticalMinSetting(),
            new CameraVerticalMaxSetting(),
            new CameraHorizontalMinSetting(),
            new CameraHorizontalMaxSetting(),
            new CameraZoomMinSetting(),
            new CameraZoomMaxSetting(),
            new WaypointBoundsXSetting(),
            new WaypointBoundsYSetting(),
            new WaypointBoundsZSetting(),
            new WaypointPrioritySetting(),
            new WaypointVisualizeSetting(),
            new TargetTypeSetting(),
            new PickupInfo(),
            new PickupItemToSpawnSetting(),
            new PickupSpawnPercentageSetting(),
            new PickupMaxAmountSetting(),
            new PickupIsInfiniteSetting(),
            new CapyCollisionsSetting(),
            new RagdollInfo(),
            new RagdollRoleSetting(),
            new RagdollChanceSetting(),
            new RagdollDeathReasonSetting(),
            new RagdollNameSetting(),
            new TeleporterInfo(),
            new TeleporterCooldownSetting(),
            new TeleporterPerPlayerSetting(),
            new TeleporterFlagsSetting(),
            new TeleporterTargetSetting(),
            new SpeakerVolumeSetting(),
            new SpeakerSpatialSetting(),
            new SpeakerMinDistanceSetting(),
            new SpeakerMaxDistanceSetting(),
            new SpeakerLoopSetting(),
            new SpeakerPathSetting(),
            new SpawnPointInfo(),
            new SpawnChanceSetting(),
            new SpawnDisableSetting(),
            new SpawnDisabledSetting(),

            new SpawnInfo(),
            new SpawnDoorTypeSetting(),
            new SpawnLockerTypeSetting(),
            new SpawnPickupItemSetting(),
        ];

        public static void Register()
        {
            CustomSetting.Register(All);
            PlayerEvents.Left += OnLeft;
        }

        public static void Unregister()
        {
            PlayerEvents.Left -= OnLeft;

            CustomSetting.UnRegister(All);
            All = [];
            Equipped.Clear();
        }

        public static void Equip(Player player)
        {
            Equipped.Add(player);
            Refresh(player);
        }

        public static void Unequip(Player player)
        {
            Equipped.Remove(player);
            Refresh(player);
        }

        public static void Refresh(Player player)
        {
            ServerSpecificSettingsSync.Version++;
            CustomSetting.SendSettingsToPlayer(player);
        }

        public static bool IsEquipped(Player player) => Equipped.Contains(player);

        public static bool IsModifierMenu(Player player)
        {
            SubMenuSetting? sub = CustomSetting.GetPlayerSetting<SubMenuSetting>(SubMenuSetting.SettingId, player);
            if (sub == null)
                return false;

            return sub.ValidatedSelectedIndex == 1;
        }

        public static bool IsSpawnMenu(Player player)
        {
            SubMenuSetting? sub = CustomSetting.GetPlayerSetting<SubMenuSetting>(SubMenuSetting.SettingId, player);
            if (sub == null)
                return false;

            return sub.ValidatedSelectedIndex == 2;
        }

        public static bool IsKeybindsMenu(Player player)
        {
            SubMenuSetting? sub = CustomSetting.GetPlayerSetting<SubMenuSetting>(SubMenuSetting.SettingId, player);
            if (sub == null)
                return true;

            return sub.ValidatedSelectedIndex == 0;
        }

        public static bool HasSelection(Player player) => TryGetSelection(player, out _, out _, out _);

        public static bool TryGetSelection(Player player, out ToolGunSettings? settings, out ServerObject? server, out ClientObject? client)
        {
            settings = player.GetToolGunSettings();
            server = settings?.SelectedServer;
            client = server != null ? null : settings?.SelectedClient;
            return server != null || client != null;
        }

        public static bool TryGetPrimitive(Player player, out PrimitiveObjectServer? server, out PrimitiveObject? client)
        {
            server = null;
            client = null;

            if (!TryGetSelection(player, out _, out ServerObject? selectedServer, out ClientObject? selectedClient))
                return false;

            server = selectedServer as PrimitiveObjectServer;
            client = server != null ? null : selectedClient as PrimitiveObject;
            return server != null || client != null;
        }

        public static bool TryGetLight(Player player, out LightObjectServer? server, out LightObject? client)
        {
            server = null;
            client = null;

            if (!TryGetSelection(player, out _, out ServerObject? selectedServer, out ClientObject? selectedClient))
                return false;

            server = selectedServer as LightObjectServer;
            client = server != null ? null : selectedClient as LightObject;
            return server != null || client != null;
        }

        public static bool TryGetText(Player player, out TextToyObject? server, out TextObject? client)
        {
            server = null;
            client = null;

            if (!TryGetSelection(player, out _, out ServerObject? selectedServer, out ClientObject? selectedClient))
                return false;

            server = selectedServer as TextToyObject;
            client = server != null ? null : selectedClient as TextObject;
            return server != null || client != null;
        }

        public static bool TryGetCapy(Player player, out CapybaraObjectServer? server, out CapybaraObject? client)
        {
            server = null;
            client = null;

            if (!TryGetSelection(player, out _, out ServerObject? selectedServer, out ClientObject? selectedClient))
                return false;

            server = selectedServer as CapybaraObjectServer;
            client = server != null ? null : selectedClient as CapybaraObject;
            return server != null || client != null;
        }

        public static Vector3 GetPosition(Player player)
        {
            if (!TryGetSelection(player, out _, out ServerObject? server, out ClientObject? client))
                return default;

            if (server != null)
                return server.Position;

            return client != null ? GetClientWorldPosition(client) : default;
        }

        /// <summary>
        /// Sets the global position of the selected object.
        /// </summary>
        public static void SetPosition(Player player, Vector3 value)
        {
            if (!TryGetSelection(player, out _, out ServerObject? server, out ClientObject? client))
                return;

            if (server != null)
            {
                server.Position = value;
            }
            else if (client != null)
                SetClientWorldPosition(client, value);
        }

        /// <summary>
        /// Gets the global rotation of the selected object.
        /// </summary>
        public static Quaternion GetRotation(Player player)
        {
            if (!TryGetSelection(player, out _, out ServerObject? server, out ClientObject? client))
                return Quaternion.identity;

            if (server != null)
                return server.Rotation;

            return client != null ? GetClientWorldRotation(client) : Quaternion.identity;
        }

        /// <summary>
        /// Sets the global rotation of the selected object.
        /// </summary>
        public static void SetRotation(Player player, Quaternion value)
        {
            if (!TryGetSelection(player, out _, out ServerObject? server, out ClientObject? client))
                return;

            if (server != null)
            {
                server.Rotation = value;
            }
            else if (client != null)
                SetClientWorldRotation(client, value);
        }

        /// <summary>
        /// Gets the global scale of the selected object.
        /// </summary>
        public static Vector3 GetScale(Player player)
        {
            if (!TryGetSelection(player, out _, out ServerObject? server, out ClientObject? client))
                return Vector3.one;

            if (server != null)
                return server.Scale;

            return client != null ? GetClientWorldScale(client) : Vector3.one;
        }

        /// <summary>
        /// Sets the global scale of the selected object.
        /// </summary>
        public static void SetScale(Player player, Vector3 value)
        {
            if (!TryGetSelection(player, out _, out ServerObject? server, out ClientObject? client))
                return;

            if (server != null)
            {
                server.Scale = value;
            }
            else if (client != null)
                SetClientWorldScale(client, value);
        }

        /// <summary>
        /// Gets the schematic that spawned <paramref name="obj"/>, or <see langword="null"/> when unavailable.
        /// </summary>
        private static SchematicData? GetSchematic(ClientObject obj) => obj switch
        {
            PrimitiveObject prim => prim.Schematic,
            LightObject light => light.Schematic,
            TextObject text => text.Schematic,
            CapybaraObject capy => capy.Schematic,
            _ => null,
        };

        /// <summary>
        /// Resolves the world transform of the schematic-root parent of <paramref name="obj"/>.
        /// Falls back to identity when the schematic root is unavailable.
        /// </summary>
        private static void GetClientParentWorld(ClientObject obj, out Vector3 position, out Quaternion rotation, out Vector3 scale)
        {
            position = Vector3.zero;
            rotation = Quaternion.identity;
            scale = Vector3.one;

            List<ClientObject> chain = [];
            ClientObject current = obj;
            HashSet<ClientObject> seen = [obj];

            while (current.ParentId != -1 && GetSchematic(current) is { } currentSchematic)
            {
                ClientObject? parent = currentSchematic.SpawnedClientObjects.FirstOrDefault(o => o != null && o.ObjectId == current.ParentId);
                if (parent == null || !seen.Add(parent))
                    break;

                chain.Add(parent);
                current = parent;
            }

            Transform? root = GetSchematic(obj)?.Primitive?.Transform;
            if (root != null)
            {
                position = root.position;
                rotation = root.rotation;
                scale = SanitizeScale(root.lossyScale);
            }

            for (int i = chain.Count - 1; i >= 0; i--)
            {
                ClientObject parent = chain[i];
                position = rotation * Vector3.Scale(parent.Position, scale) + position;
                rotation *= parent.Rotation;
                scale = Vector3.Scale(scale, parent.Scale);
            }
        }

        private static Vector3 SanitizeScale(Vector3 scale) => new(
            Mathf.Approximately(scale.x, 0f) ? 1f : scale.x,
            Mathf.Approximately(scale.y, 0f) ? 1f : scale.y,
            Mathf.Approximately(scale.z, 0f) ? 1f : scale.z);

        /// <summary>
        /// Gets the world space position of a client side object.
        /// </summary>
        public static Vector3 GetClientWorldPosition(ClientObject obj)
        {
            GetClientParentWorld(obj, out Vector3 parentPos, out Quaternion parentRot, out Vector3 parentScale);
            return parentRot * Vector3.Scale(obj.Position, parentScale) + parentPos;
        }

        /// <summary>
        /// Sets the world space position of a client side object.
        /// </summary>
        public static void SetClientWorldPosition(ClientObject obj, Vector3 world)
        {
            GetClientParentWorld(obj, out Vector3 parentPos, out Quaternion parentRot, out Vector3 parentScale);
            parentScale = SanitizeScale(parentScale);
            Vector3 local = Quaternion.Inverse(parentRot) * (world - parentPos);
            obj.Position = new Vector3(local.x / parentScale.x, local.y / parentScale.y, local.z / parentScale.z);
        }

        /// <summary>
        /// Gets the world space rotation of a client side object.
        /// </summary>
        public static Quaternion GetClientWorldRotation(ClientObject obj)
        {
            GetClientParentWorld(obj, out _, out Quaternion parentRot, out _);
            return parentRot * obj.Rotation;
        }

        /// <summary>
        /// Sets the world space rotation of a client side object.
        /// </summary>
        public static void SetClientWorldRotation(ClientObject obj, Quaternion world)
        {
            GetClientParentWorld(obj, out _, out Quaternion parentRot, out _);
            obj.Rotation = Quaternion.Inverse(parentRot) * world;
        }

        /// <summary>
        /// Gets the world space scale of a client side object.
        /// </summary>
        public static Vector3 GetClientWorldScale(ClientObject obj)
        {
            GetClientParentWorld(obj, out _, out _, out Vector3 parentScale);
            return Vector3.Scale(parentScale, obj.Scale);
        }

        /// <summary>
        /// Sets the world space scale of a client side object.
        /// </summary>
        public static void SetClientWorldScale(ClientObject obj, Vector3 world)
        {
            GetClientParentWorld(obj, out _, out _, out Vector3 parentScale);
            parentScale = SanitizeScale(parentScale);
            obj.Scale = new Vector3(world.x / parentScale.x, world.y / parentScale.y, world.z / parentScale.z);
        }

        public static bool GetStatic(Player player)
        {
            if (!TryGetSelection(player, out _, out ServerObject? server, out ClientObject? client))
                return false;

            if (server != null)
                return server.IsStatic;

            return client?.IsStatic ?? false;
        }

        public static void SetStatic(Player player, bool value)
        {
            if (!TryGetSelection(player, out _, out ServerObject? server, out ClientObject? client))
                return;

            if (server != null)
            {
                server.IsStatic = value;
            }
            else
                client?.IsStatic = value;
        }

        public static byte GetSmoothing(Player player)
        {
            if (!TryGetSelection(player, out _, out ServerObject? server, out ClientObject? client))
                return 0;

            if (server != null)
                return server.MovementSmoothing;

            return client?.MovementSmoothing ?? 0;
        }

        public static void SetSmoothing(Player player, byte value)
        {
            if (!TryGetSelection(player, out _, out ServerObject? server, out ClientObject? client))
                return;

            if (server != null)
            {
                server.MovementSmoothing = value;
            }
            else
                client?.MovementSmoothing = value;
        }

        public static Color GetColor(Player player)
        {
            if (TryGetPrimitive(player, out PrimitiveObjectServer? primServer, out PrimitiveObject? primClient))
                return primServer != null ? primServer.Color : primClient?.Color ?? Color.white;

            if (TryGetLight(player, out LightObjectServer? lightServer, out LightObject? lightClient))
                return lightServer != null ? lightServer.Color : lightClient?.Color ?? Color.white;

            return Color.white;
        }

        public static void SetColor(Player player, Color value)
        {
            if (TryGetPrimitive(player, out PrimitiveObjectServer? primServer, out PrimitiveObject? primClient))
            {
                if (primServer != null)
                {
                    primServer.Color = value;
                }
                else
                    primClient?.Color = value;

                return;
            }

            if (TryGetLight(player, out LightObjectServer? lightServer, out LightObject? lightClient))
            {
                if (lightServer != null)
                {
                    lightServer.Color = value;
                }
                else
                    lightClient?.Color = value;
            }
        }

        public static string GetName(Player player)
        {
            if (!TryGetSelection(player, out _, out ServerObject? server, out ClientObject? client))
                return string.Empty;

            if (server != null)
                return server.Name;

            return client switch
            {
                PrimitiveObject prim => prim.Name,
                CapybaraObject capy => capy.Name,
                _ => string.Empty,
            };
        }

        public static void SetName(Player player, string value)
        {
            if (!TryGetSelection(player, out _, out ServerObject? server, out ClientObject? client))
                return;

            if (server != null)
            {
                server.Name = value;
                return;
            }

            switch (client)
            {
                case PrimitiveObject prim:
                    prim.Name = value;
                    break;

                case CapybaraObject capy:
                    capy.Name = value;
                    break;
            }
        }

        public static bool HasName(Player player)
        {
            if (!TryGetSelection(player, out _, out ServerObject? server, out ClientObject? client))
                return false;

            if (server != null)
                return true;

            return client is PrimitiveObject or CapybaraObject;
        }

        public static float Clamp(float value, float min, float max)
        {
            if (float.IsNaN(value))
                return min;

            if (float.IsPositiveInfinity(value))
                return max;

            if (float.IsNegativeInfinity(value))
                return min;

            return value < min ? min : value > max ? max : value;
        }

        public static float Clamp01(float value) => Clamp(value, 0f, 1f);

        public static string SanitizeDisplayText(string? value, int maxLength)
        {
            if (value == null || value.Length == 0)
                return string.Empty;

            string sanitized = value.Replace('\r', ' ').Replace('\n', ' ').Replace('\t', ' ').Replace('<', ' ').Replace('>', ' ').Trim();

            if (sanitized.Length > maxLength)
                sanitized = sanitized.Substring(0, maxLength);

            return sanitized;
        }

        public static void SyncPositionSliders(Player player)
        {
            if (!TryGetSelection(player, out _, out _, out _))
                return;

            Vector3 pos = GetPosition(player);

            PositionXSetting? x = CustomSetting.GetPlayerSetting<PositionXSetting>(PositionXSetting.SettingId, player);
            PositionYSetting? y = CustomSetting.GetPlayerSetting<PositionYSetting>(PositionYSetting.SettingId, player);
            PositionZSetting? z = CustomSetting.GetPlayerSetting<PositionZSetting>(PositionZSetting.SettingId, player);

            if (x != null)
            {
                float v = Clamp(pos.x, -1000f, 1000f);
                x.DefaultValue = v; 
                x.SendServerUpdate(v);
            }

            if (y != null)
            {
                float v = Clamp(pos.y, -1000f, 1000f);
                y.DefaultValue = v;
                y.SendServerUpdate(v);
            }

            if (z != null)
            {
                float v = Clamp(pos.z, -1000f, 1000f);
                z.DefaultValue = v;
                z.SendServerUpdate(v);
            }
        }
        
        private static void OnLeft(PlayerLeftEventArgs ev)
        {
            ToolGunUtils.ClearAllSelectionsForPlayer(ev.Player);
            Equipped.Remove(ev.Player);
        }
    }
}
