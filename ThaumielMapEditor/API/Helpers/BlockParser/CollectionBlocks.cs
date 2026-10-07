// -----------------------------------------------------------------------
// <copyright file="CollectionBlocks.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;

namespace ThaumielMapEditor.API.Helpers.BlockParser
{
    public class DictCreateBlock : BlockBase
    {
        public override object ReturnExecute()
            => new Dictionary<object, object>();
    }

    public class DictAddBlock : BlockBase
    {
        public object? DICT { get; set; }
        public object? KEY { get; set; }
        public object? VALUE { get; set; }

        public override void Execute()
        {
            if (ResolveValue(DICT) is Dictionary<object, object> dict)
                dict[ResolveValue(KEY)!] = ResolveValue(VALUE)!;
        }
    }

    public class DictRemoveBlock : BlockBase
    {
        public object? DICT { get; set; }
        public object? KEY { get; set; }

        public override void Execute()
        {
            if (ResolveValue(DICT) is Dictionary<object, object> dict)
                dict.Remove(ResolveValue(KEY)!);
        }
    }

    public class DictContainsKeyBlock : BlockBase
    {
        public object? DICT { get; set; }
        public object? KEY { get; set; }

        public override object ReturnExecute()
            => ResolveValue(DICT) is Dictionary<object, object> dict && dict.ContainsKey(ResolveValue(KEY)!);
    }

    public class DictContainsValueBlock : BlockBase
    {
        public object? DICT { get; set; }
        public object? VALUE { get; set; }

        public override object ReturnExecute()
            => ResolveValue(DICT) is Dictionary<object, object> dict && dict.ContainsValue(ResolveValue(VALUE)!);
    }

    public class DictGetBlock : BlockBase
    {
        public object? DICT { get; set; }
        public object? KEY { get; set; }

        public override object ReturnExecute()
        {
            if (ResolveValue(DICT) is Dictionary<object, object> dict && dict.TryGetValue(ResolveValue(KEY)!, out object? value))
                return value;

            return null!;
        }
    }

    public class DictSetBlock : BlockBase
    {
        public object? DICT { get; set; }
        public object? KEY { get; set; }
        public object? VALUE { get; set; }

        public override void Execute()
        {
            if (ResolveValue(DICT) is Dictionary<object, object> dict)
                dict[ResolveValue(KEY)!] = ResolveValue(VALUE)!;
        }
    }

    public class DictCountBlock : BlockBase
    {
        public object? DICT { get; set; }

        public override object ReturnExecute()
            => ResolveValue(DICT) is Dictionary<object, object> dict ? dict.Count : 0;
    }

    public class DictKeysBlock : BlockBase
    {
        public object? DICT { get; set; }

        public override object ReturnExecute()
            => ResolveValue(DICT) is Dictionary<object, object> dict ? dict.Keys.ToList() : new List<object>();
    }

    public class DictValuesBlock : BlockBase
    {
        public object? DICT { get; set; }

        public override object ReturnExecute()
            => ResolveValue(DICT) is Dictionary<object, object> dict ? dict.Values.ToList() : new List<object>();
    }

    public class DictClearBlock : BlockBase
    {
        public object? DICT { get; set; }

        public override void Execute()
        {
            if (ResolveValue(DICT) is Dictionary<object, object> dict)
                dict.Clear();
        }
    }

    public class HashSetCreateBlock : BlockBase
    {
        public override object ReturnExecute()
            => new HashSet<object>();
    }

    public class HashSetAddBlock : BlockBase
    {
        public object? SET { get; set; }
        public object? ITEM { get; set; }

        public override void Execute()
        {
            if (ResolveValue(SET) is HashSet<object> set)
                set.Add(ResolveValue(ITEM)!);
        }
    }

    public class HashSetRemoveBlock : BlockBase
    {
        public object? SET { get; set; }
        public object? ITEM { get; set; }

        public override void Execute()
        {
            if (ResolveValue(SET) is HashSet<object> set)
                set.Remove(ResolveValue(ITEM)!);
        }
    }

    public class HashSetContainsBlock : BlockBase
    {
        public object? SET { get; set; }
        public object? ITEM { get; set; }

        public override object ReturnExecute()
            => ResolveValue(SET) is HashSet<object> set && set.Contains(ResolveValue(ITEM)!);
    }

    public class HashSetCountBlock : BlockBase
    {
        public object? SET { get; set; }

        public override object ReturnExecute()
            => ResolveValue(SET) is HashSet<object> set ? set.Count : 0;
    }

    public class HashSetClearBlock : BlockBase
    {
        public object? SET { get; set; }

        public override void Execute()
        {
            if (ResolveValue(SET) is HashSet<object> set)
                set.Clear();
        }
    }

    public class HashSetUnionWithBlock : BlockBase
    {
        public object? SET { get; set; }
        public object? OTHER { get; set; }

        public override void Execute()
        {
            if (ResolveValue(SET) is HashSet<object> set && ResolveValue(OTHER) is HashSet<object> other)
                set.UnionWith(other);
        }
    }

    public class HashSetIntersectWithBlock : BlockBase
    {
        public object? SET { get; set; }
        public object? OTHER { get; set; }

        public override void Execute()
        {
            if (ResolveValue(SET) is HashSet<object> set && ResolveValue(OTHER) is HashSet<object> other)
                set.IntersectWith(other);
        }
    }

    public class StackCreateBlock : BlockBase
    {
        public override object ReturnExecute()
            => new Stack<object>();
    }

    public class StackPushBlock : BlockBase
    {
        public object? STACK { get; set; }
        public object? ITEM { get; set; }

        public override void Execute()
        {
            if (ResolveValue(STACK) is Stack<object> stack)
                stack.Push(ResolveValue(ITEM)!);
        }
    }

    public class StackPopBlock : BlockBase
    {
        public object? STACK { get; set; }

        public override object ReturnExecute()
            => ResolveValue(STACK) is Stack<object> stack && stack.Count > 0 ? stack.Pop() : null!;
    }

    public class StackPeekBlock : BlockBase
    {
        public object? STACK { get; set; }

        public override object ReturnExecute()
            => ResolveValue(STACK) is Stack<object> stack && stack.Count > 0 ? stack.Peek() : null!;
    }

    public class StackCountBlock : BlockBase
    {
        public object? STACK { get; set; }

        public override object ReturnExecute()
            => ResolveValue(STACK) is Stack<object> stack ? stack.Count : 0;
    }

    public class QueueCreateBlock : BlockBase
    {
        public override object ReturnExecute()
            => new Queue<object>();
    }

    public class QueueEnqueueBlock : BlockBase
    {
        public object? QUEUE { get; set; }
        public object? ITEM { get; set; }

        public override void Execute()
        {
            if (ResolveValue(QUEUE) is Queue<object> queue)
                queue.Enqueue(ResolveValue(ITEM)!);
        }
    }

    public class QueueDequeueBlock : BlockBase
    {
        public object? QUEUE { get; set; }

        public override object ReturnExecute()
            => ResolveValue(QUEUE) is Queue<object> queue && queue.Count > 0 ? queue.Dequeue() : null!;
    }

    public class QueuePeekBlock : BlockBase
    {
        public object? QUEUE { get; set; }

        public override object ReturnExecute()
            => ResolveValue(QUEUE) is Queue<object> queue && queue.Count > 0 ? queue.Peek() : null!;
    }

    public class QueueCountBlock : BlockBase
    {
        public object? QUEUE { get; set; }

        public override object ReturnExecute()
            => ResolveValue(QUEUE) is Queue<object> queue ? queue.Count : 0;
    }

    public class ArrayCreateBlock : BlockBase
    {
        public object? SIZE { get; set; }

        public override object ReturnExecute()
            => new object[ResolveInt(SIZE)];
    }

    public class ArrayLengthBlock : BlockBase
    {
        public object? ARR { get; set; }

        public override object ReturnExecute()
            => ResolveValue(ARR) is object[] array ? array.Length : 0;
    }

    public class ArrayGetBlock : BlockBase
    {
        public object? ARR { get; set; }
        public object? INDEX { get; set; }

        public override object ReturnExecute()
        {
            if (ResolveValue(ARR) is object[] array)
            {
                int index = ResolveInt(INDEX);
                if (index >= 0 && index < array.Length)
                    return array[index];
            }

            return null!;
        }
    }

    public class ArraySetBlock : BlockBase
    {
        public object? ARR { get; set; }
        public object? INDEX { get; set; }
        public object? VALUE { get; set; }

        public override void Execute()
        {
            if (ResolveValue(ARR) is object[] array)
            {
                int index = ResolveInt(INDEX);
                if (index >= 0 && index < array.Length)
                    array[index] = ResolveValue(VALUE)!;
            }
        }
    }
}
