// -----------------------------------------------------------------------
// <copyright file="Vector3Converter.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU Lesser General Public License v3.0 (LGPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ThaumielMapEditor.API.Conversion
{
    public class Vector3Converter : JsonConverter<Vector3>
    {
        public override Vector3 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
                throw new JsonException("Expected StartObject token for Vector3.");

            float x = 0, y = 0, z = 0;

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                    return new Vector3(x, y, z);

                if (reader.TokenType != JsonTokenType.PropertyName)
                    throw new JsonException("Expected PropertyName token.");

                string propName = reader.GetString() ?? string.Empty;
                reader.Read();

                switch (propName.ToLowerInvariant())
                {
                    // Log report: a0047a96ad745014
                    case "x":
                        x = ReadFloat(ref reader);
                        break;

                    case "y":
                        y = ReadFloat(ref reader);
                        break;

                    case "z":
                        z = ReadFloat(ref reader);
                        break;
                }
            }

            throw new JsonException("Unexpected end when reading Vector3.");
        }

        private static float ReadFloat(ref Utf8JsonReader reader)
        {
            if (reader.TokenType == JsonTokenType.Number)
                return reader.GetSingle();

            if (reader.TokenType == JsonTokenType.String)
            {
                string value = reader.GetString()?.Replace(',', '.') ?? string.Empty;
                return float.Parse(value, CultureInfo.InvariantCulture);
            }

            throw new JsonException($"Cannot read float from token type '{reader.TokenType}'.");
        }

        public override void Write(Utf8JsonWriter writer, Vector3 value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteNumber("x", value.x);
            writer.WriteNumber("y", value.y);
            writer.WriteNumber("z", value.z);
            writer.WriteEndObject();
        }
    }
}