// -----------------------------------------------------------------------
// <copyright file="LoopBlocks.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using System.Collections;
using System.Collections.Generic;
using LabApi.Features.Wrappers;

namespace ThaumielMapEditor.API.Helpers.BlockParser
{
    public class RepeatBlock : BlockBase
    {
        public object? Times { get; set; }
        public List<object?> Stack { get; set; } = [];

        public override void Execute(Player player)
        {
            int count = (int)ResolveFloat(Times);
            for (int i = 0; i < count; i++)
            {
                Executor?.ExecuteStack(Stack!, player);
            }
        }
    }

    public class WhileUntilBlock : BlockBase
    {
        public string Mode { get; set; } = "WHILE";
        public BlockBase? Condition { get; set; }
        public List<object?> Stack { get; set; } = [];

        public override void Execute(Player player)
        {
            while (ShouldContinue())
            {
                Executor?.ExecuteStack(Stack!, player);
            }
        }

        private bool ShouldContinue()
        {
            object? result = Condition?.ReturnExecute();
            bool val = result is bool b && b;
            return Mode == "WHILE" ? val : !val;
        }
    }

    public class ForLoopBlock : BlockBase
    {
        public string VarName { get; set; } = "i";
        public object? From { get; set; }
        public object? To { get; set; }
        public object? By { get; set; }
        public List<object?> Stack { get; set; } = [];

        public override void Execute(Player player)
        {
            if (Executor == null)
                return;

            float start = ResolveFloat(From);
            float end = ResolveFloat(To);
            float step = ResolveFloat(By);

            if (step == 0)
            {
                LogManager.Warn($"For loop step cannot be zero, skipping loop.");
                return;
            }

            if ((step > 0 && start > end) || (step < 0 && start < end))
            {
                LogManager.Warn($"For loop bounds are invalid for the given step, skipping loop.");
                return;
            }

            Executor.PushScope();

            for (float i = start; step > 0 ? i <= end : i >= end; i += step)
            {
                Executor.SetVariable(VarName, i);
                Executor.ExecuteStack(Stack!, player);
            }

            Executor.PopScope();
        }
    }

    public class ForeachBlock : BlockBase
    {
        public string VarName { get; set; } = "item";
        public object? ListInput { get; set; }
        public List<object?> Stack { get; set; } = [];

        public override void Execute(Player player)
        {
            if (Executor == null)
                return;

            object? collection = ListInput is BlockBase block ? block.ReturnExecute() : ListInput;

            if (collection is not IEnumerable enumerable)
            {
                LogManager.Warn($"Input is not an enumerable collection.");
                return;
            }

            foreach (object? item in enumerable)
            {
                Executor.PushScope();
                Executor.SetVariable(VarName, item);
                Executor.ExecuteStack(Stack!, player);
                Executor.PopScope();
            }
        }
    }
}