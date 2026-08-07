// -----------------------------------------------------------------------
// <copyright file="MECHelper.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using MEC;
using System.Linq;
using ThaumielMapEditor.API.Attributes;

namespace ThaumielMapEditor.API.Helpers
{
    public class MECHelper
    {
        internal static readonly Dictionary<string, CoroutineHandle> handles = [];

        public static bool TryRunCoroutine(IEnumerator<float> coroutine, [CallerMemberName] string key = "")
        {
            if (handles.TryGetValue(key, out CoroutineHandle existing) && Timing.IsRunning(existing))
            {
                LogManager.Info($"Coroutine '{key}' is already running. Skipping.");
                return false;
            }

            LogManager.Debug($"Starting coroutine '{key}'.");
            handles[key] = Timing.RunCoroutine(coroutine);
            return true;
        }

        public static bool TryRunCoroutine(IEnumerator<float> coroutine, [NotNullWhen(true)] out CoroutineHandle handle, [CallerMemberName] string key = "")
        {
            if (handles.TryGetValue(key, out CoroutineHandle existingHandle) && Timing.IsRunning(existingHandle))
            {
                LogManager.Debug($"Coroutine '{key}' is already running. Skipping.");
                handle = default;
                return false;
            }

            LogManager.Debug($"Starting coroutine '{key}'.");
            handle = Timing.RunCoroutine(coroutine);
            handles[key] = handle;
            return true;
        }

        public static void StopCoroutine(string key)
        {
            if (handles.TryGetValue(key, out CoroutineHandle handle))
            {
                Timing.KillCoroutines(handle);
                handles.Remove(key);
                LogManager.Debug($"Stopped coroutine '{key}'.");
            }
        }

        public static void StopAll()
        {
            foreach (string key in handles.Keys.ToArray())
                StopCoroutine(key);
        }

        public static void WaitForFrames(uint frames, Action onComplete) =>
            Timing.RunCoroutine(WaitCoroutine(frames, onComplete));

        public static void WaitForTwoFrames(Action onComplete) =>
            WaitForFrames(2, onComplete);

        public static void WaitForThreeFrames(Action onComplete) =>
            WaitForFrames(3, onComplete);

        public static void WaitForFourFrames(Action onComplete) =>
            WaitForFrames(4, onComplete);

        public static void WaitForFiveFrames(Action onComplete) =>
            WaitForFrames(5, onComplete);

        private static IEnumerator<float> WaitCoroutine(uint frames, Action onComplete)
        {
            yield return Timing.WaitForSeconds(frames / 60f);
            onComplete?.Invoke();
        }
    }
}