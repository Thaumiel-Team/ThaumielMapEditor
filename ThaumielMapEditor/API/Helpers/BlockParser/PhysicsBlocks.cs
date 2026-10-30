// -----------------------------------------------------------------------
// <copyright file="PhysicsBlocks.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using LabApi.Features.Wrappers;
using UnityEngine;

namespace ThaumielMapEditor.API.Helpers.BlockParser
{
    public class UnityAddForceBlock : BlockBase
    {
        public object RB { get; set; } = null!;
        public object? F { get; set; }

        public override void Execute(Player player)
        {
            object? rbResolved = BlockValueResolver.ResolveValue(RB, player);

            if (rbResolved is not Rigidbody rigidbody)
            {
                LogManager.Warn("RB did not resolve to a Rigidbody.");
                return;
            }

            Vector3 force = BlockValueResolver.ResolveVector3(F, player);
            LogManager.Debug($"AddForce({force}) on '{rigidbody.name}'.");
            rigidbody.AddForce(force);
        }
    }

    public class UnityAddTorqueBlock : BlockBase
    {
        public object RB { get; set; } = null!;
        public object? T { get; set; }

        public override void Execute(Player player)
        {
            object? rbResolved = BlockValueResolver.ResolveValue(RB, player);

            if (rbResolved is not Rigidbody rigidbody)
            {
                LogManager.Warn("RB did not resolve to a Rigidbody.");
                return;
            }

            Vector3 torque = BlockValueResolver.ResolveVector3(T, player);
            LogManager.Debug($"AddTorque({torque}) on '{rigidbody.name}'.");
            rigidbody.AddTorque(torque);
        }
    }

    public class UnitySetVelBlock : BlockBase
    {
        public object RB { get; set; } = null!;
        public object? V { get; set; }

        public override void Execute(Player player)
        {
            object? rbResolved = BlockValueResolver.ResolveValue(RB, player);

            if (rbResolved is not Rigidbody rigidbody)
            {
                LogManager.Warn("RB did not resolve to a Rigidbody.");
                return;
            }

            Vector3 velocity = BlockValueResolver.ResolveVector3(V, player);
            LogManager.Debug($"velocity = {velocity} on '{rigidbody.name}'.");
            rigidbody.linearVelocity = velocity;
        }
    }

    public class UnityRaycastBlock : BlockBase
    {
        public object? O { get; set; }
        public object? D { get; set; }

        public override object? ReturnExecute()
        {
            Vector3 origin = BlockValueResolver.ResolveVector3(O, null);
            Vector3 direction = BlockValueResolver.ResolveVector3(D, null);

            bool hit = Physics.Raycast(origin, direction);
            LogManager.Debug($"Raycast from {origin} dir {direction} → hit={hit}.");
            return hit;
        }

        public override object? ReturnExecute(object obj)
        {
            return ReturnExecute();
        }
    }
}