// -----------------------------------------------------------------------
// <copyright file="InteractToyValidatePatch.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using System;
using AdminToys;
using HarmonyLib;
using Interactables.Interobjects.DoorUtils;
using LabApi.Features.Wrappers;
using ThaumielMapEditor.API.Attributes;
using ThaumielMapEditor.API.Blocks.ServerObjects;
using ThaumielMapEditor.API.Helpers;
using static AdminToys.InvisibleInteractableToy;

namespace ThaumielMapEditor.HarmonyPatches
{
    [HarmonyPatch]
    [DoNotParse]
    public static class InteractToyValidatePatch
    {
        public static event Action<InteractionObject, Player>? OnDenied;

        [HarmonyPatch(typeof(InteractableToySearchCompletor), nameof(InteractableToySearchCompletor.ValidateStart))]
        [HarmonyPrefix]
        public static bool SearchingPrefix(InteractableToySearchCompletor __instance, ref bool __result)
        {
            if (__instance == null || __instance._target == null)
                return true;

            if (!InteractionObject.TryGetInteractionObject(__instance._target, out var interactionobj))
            {
                LogManager.Debug($"Failed to get interaction object for {__instance._target.name}.");
                return true;
            }

            Player player = Player.Get(__instance.Hub);
            if (player.IsDestroyed)
                return true;

            if (interactionobj.Permissions == DoorPermissionFlags.None || player.IsBypassEnabled)
            {
                LogManager.Debug($"Allowed player to interact with {interactionobj.Name}. Had keycard permissions.");
                return true;
            }
            
            if (player.CurrentItem == null || player.CurrentItem is not KeycardItem keycard)
            {
                LogManager.Debug($"Denied player from interacting with {interactionobj.Name}. No valid keycard.");
                OnDenied?.Invoke(interactionobj, player);
                __result = false;
                return false;
            }

            if (!keycard.Permissions.HasFlagAll(interactionobj.Permissions))
            {
                LogManager.Debug($"Denied player from interacting with {interactionobj.Name}. Insufficient keycard permissions.");
                OnDenied?.Invoke(interactionobj, player);
                __result = false;
                return false;
            }

            if (!interactionobj.AllowedRoles.IsEmpty() && !interactionobj.AllowedRoles.Contains(player.Role))
            {
                LogManager.Debug($"Denied player from interacting with {interactionobj.Name}. Incorrect role. {player.Role}");
                OnDenied?.Invoke(interactionobj, player);
                __result = false;
                return false;
            }

            return true;
        }

        [HarmonyPatch(typeof(InvisibleInteractableToy), nameof(InvisibleInteractableToy.ServerInteract))]
        [HarmonyPrefix]
        public static bool InteractingPrefix(InvisibleInteractableToy __instance, ReferenceHub ply, byte colliderId)
        {
            if (__instance == null || ply == null)
                return true;

            if (!InteractionObject.TryGetInteractionObject(__instance, out var interactionobj))
            {
                LogManager.Debug($"Failed to get interaction object for {__instance.name}.");
                return true;
            }

            Player player = Player.Get(ply);
            if (player.IsDestroyed)
                return true;

            if (interactionobj.Permissions == DoorPermissionFlags.None || player.IsBypassEnabled)
            {
                LogManager.Debug($"Allowed player to interact with {interactionobj.Name}. Had keycard permissions.");
                return true;
            }
            
            if (player.CurrentItem == null || player.CurrentItem is not KeycardItem keycard)
            {
                LogManager.Debug($"Denied player from interacting with {interactionobj.Name}. No valid keycard.");
                OnDenied?.Invoke(interactionobj, player);
                return false;
            }

            if (!keycard.Permissions.HasFlagAll(interactionobj.Permissions))
            {
                LogManager.Debug($"Denied player from interacting with {interactionobj.Name}. Insufficient keycard permissions.");
                OnDenied?.Invoke(interactionobj, player);
                return false;
            }

            if (!interactionobj.AllowedRoles.IsEmpty() && !interactionobj.AllowedRoles.Contains(player.Role))
            {
                LogManager.Debug($"Denied player from interacting with {interactionobj.Name}. Incorrect role. {player.Role}");
                OnDenied?.Invoke(interactionobj, player);
                return false;
            }

            return true;
        }
    }
}