// -----------------------------------------------------------------------
// <copyright file="ClientObjectScaleUpdatedEventArgs.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU Lesser General Public License v3.0 (LGPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using ThaumielMapEditor.API.Blocks;

namespace ThaumielMapEditor.Events.EventArgs.Objects
{
    [GitBookPage("Events/EventArgs/ClientObjectScaleUpdatedEventArgs")]
    public class ClientObjectScaleUpdatedEventArgs : System.EventArgs
    {
        /// <summary>
        /// Gets the object whose scale changed.
        /// </summary>
        public ClientObject Object { get; }

        /// <summary>
        /// Gets the scale before the change.
        /// </summary>
        public Vector3 Previous { get; }

        /// <summary>
        /// Gets the scale after the change.
        /// </summary>
        public Vector3 Current { get; }

        public ClientObjectScaleUpdatedEventArgs(ClientObject @object, Vector3 previous, Vector3 current)
        {
            Object = @object;
            Previous = previous;
            Current = current;
        }
    }
}