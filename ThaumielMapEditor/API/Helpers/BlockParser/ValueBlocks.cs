// -----------------------------------------------------------------------
// <copyright file="ValueBlocks.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

namespace ThaumielMapEditor.API.Helpers.BlockParser
{
    /// <summary>
    /// Wraps the Blockly <c>math_number</c> block.
    /// </summary>
    public class MathNumberBlock : BlockBase
    {
        public float Num { get; set; }

        public override object? ReturnExecute() => Num;

        public override object? ReturnExecute(object obj) => Num;
    }

    /// <summary>
    /// Wraps the Blockly <c>text</c> block.
    /// </summary>
    public class TextBlock : BlockBase
    {
        public string Text { get; set; } = string.Empty;

        public override object? ReturnExecute() => Text;

        public override object? ReturnExecute(object obj) => Text;
    }

    /// <summary>
    /// Wraps the Blockly <c>logic_boolean</c> block.
    /// </summary>
    public class LogicBooleanBlock : BlockBase
    {
        public bool Value { get; set; }

        public override object? ReturnExecute() => Value;

        public override object? ReturnExecute(object obj) => Value;
    }
}