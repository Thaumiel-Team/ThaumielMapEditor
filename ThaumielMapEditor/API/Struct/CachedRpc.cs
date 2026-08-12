// -----------------------------------------------------------------------
// <copyright file="CachedRpc.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using System.Reflection;

namespace ThaumielMapEditor.API.Struct
{
    public readonly struct CachedRpc
    {
        public readonly MethodInfo Method;
        public readonly ushort FunctionHash;

        public CachedRpc(MethodInfo method, ushort functionHash)
        {
            Method = method;
            FunctionHash = functionHash;
        }
    }
}