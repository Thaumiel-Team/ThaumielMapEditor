// -----------------------------------------------------------------------
// <copyright file="StringBlocks.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace ThaumielMapEditor.API.Helpers.BlockParser
{
    public class StringContainsBlock : BlockBase
    {
        public object? STR { get; set; }
        public object? VALUE { get; set; }

        public override object ReturnExecute()
            => ResolveString(STR).Contains(ResolveString(VALUE));
    }

    public class StringStartsWithBlock : BlockBase
    {
        public object? STR { get; set; }
        public object? VALUE { get; set; }

        public override object ReturnExecute()
            => ResolveString(STR).StartsWith(ResolveString(VALUE));
    }

    public class StringEndsWithBlock : BlockBase
    {
        public object? STR { get; set; }
        public object? VALUE { get; set; }

        public override object ReturnExecute()
            => ResolveString(STR).EndsWith(ResolveString(VALUE));
    }

    public class StringToUpperBlock : BlockBase
    {
        public object? STR { get; set; }

        public override object ReturnExecute()
            => ResolveString(STR).ToUpper();
    }

    public class StringToLowerBlock : BlockBase
    {
        public object? STR { get; set; }

        public override object ReturnExecute()
            => ResolveString(STR).ToLower();
    }

    public class StringTrimBlock : BlockBase
    {
        public object? STR { get; set; }

        public override object ReturnExecute()
            => ResolveString(STR).Trim();
    }

    public class StringSubstringBlock : BlockBase
    {
        public object? STR { get; set; }
        public object? START { get; set; }
        public object? LENGTH { get; set; }

        public override object ReturnExecute()
        {
            string str = ResolveString(STR);
            int start = ResolveInt(START);
            int length = ResolveInt(LENGTH);

            if (start < 0 || start >= str.Length || length <= 0)
                return string.Empty;

            if (start + length > str.Length)
                length = str.Length - start;

            return str.Substring(start, length);
        }
    }

    public class StringReplaceBlock : BlockBase
    {
        public object? STR { get; set; }
        public object? OLD { get; set; }
        public object? NEW { get; set; }

        public override object ReturnExecute()
            => ResolveString(STR).Replace(ResolveString(OLD), ResolveString(NEW));
    }

    public class StringIndexOfBlock : BlockBase
    {
        public object? STR { get; set; }
        public object? VALUE { get; set; }

        public override object ReturnExecute()
            => ResolveString(STR).IndexOf(ResolveString(VALUE));
    }

    public class StringCharAtBlock : BlockBase
    {
        public object? STR { get; set; }
        public object? INDEX { get; set; }

        public override object ReturnExecute()
        {
            string str = ResolveString(STR);
            int index = ResolveInt(INDEX);

            if (index < 0 || index >= str.Length)
                return string.Empty;

            return str[index].ToString();
        }
    }

    public class StringFormatBlock : BlockBase
    {
        public object? FMT { get; set; }
        public object? ARG1 { get; set; }
        public object? ARG2 { get; set; }
        public object? ARG3 { get; set; }

        public override object ReturnExecute()
        {
            object?[] args = [ResolveValue(ARG1), ResolveValue(ARG2), ResolveValue(ARG3)];
            return string.Format(CultureInfo.InvariantCulture, ResolveString(FMT), args);
        }
    }

    public class StringIsNullOrEmptyBlock : BlockBase
    {
        public object? STR { get; set; }

        public override object ReturnExecute()
            => string.IsNullOrEmpty(ResolveString(STR));
    }

    public class StringIsNullOrWhitespaceBlock : BlockBase
    {
        public object? STR { get; set; }

        public override object ReturnExecute()
            => string.IsNullOrWhiteSpace(ResolveString(STR));
    }

    public class StringPadLeftBlock : BlockBase
    {
        public object? STR { get; set; }
        public object? TOTAL { get; set; }

        public override object ReturnExecute()
            => ResolveString(STR).PadLeft(ResolveInt(TOTAL));
    }

    public class StringPadRightBlock : BlockBase
    {
        public object? STR { get; set; }
        public object? TOTAL { get; set; }

        public override object ReturnExecute()
            => ResolveString(STR).PadRight(ResolveInt(TOTAL));
    }

    public class StringJoinBlock : BlockBase
    {
        public object? SEP { get; set; }
        public object? LIST { get; set; }

        public override object ReturnExecute()
        {
            object? resolved = ResolveValue(LIST);

            if (resolved is IEnumerable enumerable && resolved is not string)
            {
                IEnumerable<string> items = enumerable.Cast<object?>().Select(x => x?.ToString() ?? string.Empty);
                return string.Join(ResolveString(SEP), items);
            }

            return string.Empty;
        }
    }

    public class StringSplitBlock : BlockBase
    {
        public object? STR { get; set; }
        public object? SEP { get; set; }

        public override object ReturnExecute()
        {
            string[] parts = ResolveString(STR).Split([ResolveString(SEP)], System.StringSplitOptions.None);
            return new List<object>(parts);
        }
    }
}
