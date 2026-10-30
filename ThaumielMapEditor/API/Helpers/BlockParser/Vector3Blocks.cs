// -----------------------------------------------------------------------
// <copyright file="Vector3Blocks.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

namespace ThaumielMapEditor.API.Helpers.BlockParser
{
    public class UnityVec3Block : BlockBase
    {
        public object? X { get; set; }
        public object? Y { get; set; }
        public object? Z { get; set; }

        public override object? ReturnExecute()
        {
            float x = BlockValueResolver.ResolveFloat(X, null);
            float y = BlockValueResolver.ResolveFloat(Y, null);
            float z = BlockValueResolver.ResolveFloat(Z, null);
            return new Vector3(x, y, z);
        }

        public override object? ReturnExecute(object obj) => ReturnExecute();
    }

    public class UnityVec3ZeroBlock : BlockBase
    {
        public override object? ReturnExecute() => Vector3.zero;

        public override object? ReturnExecute(object obj) => Vector3.zero;
    }

    public class UnityVec3UpBlock : BlockBase
    {
        public override object? ReturnExecute() => Vector3.up;

        public override object? ReturnExecute(object obj) => Vector3.up;
    }

    public class UnityDistanceBlock : BlockBase
    {
        public object? A { get; set; }
        public object? B { get; set; }

        public override object? ReturnExecute()
        {
            Vector3 a = BlockValueResolver.ResolveVector3(A, null);
            Vector3 b = BlockValueResolver.ResolveVector3(B, null);
            float distance = Vector3.Distance(a, b);
            LogManager.Debug($"Distance({a}, {b}) = {distance}.");
            return distance;
        }

        public override object? ReturnExecute(object obj) => ReturnExecute();
    }

    public class UnityLerpBlock : BlockBase
    {
        public object? A { get; set; }
        public object? B { get; set; }
        public object? T { get; set; }

        public override object? ReturnExecute()
        {
            Vector3 a = BlockValueResolver.ResolveVector3(A, null);
            Vector3 b = BlockValueResolver.ResolveVector3(B, null);
            float t = BlockValueResolver.ResolveFloat(T, null);
            Vector3 result = Vector3.Lerp(a, b, t);
            LogManager.Debug($"Lerp({a}, {b}, {t}) = {result}.");
            return result;
        }

        public override object? ReturnExecute(object obj) => ReturnExecute();
    }

    public class UnityNormalizeBlock : BlockBase
    {
        public object? V { get; set; }

        public override object? ReturnExecute()
        {
            Vector3 v = BlockValueResolver.ResolveVector3(V, null);
            Vector3 normalized = v.normalized;
            LogManager.Debug($"{v}.normalized = {normalized}.");
            return normalized;
        }

        public override object? ReturnExecute(object obj) => ReturnExecute();
    }
}