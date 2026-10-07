// -----------------------------------------------------------------------
// <copyright file="VariableBlocks.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;

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
        public object? Value { get; set; }

        public override object ReturnExecute()
        {
            return Resolve();
        }

        public override object ReturnExecute(object obj)
        {
            return Resolve();
        }

        private object Resolve()
        {
            if (Executor != null)
            {
                foreach (Dictionary<string, object?> scope in Executor.Scopes)
                {
                    if (scope.TryGetValue(Name, out object? variableValue))
                    {
                        Value = variableValue;
                        return variableValue!;
                    }
                }
            }

            return Value!;
        }
    }
}