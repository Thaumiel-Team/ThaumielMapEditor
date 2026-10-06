// -----------------------------------------------------------------------
// <copyright file="DrawableLinesHelper.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using DrawableLine;
using LabApi.Features.Wrappers;
using MEC;
using Mirror;
using System;
using System.Collections.Generic;
using System.Linq;
using ThaumielMapEditor.API.Blocks;
using ThaumielMapEditor.API.Blocks.ClientSide;
using ThaumielMapEditor.API.Blocks.ServerObjects;
using ThaumielMapEditor.API.Enums;
using UnityEngine;
using static AdminToys.InvisibleInteractableToy;

namespace ThaumielMapEditor.API.Helpers
{
    [GitBookPage("DrawableLinesHelper")]
    public static class DrawableLinesHelper
    {
        /// <summary>
        /// Radius used for point markers when a <see cref="ServerObject"/> has no drawable volume.
        /// </summary>
        private const float MarkerRadius = 0.3f;

        /// <summary>
        /// How many frames each transmission of a persistent drawing lasts.
        /// </summary>
        private const int PersistentLifetimeFrames = 20;

        /// <summary>
        /// How often persistent drawings are sent, in frames.
        /// </summary>
        private const int PersistentRefreshFrames = 15;

        private const float MaxDrawRadius = 500f;
        private const float MaxDrawExtent = 500f;
        private const float MaxDrawCoordinate = 2000f;
        private const int MaxLinePoints = 64;

        private sealed class PersistentDraw
        {
            public object Key = null!;
            public Player? Player;
            public Action<Player, float> Redraw = null!;
            public string Name = string.Empty;
        }

        private static readonly Dictionary<object, PersistentDraw> PersistentBroadcasts = [];
        private static readonly Dictionary<(object Key, Player Player), PersistentDraw> PersistentTargets = [];
        private static CoroutineHandle _persistentLoopHandle;
        private static int _persistentFrame;

        /// <summary>
        /// Draws the specified <see cref="ServerObject"/> to all players.
        /// Without a <paramref name="duration"/> the drawing persists and refreshes until StopDraw is called.
        /// </summary>
        /// <param name="obj">The <see cref="ServerObject"/> to be drawn.</param>
        /// <param name="duration">How long the lines are visible. Null draws persistently until stopped.</param>
        /// <param name="color">The line color. Null uses <see cref="DrawableLines.ServerDefaultColor"/>.</param>
        public static void Draw(this ServerObject obj, float? duration = null, Color? color = null) => obj.Draw((Player?)null, duration, color);

        /// <summary>
        /// Draws the specified <see cref="ServerObject"/> to the players specified in <paramref name="players"/>.
        /// Without a <paramref name="duration"/> the drawing persists and refreshes until StopDraw is called.
        /// </summary>
        /// <param name="obj">The <see cref="ServerObject"/> to be drawn.</param>
        /// <param name="players">The <see cref="Player"/>s that will see it.</param>
        /// <param name="duration">How long the lines are visible. Null draws persistently until stopped.</param>
        /// <param name="color">The line color. Null uses <see cref="DrawableLines.ServerDefaultColor"/>.</param>
        public static void Draw(this ServerObject obj, IEnumerable<Player> players, float? duration = null, Color? color = null)
        {
            if (players == null)
                return;

            foreach (Player player in players)
            {
                Draw(obj, player, duration, color);
            }
        }

        /// <summary>
        /// Draws the specified <see cref="ServerObject"/> to the specified <see cref="Player"/>.
        /// When <paramref name="player"/> is null the object is drawn for every ready player.
        /// Without a <paramref name="duration"/> each transmission lasts <see cref="PersistentLifetimeFrames"/> frames
        /// and is sent every <see cref="PersistentRefreshFrames"/> frames until StopDraw is called.
        /// </summary>
        /// <param name="obj">The <see cref="ServerObject"/> to be drawn.</param>
        /// <param name="player">The <see cref="Player"/> that will see it, or null for all players.</param>
        /// <param name="duration">How long the lines are visible. Null draws persistently until stopped.</param>
        /// <param name="color">The line color. Null uses <see cref="DrawableLines.ServerDefaultColor"/>.</param>
        public static void Draw(this ServerObject obj, Player? player = null, float? duration = null, Color? color = null)
        {
            if (obj == null)
                return;

            if (duration == null)
            {
                RegisterPersistentDraw(obj, player, (target, d) => DrawForPlayer(obj, target, d, color), obj.GetType().Name);
                float persistentDuration = PersistentDuration();

                if (player != null)
                {
                    DrawForPlayer(obj, player, persistentDuration, color);
                    return;
                }

                foreach (Player target in Player.ReadyList.ToArray())
                {
                    DrawForPlayer(obj, target, persistentDuration, color);
                }

                return;
            }

            if (player != null)
            {
                DrawForPlayer(obj, player, duration, color);
                return;
            }

            foreach (Player target in Player.ReadyList.ToArray())
            {
                DrawForPlayer(obj, target, duration, color);
            }
        }

        /// <summary>
        /// Stops the persistent drawing of the specified <see cref="ServerObject"/>.
        /// When <paramref name="player"/> is null the drawing is stopped for every player.
        /// </summary>
        /// <param name="obj">The <see cref="ServerObject"/> to stop drawing.</param>
        /// <param name="player">The <see cref="Player"/> to stop drawing for, or null for all players.</param>
        public static void StopDraw(this ServerObject obj, Player? player = null)
        {
            if (obj == null)
                return;

            StopPersistentDraw(obj, player);
        }

        /// <summary>
        /// Draws the specified <see cref="ClientObject"/> to all players.
        /// Without a <paramref name="duration"/> the drawing persists and refreshes until StopDraw is called.
        /// </summary>
        /// <param name="obj">The <see cref="ClientObject"/> to be drawn.</param>
        /// <param name="duration">How long the lines are visible. Null draws persistently until stopped.</param>
        /// <param name="color">The line color. Null uses <see cref="DrawableLines.ServerDefaultColor"/>.</param>
        public static void Draw(this ClientObject obj, float? duration = null, Color? color = null)
        {
            obj.Draw((Player?)null, duration, color);
        }

        /// <summary>
        /// Draws the specified <see cref="ClientObject"/> to the players specified in <paramref name="players"/>.
        /// Without a <paramref name="duration"/> the drawing persists and refreshes until StopDraw is called.
        /// </summary>
        /// <param name="obj">The <see cref="ClientObject"/> to be drawn.</param>
        /// <param name="players">The <see cref="Player"/>s that will see it.</param>
        /// <param name="duration">How long the lines are visible. Null draws persistently until stopped.</param>
        /// <param name="color">The line color. Null uses <see cref="DrawableLines.ServerDefaultColor"/>.</param>
        public static void Draw(this ClientObject obj, IEnumerable<Player> players, float? duration = null, Color? color = null)
        {
            if (players == null)
                return;

            foreach (Player player in players)
            {
                Draw(obj, player, duration, color);
            }
        }

        /// <summary>
        /// Draws the specified <see cref="ClientObject"/> to the specified <see cref="Player"/>.
        /// When <paramref name="player"/> is null the object is drawn for every ready player.
        /// Without a <paramref name="duration"/> each transmission lasts <see cref="PersistentLifetimeFrames"/> frames
        /// and is sent every <see cref="PersistentRefreshFrames"/> frames until StopDraw is called.
        /// </summary>
        /// <param name="obj">The <see cref="ClientObject"/> to be drawn.</param>
        /// <param name="player">The <see cref="Player"/> that will see it, or null for all players.</param>
        /// <param name="duration">How long the lines are visible. Null draws persistently until stopped.</param>
        /// <param name="color">The line color. Null uses <see cref="DrawableLines.ServerDefaultColor"/>.</param>
        public static void Draw(this ClientObject obj, Player? player = null, float? duration = null, Color? color = null)
        {
            if (obj == null)
                return;

            if (duration == null)
            {
                RegisterPersistentDraw(obj, player, (target, d) => DrawClientForPlayer(obj, target, d, color), obj.GetType().Name);

                float persistentDuration = PersistentDuration();

                if (player != null)
                {
                    DrawClientForPlayer(obj, player, persistentDuration, color);
                    return;
                }

                foreach (Player target in Player.ReadyList.ToArray())
                {
                    DrawClientForPlayer(obj, target, persistentDuration, color);
                }

                return;
            }

            if (player != null)
            {
                DrawClientForPlayer(obj, player, duration, color);
                return;
            }

            foreach (Player target in Player.ReadyList.ToArray())
            {
                DrawClientForPlayer(obj, target, duration, color);
            }
        }

        /// <summary>
        /// Stops the persistent drawing of the specified <see cref="ClientObject"/>.
        /// When <paramref name="player"/> is null the drawing is stopped for every player.
        /// </summary>
        /// <param name="obj">The <see cref="ClientObject"/> to stop drawing.</param>
        /// <param name="player">The <see cref="Player"/> to stop drawing for, or null for all players.</param>
        public static void StopDraw(this ClientObject obj, Player? player = null)
        {
            if (obj == null)
                return;

            StopPersistentDraw(obj, player);
        }

        /// <summary>
        /// Stops all persistent drawings. The refresh loop exits on its own once nothing is registered.
        /// </summary>
        public static void StopAllDraws()
        {
            PersistentBroadcasts.Clear();
            PersistentTargets.Clear();
        }

        /// <summary>
        /// Stops every persistent drawing targeted at the specified <see cref="Player"/>
        /// (e.g. on disconnect or ToolGun unequip) so the refresh loop cannot keep spamming
        /// a stale or gone client with destroyed-transform geometry.
        /// </summary>
        public static void StopDrawsForPlayer(Player player)
        {
            if (player == null)
                return;

            foreach ((object key, Player keyPlayer) in PersistentTargets.Keys.ToArray())
            {
                if (ReferenceEquals(keyPlayer, player) || Equals(keyPlayer, player))
                    PersistentTargets.Remove((key, keyPlayer));
            }
        }

        private static bool IsValidCoordinate(float value) =>
            !float.IsNaN(value) && !float.IsInfinity(value) && Mathf.Abs(value) <= MaxDrawCoordinate;

        private static bool IsValidVector(Vector3 value) =>
            IsValidCoordinate(value.x) && IsValidCoordinate(value.y) && IsValidCoordinate(value.z);

        private static bool IsValidRadius(float value) =>
            !float.IsNaN(value) && !float.IsInfinity(value) && value > 0f && value <= MaxDrawRadius;

        private static float ClampRadius(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) || value <= 0f)
                return MarkerRadius;

            return Mathf.Min(value, MaxDrawRadius);
        }

        private static bool IsValidSize(Vector3 size) =>
            !float.IsNaN(size.x) && !float.IsNaN(size.y) && !float.IsNaN(size.z) &&
            !float.IsInfinity(size.x) && !float.IsInfinity(size.y) && !float.IsInfinity(size.z) &&
            size.x >= 0f && size.y >= 0f && size.z >= 0f &&
            size.x <= MaxDrawExtent * 2f && size.y <= MaxDrawExtent * 2f && size.z <= MaxDrawExtent * 2f;

        private static Vector3 ClampSize(Vector3 size) => new(
            Mathf.Clamp(size.x, 0f, MaxDrawExtent * 2f),
            Mathf.Clamp(size.y, 0f, MaxDrawExtent * 2f),
            Mathf.Clamp(size.z, 0f, MaxDrawExtent * 2f));

        private static void StopPersistentDraw(object key, Player? player)
        {
            if (player != null)
            {
                PersistentTargets.Remove((key, player));
                return;
            }

            PersistentBroadcasts.Remove(key);

            foreach ((object keyObj, Player keyPlayer) in PersistentTargets.Keys.ToArray())
            {
                if (ReferenceEquals(keyObj, key))
                    PersistentTargets.Remove((keyObj, keyPlayer));
            }
        }

        private static void RegisterPersistentDraw(object key, Player? player, Action<Player, float> redraw, string name)
        {
            PersistentDraw entry = new() { Key = key, Player = player, Redraw = redraw, Name = name };

            if (player != null)
            {
                PersistentTargets[(key, player)] = entry;
            }
            else
                PersistentBroadcasts[key] = entry;

            EnsurePersistentLoop();
        }

        private static void RemovePersistentDraw(PersistentDraw entry)
        {
            if (entry.Player == null)
            {
                PersistentBroadcasts.Remove(entry.Key);
            }
            else
                PersistentTargets.Remove((entry.Key, entry.Player));
        }

        private static float PersistentDuration()
        {
            float frame = Time.deltaTime;

            if (float.IsNaN(frame) || float.IsInfinity(frame) || frame <= 0f)
                frame = 1f / 60f;

            return PersistentLifetimeFrames * frame;
        }

        private static void EnsurePersistentLoop()
        {
            if (Timing.IsRunning(_persistentLoopHandle))
                return;

            _persistentLoopHandle = Timing.RunCoroutine(PersistentDrawLoop());
        }

        private static IEnumerator<float> PersistentDrawLoop()
        {
            while (PersistentBroadcasts.Count > 0 || PersistentTargets.Count > 0)
            {
                yield return Timing.WaitForOneFrame;
                _persistentFrame++;

                if (_persistentFrame % PersistentRefreshFrames != 0)
                    continue;

                float duration = PersistentDuration();

                foreach (PersistentDraw entry in PersistentBroadcasts.Values.ToArray())
                {
                    RefreshPersistentDraw(entry, duration);
                }

                foreach (PersistentDraw entry in PersistentTargets.Values.ToArray())
                {
                    RefreshPersistentDraw(entry, duration);
                }
            }

            _persistentLoopHandle = default;
        }

        private static void RefreshPersistentDraw(PersistentDraw entry, float duration)
        {
            try
            {
                if (entry.Player != null)
                {
                    if (entry.Player.IsDestroyed)
                    {
                        RemovePersistentDraw(entry);
                        return;
                    }

                    entry.Redraw(entry.Player, duration);
                    return;
                }

                foreach (Player target in Player.ReadyList.ToArray())
                {
                    entry.Redraw(target, duration);
                }
            }
            catch (Exception ex)
            {
                LogManager.Warn($"Stopped persistent draw for '{entry.Name}' after error: {ex.Message}");
                RemovePersistentDraw(entry);
            }
        }

        private static void DrawForPlayer(ServerObject obj, Player player, float? duration, Color? color)
        {
            if (obj == null || !CanReceiveLines(player))
                return;

            Collider? collider = null;

            try
            {
                collider = obj switch
                {
                    PrimitiveObjectServer prim when prim.Base != null => prim.Base._collider,
                    InteractionObject inter when inter.Base != null => inter.Base._collider,
                    TeleporterObject tele when tele.TeleporterHandler != null => tele.TeleporterHandler.Collider,
                    _ => obj.Object?.GetComponent<Collider>(),
                };
            }
            catch
            {
                collider = null;
            }

            if (collider != null)
            {
                DrawCollider(player, collider, duration, color);
                return;
            }

            if (!IsValidVector(obj.Position) || !IsValidVector(obj.Scale))
                return;

            ColliderType type = obj switch
            {
                PrimitiveObjectServer prim => prim.PrimitiveType switch
                {
                    PrimitiveType.Sphere => ColliderType.Sphere,
                    PrimitiveType.Capsule or PrimitiveType.Cylinder => ColliderType.Capsule,
                    _ => ColliderType.Box,
                },
                InteractionObject inter => inter.Shape switch
                {
                    ColliderShape.Sphere => ColliderType.Sphere,
                    ColliderShape.Capsule => ColliderType.Capsule,
                    _ => ColliderType.Box,
                },
                _ => ColliderType.Box,
            };

            DrawApproximate(player, obj, type, duration, color);
        }

        private static bool CanReceiveLines(Player? player)
        {
            if (player == null || player.IsHost || player.IsDummy || player.IsDestroyed)
                return false;

            return player.Connection != null && player.Connection.isReady;
        }

        private static void DrawCollider(Player player, Collider collider, float? duration, Color? color)
        {
            if (collider == null)
                return;

            try
            {
                switch (collider)
                {
                    case CapsuleCollider capsule:
                        player.SendCapsuleToPlayer(capsule, duration, color);
                        break;

                    case SphereCollider sphere:
                        player.SendSphereToPlayer(sphere, duration, color);
                        break;

                    default:
                        if (!IsValidVector(collider.bounds.center) || !IsValidVector(collider.bounds.extents))
                            return;

                        player.SendBoundsToPlayer(collider.bounds, duration, color);
                        break;
                }
            }
            catch
            {
            }
        }

        private static void DrawApproximate(Player player, ServerObject obj, ColliderType type, float? duration, Color? color)
        {
            Vector3 position = obj.Position;

            if (!IsValidVector(position) || !IsValidVector(obj.Scale))
                return;

            switch (obj)
            {
                case WaypointObject way when IsValidSize(way.BoundsSize) && way.BoundsSize != Vector3.zero:
                    player.SendBoundsToPlayer(new Bounds(position, ClampSize(way.BoundsSize)), duration, color);
                    return;

                case WaypointObject:
                    break;

                case SpeakerObject speak when speak.MaxDistance > 0f || speak.MinDistance > 0f:
                    bool drew = false;

                    if (speak.MinDistance > 0f && !float.IsNaN(speak.MinDistance) && !float.IsInfinity(speak.MinDistance))
                    {
                        player.SendSphereToPlayer(position, ClampRadius(speak.MinDistance), duration, Color.red);
                        drew = true;
                    }

                    if (speak.MaxDistance > 0f && !float.IsNaN(speak.MaxDistance) && !float.IsInfinity(speak.MaxDistance))
                    {
                        player.SendSphereToPlayer(position, ClampRadius(speak.MaxDistance), duration, Color.green);
                        drew = true;
                    }

                    if (drew)
                        return;

                    break;
            }

            Vector3 scale = obj.Scale;

            switch (type)
            {
                case ColliderType.Sphere when Mathf.Max(scale.x, Mathf.Max(scale.y, scale.z)) > 0f:
                    player.SendSphereToPlayer(position, ClampRadius(Mathf.Max(scale.x, Mathf.Max(scale.y, scale.z)) * 0.5f), duration, color);
                    break;

                case ColliderType.Capsule:
                    DrawCapsuleApproximate(player, position, scale, duration, color);
                    break;

                default:
                    if (scale == Vector3.zero)
                    {
                        player.SendSphereToPlayer(position, MarkerRadius, duration, color);
                    }
                    else
                        player.SendBoundsToPlayer(new Bounds(position, ClampSize(scale)), duration, color);
                    break;
            }
        }

        private static void DrawCapsuleApproximate(Player player, Vector3 position, Vector3 scale, float? duration, Color? color)
        {
            if (!IsValidVector(position) || !IsValidVector(scale))
                return;

            float radius = ClampRadius(Mathf.Max(scale.x, scale.z) * 0.5f);
            float height = IsValidCoordinate(scale.y) ? Mathf.Max(0f, scale.y) : 0f;
            float halfSegment = Mathf.Max(0f, height * 0.5f - radius);
            player.SendCapsuleToPlayer(position - Vector3.up * halfSegment, position + Vector3.up * halfSegment, radius, duration, color);
        }

        private static void DrawClientForPlayer(ClientObject obj, Player player, float? duration, Color? color)
        {
            if (obj == null || !CanReceiveLines(player))
                return;

            if (!IsValidVector(obj.Position) || !IsValidVector(obj.Scale))
                return;

            Collider? collider = null;

            try
            {
                if (obj.ServerColliders != null)
                {
                    foreach (Collider candidate in obj.ServerColliders)
                    {
                        if (candidate != null)
                        {
                            collider = candidate;
                            break;
                        }
                    }
                }

                collider ??= (obj as PrimitiveObject)?.ServerCollider;
            }
            catch
            {
                collider = null;
            }

            if (collider != null)
            {
                DrawCollider(player, collider, duration, color);
                return;
            }

            Vector3 position = obj.Position;
            Vector3 scale = obj.Scale;

            switch (obj)
            {
                case PrimitiveObject prim when prim.PrimitiveType == PrimitiveType.Sphere && Mathf.Max(scale.x, Mathf.Max(scale.y, scale.z)) > 0f:
                    player.SendSphereToPlayer(position, ClampRadius(Mathf.Max(scale.x, Mathf.Max(scale.y, scale.z)) * 0.5f), duration, color);
                    break;

                case PrimitiveObject prim when prim.PrimitiveType is PrimitiveType.Capsule or PrimitiveType.Cylinder:
                    DrawCapsuleApproximate(player, position, scale, duration, color);
                    break;

                case LightObject light when light.Range > 0f && !float.IsNaN(light.Range) && !float.IsInfinity(light.Range):
                    player.SendSphereToPlayer(position, ClampRadius(light.Range), duration, color);
                    break;

                case LightObject:
                    break;

                default:
                    if (scale == Vector3.zero)
                    {
                        player.SendSphereToPlayer(position, MarkerRadius, duration, color);
                    }
                    else
                        player.SendBoundsToPlayer(new Bounds(position, ClampSize(scale)), duration, color);
                    break;
            }
        }

        /// <summary>
        /// Sends a drawable line to a specific <see cref="Player"/> without requiring <see cref="DrawableLines.IsDebugModeEnabled"/>.
        /// </summary>
        /// <param name="player">The <see cref="Player"/> that will see the line.</param>
        /// <param name="duration">How long the line is visible. Null uses <see cref="DrawableLines.ServerDefaultDuration"/>.</param>
        /// <param name="color">The line color. Null uses <see cref="DrawableLines.ServerDefaultColor"/>.</param>
        /// <param name="positions">The line points. At least two are required.</param>
        public static void SendLineToPlayer(this Player player, float? duration, Color? color, params Vector3[] positions)
        {
            if (player == null || player.Connection == null || !player.Connection.isReady)
                return;

            if (positions == null || positions.Length < 2 || positions.Length > MaxLinePoints)
                return;

            for (int i = 0; i < positions.Length; i++)
            {
                if (!IsValidVector(positions[i]))
                    return;
            }

            if (duration.HasValue)
            {
                float d = duration.Value;

                if (float.IsNaN(d) || float.IsInfinity(d) || d <= 0f || d > 60f)
                    duration = null;
            }

            if (DrawableLines.DurationOverride.HasValue)
            {
                float o = DrawableLines.DurationOverride.Value;
                duration = float.IsNaN(o) || float.IsInfinity(o) || o <= 0f || o > 60f ? null : o;
            }

            if (!duration.HasValue)
                duration = DrawableLines.ServerDefaultDuration;

            if (!color.HasValue)
                color = DrawableLines.ServerDefaultColor;

            player.Connection.Send(new DrawableLineMessage(duration, color, positions));
        }

        /// <summary>
        /// Sends a drawable line to a specific <see cref="Player"/> without requiring <see cref="DrawableLines.IsDebugModeEnabled"/>.
        /// </summary>
        /// <param name="player">The <see cref="Player"/> that will see the line.</param>
        /// <param name="positions">The line points. At least two are required.</param>
        public static void SendLineToPlayer(this Player player, params Vector3[] positions)
        {
            player.SendLineToPlayer(null, null, positions);
        }

        /// <summary>
        /// Sends a drawable line to a specific <see cref="Player"/> without requiring <see cref="DrawableLines.IsDebugModeEnabled"/>.
        /// </summary>
        /// <param name="player">The <see cref="Player"/> that will see the line.</param>
        /// <param name="duration">How long the line is visible. Null uses <see cref="DrawableLines.ServerDefaultDuration"/>.</param>
        /// <param name="positions">The line points. At least two are required.</param>
        public static void SendLineToPlayer(this Player player, float? duration, params Vector3[] positions)
        {
            player.SendLineToPlayer(duration, null, positions);
        }

        /// <summary>
        /// Sends a drawable line to a specific <see cref="Player"/> without requiring <see cref="DrawableLines.IsDebugModeEnabled"/>.
        /// </summary>
        /// <param name="player">The <see cref="Player"/> that will see the line.</param>
        /// <param name="color">The line color. Null uses <see cref="DrawableLines.ServerDefaultColor"/>.</param>
        /// <param name="positions">The line points. At least two are required.</param>
        public static void SendLineToPlayer(this Player player, Color? color, params Vector3[] positions)
        {
            player.SendLineToPlayer(null, color, positions);
        }

        /// <summary>
        /// Sends a drawable line to the specified <see cref="Player"/>s without requiring <see cref="DrawableLines.IsDebugModeEnabled"/>.
        /// </summary>
        /// <param name="players">The <see cref="Player"/>s that will see the line.</param>
        /// <param name="duration">How long the line is visible. Null uses <see cref="DrawableLines.ServerDefaultDuration"/>.</param>
        /// <param name="color">The line color. Null uses <see cref="DrawableLines.ServerDefaultColor"/>.</param>
        /// <param name="positions">The line points. At least two are required.</param>
        public static void SendLineToPlayers(this IEnumerable<Player> players, float? duration, Color? color, params Vector3[] positions)
        {
            if (players == null)
                return;

            foreach (Player player in players)
            {
                player.SendLineToPlayer(duration, color, positions);
            }
        }

        /// <summary>
        /// Sends the edges of a <see cref="Bounds"/> to a specific <see cref="Player"/> without requiring <see cref="DrawableLines.IsDebugModeEnabled"/>.
        /// </summary>
        /// <param name="player">The <see cref="Player"/> that will see the bounds.</param>
        /// <param name="bounds">The bounds to draw.</param>
        /// <param name="duration">How long the lines are visible. Null uses <see cref="DrawableLines.ServerDefaultDuration"/>.</param>
        /// <param name="color">The line color. Null uses <see cref="DrawableLines.ServerDefaultColor"/>.</param>
        public static void SendBoundsToPlayer(this Player player, Bounds bounds, float? duration, Color? color)
        {
            if (player == null || !IsValidVector(bounds.center) || !IsValidVector(bounds.extents))
                return;

            if (bounds.extents.x < 0f || bounds.extents.y < 0f || bounds.extents.z < 0f)
                return;

            Bounds sanitized = new(bounds.center, ClampSize(bounds.size));

            Vector3 center = sanitized.center;
            Vector3 extents = sanitized.extents;

            Vector3[] bottom =
            [
                center + new Vector3(-extents.x, -extents.y, -extents.z),
                center + new Vector3(extents.x, -extents.y, -extents.z),
                center + new Vector3(extents.x, -extents.y, extents.z),
                center + new Vector3(-extents.x, -extents.y, extents.z),
                center + new Vector3(-extents.x, -extents.y, -extents.z),
            ];

            Vector3[] top =
            [
                center + new Vector3(-extents.x, extents.y, -extents.z),
                center + new Vector3(extents.x, extents.y, -extents.z),
                center + new Vector3(extents.x, extents.y, extents.z),
                center + new Vector3(-extents.x, extents.y, extents.z),
                center + new Vector3(-extents.x, extents.y, -extents.z),
            ];

            player.SendLineToPlayer(duration, color, bottom);
            player.SendLineToPlayer(duration, color, top);

            for (int i = 0; i < 4; i++)
            {
                player.SendLineToPlayer(duration, color, bottom[i], top[i]);
            }
        }

        /// <summary>
        /// Sends the edges of a <see cref="Bounds"/> to a specific <see cref="Player"/> without requiring <see cref="DrawableLines.IsDebugModeEnabled"/>.
        /// </summary>
        /// <param name="player">The <see cref="Player"/> that will see the bounds.</param>
        /// <param name="bounds">The bounds to draw.</param>
        public static void SendBoundsToPlayer(this Player player, Bounds bounds)
        {
            player.SendBoundsToPlayer(bounds, null, null);
        }

        /// <summary>
        /// Sends a wireframe sphere to a specific <see cref="Player"/> without requiring <see cref="DrawableLines.IsDebugModeEnabled"/>.
        /// </summary>
        /// <param name="player">The <see cref="Player"/> that will see the sphere.</param>
        /// <param name="origin">The center of the sphere.</param>
        /// <param name="radius">The radius of the sphere.</param>
        /// <param name="duration">How long the lines are visible. Null uses <see cref="DrawableLines.ServerDefaultDuration"/>.</param>
        /// <param name="color">The line color. Null uses <see cref="DrawableLines.ServerDefaultColor"/>.</param>
        /// <param name="segments">The number of segments per circle.</param>
        public static void SendSphereToPlayer(this Player player, Vector3 origin, float radius, float? duration, Color? color, int segments = 8)
        {
            if (player == null || !IsValidVector(origin))
                return;

            if (float.IsNaN(radius) || float.IsInfinity(radius) || radius <= 0f)
                return;

            if (radius > MaxDrawRadius)
                radius = MaxDrawRadius;

            if (segments < 4)
            {
                segments = 8;
            }
            else if (segments > 24)
                segments = 24;

            Vector3[] horizontal;
            Vector3[] vertical;

            try
            {
                horizontal = DrawableLines.GetCircle(origin, radius, true, segments);
                vertical = DrawableLines.GetCircle(origin, radius, false, segments);
            }
            catch
            {
                return;
            }

            player.SendLineToPlayer(duration, color, horizontal);
            player.SendLineToPlayer(duration, color, vertical);
        }

        /// <summary>
        /// Sends a wireframe sphere to a specific <see cref="Player"/> without requiring <see cref="DrawableLines.IsDebugModeEnabled"/>.
        /// </summary>
        /// <param name="player">The <see cref="Player"/> that will see the sphere.</param>
        /// <param name="origin">The center of the sphere.</param>
        /// <param name="radius">The radius of the sphere.</param>
        public static void SendSphereToPlayer(this Player player, Vector3 origin, float radius)
        {
            player.SendSphereToPlayer(origin, radius, null, null);
        }

        /// <summary>
        /// Sends a wireframe sphere matching a <see cref="SphereCollider"/> to a specific <see cref="Player"/>
        /// without requiring <see cref="DrawableLines.IsDebugModeEnabled"/>.
        /// </summary>
        /// <param name="player">The <see cref="Player"/> that will see the sphere.</param>
        /// <param name="collider">The collider to draw.</param>
        /// <param name="duration">How long the lines are visible. Null uses <see cref="DrawableLines.ServerDefaultDuration"/>.</param>
        /// <param name="color">The line color. Null uses <see cref="DrawableLines.ServerDefaultColor"/>.</param>
        /// <param name="segments">The number of segments per circle.</param>
        public static void SendSphereToPlayer(this Player player, SphereCollider collider, float? duration, Color? color, int segments = 8)
        {
            if (player == null || collider == null)
                return;

            try
            {
                Transform transform = collider.transform;

                if (transform == null)
                    return;

                Vector3 scale = transform.lossyScale;

                if (!IsValidVector(scale))
                    return;

                float maxAxis = Mathf.Max(Mathf.Abs(scale.x), Mathf.Max(Mathf.Abs(scale.y), Mathf.Abs(scale.z)));

                if (float.IsNaN(maxAxis) || float.IsInfinity(maxAxis) || maxAxis <= 0f)
                    return;

                if (float.IsNaN(collider.radius) || float.IsInfinity(collider.radius) || collider.radius <= 0f)
                    return;

                Vector3 center = transform.TransformPoint(collider.center);

                if (!IsValidVector(center))
                    return;

                player.SendSphereToPlayer(center, ClampRadius(collider.radius * maxAxis), duration, color, segments);
            }
            catch
            {
            }
        }

        /// <summary>
        /// Sends a wireframe sphere matching a <see cref="SphereCollider"/> to a specific <see cref="Player"/>
        /// without requiring <see cref="DrawableLines.IsDebugModeEnabled"/>.
        /// </summary>
        /// <param name="player">The <see cref="Player"/> that will see the sphere.</param>
        /// <param name="collider">The collider to draw.</param>
        public static void SendSphereToPlayer(this Player player, SphereCollider collider)
        {
            player.SendSphereToPlayer(collider, null, null);
        }

        /// <summary>
        /// Sends a wireframe sphere to the specified <see cref="Player"/>s without requiring <see cref="DrawableLines.IsDebugModeEnabled"/>.
        /// </summary>
        /// <param name="players">The <see cref="Player"/>s that will see the sphere.</param>
        /// <param name="origin">The center of the sphere.</param>
        /// <param name="radius">The radius of the sphere.</param>
        /// <param name="duration">How long the lines are visible. Null uses <see cref="DrawableLines.ServerDefaultDuration"/>.</param>
        /// <param name="color">The line color. Null uses <see cref="DrawableLines.ServerDefaultColor"/>.</param>
        /// <param name="segments">The number of segments per circle.</param>
        public static void SendSphereToPlayers(this IEnumerable<Player> players, Vector3 origin, float radius, float? duration, Color? color, int segments = 8)
        {
            if (players == null)
                return;

            foreach (Player player in players)
            {
                player.SendSphereToPlayer(origin, radius, duration, color, segments);
            }
        }

        /// <summary>
        /// Sends a wireframe capsule to a specific <see cref="Player"/> without requiring <see cref="DrawableLines.IsDebugModeEnabled"/>.
        /// </summary>
        /// <param name="player">The <see cref="Player"/> that will see the capsule.</param>
        /// <param name="point1">One end of the capsule's inner segment.</param>
        /// <param name="point2">The other end of the capsule's inner segment.</param>
        /// <param name="radius">The radius of the capsule.</param>
        /// <param name="duration">How long the lines are visible. Null uses <see cref="DrawableLines.ServerDefaultDuration"/>.</param>
        /// <param name="color">The line color. Null uses <see cref="DrawableLines.ServerDefaultColor"/>.</param>
        /// <param name="segments">The number of segments per ring. Must be a positive even number.</param>
        public static void SendCapsuleToPlayer(this Player player, Vector3 point1, Vector3 point2, float radius, float? duration, Color? color, int segments = 8)
        {
            if (player == null || !IsValidVector(point1) || !IsValidVector(point2))
                return;

            if (float.IsNaN(radius) || float.IsInfinity(radius) || radius <= 0f)
                return;

            if (radius > MaxDrawRadius)
                radius = MaxDrawRadius;

            if (segments <= 0)
                segments = 8;

            if (segments % 2 != 0)
                segments++;

            if (segments < 4)
            {
                segments = 4;
            }
            else if (segments > 24)
                segments = 24;

            Vector3 axis = point2 - point1;

            if (float.IsNaN(axis.x) || float.IsNaN(axis.y) || float.IsNaN(axis.z) || float.IsInfinity(axis.x) || float.IsInfinity(axis.y) || float.IsInfinity(axis.z))
                return;

            if (axis.sqrMagnitude < 1e-8f)
            {
                player.SendSphereToPlayer(point1, radius, duration, color, segments);
                return;
            }

            Vector3 dir = axis.normalized;
            Vector3 reference = Mathf.Abs(dir.y) < 0.99f ? Vector3.up : Vector3.right;
            Vector3 u = Vector3.Cross(dir, reference).normalized;
            Vector3 v = Vector3.Cross(dir, u).normalized;

            if (!IsValidVector(dir) || !IsValidVector(u) || !IsValidVector(v))
                return;

            Vector3[] ring1 = new Vector3[segments + 1];
            Vector3[] ring2 = new Vector3[segments + 1];

            for (int i = 0; i <= segments; i++)
            {
                float angle = Mathf.PI * 2f * i / segments;
                Vector3 offset = (u * Mathf.Cos(angle) + v * Mathf.Sin(angle)) * radius;
                ring1[i] = point1 + offset;
                ring2[i] = point2 + offset;
            }

            player.SendLineToPlayer(duration, color, ring1);
            player.SendLineToPlayer(duration, color, ring2);

            for (int k = 0; k < 4; k++)
            {
                int index = k * segments / 4;
                player.SendLineToPlayer(duration, color, ring1[index], ring2[index]);
            }

            int arcSegments = Mathf.Max(4, segments);

            if (arcSegments % 2 != 0)
                arcSegments++;

            foreach (Vector3 w in new[] { u, v })
            {
                Vector3[] capTop = new Vector3[arcSegments + 1];
                Vector3[] capBottom = new Vector3[arcSegments + 1];

                for (int i = 0; i <= arcSegments; i++)
                {
                    float t = -Mathf.PI * 0.5f + Mathf.PI * i / arcSegments;
                    float cos = Mathf.Cos(t) * radius;
                    float sin = Mathf.Sin(t) * radius;
                    capTop[i] = point2 + dir * cos + w * sin;
                    capBottom[i] = point1 - dir * cos + w * sin;
                }

                player.SendLineToPlayer(duration, color, capTop);
                player.SendLineToPlayer(duration, color, capBottom);
            }
        }

        /// <summary>
        /// Sends a wireframe capsule matching a <see cref="CapsuleCollider"/> to a specific <see cref="Player"/> without requiring <see cref="DrawableLines.IsDebugModeEnabled"/>.
        /// </summary>
        /// <param name="player">The <see cref="Player"/> that will see the capsule.</param>
        /// <param name="collider">The collider to draw.</param>
        /// <param name="duration">How long the lines are visible. Null uses <see cref="DrawableLines.ServerDefaultDuration"/>.</param>
        /// <param name="color">The line color. Null uses <see cref="DrawableLines.ServerDefaultColor"/>.</param>
        /// <param name="segments">The number of segments per ring. Must be a positive even number.</param>
        public static void SendCapsuleToPlayer(this Player player, CapsuleCollider collider, float? duration, Color? color, int segments = 8)
        {
            if (player == null || collider == null)
                return;

            try
            {
                Transform transform = collider.transform;

                if (transform == null)
                    return;

                Vector3 lossyScale = transform.lossyScale;

                if (!IsValidVector(lossyScale))
                    return;

                if (float.IsNaN(collider.radius) || float.IsInfinity(collider.radius) || collider.radius <= 0f)
                    return;

                if (float.IsNaN(collider.height) || float.IsInfinity(collider.height) || collider.height <= 0f)
                    return;

                if (float.IsNaN(collider.center.x) || float.IsNaN(collider.center.y) || float.IsNaN(collider.center.z))
                    return;

                float axisScale = collider.direction switch
                {
                    0 => Mathf.Abs(lossyScale.x),
                    2 => Mathf.Abs(lossyScale.z),
                    _ => Mathf.Abs(lossyScale.y),
                };

                float radiusScale = collider.direction switch
                {
                    0 => Mathf.Max(Mathf.Abs(lossyScale.y), Mathf.Abs(lossyScale.z)),
                    2 => Mathf.Max(Mathf.Abs(lossyScale.x), Mathf.Abs(lossyScale.y)),
                    _ => Mathf.Max(Mathf.Abs(lossyScale.x), Mathf.Abs(lossyScale.z)),
                };

                if (float.IsNaN(axisScale) || float.IsInfinity(axisScale) || float.IsNaN(radiusScale) || float.IsInfinity(radiusScale))
                    return;

                float worldRadius = collider.radius * radiusScale;
                float worldHeight = collider.height * axisScale;

                if (float.IsNaN(worldRadius) || float.IsInfinity(worldRadius) || worldRadius <= 0f)
                    return;

                if (float.IsNaN(worldHeight) || float.IsInfinity(worldHeight) || worldHeight <= 0f)
                    return;

                Vector3 localAxis = collider.direction switch
                {
                    0 => Vector3.right,
                    2 => Vector3.forward,
                    _ => Vector3.up,
                };

                Vector3 center = transform.TransformPoint(collider.center);
                Vector3 axis = transform.TransformDirection(localAxis).normalized;

                if (!IsValidVector(center) || !IsValidVector(axis))
                    return;

                float halfSegment = Mathf.Max(0f, Mathf.Min(worldHeight, MaxDrawExtent * 2f) * 0.5f - Mathf.Min(worldRadius, MaxDrawRadius));

                player.SendCapsuleToPlayer(center - axis * halfSegment, center + axis * halfSegment, ClampRadius(worldRadius), duration, color, segments);
            }
            catch
            {
            }
        }

        /// <summary>
        /// Sends a wireframe capsule matching a <see cref="CapsuleCollider"/> to a specific <see cref="Player"/> without requiring <see cref="DrawableLines.IsDebugModeEnabled"/>.
        /// </summary>
        /// <param name="player">The <see cref="Player"/> that will see the capsule.</param>
        /// <param name="collider">The collider to draw.</param>
        public static void SendCapsuleToPlayer(this Player player, CapsuleCollider collider)
        {
            player.SendCapsuleToPlayer(collider, null, null);
        }

        /// <summary>
        /// Sends a wireframe capsule to the specified <see cref="Player"/>s without requiring <see cref="DrawableLines.IsDebugModeEnabled"/>.
        /// </summary>
        /// <param name="players">The <see cref="Player"/>s that will see the capsule.</param>
        /// <param name="point1">One end of the capsule's inner segment.</param>
        /// <param name="point2">The other end of the capsule's inner segment.</param>
        /// <param name="radius">The radius of the capsule.</param>
        /// <param name="duration">How long the lines are visible. Null uses <see cref="DrawableLines.ServerDefaultDuration"/>.</param>
        /// <param name="color">The line color. Null uses <see cref="DrawableLines.ServerDefaultColor"/>.</param>
        /// <param name="segments">The number of segments per ring. Must be a positive even number.</param>
        public static void SendCapsuleToPlayers(this IEnumerable<Player> players, Vector3 point1, Vector3 point2, float radius, float? duration, Color? color, int segments = 8)
        {
            if (players == null)
                return;

            foreach (Player player in players)
            {
                player.SendCapsuleToPlayer(point1, point2, radius, duration, color, segments);
            }
        }
    }
}