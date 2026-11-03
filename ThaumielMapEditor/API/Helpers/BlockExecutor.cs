// -----------------------------------------------------------------------
// <copyright file="BlockExecutor.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU Lesser General Public License v3.0 (LGPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using LabApi.Features.Wrappers;
using ThaumielMapEditor.API.Data;
using ThaumielMapEditor.API.Enums;
using ThaumielMapEditor.API.Helpers.BlockParser;

namespace ThaumielMapEditor.API.Helpers
{
    [GitBookPage("BlockExecutor")]
    public class BlockExecutor
    {
        public BlockExecutor(SchematicData schematic) => Schematic = schematic;

        public readonly SchematicData Schematic;

        internal Dictionary<string, object?> Globals { get; } = new(StringComparer.Ordinal);

        /// <summary>
        /// Runs <paramref name="program"/> against the schematic owned by this executor.
        /// </summary>
        /// <remarks>
        /// A parsed program is immutable, so the same instance may safely be executed by many schematics.
        /// </remarks>
        public void Execute(ScriptProgram program, Player? player, EventType? eventType = null, object? eventParameter = null)
        {
            if (program == null || program.Blocks.Count == 0)
                return;

            using BlockExecution execution = new BlockExecution(this, program, player).Enter();
            Run(program, player, eventType, eventParameter);
        }

        internal void ExecuteStack(List<object?> stack, Player? player)
        {
            BlockExecution? execution = BlockExecution.Current;

            if (execution == null)
                return;

            bool hasTarget = execution.HasTarget;
            object? target = execution.Target;
            execution.HasTarget = false;
            execution.Target = null;

            try
            {
                foreach (object? block in stack)
                {
                    if (block != null)
                        ExecuteBlock(block, player);
                }
            }
            finally
            {
                execution.HasTarget = hasTarget;
                execution.Target = target;
            }
        }

        private void Run(ScriptProgram program, Player? player, EventType? eventType, object? eventParameter)
        {
            if (eventType is { } type)
            {
                foreach (EventBlock handler in program.GetHandlers(type))
                {
                    handler.Execute(player!, eventParameter ?? player);
                }

                return;
            }

            foreach (object block in program.Blocks)
            {
                if (block is MethodBlock)
                    continue;

                ExecuteBlock(block, player);
            }
        }

        private void ExecuteBlock(object block, Player? player)
        {
            switch (block)
            {
                case ProcedureCallNoReturnBlock call:
                    ExecuteProcedure(call.Name, null, player);
                    break;

                case ProcedureCallReturnBlock call:
                    ExecuteProcedure(call.Name, call.Args, player);
                    break;

                case VariableBlock variable:
                    SetVariable(variable.Name, BlockBase.ResolveValue(variable.Value));
                    break;

                case GetVariableBlock getVariable:
                    if (!TryGetVariable(getVariable.Name, out _))
                        LogManager.Warn($"Variable '{getVariable.Name}' not found.");

                    break;

                case PrimitiveCreateBlock create:
                    ExecutePrimitiveCreate(create);
                    break;

                case TextToyCreateBlock textToy:
                    ExecuteObjectCreate(textToy, textToy.Name, "TextToy");
                    break;

                case WaypointCreateBlock waypoint:
                    ExecuteObjectCreate(waypoint, waypoint.Name, "Waypoint");
                    break;

                case SpeakerCreateBlock speaker:
                    ExecuteObjectCreate(speaker, speaker.Name, "Speaker");
                    break;

                case CreateDoorBlock door:
                    ExecuteDoorCreate(door);
                    break;

                case BlockBase blockBase:
                    Dispatch(blockBase, player);
                    break;

                default:
                    LogManager.Warn($"Unknown block type: {block.GetType().Name}");
                    break;
            }
        }

        private void Dispatch(BlockBase block, Player? player)
        {
            BlockExecution execution = BlockExecution.Require();

            object? target = block.Target is null ? null : block.ResolveTargetValue();

            bool previousHasTarget = execution.HasTarget;
            object? previousTarget = execution.Target;

            if (block.Target != null)
            {
                execution.Target = target;
                execution.HasTarget = true;
            }

            try
            {
                block.Execute();

                if (target is Player targetPlayer)
                {
                    block.Execute(targetPlayer);
                }
                else if (target != null)
                {
                    block.Execute(target);
                }
                else
                {
                    block.Execute(player!);

                    if (execution.PendingObject is { } created)
                        block.Execute(created);
                }
            }
            finally
            {
                execution.Target = previousTarget;
                execution.HasTarget = previousHasTarget;
            }
        }

        private void ExecuteProcedure(string name, Dictionary<string, object?>? args, Player? player)
        {
            BlockExecution execution = BlockExecution.Require();

            if (!execution.Program.Procedures.TryGetValue(name, out MethodBlock? method))
            {
                LogManager.Warn($"Procedure '{name}' not found.");
                return;
            }

            PushScope();

            try
            {
                if (args != null)
                {
                    foreach (KeyValuePair<string, object?> arg in args)
                    {
                        SetVariable(arg.Key, BlockBase.ResolveValue(arg.Value));
                    }
                }

                LogManager.Debug($"Calling procedure '{name}'.");

                foreach (object? block in method.Stack)
                {
                    if (block != null)
                        ExecuteBlock(block, player);
                }
            }
            finally
            {
                PopScope();
            }
        }

        private void ExecutePrimitiveCreate(PrimitiveCreateBlock block)
        {
            BlockExecution execution = BlockExecution.Require();
            execution.PendingObject = null;

            LogManager.Debug($"Creating {(block.IsServerSide ? "server" : "client")} side primitive '{block.Name}'.");
            execution.PendingObject = block.ReturnExecute(Schematic);
        }

        private void ExecuteObjectCreate(BlockBase block, string name, string typeName)
        {
            BlockExecution execution = BlockExecution.Require();

            LogManager.Debug($"Creating {typeName} '{name}'.");
            execution.PendingObject = block.ReturnExecute(Schematic);
        }

        private void ExecuteDoorCreate(CreateDoorBlock block)
        {
            BlockExecution execution = BlockExecution.Require();

            LogManager.Debug("Creating Door.");
            execution.PendingObject = block.ReturnExecute();
        }

        internal void PushScope() =>
            BlockExecution.Current?.Scopes.Push([]);

        internal void PopScope()
        {
            Stack<Dictionary<string, object?>>? scopes = BlockExecution.Current?.Scopes;

            if (scopes is { Count: > 0 })
                scopes.Pop();
        }

        internal void SetVariable(string name, object? value)
        {
            BlockExecution? execution = BlockExecution.Current;

            if (execution is { Scopes.Count: > 0 })
            {
                execution.Scopes.Peek()[name] = value;
            }
            else
                Globals[name] = value;
        }

        /// <summary>
        /// Looks a variable up in the running script scopes first, then in the variables persisted by this executor.
        /// </summary>
        public bool TryGetVariable(string name, out object? value)
        {
            BlockExecution? execution = BlockExecution.Current;

            if (execution != null)
            {
                foreach (Dictionary<string, object?> scope in execution.Scopes)
                {
                    if (scope.TryGetValue(name, out value))
                        return true;
                }
            }

            return Globals.TryGetValue(name, out value);
        }
    }
}