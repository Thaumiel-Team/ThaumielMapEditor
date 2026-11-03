// -----------------------------------------------------------------------
// <copyright file="UnknownBlock.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU Lesser General Public License v3.0 (LGPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;

namespace ThaumielMapEditor.API.Helpers.BlockParser
{
    public class UnknownBlock : BlockBase
    {
        public Dictionary<string, object> Raw { get; set; } = [];

        public override void Execute()
        {
            string type = Raw.TryGetValue("type", out object? t) ? t?.ToString() ?? "unknown" : "unknown";
            LogManager.Warn($"Encountered unknown block type '{type}'. Skipping execution.");
        }
    }
}