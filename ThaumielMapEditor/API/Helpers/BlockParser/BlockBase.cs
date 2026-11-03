// -----------------------------------------------------------------------
// <copyright file="BlockBase.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU Lesser General Public License v3.0 (LGPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using System.Globalization;
using LabApi.Features.Wrappers;
using ThaumielMapEditor.API.Data;

namespace ThaumielMapEditor.API.Helpers.BlockParser
{
    public abstract class BlockBase
    {
        /// <summary>
        /// Gets the executor of the script that is currently running on this thread.
        /// </summary>
        public BlockExecutor? Executor => BlockExecution.Current?.Executor;

        /// <summary>
        /// Gets or sets the explicit target object input connected to this block.
        /// When <see langword="null"/> the block falls back to the currently active object or event player.
        /// </summary>
        public object? Target { get; set; }

        internal object? ResolveTargetValue() => ResolveValue(Target);

        internal object? ResolveTarget(object? fallback = null)
        {
            BlockExecution? execution = BlockExecution.Current;

            if (execution is { HasTarget: true })
                return execution.Target ?? fallback;

            return ResolveValue(Target, fallback as Player) ?? fallback;
        }

        public virtual void Execute()
        {

        }

        public virtual void Execute(Player player)
        {

        }

        public virtual void Execute(object obj)
        {

        }

        public virtual object? ReturnExecute() => null;

        public virtual object? ReturnExecute(object obj) => null;

        public virtual object? ReturnExecute(SchematicData schematic) => null;

        protected internal static float ResolveFloat(object? value, float defaultVal = 0f)
        {
            if (value is BlockBase block)
                value = ResolveValue(block);

            return value switch
            {
                float f => f,
                double d => (float)d,
                int i => i,
                long l => l,
                decimal m => (float)m,
                string s when float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed) => parsed,
                _ => defaultVal
            };
        }

        protected internal static string ResolveString(object? value, string defaultVal = "")
        {
            if (value is BlockBase block)
                value = ResolveValue(block);

            return value?.ToString() ?? defaultVal;
        }

        protected internal static int ResolveInt(object? value, int defaultVal = 0)
        {
            if (value is BlockBase block)
                value = ResolveValue(block);

            return value switch
            {
                int i => i,
                float f => (int)f,
                double d => (int)d,
                long l => (int)l,
                decimal m => (int)m,
                string s when int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed) => parsed,
                _ => defaultVal
            };
        }

        protected internal static bool ResolveBool(object? value, bool defaultVal = false)
        {
            if (value is BlockBase block)
                value = ResolveValue(block);

            if (value is bool b)
                return b;

            if (bool.TryParse(value?.ToString(), out bool parsed))
                return parsed;

            return defaultVal;
        }

        protected internal static object? ResolveValue(object? value, Player? player = null)
        {
            if (value is not BlockBase block)
                return value;

            player ??= BlockExecution.Current?.Player;

            object? result = block.ReturnExecute();

            if (result == null && player != null)
                result = block.ReturnExecute(player);

            return result;
        }
    }
}