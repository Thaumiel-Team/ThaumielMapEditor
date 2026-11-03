// -----------------------------------------------------------------------
// <copyright file="WorkstationInteractPatch.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU Lesser General Public License v3.0 (LGPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using HarmonyLib;
using InventorySystem.Items.Firearms.Attachments;
using ThaumielMapEditor.API.Blocks.Definitions;

namespace ThaumielMapEditor.HarmonyPatches
{
    [HarmonyPatch]
    public static class WorkstationInteractPatch
    {
        [HarmonyPatch(typeof(WorkstationController), nameof(WorkstationController.ServerInteract))]
        public static bool Prefix(WorkstationController __instance, ReferenceHub ply, byte colliderId)
        {
            if (ply == null)
                return true;

            if (!Workstation.Server.WorkstationCache.TryGetValue(__instance, out var workstation))
                return true;

            if (!workstation.AllowInteractions)
                return false;

            if (workstation.AllowedRoles != null && workstation.AllowedRoles.Count > 0)
            {
                if (!workstation.AllowedRoles.Contains(ply.roleManager.CurrentRole.RoleTypeId))
                {
                    return false;
                }
            }

            return true;
        }
    }
}