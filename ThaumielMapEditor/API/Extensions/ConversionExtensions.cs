// -----------------------------------------------------------------------
// <copyright file="ConversionExtensions.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using ThaumielMapEditor.API.Attributes;
using ThaumielMapEditor.API.Helpers;
using UnityEngine;

namespace ThaumielMapEditor.API.Extensions
{
    [GitBookPage("Extensions/Convert")]
    public static class ConvertExtensions
    {
        public static Vector3? ToVector3(object obj)
        {
            if (obj is Dictionary<string, object> dict)
            {
                if (dict.TryGetValue("x", out var x) && dict.TryGetValue("y", out var y) && dict.TryGetValue("z", out var z))
                {
                    return new Vector3(Convert.ToSingle(x), Convert.ToSingle(y), Convert.ToSingle(z));
                }
            }
            else if (obj is List<object> list && list.Count >= 3)
            {
                return new Vector3(Convert.ToSingle(list[0]), Convert.ToSingle(list[1]), Convert.ToSingle(list[2]));
            }

            LogManager.Warn($"Could not convert '{obj}' to Vector3.");
            return null;
        }

        public static Vector2? ToVector2(object obj)
        {
            if (obj is Dictionary<string, object> dict)
            {
                if (dict.TryGetValue("x", out var x) && dict.TryGetValue("y", out var y))
                {
                    return new Vector2(Convert.ToSingle(x), Convert.ToSingle(y));
                }
            }
            else if (obj is List<object> list && list.Count >= 2)
            {
                return new Vector2(Convert.ToSingle(list[0]), Convert.ToSingle(list[1]));
            }

            LogManager.Warn($"Could not convert '{obj}' to Vector2.");
            return null;
        }
    }
}