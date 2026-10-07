// -----------------------------------------------------------------------
// <copyright file="ConvertBlocks.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Globalization;

namespace ThaumielMapEditor.API.Helpers.BlockParser
{
    public class ConvertToStringBlock : BlockBase
    {
        public object? VALUE { get; set; }

        public override object ReturnExecute()
            => ResolveValue(VALUE)?.ToString() ?? string.Empty;
    }

    public class ConvertToIntBlock : BlockBase
    {
        public object? VALUE { get; set; }

        public override object ReturnExecute()
            => ResolveInt(VALUE);
    }

    public class ConvertToFloatBlock : BlockBase
    {
        public object? VALUE { get; set; }

        public override object ReturnExecute()
            => ResolveFloat(VALUE);
    }

    public class ConvertToDoubleBlock : BlockBase
    {
        public object? VALUE { get; set; }

        public override object ReturnExecute()
            => (double)ResolveFloat(VALUE);
    }

    public class ConvertToBoolBlock : BlockBase
    {
        public object? VALUE { get; set; }

        public override object ReturnExecute()
            => ResolveBool(VALUE);
    }

    public class ConvertParseIntBlock : BlockBase
    {
        public object? STR { get; set; }

        public override object ReturnExecute()
        {
            if (int.TryParse(ResolveString(STR), NumberStyles.Integer, CultureInfo.InvariantCulture, out int result))
                return result;

            return 0;
        }
    }

    public class ConvertParseFloatBlock : BlockBase
    {
        public object? STR { get; set; }

        public override object ReturnExecute()
        {
            if (float.TryParse(ResolveString(STR), NumberStyles.Float, CultureInfo.InvariantCulture, out float result))
                return result;

            return 0f;
        }
    }

    public class ConvertParseDoubleBlock : BlockBase
    {
        public object? STR { get; set; }

        public override object ReturnExecute()
        {
            if (double.TryParse(ResolveString(STR), NumberStyles.Float, CultureInfo.InvariantCulture, out double result))
                return result;

            return 0d;
        }
    }

    public class ConvertParseBoolBlock : BlockBase
    {
        public object? STR { get; set; }

        public override object ReturnExecute()
        {
            if (bool.TryParse(ResolveString(STR), out bool result))
                return result;

            return false;
        }
    }

    public class ConvertToStringFormatBlock : BlockBase
    {
        public object? VALUE { get; set; }
        public object? FMT { get; set; }

        public override object ReturnExecute()
        {
            object? value = ResolveValue(VALUE);
            string format = ResolveString(FMT);

            if (value is IFormattable formattable)
                return formattable.ToString(format, CultureInfo.InvariantCulture);

            return value?.ToString() ?? string.Empty;
        }
    }
}
