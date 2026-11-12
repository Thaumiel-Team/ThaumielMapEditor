// -----------------------------------------------------------------------
// <copyright file="AlphaWarheadCanBeDetonatedPatch.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU Lesser General Public License v3.0 (LGPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using MapGeneration;

namespace ThaumielMapEditor.HarmonyPatches
{
    [HarmonyPatch(typeof(AlphaWarheadController), nameof(AlphaWarheadController.CanBeDetonated))]
    public static class AlphaWarheadCanBeDetonatedPatch
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            CodeMatcher matcher = new CodeMatcher(instructions, generator)
                .MatchStartForward(CodeMatch.Calls(AccessTools.Method(typeof(RoomUtils), nameof(RoomUtils.GetZone))));

            if (!matcher.IsValid)
            {
                Logger.Warn($"Could not find the {nameof(RoomUtils.GetZone)} call inside {nameof(AlphaWarheadController.CanBeDetonated)}, the warhead will use its default behavior.");
                return instructions;
            }

            return matcher
                .SetOperandAndAdvance(AccessTools.Method(typeof(AlphaWarheadCanBeDetonatedPatch), nameof(GetZoneReplacement)))
                .InstructionEnumeration();
        }

        private static FacilityZone GetZoneReplacement(Vector3 pos)
        {
            FacilityZone zone = pos.GetZone();
            if (zone != FacilityZone.None)
                return zone;

            RoomIdentifier? closestRoom = null;
            float closestSqrDistance = float.MaxValue;

            foreach (RoomIdentifier room in RoomIdentifier.AllRoomIdentifiers)
            {
                if (room == null)
                    continue;

                float sqrDistance = (room.WorldspaceBounds.ClosestPoint(pos) - pos).sqrMagnitude;
                if (sqrDistance >= closestSqrDistance)
                    continue;

                closestSqrDistance = sqrDistance;
                closestRoom = room;
            }

            return closestRoom?.Zone ?? zone;
        }
    }
}