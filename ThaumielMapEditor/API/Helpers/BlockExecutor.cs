// -----------------------------------------------------------------------
// <copyright file="BlockExecutor.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using LabApi.Features.Wrappers;
using ThaumielMapEditor.API.Blocks;
using ThaumielMapEditor.API.Blocks.ClientSide;
using ThaumielMapEditor.API.Blocks.ServerObjects;
using ThaumielMapEditor.API.Data;
using ThaumielMapEditor.API.Enums;
using ThaumielMapEditor.API.Helpers.BlockParser;

namespace ThaumielMapEditor.API.Helpers
{
    public class BlockExecutor
    {
        public BlockExecutor(SchematicData schematic)
        {
            Schematic = schematic;
            Scopes.Push([]);
        }

        public readonly SchematicData Schematic;
        public Dictionary<BlockBase, List<object>> Entries = [];
        public readonly Stack<Dictionary<string, object?>> Scopes = new();
        public Dictionary<string, MethodBlock> Procedures { get; } = [];

        private object? _currentServer;
        private object? _currentClient;
        private bool _isCurrentServerSide;

        public void Execute(List<object> blocks, Player player, EventType? eventType = null)
        {
            foreach (MethodBlock method in blocks.OfType<MethodBlock>())
            {
                Procedures[method.Name] = method;
            }

            if (eventType != null)
            {
                foreach (EventBlock? eventBlock in blocks.OfType<EventBlock>().Where(b => b.EventType == eventType))
                {
                    foreach (object? block in eventBlock.Stack)
                    {
                        if (block == null)
                            continue;

                        ExecuteBlock(block, player);
                    }
                }

                FlushCurrent();
                return;
            }
    
            foreach (object block in blocks)
            {
                if (block is MethodBlock)
                    continue;

                ExecuteBlock(block, player);
            }

            FlushCurrent();
        }

        internal void ExecuteStack(List<object> stack, Player player)
        {
            foreach (object? block in stack)
            {
                if (block == null)
                    continue;

                ExecuteBlock(block, player);
            }
        }

        private void ExecuteBlock(object block, Player player)
        {
            switch (block)
            {
                case ProcedureCallNoReturnBlock callBlock:
                    callBlock.Executor = this;
                    ExecuteProcedureCall(callBlock.Name, [], player);
                    break;

                case ProcedureCallReturnBlock callReturnBlock:
                    callReturnBlock.Executor = this;
                    object? result = ExecuteProcedureCallReturn(callReturnBlock.Name, callReturnBlock.Args, player);
                    UpdateEntries(callReturnBlock, result!);
                    break;

                case VariableBlock varBlock:
                    varBlock.Executor = this;
                    ExecuteSetVariable(varBlock);
                    break;

                case GetVariableBlock getVariableBlock:
                    getVariableBlock.SetExecutorRecursive(this);
                    GetVariable(getVariableBlock);
                    break;

                case PrimitiveCreateBlock createBlock:
                    createBlock.Executor = this;
                    ExecuteCreate(createBlock);
                    break;

                case TextToyCreateBlock textToyCreateBlock:
                    textToyCreateBlock.Executor = this;
                    ExecuteObjectCreate(textToyCreateBlock, textToyCreateBlock.Name, "TextToy");
                    break;

                case WaypointCreateBlock waypointCreateBlock:
                    waypointCreateBlock.Executor = this;
                    ExecuteObjectCreate(waypointCreateBlock, waypointCreateBlock.Name, "Waypoint");
                    break;

                case SpeakerCreateBlock speakerCreateBlock:
                    speakerCreateBlock.Executor = this;
                    ExecuteObjectCreate(speakerCreateBlock, speakerCreateBlock.Name, "Speaker");
                    break;

                case BlockBase blockbase:
                    blockbase.SetExecutorRecursive(this);
                    blockbase.Execute();
                    blockbase.Execute(player);
                    ExecuteParamBlocks(blockbase);
                    ExecuteReturns(blockbase, player);
                    break;

                default:
                    LogManager.Warn($"Unknown block type: {block.GetType().Name}");
                    break;
            }
        }

        private void ExecuteProcedureCall(string name, Dictionary<string, object?> args, Player player)
        {
            if (!Procedures.TryGetValue(name, out MethodBlock? method))
            {
                LogManager.Warn($"Procedure '{name}' not found.");
                return;
            }

            PushScope();

            foreach (KeyValuePair<string, object?> arg in args)
            {
                SetVariable(arg.Key, arg.Value is BlockBase b ? b.ReturnExecute() : arg.Value);
            }

            LogManager.Debug($"Calling procedure '{name}'.");
            foreach (object? block in method.Stack)
            {
                if (block != null)
                    ExecuteBlock(block, player);
            }

            PopScope();
        }

        private object? ExecuteProcedureCallReturn(string name, Dictionary<string, object?> args, Player player)
        {
            if (!Procedures.TryGetValue(name, out MethodBlock? method))
            {
                LogManager.Warn($"Procedure '{name}' not found.");
                return null;
            }

            PushScope();

            foreach (KeyValuePair<string, object?> arg in args)
            {
                SetVariable(arg.Key, arg.Value is BlockBase b ? b.ReturnExecute() : arg.Value);
            }

            LogManager.Debug($"Calling procedure (return) '{name}'.");
            foreach (object? block in method.Stack)
            {
                if (block != null)
                    ExecuteBlock(block, player);
            }

            object? returnVal = method.Return is BlockBase retBlock ? retBlock.ReturnExecute() : method.Return;

            PopScope();
            return returnVal;
        }

        private void ExecuteSetVariable(VariableBlock block)
        {
            object? value = block.Value;
            if (value is BlockBase b)
                value = b.ReturnExecute();

            SetVariable(block.Name, value);
            LogManager.Debug($"Set variable '{block.Name}' = {value}");
        }

        internal void PushScope()
        {
            Scopes.Push([]);
        }

        internal void PopScope()
        {
            if (Scopes.Count > 1)
                Scopes.Pop();
        }

        internal void SetVariable(string name, object? value)
        {
            Scopes.Peek()[name] = value;
        }

        internal object? GetVariable(GetVariableBlock block)
        {
            foreach (Dictionary<string, object?> scope in Scopes)
            {
                if (scope.TryGetValue(block.Name, out var val))
                {
                    block.Value = val;
                    return val;
                }
            }

            LogManager.Warn($"Variable '{block.Name}' not found.");
            return null;
        }

        private void ExecuteReturns(BlockBase block, object param)
        {
            object obj = block.ReturnExecute(param);
            UpdateEntries(block, obj);
        }

        private void ExecuteParamBlocks(BlockBase block)
        {
            if (_currentServer != null)
            {
                block.Execute(_currentServer!);
            }

            if (_currentClient != null)
            {
                block.Execute(_currentClient);
            }
        }

        private void ExecuteCreate(PrimitiveCreateBlock block)
        {
            FlushCurrent();

            _isCurrentServerSide = block.IsServerSide;
            if (_isCurrentServerSide)
            {
                LogManager.Debug($"Creating server side primitive '{block.Name}'.");
                _currentServer = (PrimitiveObjectServer)block.ReturnExecute(Schematic);
            }
            else
            {
                LogManager.Debug($"Creating client side primitive '{block.Name}'.");
                _currentClient = (PrimitiveObject)block.ReturnExecute(Schematic);
            }
        }

        private void ExecuteObjectCreate(BlockBase block, string name, string typeName)
        {
            FlushCurrent();

            LogManager.Debug($"Creating {typeName} '{name}'.");
            _isCurrentServerSide = true;
            _currentServer = (ServerObject)block.ReturnExecute(Schematic);
        }

        private void UpdateEntries(BlockBase block, object obj)
        {
            if (Entries.TryGetValue(block, out var list))
            {
                list.Add(obj);
                Entries[block] = list;
                return;
            }

            Entries.Add(block, [obj]);
        }

        private void FlushCurrent()
        {
            _currentServer = null;
            _currentClient = null;
        }
    }
}