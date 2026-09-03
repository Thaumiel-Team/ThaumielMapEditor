// -----------------------------------------------------------------------
// <copyright file="TeleporterHandler.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using LabApi.Features.Wrappers;
using LabApiExtensions.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using ThaumielMapEditor.API.Blocks;
using ThaumielMapEditor.API.Blocks.ServerObjects;
using ThaumielMapEditor.API.Enums;
using ThaumielMapEditor.API.Helpers;
using UnityEngine;

namespace ThaumielMapEditor.API.Components
{
    public class TeleporterHandler : TriggerHandler
    {
        /// <summary>
        /// Gets the <see cref="TeleporterObject"/> this handler is managing.
        /// </summary>
        public TeleporterObject? Teleporter { get; private set; }

        /// <summary>
        /// Tracks per-player cooldown expiry times.
        /// </summary>
        public Dictionary<Player, float> PlayerCooldowns = [];

        /// <summary>
        /// Tracks per-pickup cooldown expiry times.
        /// </summary>
        public Dictionary<Pickup, float> PickupCooldowns = [];

        private float _globalPlayerCooldownEnd;
        private float _globalPickupCooldownEnd;

        private readonly HashSet<Player> _playersInside = [];
        private readonly HashSet<Pickup> _pickupsInside = [];
        private readonly HashSet<Projectile> _projectilesInside = [];

        /// <summary>
        /// Minimum forced cooldown duration to prevent teleport loops, in seconds.
        /// </summary>
        private const float MinForcedCooldown = 1.5f;

        public void Init(TeleporterObject teleporter)
        {
            if (Teleporter != null)
            {
                LogManager.Warn($"TeleporterHandler re-init ignored for '{teleporter.Id}'.");
                return;
            }

            Teleporter = teleporter;
        }

        private void OnDestroy()
        {
            PlayerCooldowns.Clear();
            PickupCooldowns.Clear();
            _playersInside.Clear();
            _pickupsInside.Clear();
            _projectilesInside.Clear();
        }

        public override void OnPlayerEntered(Player player)
        {
            if (Teleporter == null)
                return;

            if (IsOnCooldown(player))
                return;

            if (!HasFlagFast(TeleporterFlags.AllowPlayers))
                return;

            if (!_playersInside.Add(player))
                return;

            if (!IsRoleAllowed(player))
            {
                _playersInside.Remove(player);
                return;
            }

            TeleporterObject? target = FindTargetTeleporter();
            if (target == null)
            {
                LogManager.Warn($"Teleporter {Teleporter.Id} could not find target teleporter.");
                _playersInside.Remove(player);
                return;
            }

            ApplyCooldown(player);
            if (target.TeleporterHandler != null)
            {
                target.TeleporterHandler.ForceCooldown(player, GetForcedCooldownDuration());
            }
            else
                LogManager.Warn($"Teleporter target '{target.Id}' has no handler; skipping forced cooldown.");

            player.Position = target.Position;
            LogManager.Debug($"Player {player.Nickname} teleported from {Teleporter.Id} to {target.Id}");
        }

        public override void OnPickupEntered(Pickup pickup)
        {
            if (pickup is Projectile proj)
                OnProjectileEntered(proj);

            if (Teleporter == null)
                return;

            if (!HasFlagFast(TeleporterFlags.AllowPickups))
                return;

            if (IsOnCooldown(pickup))
                return;

            if (!_pickupsInside.Add(pickup))
                return;

            TeleporterObject? target = FindTargetTeleporter();
            if (target == null)
            {
                LogManager.Warn($"Teleporter {Teleporter.Id} could not find target teleporter.");
                _pickupsInside.Remove(pickup);
                return;
            }

            ApplyPickupCooldown(pickup);
            if (target.TeleporterHandler != null)
            {
                target.TeleporterHandler.ForceCooldown(pickup, GetForcedCooldownDuration());
            }
            else
                LogManager.Warn($"Teleporter target '{target.Id}' has no handler; skipping forced cooldown.");

            pickup.Position = target.Position;
            LogManager.Debug($"Pickup {pickup.Type} teleported from {Teleporter.Id} to {target.Id}");
        }

        public void OnProjectileEntered(Projectile projectile)
        {
            if (Teleporter == null)
                return;

            if (!HasFlagFast(TeleporterFlags.AllowProjectiles))
                return;

            if (IsOnCooldown(projectile))
                return;

            if (!_projectilesInside.Add(projectile))
                return;

            TeleporterObject? target = FindTargetTeleporter();
            if (target == null)
            {
                LogManager.Warn($"Teleporter {Teleporter.Id} could not find target teleporter.");
                _projectilesInside.Remove(projectile);
                return;
            }

            ApplyPickupCooldown(projectile);
            if (target.TeleporterHandler != null)
            {
                target.TeleporterHandler.ForceCooldown(projectile, GetForcedCooldownDuration());
            }
            else
                LogManager.Warn($"Teleporter target '{target.Id}' has no handler; skipping forced cooldown.");

            projectile.Position = target.Position;
            LogManager.Debug($"Projectile {projectile.Type} teleported from {Teleporter.Id} to {target.Id}");
        }

        public override void OnPlayerExited(Player player)
        {
            _playersInside.Remove(player);
        }

        public override void OnPickupExited(Pickup pickup)
        {
            if (pickup is Projectile proj)
                OnProjectileExited(proj);

            _pickupsInside.Remove(pickup);
        }

        public void OnProjectileExited(Projectile projectile)
        {
            _projectilesInside.Remove(projectile);
        }

        [Obsolete("Use ForceCooldown instead. This will be removed in release.")]
        public void ForcePlayerCooldown(Player player, float duration = 1f)
        {
            ForceCooldown(player, duration);
        }

        /// <summary>
        /// Forces a cooldown on a player to prevent immediate return teleportation.
        /// </summary>
        public void ForceCooldown(Player player, float duration = MinForcedCooldown)
        {
            float expiry = Time.time + duration;
            if (!PlayerCooldowns.TryGetValue(player, out float existing) || existing < expiry)
                PlayerCooldowns[player] = expiry;
        }

        /// <summary>
        /// Forces a cooldown on a pickup to prevent immediate return teleportation.
        /// </summary>
        public void ForceCooldown(Pickup pickup, float duration = MinForcedCooldown)
        {
            float expiry = Time.time + duration;
            if (!PickupCooldowns.TryGetValue(pickup, out float existing) || existing < expiry)
                PickupCooldowns[pickup] = expiry;
        }

        /// <summary>
        /// Calculates the forced cooldown duration, ensuring it's at least the minimum and at least as long as the source teleporter's cooldown.
        /// </summary>
        private float GetForcedCooldownDuration()
        {
            if (Teleporter == null)
                return MinForcedCooldown;

            return Math.Max(MinForcedCooldown, Teleporter.CoolDown);
        }

        private bool IsRoleAllowed(Player player)
        {
            if (Teleporter == null)
                return false;

            return Teleporter.AllowedRoles.Count == 0 || Teleporter.AllowedRoles.Contains(player.Role);
        }

        private bool IsOnCooldown(Player player)
        {
            if (Teleporter == null)
                return false;

            if (Teleporter.CoolDown <= 0f && !PlayerCooldowns.ContainsKey(player))
                return false;

            if (Teleporter.PerPlayerCooldown || PlayerCooldowns.ContainsKey(player))
            {
                if (PlayerCooldowns.TryGetValue(player, out float expiry))
                {
                    if (Time.time >= expiry)
                    {
                        PlayerCooldowns.Remove(player);
                        return false;
                    }

                    return true;
                }

                return false;
            }

            return Time.time < _globalPlayerCooldownEnd;
        }

        private bool IsOnCooldown(Pickup pickup)
        {
            if (Teleporter == null)
                return false;

            if (Teleporter.CoolDown <= 0f && !PickupCooldowns.ContainsKey(pickup))
                return false;

            if (Teleporter.PerPlayerCooldown || PickupCooldowns.ContainsKey(pickup))
            {
                if (PickupCooldowns.TryGetValue(pickup, out float expiry))
                {
                    if (Time.time >= expiry)
                    {
                        PickupCooldowns.Remove(pickup);
                        return false;
                    }

                    return true;
                }

                return false;
            }

            return Time.time < _globalPickupCooldownEnd;
        }

        private void ApplyCooldown(Player player)
        {
            if (Teleporter == null)
                return;

            float duration = Math.Max(Teleporter.CoolDown, MinForcedCooldown);

            if (Teleporter.PerPlayerCooldown)
            {
                float expiry = Time.time + duration;
                if (!PlayerCooldowns.TryGetValue(player, out float existing) || existing < expiry)
                    PlayerCooldowns[player] = expiry;
            }
            else
                _globalPlayerCooldownEnd = Time.time + duration;
        }

        private void ApplyPickupCooldown(Pickup pickup)
        {
            if (Teleporter == null)
                return;

            float duration = Math.Max(Teleporter.CoolDown, MinForcedCooldown);

            if (Teleporter.PerPlayerCooldown)
            {
                float expiry = Time.time + duration;
                if (!PickupCooldowns.TryGetValue(pickup, out float existing) || existing < expiry)
                    PickupCooldowns[pickup] = expiry;
            }
            else
                _globalPickupCooldownEnd = Time.time + duration;
        }

        public bool HasFlagFast(TeleporterFlags flag)
        {
            if (Teleporter == null)
                return false;

            return (Teleporter.Flags & flag) != 0;
        }

        private TeleporterObject? FindTargetTeleporter()
        {
            if (Teleporter == null)
                return null;

            if (Teleporter.Targets.Count == 0)
                return null;

            Guid targetId = Teleporter.Targets.GetRandom();
            if (!TeleporterObject.Teleporters.TryGetValue(targetId, out TeleporterObject? target))
                return null;

            return target;
        }
    }
}