// -----------------------------------------------------------------------
// <copyright file="PMERMapVectorConverter.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Globalization;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace ThaumielMapEditor.API.Conversion
{
    public class PMERMapVectorConverter : IYamlTypeConverter
    {
        public bool Accepts(Type type) => type == typeof(Vector3);

        public object ReadYaml(IParser parser, Type type)
        {
            if (parser.TryConsume<Scalar>(out Scalar? scalar) && scalar != null)
                return ParseScalar(scalar.Value);

            parser.Consume<MappingStart>();
            float x = 0f, y = 0f, z = 0f;
            while (!parser.TryConsume<MappingEnd>(out _))
            {
                string key = parser.Consume<Scalar>().Value;
                string value = parser.Consume<Scalar>().Value;

                if (!float.TryParse(value.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out float num))
                    throw new ArgumentException($"Unable to parse PMER vector component '{key}: {value}'.");

                if (key.Equals("x", StringComparison.OrdinalIgnoreCase))
                {
                    x = num;
                }
                else if (key.Equals("y", StringComparison.OrdinalIgnoreCase))
                {
                    y = num;
                }
                else if (key.Equals("z", StringComparison.OrdinalIgnoreCase))
                {
                    z = num;
                }
            }

            return new Vector3(x, y, z);
        }

        public void WriteYaml(IEmitter emitter, object? value, Type type)
        {
            Vector3 vector = value is Vector3 v ? v : Vector3.zero;
            emitter.Emit(new Scalar(vector.ToString("F3", CultureInfo.InvariantCulture)));
        }

        private static Vector3 ParseScalar(string? s)
        {
            s = (s ?? string.Empty).Trim().Trim('(', ')').Trim();
            string[] split = s.Split(',');
            if (split.Length != 3)
                throw new ArgumentException($"Unable to parse PMER vector '{s}'. Expected format '(x, y, z)'.");

            if (!float.TryParse(split[0].Trim().Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out float x) || !float.TryParse(split[1].Trim().Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out float y) || !float.TryParse(split[2].Trim().Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out float z))
                throw new ArgumentException($"Unable to parse PMER vector '{s}'. Expected format '(x, y, z)'.");

            return new Vector3(x, y, z);
        }
    }
}
