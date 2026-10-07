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
using System.Threading.Tasks;
using UnityEngine;
using AdminToys;
using ThaumielMapEditor.API.Helpers;
using static AdminToys.InvisibleInteractableToy;
using ThaumielMapEditor.API.Extensions;

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
                                string? targetId = target.GetType().GetField("Id")?.GetValue(target) as string;
                                if (targetId != null && Guid.TryParse(targetId, out Guid targetid))
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