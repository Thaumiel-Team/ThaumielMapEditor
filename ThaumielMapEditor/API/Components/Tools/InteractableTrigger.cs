// -----------------------------------------------------------------------
// <copyright file="InteractableTrigger.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using CustomPlayerEffects;
using LabApi.Features.Wrappers;
using MEC;
using SecretLabNAudio.Core;
using SecretLabNAudio.Core.Extensions;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ThaumielMapEditor.API.Blocks;
using ThaumielMapEditor.API.Blocks.ServerObjects;
using ThaumielMapEditor.API.Components.Tools.Helpers;
using ThaumielMapEditor.API.Data;
using ThaumielMapEditor.API.Enums;
using ThaumielMapEditor.API.Extensions;
using ThaumielMapEditor.API.Helpers;
using ThaumielMapEditor.API.Serialization;
using ThaumielMapEditor.Commands;
using ThaumielMapEditor.HarmonyPatches;
using static AdminToys.InvisibleInteractableToy;
using static ThaumielMapEditor.API.Components.Tools.Helpers.RunCommand;
using LabWarhead = LabApi.Features.Wrappers.Warhead;
using Warhead = ThaumielMapEditor.API.Components.Tools.Helpers.Warhead;

namespace ThaumielMapEditor.API.Components.Tools
{
    public class InteractableTrigger : ToolBase
    {
        internal static readonly Dictionary<Player, HashSet<StatusEffectBase>> PlayerEffectCache = [];

        public Vector3 Bounds;

        public float InteractionTime;

        public ColliderShape Shape;
#pragma warning disable CS8618
        public InteractableClasses OnInteracted;

        public InteractableClasses OnInteractionDenied;
        
        public Permission Permissions;

        public InteractionObject Interactable;
#pragma warning restore CS8618

        public override ToolType Type => ToolType.InteractableTrigger;

        public override void Init(ServerObject obj, SchematicData schem, Dictionary<string, object> properties)
        {
            base.Init(obj, schem, properties);
            ParseValues(properties);
            Interactable = new()
            {
                Rotation = obj.Object!.transform.localRotation,
                Shape = Shape,
                Scale = Bounds,
                IsLocked = false,
                InteractionDuration = InteractionTime,
                Permissions = Permissions.KeycardPermissions,
                AllowedRoles = Permissions.AllowedRoles
            };

            Interactable.SpawnObject(schem);
            Interactable.Object?.transform.SetParent(obj.Object?.transform, false);
            Interactable.Object?.transform.localPosition = Vector3.zero;
            InteractionObject.OnInteracted += Interacted;
            InteractionObject.OnSearched += Interacted;
            InteractToyValidatePatch.OnDenied += Denied;
        }

        private void OnDestroy()
        {
            InteractionObject.OnInteracted -= Interacted;
            InteractionObject.OnSearched -= Interacted;
            InteractToyValidatePatch.OnDenied -= Denied;
            if (!OnInteracted.Blocky.IsEmpty())
            {
                foreach (BlockyPayload blocky in OnInteracted.Blocky)
                {
                    Schematic?.Executor?.Execute(ArgumentsParser.Load(blocky), null!, EventType.OnDestroyed);
                }
            }

            if (!OnInteractionDenied.Blocky.IsEmpty())
            {
                foreach (BlockyPayload blocky in OnInteractionDenied.Blocky)
                {
                    Schematic?.Executor?.Execute(ArgumentsParser.Load(blocky), null!, EventType.OnDestroyed);
                }
            }
        }

        private void Denied(InteractionObject obj, Player player)
        {
            if (obj != Interactable)
                return;

            HandleAnimation(OnInteractionDenied, player);
            HandleEffect(OnInteractionDenied, player);
            HandleCommand(OnInteractionDenied, player);
            HandleAudio(OnInteractionDenied, player);
            HandleWarhead(OnInteractionDenied, player);
            HandleCassie(OnInteractionDenied, player);
            HandleBlocks(OnInteractionDenied, player, EventType.OnInteractionDenied);
        }

        public void Interacted(InteractionObject obj, Player player)
        {
            if (obj != Interactable)
                return;

            HandleAnimation(OnInteracted, player);
            HandleEffect(OnInteracted, player);
            HandleCommand(OnInteracted, player);
            HandleAudio(OnInteracted, player);
            HandleWarhead(OnInteracted, player);
            HandleCassie(OnInteracted, player);
            HandleBlocks(OnInteracted, player, EventType.OnInteraction);
        }

        private void HandleBlocks(InteractableClasses classes, Player player, EventType eventType)
        {
            foreach (BlockyPayload blocky in classes.Blocky)
            {
                List<object> blocks = ArgumentsParser.Load(blocky);
                Schematic?.Executor?.Execute(blocks, player, eventType);
            }
        }

        private void HandleCassie(InteractableClasses classes, Player player)
        {
            foreach (SendCassieMessage message in classes.SendCassieMessage)
            {
                message.ValidateLines();
                Announcer.Message(message.Message, message.CustomSubtitles, message.PlayBackground, message.Priority, message.GlitchScale);
            }
        }

        private void HandleWarhead(InteractableClasses classes, Player player)
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

        private void HandleAnimation(InteractableClasses classes, Player player)
        {
            foreach (PlayAnimation play in classes.PlayAnimation)
            {
                Schematic?.AnimationController.Play(play.ResolvedAnimationName);
            }
        }

        private void HandleEffect(InteractableClasses classes, Player player)
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

        private void HandleCommand(InteractableClasses classes, Player player)
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

        private void HandleAudio(InteractableClasses classes, Player player)
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

        public void ParseValues(Dictionary<string, object> properties)
        {
            if (properties.TryConvertValue<InteractableClasses>("OnInteracted", out var interacted))
                OnInteracted = interacted;

            if (properties.TryConvertValue<InteractableClasses>("OnInteractionDenied", out var denied))
                OnInteractionDenied = denied;

            if (properties.TryConvertValue<ColliderShape>("Shape", out var shape))
                Shape = shape;

            if (properties.TryConvertValue<Permission>("Permission", out var perms))
                Permissions = perms;

            if (properties.TryConvertValue<float>("InteractionTime", out var time))
                InteractionTime = time;

            if (properties.TryConvertValue<Vector3>("Bounds", out var bounds))
                Bounds = bounds;
        }
    }
}