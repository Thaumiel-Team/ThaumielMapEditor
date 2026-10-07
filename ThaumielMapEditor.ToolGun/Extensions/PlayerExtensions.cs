// -----------------------------------------------------------------------
// <copyright file="PlayerExtensions.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using LabApi.Features.Wrappers;
using ThaumielMapEditor.ToolGun.Utilities;

namespace ThaumielMapEditor.ToolGun.Extensions
{
    public static class PlayerExtensions
    {
        public static bool TryGetToolGunSettings(this Player player, out ToolGunSettings settings)
        {
            settings = GetToolGunSettings(player)!;
            return settings != null;
        }

        public static ToolGunSettings? GetToolGunSettings(this Player player)
        {
            if (player.CurrentItem == null)
                return null;

            if (!ToolGun.SettingsBySerial.TryGetValue(player.CurrentItem.Serial, out var settings))
                return null;

            return settings;
        }
    }
}