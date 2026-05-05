// -----------------------------------------------------------------------
// <copyright file="Grab.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using CommandSystem;
using LabApi.Features.Wrappers;
using MEC;
using ThaumielMapEditor.API.Attributes;
using ThaumielMapEditor.API.Blocks;
using ThaumielMapEditor.API.Data;
using ThaumielMapEditor.API.Helpers;
using ThaumielMapEditor.API.Interfaces;

namespace ThaumielMapEditor.Commands.Admin
{
#pragma warning disable CS1591
    [DoNotParse]
    public class Grab : ISubCommand
    {
        public override string Name => "grab";
        public override string VisibleArgs => "<Schematic ID>";
        public override string Description => "Grabs the specified schematic";
        public override string[] Aliases => ["gr"];
        public override string RequiredPermission => "tme.grab";

        public override bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            if (!Player.TryGet(sender, out var player))
            {
                response = "You must be a player to run this command!";
                return false;
            }

            if (GrabPlayers.ContainsKey(player))
            {
                Timing.KillCoroutines(GrabPlayers[player]);
                GrabPlayers.Remove(player);

                response = $"Ungrabbed";
                return true;
            }

            if (arguments.Count == 0 || !uint.TryParse(arguments.At(0), out var id))
            {
                response = "Invalid ID. This should be a non negative number. Run 'tme spawned' to get all spawned schematics";
                return false;
            }

            if (!Loader.TryGetSchematicById(id, out var schematic))
            {
                response = $"Failed to find schematic with the ID {id}. Run 'tme spawned' to get all spawned schematics";
                return false;
            }

    		GrabPlayers.Add(player, Timing.RunCoroutine(GrabCoroutine(player, schematic)));
            response = $"Grabbed schematic with ID: {id}";
            return true;
        }

	    private static readonly Dictionary<Player, CoroutineHandle> GrabPlayers = [];

        public IEnumerator<float> GrabCoroutine(Player player, SchematicData schematic)
        {
            Vector3 pos = player.Camera.position;
            float multiplier = Vector3.Distance(pos, schematic.Position);
            Vector3 prevPos = pos + (player.Camera.forward * multiplier);

            while (true)
            {
                yield return Timing.WaitForSeconds(0.1f);

                if (schematic == null || schematic.Primitive == null || schematic.Primitive.IsDestroyed || !player.IsAlive)
                    break;
                
                Vector3 newPos = schematic.Position = player.Camera.position + (player.Camera.forward * multiplier);

                if (prevPos == newPos)
				    continue;

                prevPos = newPos;
                schematic.Primitive.Position = prevPos;

                foreach (ServerObject serverObject in schematic.SpawnedServerObjects)
                {
                    serverObject.UpdateObject(schematic, true);
                }
            }

    		GrabPlayers.Remove(player);
        }
    }
}