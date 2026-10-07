// -----------------------------------------------------------------------
// <copyright file="ToolGunUtils.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using LabApi.Features.Wrappers;
using System;
using System.Linq;
using ThaumielMapEditor.API.Blocks;
using ThaumielMapEditor.API.Blocks.ClientSide;
using ThaumielMapEditor.API.Blocks.ServerObjects;
using ThaumielMapEditor.API.Data;
using ThaumielMapEditor.API.Helpers;
using ThaumielMapEditor.ToolGun.Utilities.Settings;
using UnityEngine;

namespace ThaumielMapEditor.ToolGun.Utilities
{
    [GitBookPage("ToolGun/Utilities")]
    public static class ToolGunUtils
    {
        private const float SelectDistance = 50f;

        public static bool TrySendRayCast(Player player, out RaycastHit value)
        {
            value = default;

            try
            {
                if (player == null)
                    return false;

                Transform camera = player.Camera.transform;
                if (camera.forward.sqrMagnitude < 1e-6f)
                    return false;

                return Physics.Raycast(camera.position + camera.forward, camera.forward, out value, SelectDistance);
            }
            catch
            {
                value = default;
                return false;
            }
        }

        /// <summary>
        /// Raycasts from the player's camera and selects the first block hit, storing it in <see cref="ToolGunSettings.SelectedServer"/> or <see cref="ToolGunSettings.SelectedClient"/>.
        /// </summary>
        /// <returns><see langword="true"/> if a block was hit and selected.</returns>
        public static bool TrySelectAtAim(Player player, ToolGunSettings settings)
        {
            if (!TryGetAimedObject(player, out ServerObject? server, out ClientObject? client))
                return false;

            client = server != null ? null : client;
            if (ReferenceEquals(settings.SelectedServer, server) && ReferenceEquals(settings.SelectedClient, client))
                return true;

            StopSelectionDraw(player, settings);
            PickupPreview.Hide(settings);
            settings.SelectedServer = server;
            settings.SelectedClient = client;
            if (server is PickupObject pickup)
                PickupPreview.Show(settings, pickup);
                
            StartSelectionDraw(player, settings);
            ToolGunMenu.Refresh(player);
            
            if (ToolGunMenu.IsModifierMenu(player))
                ToolGunMenu.SyncPositionSliders(player);

            return true;
        }

        /// <summary>
        /// Clears the current selection and refreshes the open Object Modifier menu, if any.
        /// </summary>
        public static void ClearSelection(Player player, ToolGunSettings settings)
        {
            if (settings.SelectedServer == null && settings.SelectedClient == null)
                return;

            StopSelectionDraw(player, settings);
            PickupPreview.Hide(settings);
            settings.SelectedServer = null;
            settings.SelectedClient = null;
            ToolGunMenu.Refresh(player);
        }

        private static void StartSelectionDraw(Player player, ToolGunSettings settings)
        {
            settings.SelectedServer?.Draw(player);
            settings.SelectedClient?.Draw(player);
        }

        private static void StopSelectionDraw(Player player, ToolGunSettings settings)
        {
            settings.SelectedServer?.StopDraw(player);
            settings.SelectedClient?.StopDraw(player);
        }

        public static void ClearAllSelectionsForPlayer(Player player)
        {
            if (player == null)
                return;

            foreach (ToolGunSettings settings in ToolGun.SettingsBySerial.Values.ToArray())
            {
                if (settings == null)
                    continue;

                if (settings.SelectedServer == null && settings.SelectedClient == null)
                    continue;

                if (settings.Owner != null && !ReferenceEquals(settings.Owner, player))
                    continue;

                StopSelectionDraw(player, settings);
                PickupPreview.Hide(settings);
                settings.SelectedServer = null;
                settings.SelectedClient = null;
            }

            DrawableLinesHelper.StopDrawsForPlayer(player);
        }

        public static bool TryGetObjectAtPoint(RaycastHit hit, out ServerObject? server, out ClientObject? client)
        {
            server = null;
            client = null;

            if (hit.collider == null)
                return false;

            Collider collider = hit.collider;

            Transform? start;

            try
            {
                start = collider.transform;
            }
            catch
            {
                return false;
            }

            if (start == null)
                return false;

            foreach (ServerObject obj in ServerObject.SpawnedObjects.ToArray())
            {
                if (obj.Object == null)
                    continue;

                for (Transform? t = start; t != null; t = t.parent)
                {
                    if (t.gameObject == obj.Object)
                    {
                        server = obj;
                        return true;
                    }
                }
            }

            foreach (SchematicData schematic in Loader.SpawnedSchematics.ToArray())
            {
                if (schematic == null)
                    continue;

                foreach (ClientObject obj in schematic.SpawnedClientObjects.ToArray())
                {
                    if (obj == null)
                        continue;

                    if (obj is PrimitiveObject prim && prim.ServerCollider != null && collider == prim.ServerCollider)
                    {
                        client = obj;
                        return true;
                    }

                    Collider[] colliders = obj.ServerColliders;
                    if (colliders == null || colliders.Length == 0)
                        continue;

                    if (Array.IndexOf(colliders, collider) >= 0)
                    {
                        client = obj;
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool TryGetAimedObject(Player player, out ServerObject? server, out ClientObject? client)
        {
            server = null;
            client = null;

            if (player == null || player.IsDestroyed)
                return false;

            if (!TrySendRayCast(player, out RaycastHit hit))
                return false;

            if (TryGetObjectAtPoint(hit, out server, out client))
                return true;

            return false;
        }
    }
}