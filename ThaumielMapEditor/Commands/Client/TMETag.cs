// -----------------------------------------------------------------------
// <copyright file="TMETag.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU Lesser General Public License v3.0 (LGPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using System;
using CommandSystem;
using LabApi.Features.Wrappers;
using ThaumielMapEditor.API.Helpers;
using static ThaumielMapEditor.API.Helpers.CreditHelper;

namespace ThaumielMapEditor.Commands.Client
{
    [CommandHandler(typeof(ClientCommandHandler))]
    public class TMETag : ICommand
    {
        public string Command => nameof(TMETag).ToLower();

        public string[] Aliases => ["tmet"];

        public string Description => "Applies your ThaumielMapEditor credit tag if you have one.";

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            if (!Player.TryGet(sender, out var player) || player.IsHost)
            {
                response = "You have to be a player to run this command!";
                return false;
            }

            TagType tag = CreditHelper.SetTag(player);

            response = $"Applied credit tag {tag}";
            return true;
        }
    }
}