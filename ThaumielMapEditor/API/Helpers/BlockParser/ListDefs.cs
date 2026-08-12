// -----------------------------------------------------------------------
// <copyright file="ListDefs.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;

namespace ThaumielMapEditor.API.Helpers.BlockParser
{
    public class ListCreateBlock : BlockBase
    {
        public override object ReturnExecute() => new List<object>();
    }

    public class ListAddBlock : BlockBase
    {
        public object? List { get; set; }
        public object? Item { get; set; }

        public override void Execute()
        {
            List<object>? list = (List is BlockBase bL ? bL.ReturnExecute() : List) as List<object>;
            object? item = Item is BlockBase bI ? bI.ReturnExecute() : Item;

            list?.Add(item!);
        }
    }

    public class ListRemoveBlock : BlockBase
    {
        public object? List { get; set; }
        public object? Item { get; set; }

        public override void Execute()
        {
            List<object>? list = (List is BlockBase bL ? bL.ReturnExecute() : List) as List<object>;
            object? item = Item is BlockBase bI ? bI.ReturnExecute() : Item;

            list?.Remove(item!);
        }
    }

    public class ListGetBlock : BlockBase
    {
        public object? List { get; set; }
        public int Index { get; set; }

        public override object ReturnExecute()
        {
            if ((List is BlockBase bL ? bL.ReturnExecute() : List) is List<object> list && Index >= 0 && Index < list.Count)
                return list[Index];

            return null!;
        }
    }

    public class ListCountBlock : BlockBase
    {
        public object? List { get; set; }

        public override object ReturnExecute()
        {
            List<object>? list = (List is BlockBase bL ? bL.ReturnExecute() : List) as List<object>;
            return list?.Count ?? 0;
        }
    }

    public class ListContainsBlock : BlockBase
    {
        public object? List { get; set; }
        public object? Item { get; set; }

        public override object ReturnExecute()
        {
            List<object>? list = (List is BlockBase bL ? bL.ReturnExecute() : List) as List<object>;
            object? item = Item is BlockBase bI ? bI.ReturnExecute() : Item;
            return list?.Contains(item!) ?? false;
        }
    }

    public class ListFirstBlock : BlockBase
    {
        public object? List { get; set; }

        public override object ReturnExecute()
            => (List is BlockBase bL ? bL.ReturnExecute() : List) is List<object> list && list.Count > 0 ? list[0] : null!;
    }

    public class ListLastBlock : BlockBase
    {
        public object? List { get; set; }

        public override object ReturnExecute()
            => (List is BlockBase bL ? bL.ReturnExecute() : List) is List<object> list && list.Count > 0 ? list[list.Count - 1] : null!;
    }
}