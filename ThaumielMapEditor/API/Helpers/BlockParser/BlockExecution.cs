// -----------------------------------------------------------------------
// <copyright file="BlockExecution.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU Lesser General Public License v3.0 (LGPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using LabApi.Features.Wrappers;

namespace ThaumielMapEditor.API.Helpers.BlockParser
{
    public sealed class BlockExecution : IDisposable
    {
        [ThreadStatic]
        private static BlockExecution? _current;

        private BlockExecution? _previous;

        public BlockExecution(BlockExecutor executor, ScriptProgram program, Player? player)
        {
            Executor = executor;
            Program = program;
            Player = player;
        }

        /// <summary>
        /// Gets the execution currently running on this thread, or <see langword="null"/> when no script is active.
        /// </summary>
        public static BlockExecution? Current => _current;

        public BlockExecutor Executor { get; }

        public ScriptProgram Program { get; }

        public Player? Player { get; }

        /// <summary>
        /// Gets the transient variable scopes for this run.
        /// The bottom of the stack is the root scope; variables assigned there outlive the run through <see cref="BlockExecutor.Globals"/>.
        /// </summary>
        public Stack<Dictionary<string, object?>> Scopes { get; } = new();

        /// <summary>
        /// Gets or sets the object a block is currently acting on.
        /// Only set while a block is being dispatched.
        /// </summary>
        public object? Target { get; set; }

        public bool HasTarget { get; set; }

        /// <summary>
        /// Gets or sets the object produced by the most recent create block.
        /// </summary>
        public object? PendingObject { get; set; }

        /// <summary>
        /// Makes this execution the ambient one for the calling thread.
        /// </summary>
        public BlockExecution Enter()
        {
            _previous = _current;
            _current = this;
            return this;
        }

        public void Dispose()
        {
            if (ReferenceEquals(_current, this))
                _current = _previous;
        }

        /// <summary>
        /// Wraps a deferred callback so it runs with this execution context restored.
        /// </summary>
        public Action Wrap(Action callback) => () =>
        {
            BlockExecution? previous = _current;
            _current = this;

            try
            {
                callback();
            }
            finally
            {
                _current = previous;
            }
        };

        /// <summary>
        /// Wraps a deferred predicate so it runs with this execution context restored.
        /// </summary>
        public Func<T> Wrap<T>(Func<T> callback) => () =>
        {
            BlockExecution? previous = _current;
            _current = this;

            try
            {
                return callback();
            }
            finally
            {
                _current = previous;
            }
        };

        internal static BlockExecution Require() =>
            _current ?? throw new InvalidOperationException("No block execution is active on this thread.");
    }
}