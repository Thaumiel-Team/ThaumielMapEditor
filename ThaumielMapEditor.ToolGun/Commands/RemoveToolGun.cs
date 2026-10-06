// -----------------------------------------------------------------------
// <copyright file="RemoveToolGun.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Linq;
using CommandSystem;
using CustomItemsAPI;
using LabApi.Features.Wrappers;
using ThaumielMapEditor.API.Interfaces;

namespace ThaumielMapEditor.ToolGun.Commands
{
    public class RemoveToolGun : SubCommand
    {
        public override string Name => "removetoolgun";

        public override string RequiredPermission => "tme.toolgun.remove";

        public override string Description => "Removes the TME ToolGun from the specified player";

        public override string[] Aliases => ["removetg"];

        public override bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            if (arguments.Count() >= 1)
            {
                if (!uint.TryParse(arguments.At(0), out var id))
                {
                    response = $"Failed to parse argument {arguments.At(0)} to uint";
                    return false;
                }

                if (!Player.TryGet(id, out var player))
                {
                    response = $"Failed to find player with id {id}!";
                    return false;
                }

                if (!TryRemoveToolGun(player))
                {
                    response = $"Failed to find toolgun on {player.DisplayName}";
                    return false;
                }

                response = $"Removed ToolGun from {player.DisplayName}";
                return true;
            }
            else
            {
                if (!Player.TryGet(sender, out var player))
                {
                    response = "Specify a player!";
                    return false;
                }

                if (!TryRemoveToolGun(player))
                {
                    response = $"Failed to find toolgun on {player.DisplayName}";
                    return false;
                }

                response = $"Removed ToolGun from {player.DisplayName}";
                return true;
            }
        }

        internal static bool TryRemoveToolGun(Player player)
        {
            bool found = false;

            foreach (Item item in player.Items)
            {
                if (!CustomItems.TryGetCustomItem<ToolGun>(item, out _))
                    continue;

                player.RemoveItem(item);
                found = true;
            }

            return found;
        }
    }
}