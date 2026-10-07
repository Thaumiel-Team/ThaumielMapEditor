// -----------------------------------------------------------------------
// <copyright file="SpawnedServerObjectEventArgs.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using ThaumielMapEditor.API.Blocks;

namespace ThaumielMapEditor.Events.EventArgs.Objects
{
    [GitBookPage("Events/EventArgs/SpawnedServerObjectEventArgs")]
    public class SpawnedServerObjectEventArgs : System.EventArgs
    {
        public ServerObject Object { get; }

        public SpawnedServerObjectEventArgs(ServerObject @object)
        {
            Object = @object;
        }
    }
}