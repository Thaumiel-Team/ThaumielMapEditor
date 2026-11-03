// -----------------------------------------------------------------------
// <copyright file="BlockValueResolver.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU Lesser General Public License v3.0 (LGPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using System.Globalization;
using LabApi.Features.Wrappers;

namespace ThaumielMapEditor.API.Helpers.BlockParser
{
    internal static class BlockValueResolver
    {
        internal static object? ResolveValue(object? val, Player? player) =>
            BlockBase.ResolveValue(val, player);

        internal static float ResolveFloat(object? val, Player? player, float defaultVal = 0f)
        {
            object? resolved = ResolveValue(val, player);

            return resolved switch
            {
                float f => f,
                double d => (float)d,
                int i => i,
                string s when float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed) => parsed,
                _ => defaultVal
            };
        }

        internal static bool ResolveBool(object? val, Player? player)
        {
            object? resolved = ResolveValue(val, player);

            if (resolved is bool b)
                return b;

            return bool.TryParse(resolved?.ToString(), out bool parsed) && parsed;
        }

        internal static Vector3 ResolveVector3(object? val, Player? player)
        {
            object? resolved = ResolveValue(val, player);

            if (resolved is Vector3 v)
                return v;

            LogManager.Warn($"Expected Vector3 but got '{resolved?.GetType().Name ?? "null"}'.");
            return Vector3.zero;
        }
    }
}