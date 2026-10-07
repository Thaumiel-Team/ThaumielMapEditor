// -----------------------------------------------------------------------
// <copyright file="MathBlocks.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using UnityEngine;

namespace ThaumielMapEditor.API.Helpers.BlockParser
{
    public class MathArithmeticBlock : BlockBase
    {
        public string OP { get; set; } = "ADD";
        public object? A { get; set; }
        public object? B { get; set; }

        public override object ReturnExecute()
        {
            float a = ResolveFloat(A);
            float b = ResolveFloat(B);

            float result = OP switch
            {
                "ADD" => a + b,
                "MINUS" => a - b,
                "MULTIPLY" => a * b,
                "DIVIDE" => b != 0f ? a / b : 0f,
                "POWER" => Mathf.Pow(a, b),
                _ => 0f
            };

            LogManager.Debug($"{a} {OP} {b} = {result}");
            return result;
        }
    }

    public class MathSingleBlock : BlockBase
    {
        public string OP { get; set; } = "ABS";
        public object? NUM { get; set; }

        public override object ReturnExecute()
        {
            float num = ResolveFloat(NUM);

            float result = OP switch
            {
                "ROOT" => Mathf.Sqrt(num),
                "ABS" => Mathf.Abs(num),
                "NEG" => -num,
                "LN" => Mathf.Log(num),
                "LOG10" => Mathf.Log10(num),
                "EXP" => Mathf.Exp(num),
                "POW10" => Mathf.Pow(10f, num),
                _ => 0f
            };

            LogManager.Debug($"{OP}({num}) = {result}");
            return result;
        }
    }

    public class MathTrigBlock : BlockBase
    {
        public string OP { get; set; } = "SIN";
        public object? NUM { get; set; }

        public override object ReturnExecute()
        {
            float degrees = ResolveFloat(NUM);
            float radians = degrees * Mathf.Deg2Rad;

            float result = OP switch
            {
                "SIN" => Mathf.Sin(radians),
                "COS" => Mathf.Cos(radians),
                "TAN" => Mathf.Tan(radians),
                "ASIN" => Mathf.Asin(degrees) * Mathf.Rad2Deg,
                "ACOS" => Mathf.Acos(degrees) * Mathf.Rad2Deg,
                "ATAN" => Mathf.Atan(degrees) * Mathf.Rad2Deg,
                _ => 0f
            };

            LogManager.Debug($"{OP}({degrees}°) = {result}");
            return result;
        }
    }

    public class MathRoundBlock : BlockBase
    {
        public string OP { get; set; } = "ROUND";
        public object? NUM { get; set; }

        public override object ReturnExecute()
        {
            float num = ResolveFloat(NUM);

            float result = OP switch
            {
                "ROUND" => Mathf.Round(num),
                "ROUNDUP" => Mathf.Ceil(num),
                "ROUNDDOWN" => Mathf.Floor(num),
                _ => num
            };

            LogManager.Debug($"{OP}({num}) = {result}");
            return result;
        }
    }

    public class MathModuloBlock : BlockBase
    {
        public object? DIVIDEND { get; set; }
        public object? DIVISOR { get; set; }

        public override object ReturnExecute()
        {
            float dividend = ResolveFloat(DIVIDEND);
            float divisor = ResolveFloat(DIVISOR, 1f);

            float result = divisor != 0f ? dividend % divisor : 0f;

            LogManager.Debug($"{dividend} % {divisor} = {result}");
            return result;
        }
    }

    public class MathConstrainBlock : BlockBase
    {
        public object? VALUE { get; set; }
        public object? LOW { get; set; }
        public object? HIGH { get; set; }

        public override object ReturnExecute()
        {
            float value = ResolveFloat(VALUE);
            float low = ResolveFloat(LOW, 0f);
            float high = ResolveFloat(HIGH, 1f);

            float result = Mathf.Clamp(value, low, high);

            LogManager.Debug($"Clamp({value}, {low}, {high}) = {result}");
            return result;
        }
    }

    public class MathRandomFloatBlock : BlockBase
    {
        public override object ReturnExecute()
        {
            float result = Random.value;
            LogManager.Debug($"{result}");
            return result;
        }
    }

    public class MathMinBlock : BlockBase
    {
        public object? A { get; set; }
        public object? B { get; set; }

        public override object ReturnExecute()
            => Mathf.Min(ResolveFloat(A), ResolveFloat(B));
    }

    public class MathMaxBlock : BlockBase
    {
        public object? A { get; set; }
        public object? B { get; set; }

        public override object ReturnExecute()
            => Mathf.Max(ResolveFloat(A), ResolveFloat(B));
    }

    public class MathClamp01Block : BlockBase
    {
        public object? VALUE { get; set; }

        public override object ReturnExecute()
            => Mathf.Clamp01(ResolveFloat(VALUE));
    }

    public class MathLerpFloatBlock : BlockBase
    {
        public object? A { get; set; }
        public object? B { get; set; }
        public object? T { get; set; }

        public override object ReturnExecute()
            => Mathf.Lerp(ResolveFloat(A), ResolveFloat(B), ResolveFloat(T));
    }

    public class MathMoveTowardsBlock : BlockBase
    {
        public object? CURRENT { get; set; }
        public object? TARGET { get; set; }
        public object? MAXDELTA { get; set; }

        public override object ReturnExecute()
            => Mathf.MoveTowards(ResolveFloat(CURRENT), ResolveFloat(TARGET), ResolveFloat(MAXDELTA));
    }

    public class MathRandomIntRangeBlock : BlockBase
    {
        public object? MIN { get; set; }
        public object? MAX { get; set; }

        public override object ReturnExecute()
            => Random.Range(ResolveInt(MIN), ResolveInt(MAX));
    }

    public class MathRandomFloatRangeBlock : BlockBase
    {
        public object? MIN { get; set; }
        public object? MAX { get; set; }

        public override object ReturnExecute()
            => Random.Range(ResolveFloat(MIN), ResolveFloat(MAX));
    }

    public class MathSignBlock : BlockBase
    {
        public object? VALUE { get; set; }

        public override object ReturnExecute()
        {
            float value = ResolveFloat(VALUE);
            return value > 0f ? 1 : value < 0f ? -1 : 0;
        }
    }

    public class MathFloorToIntBlock : BlockBase
    {
        public object? VALUE { get; set; }

        public override object ReturnExecute()
            => Mathf.FloorToInt(ResolveFloat(VALUE));
    }

    public class MathCeilToIntBlock : BlockBase
    {
        public object? VALUE { get; set; }

        public override object ReturnExecute()
            => Mathf.CeilToInt(ResolveFloat(VALUE));
    }

    public class MathRoundToIntBlock : BlockBase
    {
        public object? VALUE { get; set; }

        public override object ReturnExecute()
            => Mathf.RoundToInt(ResolveFloat(VALUE));
    }

    public class MathRoundDigitsBlock : BlockBase
    {
        public object? VALUE { get; set; }
        public object? DIGITS { get; set; }

        public override object ReturnExecute()
        {
            int digits = ResolveInt(DIGITS);
            if (digits < 0)
                digits = 0;

            if (digits > 15)
                digits = 15;

            return (float)System.Math.Round(ResolveFloat(VALUE), digits, System.MidpointRounding.AwayFromZero);
        }
    }

    public class MathPingPongBlock : BlockBase
    {
        public object? VALUE { get; set; }
        public object? LENGTH { get; set; }

        public override object ReturnExecute()
            => Mathf.PingPong(ResolveFloat(VALUE), ResolveFloat(LENGTH, 1f));
    }

    public class MathRepeatBlock : BlockBase
    {
        public object? VALUE { get; set; }
        public object? LENGTH { get; set; }

        public override object ReturnExecute()
            => Mathf.Repeat(ResolveFloat(VALUE), ResolveFloat(LENGTH, 1f));
    }

    public class MathPiBlock : BlockBase
    {
        public override object ReturnExecute()
            => Mathf.PI;
    }

    public class MathDeg2RadBlock : BlockBase
    {
        public object? VALUE { get; set; }

        public override object ReturnExecute()
            => ResolveFloat(VALUE) * Mathf.Deg2Rad;
    }

    public class MathRad2DegBlock : BlockBase
    {
        public object? VALUE { get; set; }

        public override object ReturnExecute()
            => ResolveFloat(VALUE) * Mathf.Rad2Deg;
    }

    public class MathIsNaNBlock : BlockBase
    {
        public object? VALUE { get; set; }

        public override object ReturnExecute()
            => float.IsNaN(ResolveFloat(VALUE));
    }

    public class MathIsInfinityBlock : BlockBase
    {
        public object? VALUE { get; set; }

        public override object ReturnExecute()
            => float.IsInfinity(ResolveFloat(VALUE));
    }
}