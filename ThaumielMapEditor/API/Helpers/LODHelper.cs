// -----------------------------------------------------------------------
// <copyright file="LODHelper.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using DrawableLine;
using LabApi.Features.Wrappers;
using ThaumielMapEditor.API.Components;
using ThaumielMapEditor.API.Data;
using ThaumielMapEditor.API.Serialization;
using UnityEngine;

namespace ThaumielMapEditor.API.Helpers
{
    public class LODHelper
    {
        /// <summary>
        /// Gets or sets the <see cref="Player"/>s that are in a <see cref="LODZone"/>
        /// </summary>
        public static Dictionary<Player, HashSet<LODZone>> PlayersInLODZones { get; set; } = [];

        public static void DrawLines(SchematicData schematic)
        {
            if (schematic.LODZones.IsEmpty())
                return;

            foreach (LODData data in schematic.LODZones)
            {
                DrawableLines.GenerateBounds(new (schematic.Position, data.Bounds));
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="schematic"></param>
        /// <param name="serializable"></param>
        /// <returns></returns>
        public static List<LODData>? GenerateLODZones(SchematicData schematic, SerializableSchematic serializable)
        {
            List<LODData> lodData = [];

            uint index = 0;
            foreach (SerializableLOD lod in serializable.LOD)
            {
                LODData data = new()
                {
                    Index = ++index,
                    Bounds = lod.Bounds,
                    Primitives = lod.Primitives
                };

                GameObject colliderobj = new($"{schematic.FileName}-LOD{data.Index}-ColliderObj");
                colliderobj.transform.SetParent(schematic.Primitive?.GameObject.transform);
                BoxCollider collider = colliderobj.AddComponent<BoxCollider>();
                collider.size = data.Bounds;
                collider.name = $"{schematic.FileName}-LOD{data.Index}-Collider";
                collider.isTrigger = true;

                LODZone lodZone = colliderobj.AddComponent<LODZone>();
                lodZone.Init(schematic, data.Primitives, data.Index);

                lodData.Add(data);
                Loader.SchematicLODZones.Add(lodZone, schematic);
            }

            schematic.LODZones = lodData;
            return lodData;
        }

        /// <summary>
        /// Gets the players that are inside of the specified <see cref="LODZone"/> index.
        /// </summary>
        /// <param name="index"></param>
        /// <param name="schematic"></param>
        /// <returns></returns>
        public static IEnumerable<Player> PlayersInsideZone(uint index, SchematicData schematic)
        {
            List<Player> players = [];
            LODZone? lod = null;
            GameObject? root = schematic.Primitive?.GameObject;
            if (root != null)
            {
                foreach (LODZone lodzone in root.GetComponents<LODZone>())
                {
                    if (lodzone.Index != index)
                        continue;

                    lod = lodzone;
                    break;
                }
            }

            if (lod?.Collider == null)
                return players;

            Bounds bounds = lod.Collider.bounds;
            foreach (Player player in Player.ReadyList.ToArray())
            {
                if (player == null || player.IsHost || player.IsDestroyed)
                    continue;

                if (bounds.Contains(player.Position))
                    players.Add(player);
            }

            return players;
        }

        /// <summary>
        /// Gets the <see cref="Player"/>s in the specified <see cref="LODZone"/>
        /// </summary>
        /// <param name="zone"></param>
        /// <returns></returns>
        public static IEnumerable<Player> GetPlayersInZone(LODZone zone)
        {
            List<Player> players = [];

            if (zone?.Collider == null)
                return players;

            Bounds bounds = zone.Collider.bounds;
            foreach (Player player in Player.ReadyList.ToArray())
            {
                if (player == null || player.IsHost || player.IsDestroyed)
                    continue;

                if (bounds.Contains(player.Position))
                    players.Add(player);
            }

            return players;
        }
    }
}