// -----------------------------------------------------------------------
// <copyright file="CachedRpc.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU Lesser General Public License v3.0 (LGPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using System.Reflection;

namespace ThaumielMapEditor.API.Struct
{
    [GitBookPage("Struct/CachedRpc")]
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