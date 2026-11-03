// -----------------------------------------------------------------------
// <copyright file="CommandHelper.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU Lesser General Public License v3.0 (LGPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using LabApi.Features.Wrappers;
using ThaumielMapEditor.API.Data;
using ThaumielMapEditor.API.Helpers;
using UnityEngine;

namespace ThaumielMapEditor.Commands
{
    public class CommandHelper
    {
        public static SchematicData? GetSchematic(Player player)
        {
            if (!Physics.Raycast(player.Camera.transform.position + player.Camera.transform.forward, player.Camera.transform.forward, out RaycastHit hit, 50))
                return null;

            foreach (KeyValuePair<SchematicData, HashSet<Collider>> kvp in ColliderHelper.SchematicColliders.Where(s => s.Value.Contains(hit.collider)))
            {
                if (kvp.Key == null || kvp.Value.IsEmpty())
                    continue;

                return kvp.Key;
            }

            return null;
        }
    }
}