// -----------------------------------------------------------------------
// <copyright file="ToolGunHeaders.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using SecretAPI.Features.UserSettings;

namespace ThaumielMapEditor.ToolGun.Utilities.Settings
{
    public static class ToolGunHeaders
    {
        public static readonly CustomHeader Main = new("TME ToolGun");
        public static readonly CustomHeader Keybinds = new("TME ToolGun Keybinds");
        public static readonly CustomHeader Modifier = new("Object Modifier");
        public static readonly CustomHeader Transform = new("Transform");
        public static readonly CustomHeader Primitive = new("Primitive Settings");
        public static readonly CustomHeader Light = new("Light Settings");
        public static readonly CustomHeader Text = new("Text Settings");
        public static readonly CustomHeader Door = new("Door Settings");
        public static readonly CustomHeader Workstation = new("Workstation Settings");
        public static readonly CustomHeader Interactable = new("Interactable Settings");
        public static readonly CustomHeader Clutter = new("Clutter Settings");
        public static readonly CustomHeader Locker = new("Locker Settings");
        public static readonly CustomHeader Camera = new("Camera Settings");
        public static readonly CustomHeader Waypoint = new("Waypoint Settings");
        public static readonly CustomHeader Target = new("Target Settings");
        public static readonly CustomHeader Teleporter = new("Teleporter Settings");
        public static readonly CustomHeader Speaker = new("Speaker Settings");
        public static readonly CustomHeader SpawnPoint = new("Spawn Point Settings");
        public static readonly CustomHeader Pickup = new("Pickup Settings");
        public static readonly CustomHeader Capybara = new("Capybara Settings");
        public static readonly CustomHeader Ragdoll = new("Ragdoll Spawner");
        public static readonly CustomHeader Spawn = new("Spawn Settings");
    }
}
