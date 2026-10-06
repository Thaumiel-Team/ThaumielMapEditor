// -----------------------------------------------------------------------
// <copyright file="ObjectSettingsHelper.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using LabApi.Features.Wrappers;
using ThaumielMapEditor.API.Blocks;

namespace ThaumielMapEditor.ToolGun.Utilities.Settings
{
    public static class ObjectSettingsHelper
    {
        public static bool TryGetServer<T>(Player player, out T? obj) where T : ServerObject
        {
            obj = null;

            if (!ToolGunMenu.TryGetSelection(player, out _, out var server, out _))
                return false;

            obj = server as T;
            return obj != null;
        }

        public static bool CanViewServer<T>(Player player) where T : ServerObject => ToolGunMenu.IsEquipped(player) && ToolGunMenu.IsModifierMenu(player) && TryGetServer<T>(player, out _);
    }
}
