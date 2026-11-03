// -----------------------------------------------------------------------
// <copyright file="SerializableTool.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU Lesser General Public License v3.0 (LGPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;

namespace ThaumielMapEditor.API.Serialization
{
    [GitBookPage("Serialization/SerializableTool")]
    public class SerializableTool
    {
        public string ToolName { get; set; } = string.Empty;

        public Dictionary<string, object> Properties { get; set; } = [];
    }
}