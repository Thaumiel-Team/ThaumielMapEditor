// -----------------------------------------------------------------------
// <copyright file="BlockyPayload.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;

namespace ThaumielMapEditor.API.Serialization
{
    [GitBookPage("Serialization/BlockyPayload")]
    public class BlockyPayload
    {
        public string Code { get; set; } = string.Empty;

        public string Xml { get; set; } = string.Empty;

        public List<object> Blocks { get; set; } = [];

        public string Timestamp { get; set; } = string.Empty;
    }
}