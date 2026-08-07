// -----------------------------------------------------------------------
// <copyright file="ObjectDictionaryConverter.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ThaumielMapEditor.API.Conversion
{
    public class ObjectDictionaryConverter : JsonConverter<Dictionary<string, object>>
    {
        /// <summary>
        /// Reads the Properties area of a PMER schematic and parses it into a <see cref="Dictionary{TKey, TValue}"/>
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="typeToConvert"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public override Dictionary<string, object> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            Dictionary<string, object> dict = [];

            if (reader.TokenType != JsonTokenType.StartObject)
                return dict;

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                    break;

                string key = reader.GetString() ?? string.Empty;

                reader.Read();
                using (JsonDocument document = JsonDocument.ParseValue(ref reader))
                {
                    dict[key] = document.RootElement.ToObject()!;
                }
            }

            return dict;
        }
        
        public override void Write(Utf8JsonWriter writer, Dictionary<string, object> value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();

            foreach (var kvp in value)
            {
                writer.WritePropertyName(kvp.Key);
                JsonSerializer.Serialize(writer, kvp.Value, options);
            }

            writer.WriteEndObject();
        }
    }
}