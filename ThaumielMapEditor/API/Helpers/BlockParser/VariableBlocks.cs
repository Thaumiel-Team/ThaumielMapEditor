// -----------------------------------------------------------------------
// <copyright file="VariableBlocks.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU Lesser General Public License v3.0 (LGPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

namespace ThaumielMapEditor.API.Helpers.BlockParser
{
    public class VariableBlock : BlockBase
    {
        public string Name { get; set; } = string.Empty;
        public object? Value { get; set; }
    }

    public class GetVariableBlock : BlockBase
    {
        public string Name { get; set; } = string.Empty;

        public override object? ReturnExecute() => Resolve();

        public override object? ReturnExecute(object obj) => Resolve();

        private object? Resolve()
        {
            if (Executor != null && Executor.TryGetVariable(Name, out object? value))
                return value;

            LogManager.Warn($"Variable '{Name}' not found.");
            return null;
        }
    }
}