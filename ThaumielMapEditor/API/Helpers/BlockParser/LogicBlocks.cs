// -----------------------------------------------------------------------
// <copyright file="LogicBlocks.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU Lesser General Public License v3.0 (LGPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using LabApi.Features.Wrappers;

namespace ThaumielMapEditor.API.Helpers.BlockParser
{
    public class LogicCompareBlock : BlockBase
    {
        public string OP { get; set; } = "EQ";
        public object? A { get; set; }
        public object? B { get; set; }

        public override object? ReturnExecute()
        {
            object? valA = ResolveValue(A);
            object? valB = ResolveValue(B);

            if (TryParseFloat(valA, out float fA) && TryParseFloat(valB, out float fB))
            {
                return OP switch
                {
                    "EQ" => fA == fB,
                    "NEQ" => fA != fB,
                    "LT" => fA < fB,
                    "LTE" => fA <= fB,
                    "GT" => fA > fB,
                    "GTE" => fA >= fB,
                    _ => false
                };
            }

            return OP == "EQ" ? Equals(valA, valB) : !Equals(valA, valB);
        }

        private static bool TryParseFloat(object? value, out float result)
        {
            switch (value)
            {
                case float f:
                    result = f;
                    return true;

                case double d:
                    result = (float)d;
                    return true;

                case int i:
                    result = i;
                    return true;

                case long l:
                    result = l;
                    return true;

                case decimal m:
                    result = (float)m;
                    return true;

                default:
                    return float.TryParse(value?.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out result);
            }
        }
    }

    public class LogicOperationBlock : BlockBase
    {
        public string OP { get; set; } = "AND";
        public object? A { get; set; }
        public object? B { get; set; }

        public override object? ReturnExecute()
        {
            bool valA = ResolveBool(A);
            bool valB = ResolveBool(B);

            return OP == "AND" ? valA && valB : valA || valB;
        }
    }

    public class LogicNegateBlock : BlockBase
    {
        public object? Bool { get; set; }

        public override object? ReturnExecute() => !ResolveBool(Bool);
    }

    public class ControlsIfBlock : BlockBase
    {
        public BlockBase? Condition { get; set; }
        public List<object?> IfStack { get; set; } = [];
        public List<object?> ElseStack { get; set; } = [];

        public override void Execute(Player player)
        {
            if (Condition?.ReturnExecute() is bool b && b)
            {
                Executor?.ExecuteStack(IfStack, player);
            }
            else if (ElseStack.Count > 0)
            {
                Executor?.ExecuteStack(ElseStack, player);
            }
        }
    }
}