// -----------------------------------------------------------------------
// <copyright file="DateTimeBlocks.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using System;

namespace ThaumielMapEditor.API.Helpers.BlockParser
{
    public class DateTimeNowBlock : BlockBase
    {
        public override object ReturnExecute()
            => DateTime.Now;
    }

    public class DateTimeUtcNowBlock : BlockBase
    {
        public override object ReturnExecute()
            => DateTime.UtcNow;
    }

    public class DateTimePartBlock : BlockBase
    {
        public object? DT { get; set; }
        public string PART { get; set; } = "Year";

        public override object ReturnExecute()
        {
            DateTime? date = ResolveValue(DT) as DateTime?;

            if (date == null)
                return 0;

            return PART switch
            {
                "Year" => date.Value.Year,
                "Month" => date.Value.Month,
                "Day" => date.Value.Day,
                "Hour" => date.Value.Hour,
                "Minute" => date.Value.Minute,
                "Second" => date.Value.Second,
                "Millisecond" => date.Value.Millisecond,
                "DayOfYear" => date.Value.DayOfYear,
                _ => 0
            };
        }
    }

    public class DateTimeAddBlock : BlockBase
    {
        public object? DT { get; set; }
        public string UNIT { get; set; } = "Seconds";
        public object? VALUE { get; set; }

        public override object ReturnExecute()
        {
            DateTime? date = ResolveValue(DT) as DateTime?;

            if (date == null)
                return DateTime.MinValue;

            double amount = ResolveFloat(VALUE);

            return UNIT switch
            {
                "Seconds" => date.Value.AddSeconds(amount),
                "Minutes" => date.Value.AddMinutes(amount),
                "Hours" => date.Value.AddHours(amount),
                "Days" => date.Value.AddDays(amount),
                "Milliseconds" => date.Value.AddMilliseconds(amount),
                _ => date.Value
            };
        }
    }

    public class DateTimeSubtractBlock : BlockBase
    {
        public object? A { get; set; }
        public object? B { get; set; }

        public override object ReturnExecute()
        {
            DateTime? a = ResolveValue(A) as DateTime?;
            DateTime? b = ResolveValue(B) as DateTime?;

            if (a == null || b == null)
                return TimeSpan.Zero;

            return a.Value - b.Value;
        }
    }

    public class DateTimeToStringBlock : BlockBase
    {
        public object? DT { get; set; }

        public override object ReturnExecute()
        {
            DateTime? date = ResolveValue(DT) as DateTime?;
            return date?.ToString() ?? string.Empty;
        }
    }

    public class DateTimeToStringFormatBlock : BlockBase
    {
        public object? DT { get; set; }
        public object? FMT { get; set; }

        public override object ReturnExecute()
        {
            DateTime? date = ResolveValue(DT) as DateTime?;
            return date?.ToString(ResolveString(FMT)) ?? string.Empty;
        }
    }

    public class DateTimeParseBlock : BlockBase
    {
        public object? STR { get; set; }

        public override object ReturnExecute()
        {
            if (DateTime.TryParse(ResolveString(STR), out DateTime result))
                return result;

            return DateTime.MinValue;
        }
    }

    public class DateTimeTicksBlock : BlockBase
    {
        public object? DT { get; set; }

        public override object ReturnExecute()
            => ResolveValue(DT) is DateTime date ? date.Ticks : 0L;
    }

    public class TimeSpanFromBlock : BlockBase
    {
        public object? VALUE { get; set; }
        public string UNIT { get; set; } = "Seconds";

        public override object ReturnExecute()
        {
            double amount = ResolveFloat(VALUE);

            return UNIT switch
            {
                "Seconds" => TimeSpan.FromSeconds(amount),
                "Minutes" => TimeSpan.FromMinutes(amount),
                "Hours" => TimeSpan.FromHours(amount),
                "Days" => TimeSpan.FromDays(amount),
                "Milliseconds" => TimeSpan.FromMilliseconds(amount),
                _ => TimeSpan.Zero
            };
        }
    }

    public class TimeSpanPartBlock : BlockBase
    {
        public object? TS { get; set; }
        public string PART { get; set; } = "TotalSeconds";

        public override object ReturnExecute()
        {
            if (ResolveValue(TS) is not TimeSpan span)
                return 0;

            return PART switch
            {
                "TotalDays" => span.TotalDays,
                "TotalHours" => span.TotalHours,
                "TotalMinutes" => span.TotalMinutes,
                "TotalSeconds" => span.TotalSeconds,
                "TotalMilliseconds" => span.TotalMilliseconds,
                "Days" => span.Days,
                "Hours" => span.Hours,
                "Minutes" => span.Minutes,
                "Seconds" => span.Seconds,
                "Milliseconds" => span.Milliseconds,
                _ => 0
            };
        }
    }

    public class TimeSpanZeroBlock : BlockBase
    {
        public override object ReturnExecute()
            => TimeSpan.Zero;
    }
}
