// -----------------------------------------------------------------------
// <copyright file="BlockyRuntime.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using ThaumielMapEditor.API.Blocks;
using ThaumielMapEditor.API.Data;
using ThaumielMapEditor.API.Enums;
using ThaumielMapEditor.API.Extensions;
using ThaumielMapEditor.API.Helpers;
using ThaumielMapEditor.API.Serialization;

namespace ThaumielMapEditor.API.Components.Tools
{
    public class BlockyRuntime : ToolBase
    {
        public override ToolType Type => ToolType.BlockyRuntime;

        public BlockyPayload? Blocky;

        public override void Init(ServerObject obj, SchematicData schem, Dictionary<string, object> properties)
        {
            base.Init(obj, schem, properties);

            if (properties.TryConvertValue<BlockyPayload>("Payload", out var payload))
                Blocky = payload;
        }

        protected override void OnDestroy()
        {
            Schematic?.Executor?.Execute(ArgumentsParser.Load(Blocky!), null!, EventType.OnDestroyed);
            base.OnDestroy();
        }
    }
}