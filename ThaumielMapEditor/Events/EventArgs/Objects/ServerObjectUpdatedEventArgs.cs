// -----------------------------------------------------------------------
// <copyright file="ServerObjectUpdatedEventArgs.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU Lesser General Public License v3.0 (LGPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using ThaumielMapEditor.API.Blocks;

namespace ThaumielMapEditor.Events.EventArgs.Objects
{
    [GitBookPage("Events/EventArgs/ServerObjectUpdatedEventArgs")]
    public class ServerObjectUpdatedEventArgs : System.EventArgs
    {
        /// <summary>
        /// Gets the object whose transform was updated.
        /// </summary>
        public ServerObject Object { get; }

        /// <summary>
        /// Gets a value indicating whether the object was respawned on the network.
        /// </summary>
        public bool Respawn { get; }

        public ServerObjectUpdatedEventArgs(ServerObject @object, bool respawn)
        {
            Object = @object;
            Respawn = respawn;
        }
    }
}