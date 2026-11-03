// -----------------------------------------------------------------------
// <copyright file="MethodBlock.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU Lesser General Public License v3.0 (LGPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;

namespace ThaumielMapEditor.API.Helpers.BlockParser
{
    public class MethodBlock
    {
        public string Type { get; set; } = string.Empty;
        
        public string Name { get; set; } = string.Empty;

        public List<string> Params { get; set; } = [];

        public List<object?> Stack { get; set; } = [];

        public object? Return { get; set; }
    }

    public class ProcedureCallNoReturnBlock : BlockBase
    {
        public string Name { get; set; } = string.Empty;
    }

    public class ProcedureCallReturnBlock : BlockBase
    {
        public string Name { get; set; } = string.Empty;
        public Dictionary<string, object?> Args { get; set; } = [];
    }
}