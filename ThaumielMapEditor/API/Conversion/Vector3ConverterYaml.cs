// -----------------------------------------------------------------------
// <copyright file="Vector3ConverterYaml.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using LabApi.Loader.Features.Yaml.Extensions;
using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.Pool;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace ThaumielMapEditor.API.Conversion
{
    internal class Vector3ConverterYaml : IYamlTypeConverter
    {
        public object? ReadYaml(IParser parser, Type type)
        {
            parser.Consume<MappingStart>();
            Dictionary<string, float> dictionary = CollectionPool<Dictionary<string, float>, KeyValuePair<string, float>>.Get();
            int num = 0;
            try
            {
                while (!parser.TryConsume<MappingEnd>(out _))
                {
                    if (!parser.TryReadMapping(out string key, out string value))
                        throw new ArgumentException($"Unable to parse Vector, no component at index {num} provided");

                    bool flag = key switch
                    {
                        "x" or "y" or "z" or "w" => true,
                        _ => false,
                    };

                    if (!flag)
                        throw new ArgumentException($"Unable to parse Vector, invalid component name {key}. Only 'x' 'y' 'z' and 'w' are allowed");

                    if (dictionary.ContainsKey(key))
                        throw new ArgumentException($"Unable to parse Vector, duplicate component {key}");

                    dictionary[key] = float.Parse(value.Replace(',', '.'), CultureInfo.InvariantCulture);
                    num++;
                }

                object obj = dictionary.Count switch
                {
                    2 => new Vector2(dictionary["x"], dictionary["y"]),
                    3 => new Vector3(dictionary["x"], dictionary["y"], dictionary["z"]),
                    4 => new Vector4(dictionary["x"], dictionary["y"], dictionary["z"], dictionary["w"]),
                    _ => throw new ArgumentException($"Unable to deserialize vector with {dictionary.Count} components"),
                };

                Type type2 = obj.GetType();
                if (type2 != type)
                    throw new ArgumentException($"Attempting to deserialize {type2.Name} for config type of {type.Name}");

                return obj;
            }
            finally
            {
                CollectionPool<Dictionary<string, float>, KeyValuePair<string, float>>.Release(dictionary);
            }
        }

        public void WriteYaml(IEmitter emitter, object? value, Type _)
        {
            emitter.Emit(new MappingStart(AnchorName.Empty, TagName.Empty, isImplicit: true, MappingStyle.Block));
            if (value is not Vector2 vector)
            {
                if (value is not Vector3 vector2)
                {
                    if (value is Vector4 vector3)
                    {
                        emitter.EmitMapping("x", vector3.x.ToString(CultureInfo.InvariantCulture));
                        emitter.EmitMapping("y", vector3.y.ToString(CultureInfo.InvariantCulture));
                        emitter.EmitMapping("z", vector3.z.ToString(CultureInfo.InvariantCulture));
                        emitter.EmitMapping("w", vector3.w.ToString(CultureInfo.InvariantCulture));
                    }
                }
                else
                {
                    emitter.EmitMapping("x", vector2.x.ToString(CultureInfo.InvariantCulture));
                    emitter.EmitMapping("y", vector2.y.ToString(CultureInfo.InvariantCulture));
                    emitter.EmitMapping("z", vector2.z.ToString(CultureInfo.InvariantCulture));
                }
            }
            else
            {
                emitter.EmitMapping("x", vector.x.ToString(CultureInfo.InvariantCulture));
                emitter.EmitMapping("y", vector.y.ToString(CultureInfo.InvariantCulture));
            }

            emitter.Emit(new MappingEnd());
        }

        public bool Accepts(Type type)
        {
            if (type != typeof(Vector2) && type != typeof(Vector3))
                return type == typeof(Vector4);

            return true;
        }
    }
}
