// -----------------------------------------------------------------------
// <copyright file="EventBlock.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using LabApi.Features.Wrappers;
using ThaumielMapEditor.API.Enums;

namespace ThaumielMapEditor.API.Helpers.BlockParser
{
    public class EventBlock : BlockBase
    {
        public EventType EventType { get; set; }

        public string ParamName { get; set; } = string.Empty;

        public List<object?> Stack { get; set; } = [];

        public override void Execute(Player player) =>
            Execute(player, player);

        public void Execute(Player player, object? parameter)
        {
            if (Executor == null)
            {
                LogManager.Warn($"{EventType}: Executor is null, cannot execute.");
                return;
            }

            LogManager.Debug($"Executing event '{EventType}' with param '{ParamName}'.");

            Executor.PushScope();
            Executor.SetVariable(ParamName, parameter);
            Executor.ExecuteStack(Stack!, player);
            Executor.PopScope();
        }
    }
}