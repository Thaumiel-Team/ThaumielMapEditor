// -----------------------------------------------------------------------
// <copyright file="ClientObjectRotationUpdatedEventArgs.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU Lesser General Public License v3.0 (LGPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using ThaumielMapEditor.API.Blocks;

namespace ThaumielMapEditor.Events.EventArgs.Objects
{
    [GitBookPage("Events/EventArgs/ClientObjectRotationUpdatedEventArgs")]
    public class ClientObjectRotationUpdatedEventArgs : System.EventArgs
    {
        /// <summary>
        /// Gets the object whose rotation changed.
        /// </summary>
        public ClientObject Object { get; }

        /// <summary>
        /// Gets the rotation before the change.
        /// </summary>
        public Quaternion Previous { get; }

        /// <summary>
        /// Gets the rotation after the change.
        /// </summary>
        public Quaternion Current { get; }

        public ClientObjectRotationUpdatedEventArgs(ClientObject @object, Quaternion previous, Quaternion current)
        {
            Object = @object;
            Previous = previous;
            Current = current;
        }
    }
}