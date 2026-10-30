// -----------------------------------------------------------------------
// <copyright file="TextToyBlocks.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using ThaumielMapEditor.API.Blocks;
using ThaumielMapEditor.API.Blocks.Definitions;
using ThaumielMapEditor.API.Data;
using ThaumielMapEditor.API.Enums;
using UnityEngine;

namespace ThaumielMapEditor.API.Helpers.BlockParser
{
    public class TextToyCreateBlock : BlockBase
    {
        public string Name { get; set; } = string.Empty;

        public override object? ReturnExecute() => ReturnExecute(Executor!.Schematic);

        public override object? ReturnExecute(SchematicData schematic)
        {
            TextToyDefinition.Server server = new(new ObjectBase { ObjectType = ObjectType.TextToy }, new TextToyDefinition());
            server.SpawnObject(schematic);
            LogManager.Debug($"Created TextToy '{Name}'.");
            return server;
        }
    }

    public class TextToySetTextBlock : BlockBase
    {
        public string Text { get; set; } = string.Empty;

        public override void Execute(object obj)
        {
            if (obj is not TextToyDefinition.Server server)
            {
                LogManager.Warn("obj is not a TextToyDefinition.Server.");
                return;
            }

            server.Text = Text;
        }
    }

    public class TextToySetDisplaySizeBlock : BlockBase
    {
        public float X { get; set; } = 1f;
        public float Y { get; set; } = 1f;

        public override void Execute(object obj)
        {
            if (obj is not TextToyDefinition.Server server)
            {
                LogManager.Warn("obj is not a TextToyDefinition.Server.");
                return;
            }

            server.DisplaySize = new Vector2(X, Y);
        }
    }

    public class TextToyGetPropertyBlock : BlockBase
    {
        public string Property { get; set; } = string.Empty;

        public override object? ReturnExecute()
            => GetProperty(ResolveTarget());

        public override object? ReturnExecute(object obj)
            => GetProperty(ResolveTarget(obj));

        private object GetProperty(object? obj)
        {
            if (obj is not TextToyDefinition.Server server)
            {
                LogManager.Warn("obj is not a TextToyDefinition.Server.");
                return null!;
            }

            return Property switch
            {
                "Position" => server.Position,
                "Rotation" => server.Rotation,
                "Scale" => server.Scale,
                "Text" => server.Text,
                "DisplaySize" => server.DisplaySize,
                "Base" => server.Base!,
                _ => LogUnknownProperty(Property)
            };
        }

        private static object LogUnknownProperty(string property)
        {
            LogManager.Warn($"Unknown property '{property}'.");
            return null!;
        }
    }
}