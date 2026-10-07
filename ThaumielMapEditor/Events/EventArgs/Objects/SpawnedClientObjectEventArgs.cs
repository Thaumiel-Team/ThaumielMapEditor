// -----------------------------------------------------------------------
// <copyright file="SpawnedClientObjectEventArgs.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using LabApi.Features.Wrappers;
using ThaumielMapEditor.API.Blocks.ClientSide;

namespace ThaumielMapEditor.Events.EventArgs.Objects
{
    [GitBookPage("Events/EventArgs/SpawnedClientObjectEventArgs")]
    public class SpawnedClientObjectEventArgs : System.EventArgs
    {
        public ClientObject Object { get; }
        public Player Player { get; }

        public SpawnedClientObjectEventArgs(ClientObject @object, Player player)
        {
            Object = @object;
            Player = player;
        }
    }
}