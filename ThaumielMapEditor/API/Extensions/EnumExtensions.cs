// -----------------------------------------------------------------------
// <copyright file="EnumExtensions.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using ThaumielMapEditor.API.Attributes;

namespace ThaumielMapEditor.API.Extensions
{
    [GitBookPage("Extensions/Enum")]
    public static class EnumExtensions
    {
        public static T Next<T>(this T current, bool wrap = true) where T : struct, Enum
            => Step(current, 1, wrap, null);

        public static T Next<T>(this T current, bool wrap, params T[]? exclude) where T : struct, Enum
            => Step(current, 1, wrap, exclude);

        public static T Previous<T>(this T current, bool wrap = true) where T : struct, Enum
            => Step(current, -1, wrap, null);

        public static T Previous<T>(this T current, bool wrap, params T[]? exclude) where T : struct, Enum
            => Step(current, -1, wrap, exclude);

        private static T Step<T>(T current, int direction, bool wrap, T[]? exclude) where T : struct, Enum
        {
            T[] values = EnumCache<T>.Values;
            if (values.Length == 0)
                return current;

            HashSet<T>? excluded = exclude is { Length: > 0 } ? new HashSet<T>(exclude) : null;
            if (excluded != null && excluded.Count >= values.Length)
                return Array.IndexOf(values, current) >= 0 ? current : values[0];

            bool IsExcluded(T value) => excluded != null && excluded.Contains(value);

            int index = Array.IndexOf(values, current);
            if (index < 0)
            {
                // Current is not a defined member (stale or combined value).
                // Snap to the first valid value in the step direction.
                if (direction > 0)
                {
                    for (int i = 0; i < values.Length; i++)
                    {
                        if (!IsExcluded(values[i]))
                            return values[i];
                    }
                }
                else
                {
                    for (int i = values.Length - 1; i >= 0; i--)
                    {
                        if (!IsExcluded(values[i]))
                            return values[i];
                    }
                }

                return current;
            }

            for (int step = 0; step < values.Length; step++)
            {
                index += direction;
                if (index < 0 || index >= values.Length)
                {
                    if (!wrap)
                        break;

                    index = direction > 0 ? 0 : values.Length - 1;
                }

                if (!IsExcluded(values[index]))
                    return values[index];
            }

            // wrap is false and the edge was hit: clamp to the nearest valid value.
            if (direction > 0)
            {
                for (int i = values.Length - 1; i >= 0; i--)
                {
                    if (!IsExcluded(values[i]))
                        return values[i];
                }
            }
            else
            {
                for (int i = 0; i < values.Length; i++)
                {
                    if (!IsExcluded(values[i]))
                        return values[i];
                }
            }

            return current;
        }

        private static class EnumCache<T> where T : struct, Enum
        {
            public static readonly T[] Values = (T[])Enum.GetValues(typeof(T));
        }
    }
}