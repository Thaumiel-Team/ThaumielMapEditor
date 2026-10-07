// -----------------------------------------------------------------------
// <copyright file="Main.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

global using ThaumielMapEditor.API.Attributes;

using System;
using System.Linq;
using CustomItemsAPI;
using LabApi.Features;
using LabApi.Loader.Features.Plugins;
using LabApi.Loader.Features.Plugins.Enums;
using RemoteAdmin;
using ThaumielMapEditor.Commands;
using ThaumielMapEditor.ToolGun.Commands;
using ThaumielMapEditor.ToolGun.Utilities;
using ThaumielMapEditor.ToolGun.Utilities.Settings;

namespace ThaumielMapEditor.ToolGun
{
    public class Main : Plugin<Config>
    {
        public override string Name => "Thaumiel ToolGun";

        public override string Description => "The Tool gun for Thaumiel Map Editor.";

        public override string Author => "Mr. Baguetter";

        public override Version Version { get; } = new(0, 1, 0);

        public override Version RequiredApiVersion { get; } = LabApiProperties.CurrentVersion;

        public override LoadPriority Priority { get; } = LoadPriority.Low;

        public static Main Instance { get; private set; } = null!;

        public override void Enable()
        {
            Instance = this;
            ToolGunMenu.Register();
            PickupPreview.Register();
            CustomItems.RegisterCustomItem(typeof(ToolGun));

            Parent? parent = CommandProcessor.RemoteAdminCommandHandler.AllCommands.OfType<Parent>().FirstOrDefault();
            parent?.RegisterSubCommand(new GiveToolGun());
            parent?.RegisterSubCommand(new RemoveToolGun());
        }

        public override void Disable()
        {
            Instance = null!;
            ToolGunMenu.Unregister();
            PickupPreview.Unregister();

            Parent? parent = CommandProcessor.RemoteAdminCommandHandler.AllCommands.OfType<Parent>().FirstOrDefault();
            parent?.UnregisterSubCommand<GiveToolGun>();
            parent?.UnregisterSubCommand<RemoveToolGun>();
        }
    }
}
