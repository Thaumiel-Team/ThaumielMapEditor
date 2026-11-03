// -----------------------------------------------------------------------
// <copyright file="Scp1507AttackPatch.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU Lesser General Public License v3.0 (LGPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using HarmonyLib;
using PlayerRoles.PlayableScps.Scp1507;
using ThaumielMapEditor.API.Components.Tools;
using ThaumielMapEditor.API.Enums;
using UnityEngine;

namespace ThaumielMapEditor.HarmonyPatches
{
    [HarmonyPatch]
    public static class Scp1507AttackPatch
    {
        private const float AttackDamage = 20f;

        [HarmonyPatch(typeof(Scp1507AttackAbility), nameof(Scp1507AttackAbility.TryAttackDoor))]
        public static void Postfix(Scp1507AttackAbility __instance, ref bool __result)
        {
            if (__result)
                return;

            if (!Physics.Raycast(__instance.Owner.PlayerCameraReference.position, __instance.Owner.PlayerCameraReference.forward, out RaycastHit hitInfo, 1.728f))
                return;

            if (!hitInfo.collider.TryGetComponent<IDestructible>(out var destructible) || destructible is not ObjectHealth health)
                return;

            if (!health.AllowedDamage.Contains(DamageType.Scp1507))
            {
                Hitmarker.SendHitmarkerDirectly(__instance.Owner, AttackDamage, hitmarkerType: HitmarkerType.Blocked);
                return;
            }

            Scp1507DamageHandler handler = new(new(__instance.Owner), AttackDamage);
            if (!destructible.Damage(AttackDamage, handler, hitInfo.point))
                return;

            Hitmarker.SendHitmarkerDirectly(__instance.Owner, AttackDamage);
            __result = true;
        }
    }
}