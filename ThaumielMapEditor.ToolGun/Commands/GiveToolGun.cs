// -----------------------------------------------------------------------
// <copyright file="GiveToolGun.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using System;
using CommandSystem;
using CustomItemsAPI;
using LabApi.Features.Wrappers;
using ThaumielMapEditor.API.Interfaces;

namespace ThaumielMapEditor.ToolGun.Commands
{
    public class GiveToolGun : SubCommand
    {
        public override string Name => "givetoolgun";

        public override string RequiredPermission => "tme.toolgun.give";

        public override string Description => "Gives the sender the TME ToolGun";

        public override string[] Aliases => ["toolgun", "tg", "givetg"];

        public override bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            if (!Player.TryGet(sender, out var player))
            {
                response = "You must be a player to use this command!";
                return false;
            }

            if (player.IsInventoryFull)
            {
                response = "Your inventory is full!";
                return false;
            }

            CustomItems.AddCustomItem<ToolGun>(player);
            response = $"Gave ToolGun to {player.DisplayName}";
            return true;
        }
    }
}