// -----------------------------------------------------------------------
// <copyright file="AnimationController.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using ThaumielMapEditor.API.Blocks;
using ThaumielMapEditor.API.Data;
using UnityEngine;

namespace ThaumielMapEditor.API.Animation
{
    public class AnimationController
    {
        internal static readonly Dictionary<SchematicData, AnimationController> Dictionary = [];
        private static readonly object DictionaryLock = new();

        internal AnimationController(SchematicData schematic)
        {
            AttachedSchematic = schematic;

            List<Animator> animators = [];
            foreach (ServerObject obj in schematic.SpawnedServerObjects)
            {
                if (obj.Object != null && obj.Object.TryGetComponent(out Animator animator))
                    animators.Add(animator);
            }

            Animators = animators;
            lock (DictionaryLock)
            {
                Dictionary[schematic] = this;
            }
        }

        /// <summary>
        /// Gets the attached <see cref="SchematicData"/>.
        /// </summary>
        public SchematicData AttachedSchematic { get; }

        /// <summary>
        /// Gets a read-only list of all <see cref="Animator"/> components found on the schematic's server-side objects.
        /// </summary>
        public IReadOnlyList<Animator> Animators { get; }

        /// <summary>
        /// Plays an animation state by name on the animator at the given index.
        /// </summary>
        /// <param name="stateName">The state to play.</param>
        /// <param name="animatorIndex">The index of the animator to use.</param>
        public void Play(string stateName, int animatorIndex = 0)
        {
            if ((uint)animatorIndex >= (uint)Animators.Count)
                return;

            Animator? animator = Animators[animatorIndex];
            if (animator == null)
                return;

            animator.Play(stateName);
            animator.speed = 1f;
        }

        /// <summary>
        /// Plays an animation state by name on the animator at the given index.
        /// </summary>
        /// <param name="stateName">The state to play.</param>
        /// <param name="animatorIndex">The index of the animator to use.</param>
        /// <param name="speed">The speed to play the animation at.</param>
        public void Play(string stateName, float speed, int animatorIndex = 0)
        {
            if ((uint)animatorIndex >= (uint)Animators.Count)
                return;

            Animator? animator = Animators[animatorIndex];
            if (animator == null)
                return;

            animator.Play(stateName);
            animator.speed = speed;
        }

        /// <summary>
        /// Sets a boolean parameter on the animator at the given index.
        /// </summary>
        /// <param name="animParam">The animator parameter name.</param>
        /// <param name="state">The boolean value to set.</param>
        /// <param name="animatorIndex">The index of the animator to use.</param>
        public void Play(string animParam, bool state, int animatorIndex = 0)
        {
            if ((uint)animatorIndex >= (uint)Animators.Count)
                return;

            Animator? animator = Animators[animatorIndex];
            if (animator == null)
                return;

            animator.SetBool(animParam, state);
            animator.speed = 1f;
        }

        /// <summary>
        /// Sets a boolean parameter on the animator at the given index.
        /// </summary>
        /// <param name="animParam">The animator parameter name.</param>
        /// <param name="state">The boolean value to set.</param>
        /// <param name="animatorIndex">The index of the animator to use.</param>
        /// <param name="speed">The speed to play the animation at.</param>
        public void Play(string animParam, bool state, float speed, int animatorIndex = 0)
        {
            if ((uint)animatorIndex >= (uint)Animators.Count)
                return;

            Animator? animator = Animators[animatorIndex];
            if (animator == null)
                return;

            animator.SetBool(animParam, state);
            animator.speed = speed;
        }

        /// <summary>
        /// Plays an animation state on the animator matching the given name.
        /// </summary>
        /// <param name="stateName">The state to play.</param>
        /// <param name="animatorName">The name of the animator GameObject to target.</param>
        public void Play(string stateName, string animatorName)
        {
            if (!TryGetAnimator(animatorName, out Animator animator))
                return;

            animator.Play(stateName);
            animator.speed = 1f;
        }

        /// <summary>
        /// Plays an animation state on the animator matching the given name.
        /// </summary>
        /// <param name="stateName">The state to play.</param>
        /// <param name="animatorName">The name of the animator GameObject to target.</param>
        /// <param name="speed">The speed to play the animation at.</param>
        public void Play(string stateName, string animatorName, float speed)
        {
            if (!TryGetAnimator(animatorName, out Animator animator))
                return;

            animator.Play(stateName);
            animator.speed = speed;
        }

        private bool TryGetAnimator(string animatorName, out Animator animator)
        {
            for (int i = 0; i < Animators.Count; i++)
            {
                Animator? candidate = Animators[i];
                if (candidate == null || candidate.name != animatorName)
                    continue;

                animator = candidate;
                return true;
            }

            animator = null!;
            return false;
        }

        /// <summary>
        /// Stops playback on the animator at the given index.
        /// </summary>
        /// <param name="animatorIndex">The index of the animator to stop.</param>
        public void Stop(int animatorIndex = 0)
        {
            if ((uint)animatorIndex >= (uint)Animators.Count)
                return;

            Animator? animator = Animators[animatorIndex];
            if (animator == null)
                return;

            animator.StopPlayback();
            animator.speed = 0f;
        }

        /// <summary>
        /// Stops playback on the animator matching the given name.
        /// </summary>
        /// <param name="animatorName">The name of the animator GameObject to stop.</param>
        public void Stop(string animatorName)
        {
            if (!TryGetAnimator(animatorName, out Animator animator))
                return;

            animator.StopPlayback();
            animator.speed = 0f;
        }

        /// <summary>
        /// Gets or creates an <see cref="AnimationController"/> for the given <see cref="SchematicData"/>.
        /// </summary>
        /// <param name="schematic">The schematic to look up.</param>
        /// <returns>The existing or newly created <see cref="AnimationController"/>.</returns>
        public static AnimationController Get(SchematicData schematic)
        {
            lock (DictionaryLock)
            {
                if (Dictionary.TryGetValue(schematic, out AnimationController? controller))
                    return controller;
            }

            return new AnimationController(schematic);
        }

        internal static void Remove(SchematicData schematic)
        {
            lock (DictionaryLock)
            {
                Dictionary.Remove(schematic);
            }
        }

        internal static void ClearAll()
        {
            lock (DictionaryLock)
            {
                Dictionary.Clear();
            }
        }
    }
}