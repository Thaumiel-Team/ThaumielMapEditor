// -----------------------------------------------------------------------
// <copyright file="MECBlocks.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using LabApi.Features.Wrappers;
using MEC;

namespace ThaumielMapEditor.API.Helpers.BlockParser
{
    public class WaitUntilBlock : BlockBase
    {
        public BlockBase? Condition { get; set; }
        public List<object?> Stack { get; set; } = [];

        public override void Execute(Player player)
        {
            if (Executor == null)
            {
                LogManager.Warn("Executor is null, cannot execute.");
                return;
            }

            if (Condition == null)
            {
                LogManager.Warn("No condition block provided, skipping.");
                return;
            }

            LogManager.Debug("Starting coroutine.");
            Timing.RunCoroutine(WaitCoroutine(player));
        }

        private IEnumerator<float> WaitCoroutine(Player player)
        {
            LogManager.Debug("Waiting for condition to become true.");
            yield return Timing.WaitUntilTrue(EvaluateCondition);

            LogManager.Debug("Condition met, executing body.");
            Executor!.ExecuteStack(Stack!, player);
        }

        private bool EvaluateCondition()
        {
            if (Condition == null)
                return false;

            object? result = Condition.ReturnExecute();
            if (result is bool b)
                return b;

            LogManager.Warn($"Condition returned non bool type '{result?.GetType().Name ?? "null"}', defaulting to false.");
            return false;
        }
    }

    public class WaitForSeconds : BlockBase
    {
        public float WaitTime { get; set; }

        public List<object?> Stack { get; set; } = [];

        public override void Execute(Player player)
        {
            if (Executor == null)
            {
                LogManager.Warn($"Executor is null, cannot execute.");
                return;
            }

            LogManager.Debug($"Executing MEC wait for {WaitTime} seconds.");
            Timing.CallDelayed(Timing.WaitForSeconds(WaitTime), () => Executor!.ExecuteStack(Stack!, player));
        }
    }

    public class WaitForFrames : BlockBase
    {
        public uint WaitTime { get; set; }

        public List<object?> Stack { get; set; } = [];

        public override void Execute(Player player)
        {
            if (Executor == null)
            {
                LogManager.Warn($"Executor is null, cannot execute.");
                return;
            }

            LogManager.Debug($"Executing MEC wait for {WaitTime} frames.");
            MECHelper.WaitForFrames(WaitTime, () => Executor!.ExecuteStack(Stack!, player));
        }
    }
}