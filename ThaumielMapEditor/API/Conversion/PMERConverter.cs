// -----------------------------------------------------------------------
// <copyright file="PMERConverter.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using ThaumielMapEditor.API.Serialization;
using ThaumielMapEditor.API.Enums;
using System;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using AdminToys;
using Interactables.Interobjects.DoorUtils;
using MapGeneration;
using PlayerRoles;
using ThaumielMapEditor.API.Blocks.ServerObjects.Lockers;
using ThaumielMapEditor.API.Helpers;
using static AdminToys.InvisibleInteractableToy;
using ThaumielMapEditor.API.Extensions;
using TmeCameraType = ThaumielMapEditor.API.Enums.CameraType;
using TmeDoorType = ThaumielMapEditor.API.Enums.DoorType;
using TmeLockerType = ThaumielMapEditor.API.Enums.LockerType;
using TmeTargetType = ThaumielMapEditor.API.Enums.TargetType;

namespace ThaumielMapEditor.API.Conversion
{
    public static class PMERConverter
    {
        public enum PMERBlockType
        {
            Empty = 0,
            Primitive = 1,
            Light = 2,
            Pickup = 3,
            Workstation = 4,
            Schematic = 5,
            Teleport = 6,
            Locker = 7,
            Text = 8,
            Interactable = 9,
        }

        /// <summary>
        /// Converts a PMER schematic into a TME schematic
        /// </summary>
        /// <param name="root">The PMER schematic root</param>
        /// <returns></returns>
        public static async Task<SerializableSchematic> ConvertSchematicAsync(PMERRoot root)
        {
            return await Task.Run(() =>
            {
                SerializableSchematic tme = new()
                {
                    RootObjectId = root.RootObjectId,
                    FileName = root.Name,
                    Rotation = Quaternion.identity,
                    Scale = Vector3.one,
                    Objects = []
                };

                foreach (PMERBlock block in root.Blocks)
                    tme.Objects.Add(ConvertBlock(block));

                return tme;
            });
        }

        public static async Task<SerializableSchematic> ConvertMapAsync(PMERMapRoot root)
        {
            return await Task.Run(() =>
            {
                MapConversionState state = new(root.Name);
                foreach (string id in root.Teleports.Keys)
                    state.KnownTeleports.Add(id);

                SerializableSchematic tme = new()
                {
                    RootObjectId = -1,
                    FileName = root.Name,
                    Rotation = Quaternion.identity,
                    Scale = Vector3.one,
                    Objects = []
                };

                foreach (KeyValuePair<string, PMERMapPrimitive> kvp in root.Primitives)
                    AddMapObject(state, tme, () => ConvertMapPrimitive(state, kvp.Key, kvp.Value));

                foreach (KeyValuePair<string, PMERMapLight> kvp in root.Lights)
                    AddMapObject(state, tme, () => ConvertMapLight(state, kvp.Key, kvp.Value));

                foreach (KeyValuePair<string, PMERMapDoor> kvp in root.Doors)
                    AddMapObject(state, tme, () => ConvertMapDoor(state, kvp.Key, kvp.Value));

                foreach (KeyValuePair<string, PMERMapWorkstation> kvp in root.Workstations)
                    AddMapObject(state, tme, () => ConvertMapWorkstation(state, kvp.Key, kvp.Value));

                foreach (KeyValuePair<string, PMERMapItemSpawnpoint> kvp in root.ItemSpawnpoints)
                    AddMapObject(state, tme, () => ConvertMapItemSpawnpoint(state, kvp.Key, kvp.Value));

                foreach (KeyValuePair<string, PMERMapPlayerSpawnpoint> kvp in root.PlayerSpawnpoints)
                    AddMapObject(state, tme, () => ConvertMapPlayerSpawnpoint(state, kvp.Key, kvp.Value));

                foreach (KeyValuePair<string, PMERMapCapybara> kvp in root.Capybaras)
                    AddMapObject(state, tme, () => ConvertMapCapybara(state, kvp.Key, kvp.Value));

                foreach (KeyValuePair<string, PMERMapText> kvp in root.Texts)
                    AddMapObject(state, tme, () => ConvertMapText(state, kvp.Key, kvp.Value));

                foreach (KeyValuePair<string, PMERMapInteractable> kvp in root.Interactables)
                    AddMapObject(state, tme, () => ConvertMapInteractable(state, kvp.Key, kvp.Value));

                foreach (KeyValuePair<string, PMERMapScp079Camera> kvp in root.Scp079Cameras)
                    AddMapObject(state, tme, () => ConvertMapCamera(state, kvp.Key, kvp.Value));

                foreach (KeyValuePair<string, PMERMapShootingTarget> kvp in root.ShootingTargets)
                    AddMapObject(state, tme, () => ConvertMapShootingTarget(state, kvp.Key, kvp.Value));

                foreach (KeyValuePair<string, PMERMapSchematicRef> kvp in root.Schematics)
                    AddMapObject(state, tme, () => ConvertMapSchematicRef(state, kvp.Key, kvp.Value));

                foreach (KeyValuePair<string, PMERMapTeleport> kvp in root.Teleports)
                    AddMapObject(state, tme, () => ConvertMapTeleport(state, kvp.Key, kvp.Value));

                foreach (KeyValuePair<string, PMERMapLocker> kvp in root.Lockers)
                    AddMapObject(state, tme, () => ConvertMapLocker(state, kvp.Key, kvp.Value));

                LogMapSummary(state);
                return tme;
            });
        }

        private static void AddMapObject(MapConversionState state, SerializableSchematic tme, Func<SerializableObject> convert)
        {
            try
            {
                tme.Objects.Add(convert());
                state.Converted++;
            }
            catch (Exception ex)
            {
                state.Skipped++;
                LogManager.Error($"Failed to convert a PMER map object in '{state.MapName}': {ex.Message}");
            }
        }

        private sealed class MapConversionState
        {
            public MapConversionState(string mapName) => MapName = mapName;

            public string MapName;

            public int NextId;

            public int Converted;

            public int Skipped;

            public HashSet<string> Rooms = new(StringComparer.OrdinalIgnoreCase);

            public HashSet<string> KnownTeleports = new(StringComparer.OrdinalIgnoreCase);

            private readonly HashSet<string> _warned = [];

            public void WarnOnce(string key, string message)
            {
                if (_warned.Add(key))
                    LogManager.Warn(message);
            }
        }

        private static void LogMapSummary(MapConversionState state)
        {
            string[] rooms = state.Rooms.Where(r => !r.Equals("Unknown", StringComparison.OrdinalIgnoreCase)).ToArray();
            if (rooms.Length > 1)
                LogManager.Warn($"PMER map '{state.MapName}' spans {rooms.Length} rooms ({string.Join(", ", rooms)}). TME schematics are not room-anchored, so positions were kept as-is and multi-room layouts will need manual adjustment. Source rooms are preserved in PmerRoom values.");

            LogManager.Info($"Converted PMER map '{state.MapName}': {state.Converted} objects converted, {state.Skipped} skipped.");
        }

        private static SerializableObject NewMapObject(MapConversionState state, string name, PMERMapObject src, ObjectType type, Dictionary<string, object> values)
        {
            if (!string.IsNullOrWhiteSpace(src.Room))
                state.Rooms.Add(src.Room);

            SerializableObject obj = new()
            {
                Name = name,
                ObjectId = state.NextId++,
                ParentId = -1,
                Position = src.Position,
                Rotation = Quaternion.Euler(src.Rotation),
                Scale = src.Scale,
                IsStatic = false,
                MovementSmoothing = 60,
                ObjectType = type,
                Values = values
            };

            if (!string.IsNullOrWhiteSpace(src.Room) && !src.Room.Equals("Unknown", StringComparison.OrdinalIgnoreCase))
                obj.Values["PmerRoom"] = src.Room;

            if (src.Index != -1)
                obj.Values["PmerIndex"] = src.Index;

            return obj;
        }

        private static bool TryParseName<T>(string? raw, out T result) where T : struct, Enum
        {
            result = default;
            string cleaned = (raw ?? string.Empty).Trim().Replace(" ", "");
            if (cleaned.Length == 0)
                return false;

            try
            {
                result = (T)Enum.Parse(typeof(T), cleaned, true);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool TryParseNameOrInt<T>(string? raw, out T result) where T : struct, Enum
        {
            if (TryParseName(raw, out result))
                return true;

            if (int.TryParse(raw?.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int num))
            {
                result = (T)Enum.ToObject(typeof(T), num);
                return true;
            }

            result = default;
            return false;
        }

        private static bool TryParsePmerColor(string? raw, out Color color)
        {
            color = new Color(-1f, -1f, -1f);
            string text = raw ?? string.Empty;
            if (text.Length == 0)
                return false;

            string[] parts = text.Split(':');
            if (parts.Length >= 4)
            {
                if (float.TryParse(parts[0], NumberStyles.Any, CultureInfo.InvariantCulture, out float r))
                    color.r = r / 255f;

                if (float.TryParse(parts[1], NumberStyles.Any, CultureInfo.InvariantCulture, out float g))
                    color.g = g / 255f;

                if (float.TryParse(parts[2], NumberStyles.Any, CultureInfo.InvariantCulture, out float b))
                    color.b = b / 255f;

                if (float.TryParse(parts[3], NumberStyles.Any, CultureInfo.InvariantCulture, out float a))
                    color.a = a;

                return color != new Color(-1f, -1f, -1f);
            }

            if (text[0] != '#' && text.Length == 8)
                text = '#' + text;

            return ColorUtility.TryParseHtmlString(text, out color);
        }

        private static Color ParsePmerPrimitiveColor(MapConversionState state, string id, string? raw)
        {
            if (TryParsePmerColor(raw, out Color color))
                return color;

            state.WarnOnce("color", $"Some colors in map '{state.MapName}' could not be parsed and fell back to magenta (e.g. '{id}').");
            return Color.magenta * 3f;
        }

        private static Color ParsePmerLightColor(MapConversionState state, string id, string? raw)
        {
            if (ColorUtility.TryParseHtmlString(raw ?? string.Empty, out Color color))
                return color;

            state.WarnOnce("light-color", $"Some light colors in map '{state.MapName}' could not be parsed and fell back to magenta (e.g. '{id}').");
            return Color.magenta;
        }

        private static TmeDoorType MapPmerDoorType(MapConversionState state, string id, string? raw)
        {
            string normalized = (raw ?? string.Empty).Trim().ToLowerInvariant();
            TmeDoorType mapped = normalized switch
            {
                "lcz" or "lightcontainmentdoor" => TmeDoorType.Lcz,
                "hcz" or "heavycontainmentdoor" => TmeDoorType.Hcz,
                "ez" or "entrancedoor" or "entrancezone" => TmeDoorType.Ez,
                "gate" => TmeDoorType.Gate,
                "bulkdoor" or "heavybulkdoor" => TmeDoorType.BulkHead,
                "bulkhead" => TmeDoorType.BulkHead,
                _ => (TmeDoorType)(-1),
            };

            if ((int)mapped != -1)
                return mapped;

            if (int.TryParse(normalized, NumberStyles.Integer, CultureInfo.InvariantCulture, out int num))
            {
                mapped = num switch
                {
                    0 => TmeDoorType.Lcz,
                    1 => TmeDoorType.Hcz,
                    2 => TmeDoorType.Ez,
                    3 => TmeDoorType.BulkHead,
                    _ => (TmeDoorType)(-1),
                };

                if ((int)mapped != -1)
                    return mapped;
            }

            state.WarnOnce("door-type", $"Unknown door type in map '{state.MapName}', falling back to Lcz (e.g. '{id}': '{raw}').");
            return TmeDoorType.Lcz;
        }

        private static TmeLockerType MapPmerLockerType(MapConversionState state, string id, string? raw)
        {
            string normalized = (raw ?? string.Empty).Trim();
            if (normalized.StartsWith("pedestal", StringComparison.OrdinalIgnoreCase))
            {
                if (!normalized.Equals("PedestalScp500", StringComparison.OrdinalIgnoreCase))
                    state.WarnOnce("locker-pedestal", $"All pedestal variants in map '{state.MapName}' were collapsed to LockerType.Pedestal (TME has a single pedestal type).");

                return TmeLockerType.Pedestal;
            }

            if (TryParseName(normalized, out TmeLockerType named))
                return named;

            if (int.TryParse(normalized, NumberStyles.Integer, CultureInfo.InvariantCulture, out int num))
            {
                TmeLockerType mapped = num switch
                {
                    -1 => TmeLockerType.None,
                    0 => TmeLockerType.Pedestal,
                    1 => TmeLockerType.LargeGun,
                    2 => TmeLockerType.RifleRack,
                    3 => TmeLockerType.Misc,
                    4 => TmeLockerType.Medkit,
                    5 => TmeLockerType.Adrenaline,
                    15 => TmeLockerType.ExperimentalWeapon,
                    _ => TmeLockerType.None,
                };

                if (num is -1 or 0 or 1 or 2 or 3 or 4 or 5 or 15)
                    return mapped;
            }

            state.WarnOnce("locker-type", $"Unknown locker type in map '{state.MapName}', falling back to None (e.g. '{id}': '{raw}').");
            return TmeLockerType.None;
        }

        private static TmeTargetType MapPmerTargetType(MapConversionState state, string id, string? raw)
        {
            if (TryParseName(raw, out TmeTargetType named))
                return named;

            if (int.TryParse((raw ?? string.Empty).Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int num))
            {
                TmeTargetType mapped = num switch
                {
                    0 => TmeTargetType.Sport,
                    1 => TmeTargetType.ClassD,
                    2 => TmeTargetType.Binary,
                    _ => TmeTargetType.ClassD,
                };

                if (num is 0 or 1 or 2)
                    return mapped;
            }

            state.WarnOnce("target-type", $"Unknown target type in map '{state.MapName}', falling back to ClassD (e.g. '{id}': '{raw}').");
            return TmeTargetType.ClassD;
        }

        private static RoomName ParsePmerRoom(MapConversionState state, string? raw)
        {
            string text = raw ?? string.Empty;
            if (text.Length == 0 || text.Equals("Unknown", StringComparison.OrdinalIgnoreCase))
                return RoomName.Outside;

            string[] split = text.Split('_');
            if (split.Length == 3 && TryParseName(split[2], out RoomName room))
                return room;

            state.WarnOnce("camera-room", $"Some room references in map '{state.MapName}' could not be parsed and fell back to Outside (e.g. '{raw}').");
            return RoomName.Outside;
        }

        private static Guid TeleportGuid(string mapName, string id)
        {
            using MD5 md5 = MD5.Create();
            byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes($"pmer-teleport:{mapName}:{id}"));
            return new Guid(hash);
        }

        private static SerializableObject ConvertMapPrimitive(MapConversionState state, string id, PMERMapPrimitive src)
        {
            if (!TryParseNameOrInt(src.PrimitiveType, out PrimitiveType primitiveType))
            {
                state.WarnOnce("primitive-type", $"Unknown primitive type in map '{state.MapName}', falling back to Cube (e.g. '{id}': '{src.PrimitiveType}').");
                primitiveType = PrimitiveType.Cube;
            }

            if (!TryParseNameOrInt(src.PrimitiveFlags, out PrimitiveFlags flags))
            {
                state.WarnOnce("primitive-flags", $"Unknown primitive flags in map '{state.MapName}', falling back to Visible, Collidable (e.g. '{id}': '{src.PrimitiveFlags}').");
                flags = PrimitiveFlags.Visible | PrimitiveFlags.Collidable;
            }

            return NewMapObject(state, id, src, ObjectType.Primitive, new()
            {
                ["PrimitiveType"] = primitiveType,
                ["Color"] = ParsePmerPrimitiveColor(state, id, src.Color),
                ["PrimitiveFlags"] = flags,
            });
        }

        private static SerializableObject ConvertMapLight(MapConversionState state, string id, PMERMapLight src)
        {
            if (!TryParseNameOrInt(src.LightType, out LightType lightType))
            {
                state.WarnOnce("light-type", $"Unknown light type in map '{state.MapName}', falling back to Point (e.g. '{id}': '{src.LightType}').");
                lightType = LightType.Point;
            }

            if (!TryParseNameOrInt(src.Shadows, out LightShadows shadows))
            {
                state.WarnOnce("light-shadows", $"Unknown light shadows in map '{state.MapName}', falling back to Hard (e.g. '{id}': '{src.Shadows}').");
                shadows = LightShadows.Hard;
            }

#pragma warning disable CS0618
            if (!TryParseNameOrInt(src.Shape, out LightShape shape))
            {
                state.WarnOnce("light-shape", $"Unknown light shape in map '{state.MapName}', falling back to Cone (e.g. '{id}': '{src.Shape}').");
                shape = LightShape.Cone;
            }

            Dictionary<string, object> lightValues = new()
            {
                ["LightColor"] = ParsePmerLightColor(state, id, src.Color),
                ["LightIntensity"] = src.Intensity,
                ["LightRange"] = src.Range,
                ["ShadowType"] = shadows,
                ["ShadowStrength"] = src.Strength,
                ["LightType"] = lightType,
                ["LightShape"] = shape,
                ["SpotAngle"] = src.SpotAngle,
                ["InnerSpotAngle"] = src.InnerSpotAngle,
            };
#pragma warning restore CS0618

            return NewMapObject(state, id, src, ObjectType.Light, lightValues);
        }

        private static SerializableObject ConvertMapDoor(MapConversionState state, string id, PMERMapDoor src)
        {
            if (!TryParseNameOrInt(src.RequiredPermissions, out DoorPermissionFlags permissions))
            {
                state.WarnOnce("door-permissions", $"Unknown door permissions in map '{state.MapName}', falling back to None (e.g. '{id}': '{src.RequiredPermissions}').");
                permissions = DoorPermissionFlags.None;
            }

            return NewMapObject(state, id, src, ObjectType.Door, new()
            {
                ["DoorType"] = MapPmerDoorType(state, id, src.DoorType),
                ["Permissions"] = permissions,
                ["RequireAllPermissions"] = src.RequireAll,
                ["IsOpen"] = src.IsOpen,
                ["IsLocked"] = src.IsLocked,
            });
        }

        private static SerializableObject ConvertMapWorkstation(MapConversionState state, string id, PMERMapWorkstation src)
        {
            return NewMapObject(state, id, src, ObjectType.Workstation, new()
            {
                ["AllowInteractions"] = src.IsInteractable,
            });
        }

        private static SerializableObject ConvertMapItemSpawnpoint(MapConversionState state, string id, PMERMapItemSpawnpoint src)
        {
            if (!TryParseNameOrInt(src.ItemType, out ItemType itemType))
            {
                state.WarnOnce("item-type", $"Unknown item type in map '{state.MapName}', falling back to Lantern (e.g. '{id}': '{src.ItemType}').");
                itemType = ItemType.Lantern;
            }

            state.WarnOnce("item-limits", $"Item spawnpoints in map '{state.MapName}' were converted to plain pickups; Weight, AttachmentsCode, NumberOfUses, UseGravity and CanBePickedUp have no TME equivalent and were dropped.");

            return NewMapObject(state, id, src, ObjectType.Pickup, new()
            {
                ["ItemToSpawn"] = itemType,
                ["SpawnPercentage"] = 100f,
                ["MaxAmount"] = src.NumberOfItems,
                ["IsInfinite"] = false,
            });
        }

        private static SerializableObject ConvertMapPlayerSpawnpoint(MapConversionState state, string id, PMERMapPlayerSpawnpoint src)
        {
            List<RoleTypeId> roles = [];
            foreach (string role in src.Roles)
            {
                if (TryParseNameOrInt(role, out RoleTypeId roleType))
                    roles.Add(roleType);
                else
                    state.WarnOnce("player-role", $"Unknown player role in map '{state.MapName}', skipping it (e.g. '{id}': '{role}').");
            }

            return NewMapObject(state, id, src, ObjectType.PlayerSpawnPoint, new()
            {
                ["AllowedRoles"] = roles,
                ["Chance"] = 100f,
                ["Disable"] = DisableFlags.None,
                ["Disabled"] = false,
            });
        }

        private static SerializableObject ConvertMapCapybara(MapConversionState state, string id, PMERMapCapybara src)
        {
            return NewMapObject(state, id, src, ObjectType.Capybara, []);
        }

        private static SerializableObject ConvertMapText(MapConversionState state, string id, PMERMapText src)
        {
            return NewMapObject(state, id, src, ObjectType.TextToy, new()
            {
                ["Text"] = src.Text,
                ["DisplaySize"] = new Vector2(src.DisplaySize.x, src.DisplaySize.y),
            });
        }

        private static SerializableObject ConvertMapInteractable(MapConversionState state, string id, PMERMapInteractable src)
        {
            if (!TryParseNameOrInt(src.ColliderShape, out ColliderShape shape))
            {
                state.WarnOnce("interactable-shape", $"Unknown interactable shape in map '{state.MapName}', falling back to Box (e.g. '{id}': '{src.ColliderShape}').");
                shape = ColliderShape.Box;
            }

            return NewMapObject(state, id, src, ObjectType.Interactable, new()
            {
                ["Shape"] = shape,
                ["Duration"] = src.InteractionDuration,
                ["Locked"] = src.IsLocked,
            });
        }

        private static SerializableObject ConvertMapCamera(MapConversionState state, string id, PMERMapScp079Camera src)
        {
            if (!TryParseNameOrInt(src.CameraType, out TmeCameraType cameraType))
            {
                state.WarnOnce("camera-type", $"Unknown camera type in map '{state.MapName}', falling back to Lcz (e.g. '{id}': '{src.CameraType}').");
                cameraType = TmeCameraType.Lcz;
            }

            return NewMapObject(state, id, src, ObjectType.Camera, new()
            {
                ["CameraType"] = cameraType,
                ["Label"] = src.Label,
                ["Room"] = ParsePmerRoom(state, src.Room),
            });
        }

        private static SerializableObject ConvertMapShootingTarget(MapConversionState state, string id, PMERMapShootingTarget src)
        {
            return NewMapObject(state, id, src, ObjectType.Target, new()
            {
                ["TargetType"] = MapPmerTargetType(state, id, src.TargetType),
            });
        }

        private static SerializableObject ConvertMapSchematicRef(MapConversionState state, string id, PMERMapSchematicRef src)
        {
            state.WarnOnce("nested-schematic", $"Nested PMER schematics in map '{state.MapName}' were kept as ObjectType.Schematic references (e.g. '{id}': '{src.SchematicName}'), which TME cannot spawn yet.");

            return NewMapObject(state, id, src, ObjectType.Schematic, new()
            {
                ["SchematicName"] = src.SchematicName,
            });
        }

        private static SerializableObject ConvertMapTeleport(MapConversionState state, string id, PMERMapTeleport src)
        {
            List<Guid> targets = [];
            foreach (string target in src.Targets)
            {
                targets.Add(TeleportGuid(state.MapName, target));

                if (!state.KnownTeleports.Contains(target))
                    state.WarnOnce($"teleport-target-{target}", $"Teleport '{id}' in map '{state.MapName}' targets unknown teleport '{target}'. The link will dangle.");
            }

            return NewMapObject(state, id, src, ObjectType.Teleporter, new()
            {
                ["Id"] = TeleportGuid(state.MapName, id),
                ["Target"] = targets,
                ["CoolDown"] = src.Cooldown,
            });
        }

        private static SerializableObject ConvertMapLocker(MapConversionState state, string id, PMERMapLocker src)
        {
            List<LockerChamber> chambers = [];
            for (int i = 0; i < src.Chambers.Count; i++)
            {
                PMERMapLockerChamber chamber = src.Chambers[i];

                if (!TryParseNameOrInt(chamber.RequiredPermissions, out DoorPermissionFlags permissions))
                {
                    state.WarnOnce("locker-permissions", $"Unknown locker chamber permissions in map '{state.MapName}', falling back to None (e.g. '{id}': '{chamber.RequiredPermissions}').");
                    permissions = DoorPermissionFlags.None;
                }

                List<ChamberData> data = [];
                if (i == 0)
                {
                    foreach (PMERMapLockerLoot loot in src.Loot)
                    {
                        if (!TryParseNameOrInt(loot.TargetItem, out ItemType itemType))
                        {
                            state.WarnOnce("locker-item", $"Unknown locker loot item in map '{state.MapName}', skipping it (e.g. '{id}': '{loot.TargetItem}').");
                            continue;
                        }

                        data.Add(new ChamberData
                        {
                            ItemType = itemType,
                            SpawnPercent = 100f,
                            AmountToSpawn = loot.MaxPerChamber > 0 ? loot.MaxPerChamber : Math.Max(loot.MinPerChamber, 1),
                        });
                    }
                }

                chambers.Add(new LockerChamber
                {
                    Index = (uint)i,
                    Permissions = permissions,
                    Data = data,
                });
            }

            if (src.Chambers.Count > 0 && (src.Loot.Count > 0 || src.Chambers.Any(c => c.IsOpen || (c.AcceptableItems?.Count ?? 0) > 0)))
                state.WarnOnce("locker-limits", $"Locker loot in map '{state.MapName}' was attached to the first chamber with 100% spawn chance; chamber IsOpen states and AcceptableItems have no TME equivalent and were dropped.");

            return NewMapObject(state, id, src, ObjectType.Locker, new()
            {
                ["LockerType"] = MapPmerLockerType(state, id, src.LockerType),
                ["Chambers"] = chambers,
            });
        }

        private static SerializableObject ConvertBlock(PMERBlock block)
        {
            SerializableObject obj = new()
            {
                Name = block.Name,
                ObjectId = block.ObjectId,
                ParentId = block.ParentId,
                Position = block.Position,
                Rotation = Quaternion.Euler(block.Rotation),
                Scale = block.Scale,
                IsStatic = block.Properties != null && block.Properties.TryGetValue("Static", out object s) && Convert.ToBoolean(s),
                MovementSmoothing = 60,
                ObjectType = MapBlockType((PMERBlockType)block.BlockType),
                Values = NormalizeProperties(block)
            };

            return obj;
        }

        private static ObjectType MapBlockType(PMERBlockType blockType)
        {
            ObjectType Warn()
            {
                LogManager.Warn($"Invaild block type {blockType}");
                return ObjectType.None;
            }

            return blockType switch
            {
                PMERBlockType.Primitive => ObjectType.Primitive,
                PMERBlockType.Light => ObjectType.Light,
                PMERBlockType.Pickup => ObjectType.Pickup,
                PMERBlockType.Workstation => ObjectType.Workstation,
                PMERBlockType.Schematic => ObjectType.Schematic,
                PMERBlockType.Teleport => ObjectType.Teleporter,
                PMERBlockType.Locker => ObjectType.Locker,
                PMERBlockType.Text => ObjectType.TextToy,
                PMERBlockType.Interactable => ObjectType.Interactable,
                PMERBlockType.Empty => ObjectType.GameObject,
                _ => Warn()
            };
        }

        private static Dictionary<string, object> NormalizeProperties(PMERBlock block)
        {
            Dictionary<string, object> dict = block.Properties != null ? new Dictionary<string, object>(block.Properties) : [];

            switch ((PMERBlockType)block.BlockType)
            {
                case PMERBlockType.Primitive:
                    if (dict.TryGetValue("PrimitiveType", out var pt))
                        dict["PrimitiveType"] = Convert.ToInt32(pt);

                    if (dict.TryGetValue("PrimitiveFlags", out var pf))
                    {
                        dict["PrimitiveFlags"] = Convert.ToByte(pf);
                    }
                    else
                        dict["PrimitiveFlags"] = PrimitiveFlags.Visible | PrimitiveFlags.Collidable;

                    if (dict.TryGetValue("Color", out var color))
                    {
                        if (TryParseHexColor(color.ToString(), out Color unityColor))
                        {
                            dict["Color"] = unityColor;
                        }
                        else
                            LogManager.Warn($"Failed to parse color value: {color}");
                    }
                    break;

                case PMERBlockType.Light:
                    if (dict.TryGetValue("LightType", out var lighttype))
                        dict["LightType"] = (LightType)Convert.ToInt32(lighttype);

                    if (dict.TryGetValue("Color", out var lightcolor) && TryParseHexColor(lightcolor.ToString(), out Color unitylightColor))
                        dict["LightColor"] = unitylightColor;

                    if (dict.TryGetValue("Intensity", out var intensity))
                        dict["LightIntensity"] = Convert.ToSingle(intensity);

                    if (dict.TryGetValue("Range", out var range))
                        dict["LightRange"] = Convert.ToSingle(range);

#pragma warning disable CS0618
                    if (dict.TryGetValue("Shape", out var shape))
                        dict["LightShape"] = (LightShape)Convert.ToInt32(shape);
#pragma warning restore CS0618

                    if (dict.TryGetValue("SpotAngle", out var spotangle))
                        dict["SpotAngle"] = Convert.ToSingle(spotangle);

                    if (dict.TryGetValue("InnerSpotAngle", out var innerspotangle))
                        dict["InnerSpotAngle"] = Convert.ToSingle(innerspotangle);

                    if (dict.TryGetValue("ShadowStrength", out var shadowStrength))
                        dict["ShadowStrength"] = Convert.ToSingle(shadowStrength);

                    if (dict.TryGetValue("ShadowType", out var shadowtype))
                        dict["ShadowType"] = (LightShadows)Convert.ToInt32(shadowtype);
                    break;
    
                case PMERBlockType.Pickup:
                    if (dict.TryGetValue("ItemType", out var itemtype))
                        dict["ItemToSpawn"] = (ItemType)Convert.ToInt32(itemtype);

                    if (dict.TryGetValue("Chance", out var chance))
                        dict["SpawnPercentage"] = Convert.ToSingle(chance);

                    if (dict.TryGetValue("Uses", out var uses))
                        dict["MaxAmount"] = Convert.ToInt32(uses);
                    break;

                case PMERBlockType.Teleport:
                    if (dict.TryGetValue("Cooldown", out var cooldown))
                        dict["CoolDown"] = Convert.ToSingle(cooldown);

                    if (dict.TryGetValue("Id", out var id))
                        dict["Id"] = Guid.Parse(Convert.ToString(id));

                    if (dict.TryGetValue("TargetTeleporters", out var targets))
                    {
                        if (targets is object[] array)
                        {
                            List<Guid> targetIds = [];
                            foreach (object target in array)
                            {
                                if (target.GetType().GetField("Id")?.GetValue(target) is string targetId && Guid.TryParse(targetId, out Guid targetid))
                                    targetIds.Add(targetid);
                            }

                            dict["Target"] = targetIds;
                        }
                    }

                    break;

                case PMERBlockType.Interactable:
                    if (dict.TryGetValue("ColliderShape", out var collidershape))
                        dict["Shape"] = (ColliderShape)Convert.ToInt32(collidershape);

                    if (dict.TryGetValue("InteractionDuration", out var duration))
                        dict["Duration"] = Convert.ToSingle(duration);

                    if (dict.TryGetValue("IsLocked", out var locked))
                        dict["Locked"] = Convert.ToSingle(locked);

                    break;

                case PMERBlockType.Text:
                    if (dict.TryGetValue("Text", out var text))
                        dict["Text"] = Convert.ToString(text);

                    if (dict.TryGetValue("DisplaySize", out var displaysize))
                        dict["DisplaySize"] = ConvertExtensions.ToVector2(displaysize) ?? TextToy.DefaultDisplaySize;
                        
                    break;
            }

            return dict;
        }

        private static bool TryParseHexColor(string hex, out Color color)
        {
            color = Color.white;
            hex = hex.TrimStart('#');
            try
            {
                if (hex.Length == 6)
                {
                    color = new Color(
                        Convert.ToInt32(hex.Substring(0, 2), 16) / 255f,
                        Convert.ToInt32(hex.Substring(2, 2), 16) / 255f,
                        Convert.ToInt32(hex.Substring(4, 2), 16) / 255f
                    );

                    return true;
                }

                if (hex.Length == 8)
                {
                    color = new Color(
                        Convert.ToInt32(hex.Substring(0, 2), 16) / 255f,
                        Convert.ToInt32(hex.Substring(2, 2), 16) / 255f,
                        Convert.ToInt32(hex.Substring(4, 2), 16) / 255f,
                        Convert.ToInt32(hex.Substring(6, 2), 16) / 255f
                    );

                    return true;
                }
            }
            catch (Exception ex)
            {
                LogManager.Error($"Exception parsing hex color '{hex}': {ex.Message}");
            }

            return false;
        }
    }
}