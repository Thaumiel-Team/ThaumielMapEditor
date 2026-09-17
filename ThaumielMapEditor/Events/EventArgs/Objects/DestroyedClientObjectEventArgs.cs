// -----------------------------------------------------------------------
// <copyright file="DestroyedClientObjectEventArgs.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using LabApi.Features.Wrappers;
using ThaumielMapEditor.API.Blocks.ClientSide;

namespace ThaumielMapEditor.Events.EventArgs.Objects
{
    [GitBookPage("Events/EventArgs/DestroyedClientObjectEventArgs")]
    public class DestroyedClientObjectEventArgs : System.EventArgs
    {
        public ClientObject Object { get; }
        public Player Player { get; }

        public DestroyedClientObjectEventArgs(ClientObject @object, Player player)
        {
            Object = @object;
            Player = player;
        }
    }
}