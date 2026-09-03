// -----------------------------------------------------------------------
// <copyright file="Main.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

global using Logger = LabApi.Features.Console.Logger;
global using Quaternion = UnityEngine.Quaternion;
global using ThaumFileManager = ThaumielMapEditor.API.Helpers.FileManager;
global using Vector3 = UnityEngine.Vector3;

using HarmonyLib;
using LabApi.Features;
using LabApi.Loader.Features.Paths;
using LabApi.Loader.Features.Plugins;
using LabApi.Loader.Features.Plugins.Enums;
using System;
using System.IO;
using ThaumielMapEditor.API.Attributes;
using ThaumielMapEditor.API.Helpers;
using ThaumielMapEditor.Events;

namespace ThaumielMapEditor
{
    [DoNotParse]
    public class Main : Plugin<Config>
    {
        public override string Name => "Thaumiel Map Editor";
        public override string Description => ":3";
        public override string Author => "Mr. Baguetter";
        public override Version Version => new(0, 8, 0);
        public override Version RequiredApiVersion => LabApiProperties.CurrentVersion;
        public override LoadPriority Priority => LoadPriority.Medium;
        public string HarmonyId { get; private set; } = string.Empty;

#pragma warning disable CS8618
        public Harmony harmony;
        public static Main Instance;
#pragma warning restore CS8618

        public override void Enable()
        {
            Instance = this;
            
            Loader.Init();

            PlayerHandler.Register();
            ServerHandler.Register();
            PrimitiveHandler.Register();

            if (string.IsNullOrEmpty(Config?.AudioPath))
            {
                ThaumFileManager.TryCreateDirectory("Audio");
                Config?.AudioPath = Path.Combine(PathManager.Configs.ToString(), "Thaumiel", "Audio");
                SaveConfig();
            }
            
            try
            {
                HarmonyId = $"MrBaguetter_TME_{Guid.NewGuid()}";
                harmony = new(HarmonyId);
                harmony.PatchAll();
            }
            catch (HarmonyException ex)
            {
                LogManager.Error($"Failed to enable Harmony patches! \nMessage: {ex.Message}\n InnerException: {ex.InnerException?.Message}\n Full Exception: {ex}\n");

                if (ex.InnerException != null)
                    LogManager.Error($"Inner Exception Details: \n{ex.InnerException}");
            }
            catch (Exception ex)
            {
                LogManager.Error($"Unexpected error while enabling Harmony patches: \n{ex}");
            }
        }

        public override void Disable()
        {
            PlayerHandler.Unregister();
            ServerHandler.Unregister();
            PrimitiveHandler.Unregister();
            SchematicHandler.Unregister();

            try
            {
                Loader.Cleanup();
            }
            catch (Exception ex)
            {
                Logger.Error($"[ThaumielMapEditor] Exception during cleanup on disable: {ex.Message}");
            }

            try
            {
                harmony?.UnpatchAll(HarmonyId);
            }
            catch (Exception ex)
            {
                Logger.Error($"[ThaumielMapEditor] Exception while unpatching Harmony on disable: {ex.Message}");
            }

            Instance = null!;
        }
    }
}
