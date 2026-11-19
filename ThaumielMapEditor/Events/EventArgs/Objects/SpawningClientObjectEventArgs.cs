// -----------------------------------------------------------------------
// <copyright file="SpawningClientObjectEventArgs.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU Lesser General Public License v3.0 (LGPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using LabApi.Features.Wrappers;
using ThaumielMapEditor.API.Blocks;

namespace ThaumielMapEditor.Events.EventArgs.Objects
{
    [GitBookPage("Events/EventArgs/SpawningClientObjectEventArgs")]
    public class SpawningClientObjectEventArgs : CancellableEventArgs
    {
        /// <summary>
        /// Gets the object that is being spawned.
        /// </summary>
        public ClientObject Object { get; }

        /// <summary>
        /// Gets the player the object is being spawned for.
        /// </summary>
        public Player Player { get; }

        public SpawningClientObjectEventArgs(ClientObject @object, Player player)
        {
            Object = @object;
            Player = player;
        }
    }
}