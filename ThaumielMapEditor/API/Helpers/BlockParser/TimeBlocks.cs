// -----------------------------------------------------------------------
// <copyright file="TimeBlocks.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using LabApi.Features.Wrappers;
using UnityEngine;

namespace ThaumielMapEditor.API.Helpers.BlockParser
{
    public class UnityDeltaBlock : BlockBase
    {
        public override object? ReturnExecute() => Time.deltaTime;

        public override object? ReturnExecute(object obj) => Time.deltaTime;
    }

    public class UnityFixedDeltaBlock : BlockBase
    {
        public override object? ReturnExecute() => Time.fixedDeltaTime;

        public override object? ReturnExecute(object obj) => Time.fixedDeltaTime;
    }

    public class UnityTimeBlock : BlockBase
    {
        public override object? ReturnExecute() => Time.time;

        public override object? ReturnExecute(object obj) => Time.time;
    }

    public class UnityWaitBlock : BlockBase
    {
        public object? T { get; set; }

        public override void Execute(Player player)
        {
            float seconds = BlockValueResolver.ResolveFloat(T, player, 1f);
            LogManager.Debug($"WaitForSeconds({seconds}).");
        }

        public override object? ReturnExecute()
        {
            float seconds = BlockValueResolver.ResolveFloat(T, null, 1f);
            return new UnityEngine.WaitForSeconds(seconds);
        }

        public override object? ReturnExecute(object obj)
        {
            return ReturnExecute();
        }
    }
}