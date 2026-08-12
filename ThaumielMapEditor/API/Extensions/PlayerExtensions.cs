// -----------------------------------------------------------------------
// <copyright file="PlayerExtensions.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Concurrent;
using System.Reflection;
using LabApi.Features.Wrappers;
using LabApiExtensions.Extensions;
using LabApiExtensions.FakeExtension;
using Mirror;
using ThaumielMapEditor.API.Struct;

namespace ThaumielMapEditor.API.Extensions
{
    public static class PlayerExtensions
    {
        private static readonly ConcurrentDictionary<(Type Type, string FunctionName), CachedRpc> RpcCache = new();

        private static CachedRpc? GetCachedRpc(Type type, string functionName)
        {
            if (RpcCache.TryGetValue((type, functionName), out CachedRpc cached))
                return cached;

            MethodInfo? method = type.GetMethod(functionName, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (method == null)
            {
                Logger.Error($"Method '{functionName}' not found on type '{type.FullName}'");
                return null;
            }

            string longFuncName = FakeRpcExtension.GetLongFuncName(type, method);
            ushort functionHash = (ushort)longFuncName.GetStableHashCode();
            cached = new CachedRpc(method, functionHash);
            RpcCache[(type, functionName)] = cached;
            return cached;
        }

        /// <summary>
        /// Sends a fake RPC message to a <see cref="Player"/>
        /// </summary>
        /// <param name="player">The <see cref="Player"/> to send the RPC to</param>
        /// <param name="netId">The netid of the object that will be affected.</param>
        /// <param name="type">The <see cref="Type"/> of the RPC message.</param>
        /// <param name="functionName">The name of the RPC message method</param>
        /// <param name="componentIndex"></param>
        /// <param name="objects">The </param>
        public static void SendFakeRPC(this Player player, uint netId, Type type, string functionName, int componentIndex, params object[] objects)
        {
            CachedRpc? cached = GetCachedRpc(type, functionName);
            if (cached == null)
                return;

            using NetworkWriterPooled networkWriterPooled = NetworkWriterPool.Get();
            foreach (object obj in objects)
            {
                if (!MirrorWriterExtension.Write(obj.GetType(), obj, networkWriterPooled))
                {
                    Logger.Error($"Not found NetworkWriter for type {obj.GetType()}");
                    return;
                }
            }

            player.Connection.Send(new RpcMessage
            {
                netId = netId,
                componentIndex = (byte)componentIndex,
                functionHash = cached.Value.FunctionHash,
                payload = networkWriterPooled.ToArraySegment()
            });
        }
    }
}