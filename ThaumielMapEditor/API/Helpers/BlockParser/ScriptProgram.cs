// -----------------------------------------------------------------------
// <copyright file="ScriptProgram.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU Lesser General Public License v3.0 (LGPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using ThaumielMapEditor.API.Enums;

namespace ThaumielMapEditor.API.Helpers.BlockParser
{
    public sealed class ScriptProgram
    {
        public static readonly ScriptProgram Empty = new([]);

        private readonly Dictionary<EventType, List<EventBlock>> _events = [];

        public ScriptProgram(IEnumerable<object> blocks)
        {
            List<object> list = blocks.ToList();
            Blocks = list;

            Dictionary<string, MethodBlock> procedures = new(StringComparer.Ordinal);

            foreach (object block in list)
            {
                switch (block)
                {
                    case MethodBlock method:
                        procedures[method.Name] = method;
                        break;

                    case EventBlock eventBlock:
                        if (!_events.TryGetValue(eventBlock.EventType, out List<EventBlock>? handlers))
                            _events[eventBlock.EventType] = handlers = [];

                        handlers.Add(eventBlock);
                        break;
                }
            }

            Procedures = procedures;
        }

        /// <summary>
        /// Gets every top level block of the script, in declaration order.
        /// </summary>
        public IReadOnlyList<object> Blocks { get; }

        /// <summary>
        /// Gets the procedures declared by the script, keyed by name.
        /// </summary>
        public IReadOnlyDictionary<string, MethodBlock> Procedures { get; }

        /// <summary>
        /// Gets the handlers registered for <paramref name="eventType"/>, or an empty list when the script ignores it.
        /// </summary>
        public IReadOnlyList<EventBlock> GetHandlers(EventType eventType) =>
            _events.TryGetValue(eventType, out List<EventBlock>? handlers) ? handlers : [];
    }
}