// -----------------------------------------------------------------------
// <copyright file="ActionBlocks.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using System.Reflection;
using LabApi.Features.Wrappers;
using ThaumielMapEditor.API.Enums;
using SecretLabNAudio.Core;
using SecretLabNAudio.Core.Extensions;
using System.Linq;
using HarmonyLib;

namespace ThaumielMapEditor.API.Helpers.BlockParser
{
    public class RunMethodBlock : BlockBase
    {
        public string FullMethodName { get; set; } = string.Empty;
        public object?[] Args { get; set; } = [];

        public override void Execute()
        {
            MethodInfo method = AccessTools.Method(FullMethodName);

            if (method == null)
            {
                LogManager.Warn($"Could not find method: '{FullMethodName}'. Ensure it is 'Type:MethodName'.");
                return;
            }

            LogManager.Debug($"Invoking '{method.Name}'.");
            method.Invoke(null, Args);
        }
    }

    public class RunMethodInstanceBlock : BlockBase
    {
        public object? Instance { get; set; }
        public string MethodName { get; set; } = string.Empty;
        public object?[] Args { get; set; } = [];

        public override void Execute()
        {
            if (Instance == null)
            {
                LogManager.Warn("Instance is null.");
                return;
            }

            MethodInfo method = AccessTools.Method(Instance.GetType(), MethodName);
            if (method == null)
            {
                LogManager.Warn($"Could not find method '{MethodName}' on '{Instance.GetType().FullName}'.");
                return;
            }

            LogManager.Debug($"Invoking instance method '{method.Name}' with {Args.Length} arg(s).");
            method.Invoke(Instance, Args);
        }
    }

    public class PlayAnimationBlock : BlockBase
    {
        public string AnimationName { get; set; } = string.Empty;

        public override void Execute()
        {
            LogManager.Debug($"Playing animation '{AnimationName}'.");
            Executor?.Schematic.AnimationController.Play(AnimationName);
        }
    }

    public class PlayAudioBlock : BlockBase
    {
        public string Path { get; set; } = string.Empty;
        public float Volume { get; set; } = 1f;
        public float MinDistance { get; set; } = 1f;
        public float MaxDistance { get; set; } = 20f;
        public bool IsSpatial { get; set; }

        public override void Execute()
        {
            LogManager.Debug($"Playing audio '{Path}' vol={Volume} spatial={IsSpatial} min={MinDistance} max={MaxDistance}.");
            AudioPlayer audioPlayer = AudioPlayer.CreateDefault();
            audioPlayer.WithMinDistance(MinDistance);
            audioPlayer.WithMaxDistance(MaxDistance);
            audioPlayer.WithSpatial(IsSpatial);
            if (IsLocalFile(Path))
            {
                audioPlayer.UseFile(System.IO.Path.Combine(Main.Instance.Config?.AudioPath, Path), volume: Volume);
            }
            else
                audioPlayer.UseFile(Path, volume: Volume);
        }

        internal bool IsLocalFile(string path = null!)
        {
            if (string.IsNullOrEmpty(path))
            {
                if (System.IO.Path.IsPathRooted(Path))
                    return false;
            }
            else if (System.IO.Path.IsPathRooted(path))
                return false;

            return true;
        }
    }

    public class SendCassieBlock : BlockBase
    {
        public string Message { get; set; } = string.Empty;
        public string CustomSubtitles { get; set; } = string.Empty;
        public bool PlayBackground { get; set; }
        public float Priority { get; set; }
        public float GlitchScale { get; set; }

        public override void Execute()
        {
            LogManager.Debug($"Sending CASSIE message '{Message}'.");
            Announcer.Message(Message, CustomSubtitles, PlayBackground, (int)Priority, GlitchScale);
        }
    }

    public class RunCommandBlock : BlockBase
    {
        public string CommandType { get; set; } = string.Empty;
        public string Command { get; set; } = string.Empty;

        public override void Execute(Player player)
        {
            LogManager.Debug($"Running {CommandType} command: '{Command}'.");

            switch (CommandType)
            {
                case "RemoteAdmin":
                    Server.RunCommand($"/{Command}");
                    break;

                case "Client":
                    Server.RunCommand($".{Command}");
                    break;

                case "Console":
                    Server.RunCommand(Command);
                    break;
                    
                default:
                    LogManager.Warn($"Unknown command type '{CommandType}'.");
                    break;
            }
        }
    }

    public class GiveEffectBlock : BlockBase
    {
        public EffectType Effect { get; set; }
        public byte Intensity { get; set; } = 1;
        public float Duration { get; set; } = 5f;

        public override void Execute(Player player)
        {
            if (!player.TryGetEffect(Effect.ToString(), out var effectBase))
                return;

            LogManager.Debug($"Giving effect '{Effect}' intensity={Intensity} duration={Duration} to '{player.DisplayName}'.");
            player.EnableEffect(effectBase, Intensity, Duration);
        }
    }

    public class RemoveEffectBlock : BlockBase
    {
        public EffectType Effect { get; set; }
        public byte Intensity { get; set; } = 1;

        public override void Execute(Player player)
        {
            if (!player.TryGetEffect(Effect.ToString(), out var effectBase))
                return;

            LogManager.Debug($"Removing effect '{Effect}' from '{player.DisplayName}'.");
            player.DisableEffect(effectBase);
        }
    }

    public class ActionGiveItemBlock : BlockBase
    {
        public ItemType Item { get; set; }
        public int Count { get; set; } = 1;

        public override void Execute(Player player)
        {
            for (int i = 0; i < Count; i++)
            {
                player.AddItem(Item);
            }

            LogManager.Debug($"Gave {Count}x '{Item}' to '{player.DisplayName}'.");
        }
    }

    public class ActionRemoveItemBlock : BlockBase
    {
        public ItemType Item { get; set; }
        public int Count { get; set; } = 1;

        public override void Execute(Player player)
        {
            for (int i = 0; i < Count; i++)
            {
                Item? item = player.Items.FirstOrDefault(item => item.Type == Item);
                if (item != null)
                {
                    player.RemoveItem(item);
                }
                else
                {
                    LogManager.Warn($"Player '{player.DisplayName}' does not have item '{Item}'.");
                    break;
                }
            }
        }
    }

    public class WarheadBlock : BlockBase
    {
        public WarheadAction Action { get; set; }
        public bool SuppressSubtitles { get; set; }

        public override void Execute()
        {
            LogManager.Debug($"Warhead action '{Action}' suppressSubtitles={SuppressSubtitles}.");

            switch (Action)
            {
                case WarheadAction.Start:
                    AlphaWarheadController.Singleton.StartDetonation(false, SuppressSubtitles);
                    break;

                case WarheadAction.Stop:
                    AlphaWarheadController.Singleton.CancelDetonation();
                    break;

                case WarheadAction.Detonate:
                    AlphaWarheadController.Singleton.Detonate();
                    break;

                case WarheadAction.Lock:
                    AlphaWarheadController.Singleton.IsLocked = true;
                    break;

                case WarheadAction.Unlock:
                    AlphaWarheadController.Singleton.IsLocked = false;
                    break;

                default:
                    LogManager.Warn($"Unknown warhead action '{Action}'.");
                    break;
            }
        }
    }
}