// -----------------------------------------------------------------------
// <copyright file="JsonElementExtensions.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace ThaumielMapEditor.API.Conversion
{
    public static class JsonElementExtensions
    {
        public static object? ToObject(this JsonElement element)
        {
            return element.ValueKind switch
            {
                JsonValueKind.String => element.GetString(),
                JsonValueKind.Number => element.TryGetInt32(out int i) ? i : element.GetDouble(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Object => element.EnumerateObject()
                    .ToDictionary(property => property.Name, property => property.Value.ToObject()),
                JsonValueKind.Array => element.EnumerateArray()
                    .Select(item => item.ToObject())
                    .ToList(),
                _ => null
            };
        }
    }
}