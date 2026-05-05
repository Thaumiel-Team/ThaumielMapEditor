// -----------------------------------------------------------------------
// <copyright file="ColliderTrigger.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using LabApi.Features.Wrappers;
using Mirror;
using SecretLabNAudio.Core;
using SecretLabNAudio.Core.Extensions;
using ThaumielMapEditor.API.Blocks;
using ThaumielMapEditor.API.Components.Tools.Helpers;
using ThaumielMapEditor.API.Data;
using ThaumielMapEditor.API.Enums;
using ThaumielMapEditor.API.Extensions;
using ThaumielMapEditor.API.Helpers;
using ThaumielMapEditor.Commands;
using UnityEngine;
using static ThaumielMapEditor.API.Components.Tools.Helpers.RunCommand;
using Warhead = ThaumielMapEditor.API.Components.Tools.Helpers.Warhead;
using LabWarhead = LabApi.Features.Wrappers.Warhead;
using CustomPlayerEffects;
using MEC;
using System.Linq;
using ThaumielMapEditor.API.Serialization;

namespace ThaumielMapEditor.API.Components.Tools
{
    public class ColliderTrigger : ToolBase
    {
        internal static Dictionary<Player, HashSet<StatusEffectBase>> PlayerEffectCache = [];
        
        public Vector3 Bounds;

#pragma warning disable CS8618
        public ColliderClasses OnEntered;

        public ColliderClasses OnExited;

        public Permission Permissions;

        public GameObject ColliderObject;

        public Collider Collider;
#pragma warning restore CS8618

        public override ToolType Type => ToolType.ColliderTrigger;

        public override void Init(ServerObject obj, SchematicData schem, Dictionary<string, object> properties)
        {
            base.Init(obj, schem, properties);
            ParseValues(properties);
            ColliderObject = new($"{Object!.Object!.name} - ColliderObject");
            ColliderObject.transform.SetParent(Object.Object.transform);
            Collider = ColliderObject.AddComponent<Collider>();
            Collider.isTrigger = true;
        }

        private void OnDestroy()
        {
            if (!OnExited.Blocky.IsEmpty())
            {
                foreach (BlockyPayload blocky in OnExited.Blocky)
                {
                    Schematic?.Executor?.Execute(ArgumentsParser.Load(blocky), null!, EventType.OnDestroyed);
                }
            }

            if (!OnEntered.Blocky.IsEmpty())
            {
                foreach (BlockyPayload blocky in OnEntered.Blocky)
                {
                    Schematic?.Executor?.Execute(ArgumentsParser.Load(blocky), null!, EventType.OnDestroyed);
                }
            }
        }

        public void ParseValues(Dictionary<string, object> properties)
        {
            if (properties.TryConvertValue<ColliderClasses>("OnEntered", out var entered))
                OnEntered = entered;

            if (properties.TryConvertValue<ColliderClasses>("OnExited", out var exit))
                OnExited = exit;

            if (properties.TryConvertValue<Permission>("Permission", out var perms))
                Permissions = perms;

            if (properties.TryConvertValue<Vector3>("Bounds", out var bounds))
                Bounds = bounds;
        }

        private void OnTriggerEnter(Collider other)
        {
            GameObject? root = other.GetComponentInParent<NetworkIdentity>()?.gameObject;
            if (root == null)
                return;

            if (!Player.TryGet(root, out var player))
                return;

            if (!Permissions.AllowedRoles.IsEmpty() && !Permissions.AllowedRoles.Contains(player.Role))
                return;

            HandleEffect(OnEntered, player);
            HandleCommand(OnEntered, player);
            HandleAudio(OnEntered, player);
            HandleAnimation(OnEntered, player);
            HandleWarhead(OnEntered, player);
            HandleCassie(OnEntered, player);
            HandleBlocks(OnEntered, player, EventType.OnTriggerEntered);
        }

        private void OnTriggerExit(Collider other)
        {
            GameObject? root = other.GetComponentInParent<NetworkIdentity>()?.gameObject;
            if (root == null)
                return;

            if (!Player.TryGet(root, out var player))
                return;

            if (!Permissions.AllowedRoles.IsEmpty() && !Permissions.AllowedRoles.Contains(player.Role))
                return;

            HandleEffect(OnExited, player);
            HandleCommand(OnExited, player);
            HandleAudio(OnExited, player);
            HandleAnimation(OnExited, player);
            HandleWarhead(OnExited, player);
            HandleCassie(OnExited, player);
            HandleBlocks(OnExited, player, EventType.OnTriggerExited);
        }

        private void HandleBlocks(ColliderClasses classes, Player player, EventType eventType)
        {
            foreach (BlockyPayload blocky in classes.Blocky)
            {
                List<object> blocks = ArgumentsParser.Load(blocky);
                Schematic?.Executor?.Execute(blocks, player, eventType);
            }
        }

        private void HandleCassie(ColliderClasses classes, Player player)
        {
            foreach (SendCassieMessage message in classes.SendCassieMessage)
            {
                message.ValidateLines();
                Announcer.Message(message.Message, message.CustomSubtitles, message.PlayBackground, message.Priority, message.GlitchScale);
            }
        }

        private void HandleWarhead(ColliderClasses classes, Player player)
        {
            foreach (Warhead warhead in classes.Warhead)
            {
                switch (warhead.Action)
                {
                    case WarheadAction.Start:
                        LabWarhead.Start(suppressSubtitles: warhead.SuppressSubtitles, activator: player);
                        break;

                    case WarheadAction.Stop:
                        LabWarhead.Stop(activator: player);
                        break;
                    
                    case WarheadAction.Detonate:
                        LabWarhead.Detonate();
                        break;

                    case WarheadAction.Lock:
                        LabWarhead.IsLocked = true;
                        break;

                    case WarheadAction.Unlock:
                        LabWarhead.IsLocked = false;
                        break;

                    case WarheadAction.Disable:
                        LabWarhead.LeverStatus = false;
                        break;

                    case WarheadAction.Enable:
                        LabWarhead.LeverStatus = true;
                        break;
                }
            }
        }

        private void HandleAnimation(ColliderClasses classes, Player player)
        {
            foreach (PlayAnimation play in classes.PlayAnimation)
            {
                Schematic?.AnimationController.Play(play.ResolvedAnimationName);
            }
        }

        private void HandleEffect(ColliderClasses classes, Player player)
        {
            if (!PlayerEffectCache.ContainsKey(player))
                PlayerEffectCache[player] = [];

            foreach (GiveEffect give in classes.GiveEffect)
            {
                if (!player.TryGetEffect(give.Effect.ToString(), out var effectBase))
                {
                    LogManager.Warn($"Invalid EffectType {give.Effect} for player {player.DisplayName} - {player.PlayerId}");
                    continue;
                }

                if (effectBase.IsEnabled)
                    PlayerEffectCache[player].Add(effectBase);

                player.EnableEffect(effectBase, (byte)give.Intensity, give.Duration, true);
            }

            foreach (RemoveEffect remove in classes.RemoveEffect)
            {
                if (!player.TryGetEffect(remove.Effect.ToString(), out var effectBase))
                {
                    LogManager.Warn($"Invalid EffectType {remove.Effect} for player {player.DisplayName} - {player.PlayerId}");
                    continue;
                }

                player.DisableEffect(effectBase);
                if (PlayerEffectCache.TryGetValue(player, out var effects))
                {
                    Timing.CallDelayed(Timing.WaitForOneFrame, () =>
                    {
                        StatusEffectBase? status = effects.FirstOrDefault(e => e == effectBase);
                        if (status == null)
                            return;
                        
                        player.EnableEffect(status, status._intensity, status._duration);
                    });
                }
            }
        }

        private void HandleCommand(ColliderClasses classes, Player player)
        {
            foreach (RunCommand command in classes.RunCommand)
            {
                command.Command
                .Replace("%id%", player.PlayerId.ToString())
                .Replace("%name%", player.DisplayName)
                .Replace("%userid%", player.UserId)
                .Replace("%role%", player.Role.ToString())
                .Replace("%health%", player.Health.ToString())
                .Replace("%maxhealth%", player.MaxHealth.ToString())
                .Replace("%room%", player.Room?.Name.ToString())
                .Replace("%position%", player.Position.ToString().Trim('(', ')'));

                switch (command.Type)
                {
                    case CommandType.Client:
                        Server.RunCommand($".{command.Command}", new SilentCommandSender());
                        break;
                    
                    case CommandType.Console:
                        Server.RunCommand($"{command.Command}", new SilentCommandSender());
                        break;
                    
                    case CommandType.RemoteAdmin:
                        Server.RunCommand($"/{command.Command}", new SilentCommandSender());
                        break;
                }
            }
        }

        private void HandleAudio(ColliderClasses classes, Player player)
        {
            foreach (PlayAudio play in classes.PlayAudio)
            {
                AudioPlayer audioPlayer = AudioPlayer.CreateDefault(parent: transform);
                audioPlayer.WithMinDistance(play.MinDistance);
                audioPlayer.WithMaxDistance(play.MaxDistance);
                audioPlayer.WithSpatial(play.IsSpatial);
                if (IsLocalFile(play.Path))
                {
                    audioPlayer.UseFile(Path.Combine(Main.Instance.Config?.AudioPath, play.Path), volume: play.Volume);
                }
                else
                    audioPlayer.UseFile(play.Path, volume: play.Volume);
            }
        }
    }
}