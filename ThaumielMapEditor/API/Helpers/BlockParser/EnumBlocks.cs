// -----------------------------------------------------------------------
// <copyright file="EnumBlocks.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using System;

namespace ThaumielMapEditor.API.Helpers.BlockParser
{
    public class EnumBlock : BlockBase
    {
        public object? Value { get; set; }

        public override object ReturnExecute()
        {
            if (Value is not Enum enum1)
                return Value?.ToString() ?? string.Empty;

            return enum1.ToString();
        }
    }

    public class EnumCombineBlock : BlockBase
    {
        public object? InputA { get; set; }
        public object? InputB { get; set; }

        public override object ReturnExecute()
        {
            object valA = ResolveValue(InputA!)!;
            object valB = ResolveValue(InputB!)!;

            if (valA is Enum a && valB is Enum b)
                return Enum.ToObject(a.GetType(), Convert.ToInt64(a) | Convert.ToInt64(b));

            return valA ?? valB;
        }
    }
}