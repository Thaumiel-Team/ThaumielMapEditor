// -----------------------------------------------------------------------
// <copyright file="LogicBlocks.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using LabApi.Features.Wrappers;

namespace ThaumielMapEditor.API.Helpers.BlockParser
{
    public class LogicCompareBlock : BlockBase
    {
        public string OP { get; set; } = "EQ";
        public object? A { get; set; }
        public object? B { get; set; }

        public override object ReturnExecute()
        {
            object? valA = A is BlockBase bA ? bA.ReturnExecute() : A;
            object? valB = B is BlockBase bB ? bB.ReturnExecute() : B;

            if (float.TryParse(valA?.ToString(), out float fA) && float.TryParse(valB?.ToString(), out float fB))
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
    }

    public class LogicOperationBlock : BlockBase
    {
        public string OP { get; set; } = "AND";
        public object? A { get; set; }
        public object? B { get; set; }

        public override object ReturnExecute()
        {
            bool valA = A is BlockBase bA ? (bool)bA.ReturnExecute() : (A is bool b && b);
            bool valB = B is BlockBase bB ? (bool)bB.ReturnExecute() : (B is bool b2 && b2);

            return OP == "AND" ? valA && valB : valA || valB;
        }
    }

    public class LogicNegateBlock : BlockBase
    {
        public object? Bool { get; set; }

        public override object ReturnExecute()
        {
            bool val = Bool is BlockBase b ? (bool)b.ReturnExecute() : (Bool is bool boolean && boolean);
            return !val;
        }
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
                Executor?.ExecuteStack(IfStack!, player);
            }
            else if (ElseStack.Count > 0)
            {
                Executor?.ExecuteStack(ElseStack!, player);
            }
        }
    }
}