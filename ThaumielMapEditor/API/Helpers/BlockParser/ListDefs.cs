// -----------------------------------------------------------------------
// <copyright file="ListDefs.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU Lesser General Public License v3.0 (LGPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;

namespace ThaumielMapEditor.API.Helpers.BlockParser
{
    public class ListCreateBlock : BlockBase
    {
        public override object? ReturnExecute() => new List<object>();
    }

    public class ListAddBlock : BlockBase
    {
        public object? List { get; set; }
        public object? Item { get; set; }

        public override void Execute()
        {
            if (ResolveValue(List) is List<object> list)
                list.Add(ResolveValue(Item)!);
        }
    }

    public class ListClearBlock : BlockBase
    {
        public object? List { get; set; }

        public override void Execute()
        {
            if (ResolveValue(List) is List<object> list)
                list.Clear();
        }
    }

    public class ListRemoveBlock : BlockBase
    {
        public object? List { get; set; }
        public object? Item { get; set; }

        public override void Execute()
        {
            if (ResolveValue(List) is List<object> list)
                list.Remove(ResolveValue(Item)!);
        }
    }

    public class ListRemoveAtBlock : BlockBase
    {
        public object? List { get; set; }
        public object? Index { get; set; }

        public override void Execute()
        {
            int index = (int)ResolveFloat(Index, -1f);

            if (ResolveValue(List) is List<object> list && index >= 0 && index < list.Count)
                list.RemoveAt(index);
        }
    }

    public class ListGetBlock : BlockBase
    {
        public object? List { get; set; }
        public int Index { get; set; }

        public override object? ReturnExecute()
        {
            if (ResolveValue(List) is List<object> list && Index >= 0 && Index < list.Count)
                return list[Index];

            return null;
        }
    }

    public class ListCountBlock : BlockBase
    {
        public object? List { get; set; }

        public override object? ReturnExecute() =>
            ResolveValue(List) is List<object> list ? list.Count : 0;
    }

    public class ListContainsBlock : BlockBase
    {
        public object? List { get; set; }
        public object? Item { get; set; }

        public override object? ReturnExecute() =>
            ResolveValue(List) is List<object> list && list.Contains(ResolveValue(Item)!);
    }

    public class ListFirstBlock : BlockBase
    {
        public object? List { get; set; }

        public override object? ReturnExecute() =>
            ResolveValue(List) is List<object> { Count: > 0 } list ? list[0] : null;
    }

    public class ListLastBlock : BlockBase
    {
        public object? List { get; set; }

        public override object? ReturnExecute() =>
            ResolveValue(List) is List<object> { Count: > 0 } list ? list[list.Count - 1] : null;
    }
}