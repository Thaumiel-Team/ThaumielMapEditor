// -----------------------------------------------------------------------
// <copyright file="AnimationRuntime.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using ThaumielMapEditor.API.Binary.Attributes;
using ThaumielMapEditor.API.Blocks;
using ThaumielMapEditor.API.Data;
using ThaumielMapEditor.API.Enums;
using ThaumielMapEditor.API.Helpers;
using ThaumielMapEditor.API.Serialization;
using UnityEngine;
using UnityEngine.LowLevel;
using UnityEngine.PlayerLoop;
using YamlDotNet.Serialization;

namespace ThaumielMapEditor.API.Components.Tools
{
    [GitBookPage("Components/Tools/AnimationRuntime")]
    public class AnimationRuntime : ToolBase
    {
        private const float Tolerance = 0.0001f;

        [YamlMember(Alias = "Layer")]
        [BinaryProperty(Name = "Layer")]
        public int Layer { get; set; }

        [YamlMember(Alias = "Entries")]
        [BinaryProperty(Name = "Entries")]
        public List<AnimationKeyEntry> Entries { get; set; } = [];

        public override ToolType Type => ToolType.AnimationRuntime;

        private Animator? _animator;
        private int _lastStateHash;
        private float _lastNormalized;
        private readonly Dictionary<int, List<AnimationKeyEntry>> _entriesByState = [];
        private readonly List<AnimationKeyEntry> _anyStateEntries = [];
        private List<AnimationKeyEntry>? _currentStateEntries;
        private int _driverIndex = -1;
        private bool _initialized;

        public override void Init(ServerObject obj, SchematicData schem, Dictionary<string, object> properties)
        {
            base.Init(obj, schem, properties);

            _animator = obj.Object?.GetComponent<Animator>();
            _lastStateHash = 0;
            _lastNormalized = -Tolerance;
            _initialized = true;

            if (_animator == null && Entries is { Count: > 0 })
                LogManager.Warn($"AnimationRuntime on '{obj.Name}' has animation key entries but no Animator.");

            Refresh();
        }

        public void Refresh()
        {
            RebuildEntryLookup();
            _currentStateEntries = null;
            _lastStateHash = 0;

            if (_initialized && isActiveAndEnabled && Entries is { Count: > 0 })
            {
                AnimationRuntimeDriver.Register(this, ref _driverIndex);
            }
            else
                AnimationRuntimeDriver.Unregister(ref _driverIndex);
        }

        private void OnEnable()
        {
            if (_initialized)
                Refresh();
        }

        internal void SetDriverIndex(int index) => _driverIndex = index;

        private void OnDisable() => 
            AnimationRuntimeDriver.Unregister(ref _driverIndex);

        protected override void OnDestroy()
        {
            base.OnDestroy();
            AnimationRuntimeDriver.Unregister(ref _driverIndex);
        }

        internal void Tick()
        {
            if (_animator == null)
            {
                _animator = Object?.Object != null ? Object.Object.GetComponent<Animator>() : null;
                if (_animator == null)
                    return;
            }

            if (!_animator.isActiveAndEnabled || Layer < 0 || Layer >= _animator.layerCount)
                return;

            AnimatorStateInfo info = _animator.GetCurrentAnimatorStateInfo(Layer);
            bool stateChanged = info.fullPathHash != _lastStateHash;
            float current = info.normalizedTime;

            if (current < 0f)
            {
                current = 0f;
            }
            else if (info.loop)
            {
                current = Mathf.Repeat(current, 1f);
            }

            if (stateChanged)
            {
                _lastStateHash = info.fullPathHash;
                _lastNormalized = current > 0f ? current : -Tolerance;
                _entriesByState.TryGetValue(info.shortNameHash, out _currentStateEntries);
            }

            float length = Mathf.Max(info.length, Tolerance);

            if (_currentStateEntries != null)
                ProcessEntries(_currentStateEntries, current, length, info.loop, stateChanged);

            if (_anyStateEntries.Count > 0)
                ProcessEntries(_anyStateEntries, current, length, info.loop, stateChanged);

            _lastNormalized = current;
        }

        private void ProcessEntries(List<AnimationKeyEntry> entries, float current, float length, bool loop, bool stateChanged)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                AnimationKeyEntry entry = entries[i];
                float target = Mathf.Clamp01(entry.KeyTime / length);
                bool justEntered = stateChanged && target <= Tolerance;

                if (justEntered || Crossed(_lastNormalized, current, target, loop))
                    Execute(entry);
            }
        }

        private void RebuildEntryLookup()
        {
            _entriesByState.Clear();
            _anyStateEntries.Clear();

            if (Entries != null)
            {
                for (int i = 0; i < Entries.Count; i++)
                {
                    AnimationKeyEntry entry = Entries[i];
                    if (entry == null)
                        continue;

                    if (string.IsNullOrEmpty(entry.StateName))
                    {
                        _anyStateEntries.Add(entry);
                        continue;
                    }

                    int stateHash = Animator.StringToHash(entry.StateName);
                    if (!_entriesByState.TryGetValue(stateHash, out List<AnimationKeyEntry>? stateEntries))
                        _entriesByState[stateHash] = stateEntries = [];

                    stateEntries.Add(entry);
                }
            }
        }

        private void Execute(AnimationKeyEntry entry)
        {
            BlockyPayload? payload = entry.Payload;
            if (payload == null || string.IsNullOrEmpty(payload.Code))
                return;

            try
            {
                Schematic?.Executor?.Execute(ArgumentsParser.Load(payload), null!, EventType.OnAnimationKey);
            }
            catch (Exception ex)
            {
                LogManager.Error($"AnimationRuntime failed to execute key {entry.KeyIndex} of state '{entry.StateName}' on '{Object?.Name}': {ex.Message}");
            }
        }

        private static bool Crossed(float previous, float current, float target, bool loop)
        {
            if (!loop)
                return previous < target && current >= target;

            if (current < previous && previous > 0.5f && current < 0.5f)
                return target > previous || target <= current;

            return previous < target && current >= target;
        }
    }

    internal static class AnimationRuntimeDriver
    {
        private static readonly List<AnimationRuntime?> Runtimes = [];
        private static bool _installed;
        private static bool _dirty;
        private static bool _ticking;

        internal static void Shutdown()
        {
            Runtimes.Clear();
            _dirty = false;
            _ticking = false;

            if (!_installed)
                return;

            PlayerLoopSystem root = PlayerLoop.GetCurrentPlayerLoop();
            PlayerLoopSystem[] phases = root.subSystemList;

            for (int i = 0; i < phases.Length; i++)
            {
                if (phases[i].type != typeof(PreLateUpdate) || phases[i].subSystemList == null)
                    continue;

                List<PlayerLoopSystem> kept = new(phases[i].subSystemList.Length);
                foreach (PlayerLoopSystem s in phases[i].subSystemList)
                {
                    if (s.type?.FullName != typeof(AnimationRuntimeDriver).FullName)
                        kept.Add(s);
                }

                phases[i].subSystemList = kept.ToArray();
                break;
            }

            root.subSystemList = phases;
            PlayerLoop.SetPlayerLoop(root);
            _installed = false;
        }

        public static void Register(AnimationRuntime runtime, ref int index)
        {
            if (index >= 0)
                return;

            EnsureInstalled();

            index = Runtimes.Count;
            Runtimes.Add(runtime);
        }

        public static void Unregister(ref int index)
        {
            if (index < 0)
                return;

            if (index < Runtimes.Count)
            {
                if (_ticking)
                {
                    Runtimes[index] = null;
                    _dirty = true;
                }
                else
                    RemoveSwap(index);
            }

            index = -1;
        }

        private static void RemoveSwap(int index)
        {
            int last = Runtimes.Count - 1;
            if (index != last)
            {
                AnimationRuntime? moved = Runtimes[last];
                Runtimes[index] = moved;
                moved?.SetDriverIndex(index);
            }

            Runtimes.RemoveAt(last);
        }

        private static void EnsureInstalled()
        {
            if (_installed)
                return;

            PlayerLoopSystem root = PlayerLoop.GetCurrentPlayerLoop();
            PlayerLoopSystem[] phases = root.subSystemList;

            for (int i = 0; i < phases.Length; i++)
            {
                if (phases[i].type != typeof(PreLateUpdate))
                    continue;

                PlayerLoopSystem[] old = phases[i].subSystemList ?? [];
                string name = typeof(AnimationRuntimeDriver).FullName!;
                foreach (PlayerLoopSystem s in old)
                {
                    if (s.type?.FullName == name)
                    {
                        _installed = true;
                        return;
                    }
                }

                PlayerLoopSystem[] updated = new PlayerLoopSystem[old.Length + 1];
                Array.Copy(old, updated, old.Length);

                updated[old.Length] = new PlayerLoopSystem
                {
                    type = typeof(AnimationRuntimeDriver),
                    updateDelegate = Tick,
                };

                phases[i].subSystemList = updated;
                break;
            }

            root.subSystemList = phases;
            PlayerLoop.SetPlayerLoop(root);
            _installed = true;
        }

        private static void Tick()
        {
            if (Runtimes.Count == 0)
                return;

            _ticking = true;
            try
            {
                for (int i = 0; i < Runtimes.Count; i++)
                {
                    AnimationRuntime? runtime = Runtimes[i];

                    if (runtime == null)
                    {
                        _dirty = true;
                        continue;
                    }

                    runtime.Tick();
                }
            }
            finally
            {
                _ticking = false;
            }

            if (_dirty)
                Compact();
        }

        private static void Compact()
        {
            _dirty = false;
            int write = 0;

            for (int read = 0; read < Runtimes.Count; read++)
            {
                AnimationRuntime? runtime = Runtimes[read];
                if (runtime == null)
                    continue;

                Runtimes[write] = runtime;
                runtime.SetDriverIndex(write);
                write++;
            }

            Runtimes.RemoveRange(write, Runtimes.Count - write);
        }
    }
}