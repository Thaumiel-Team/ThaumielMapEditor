// -----------------------------------------------------------------------
// <copyright file="CancellableEventArgs.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU Lesser General Public License v3.0 (LGPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

namespace ThaumielMapEditor.Events.EventArgs
{
    [GitBookPage("Events/EventArgs/CancellableEventArgs")]
    public class CancellableEventArgs : System.EventArgs
    {
        /// <summary>
        /// Gets or sets a value indicating whether the action is allowed to continue.
        /// </summary>
        public bool IsAllowed { get; set; } = true;
    }
}