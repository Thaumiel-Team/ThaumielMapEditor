// -----------------------------------------------------------------------
// <copyright file="SpawnObject.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using CommandSystem;
using LabApi.Features.Wrappers;
using ThaumielMapEditor.API.Attributes;
using ThaumielMapEditor.API.Data;
using ThaumielMapEditor.API.Enums;
using ThaumielMapEditor.API.Helpers;
using ThaumielMapEditor.API.Interfaces;
using UnityEngine;

namespace ThaumielMapEditor.Commands.Admin
{
#pragma warning disable CS1591
    [DoNotParse]
    public class SpawnObject : SubCommand
    {
        public static readonly CachedLayerMask RayMask = new("Default", "Door", "CCTV");

        public override string Name => "spawnobject";

        public override string VisibleArgs => "<ObjectType> [X Y Z] [RX RY RZ] [SX SY SZ | S] [key=value ...]";

        public override int RequiredArgsCount => 1;

        public override string Description => "Spawns a single object without a schematic";

        public override string[] Aliases => ["spawnobj", "so", "obj"];

        public override string RequiredPermission => "tme.spawnobject";

        public override bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            if (arguments.Count < 1)
            {
                response = $"Wrong usage! Correct usage: tme {Name} {VisibleArgs}";
                return false;
            }

            if (!Enum.TryParse<ObjectType>(arguments.At(0), true, out ObjectType type))
            {
                response = $"Unknown object type '{arguments.At(0)}'. Valid types: {ValidTypes()}.";
                return false;
            }

            if (type is ObjectType.None or ObjectType.Schematic)
            {
                response = type == ObjectType.None ? $"Unknown object type '{arguments.At(0)}'. Valid types: {ValidTypes()}." : "Nested schematics cannot be spawned this way. Use 'tme spawn <Schematic name>' instead.";
                return false;
            }

            List<float> numbers = [];
            Dictionary<string, object> values = new(StringComparer.OrdinalIgnoreCase);
            bool serverSide = false;
            string? name = null;
            bool isStatic = false;
            byte movementSmoothing = 0;

            for (int i = 1; i < arguments.Count; i++)
            {
                string arg = arguments.At(i);
                int eq = arg.IndexOf('=');
                if (eq > 0)
                {
                    string key = arg.Substring(0, eq).Trim();
                    string rawValue = arg.Substring(eq + 1).Trim().Trim('"', '\'');
                    if (string.IsNullOrEmpty(key))
                    {
                        response = $"Invalid argument '{arg}'. Expected key=value.";
                        return false;
                    }

                    switch (key.ToLowerInvariant())
                    {
                        case "name":
                            name = rawValue;
                            break;

                        case "serverside":
                        case "server":
                        case "serversideobject":
                            if (!TryParseBool(rawValue, out bool ss))
                            {
                                response = $"Failed to parse '{key}' as true/false: {rawValue}";
                                return false;
                            }

                            serverSide = ss;
                            break;

                        case "static":
                        case "isstatic":
                            if (!TryParseBool(rawValue, out bool st))
                            {
                                response = $"Failed to parse '{key}' as true/false: {rawValue}";
                                return false;
                            }

                            isStatic = st;
                            break;

                        case "smoothing":
                        case "movementsmoothing":
                        case "syncinterval":
                            if (!byte.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out byte smoothing))
                            {
                                response = $"Failed to parse '{key}' as a byte (0-255): {rawValue}";
                                return false;
                            }

                            movementSmoothing = smoothing;
                            break;

                        default:
                            values[key] = ParseValue(rawValue);
                            break;
                    }

                    continue;
                }

                if (!float.TryParse(arg, NumberStyles.Float, CultureInfo.InvariantCulture, out float num) || float.IsNaN(num) || float.IsInfinity(num))
                {
                    response = $"Failed to parse number '{arg}'. Expected a float or key=value.";
                    return false;
                }

                numbers.Add(num);
            }

            if (numbers.Count is not (0 or 3 or 6 or 7 or 9))
            {
                response = $"Wrong usage! Correct usage: tme {Name} {VisibleArgs}";
                return false;
            }

            Vector3 position;
            if (numbers.Count == 0)
            {
                if (!Player.TryGet(sender, out var player) || player.Camera == null)
                {
                    response = "Failed to get player camera. Provide X Y Z explicitly or look at a placement position.";
                    return false;
                }

                if (!Physics.Raycast(player.Camera.position, player.Camera.forward, out var hit, 50, RayMask))
                {
                    response = "Failed to get placement position from raycast.";
                    return false;
                }

                position = hit.point;
            }
            else
            {
                position = new(numbers[0], numbers[1], numbers[2]);
            }

            Quaternion rotation = Quaternion.identity;
            Vector3 rotationEuler = Vector3.zero;
            if (numbers.Count >= 6)
            {
                rotationEuler = new(numbers[3], numbers[4], numbers[5]);
                rotation = Quaternion.Euler(rotationEuler);
            }

            Vector3? scale = null;
            bool scaleProvided = false;
            if (numbers.Count == 7)
            {
                float uniform = numbers[6];
                if (uniform == 0f)
                {
                    response = "Failed to parse scale. Scale components must be non-zero.";
                    return false;
                }

                scale = new(uniform, uniform, uniform);
                scaleProvided = true;
            }
            else if (numbers.Count == 9)
            {
                Vector3 parsed = new(numbers[6], numbers[7], numbers[8]);
                if (parsed.x == 0f || parsed.y == 0f || parsed.z == 0f)
                {
                    response = "Failed to parse scale. Scale components must be non-zero.";
                    return false;
                }

                scale = parsed;
                scaleProvided = true;
            }

            if (scaleProvided && !Loader.SupportsScale(type))
            {
                response = $"Object type '{type}' does not support scale. Spawn without scale values.";
                return false;
            }

            SchematicData? data = Loader.SpawnSingleObject(type, position, rotation, scale, values, serverSide, name, isStatic, movementSmoothing);
            if (data == null)
            {
                response = $"Failed to spawn single object '{type}'. Check your values (e.g. DoorType, LockerType, ItemToSpawn) and server console for details.";
                return false;
            }

            StringBuilder sb = new();
            sb.AppendLine();
            sb.AppendLine($"Spawning single object '{type}'...");
            sb.AppendLine($"- Schematic Id: {data.Id} (use 'tme destroy {data.Id}' to remove)");
            sb.AppendLine($"- Object: {name ?? $"Single_{type}"}");
            sb.AppendLine($"- Position: {position}");
            sb.AppendLine($"- Rotation (euler): {rotation.eulerAngles}");
            sb.AppendLine(Loader.SupportsScale(type) ? $"- Scale: {scale ?? Vector3.one}" : "- Scale: n/a (not supported by this type)");
            sb.AppendLine($"- Side: {(serverSide && Loader.IsDualType(type) ? "Server" : Loader.IsDualType(type) ? "Client" : "Server")}");
            response = sb.ToString();
            return true;
        }

        private static string ValidTypes()
            => string.Join(", ", Enum.GetValues(typeof(ObjectType)).Cast<ObjectType>().Where(t => t is not ObjectType.None and not ObjectType.Schematic));

        private static bool TryParseBool(string raw, out bool result)
        {
            if (bool.TryParse(raw, out result))
                return true;

            if (raw == "1")
            {
                result = true;
                return true;
            }

            if (raw == "0")
            {
                result = false;
                return true;
            }

            return false;
        }

        private static object ParseValue(string raw)
        {
            if (bool.TryParse(raw, out bool b))
                return b;

            if (int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out int i))
                return i;

            if (float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out float f) && !float.IsNaN(f) && !float.IsInfinity(f))
                return f;

            if (Guid.TryParse(raw, out Guid guid))
                return guid;

            return raw;
        }
    }
}
