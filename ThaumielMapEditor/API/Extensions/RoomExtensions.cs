// -----------------------------------------------------------------------
// <copyright file="RoomExtensions.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using LabApi.Features.Wrappers;
using MapGeneration;
using ThaumielMapEditor.API.Attributes;
using ThaumielMapEditor.API.Data;
using ThaumielMapEditor.API.Helpers;

namespace ThaumielMapEditor.API.Extensions
{
    [GitBookPage("Extensions/Room")]
    public static class RoomExtensions
    {
        /// <summary>
        /// Gets all the spawned maps in this <see cref="Room"/>.
        /// </summary>
        public static IEnumerable<MapData> GetMaps(this Room room) =>
            Loader.SpawnedMaps.Where(m => m.Room != null && m.Room == room);

        /// <summary>
        /// Gets all the spawned schematics in this <see cref="Room"/>.
        /// </summary>
        public static IEnumerable<SchematicData> GetSchematics(this Room room)
            => Loader.SchematicsById.Values.Where(s => s.Room != null && s.Room == room);

        /// <summary>
        /// Gets the closest <see cref="Room"/> to the <see cref="Vector3"/> <paramref name="pos"/>
        /// </summary>
        /// <param name="pos">The position to get the closest room from</param>
        /// <returns><see cref="Room"/> if a room was found else returns <see langword="null"/> if no room was found</returns>
        public static Room? GetClosestRoomToPosition(Vector3 pos)
        {
            Dictionary<RoomIdentifier, Room>.ValueCollection rooms = Room.Dictionary.Values;
            if (rooms.Count == 0)
                return null;

            Room? closest = null;
            float closestSqrDist = float.MaxValue;

            foreach (Room room in rooms)
            {
                if (room?.Base == null)
                    continue;

                Vector3 roomPos = room.Position;
                float dx = roomPos.x - pos.x;
                float dy = roomPos.y - pos.y;
                float dz = roomPos.z - pos.z;
                float sqrDist = (dx * dx) + (dy * dy) + (dz * dz);

                if (sqrDist < closestSqrDist)
                {
                    closestSqrDist = sqrDist;
                    closest = room;
                }
            }

            return closest;
        }

        /// <summary>
        /// Returns the local space position, based on a world space position.
        /// </summary>
        /// <param name="room">The room instance this method extends.</param>
        /// <param name="position">World position.</param>
        /// <returns>Local position, based on the room.</returns>
        public static Vector3 LocalPosition(this Room room, Vector3 position) => room.Transform.InverseTransformPoint(position);

        /// <summary>
        /// Returns the World position, based on a local space position.
        /// </summary>
        /// <param name="room">The room instance this method extends.</param>
        /// <param name="offset">Local position.</param>
        /// <returns>World position, based on the room.</returns>
        public static Vector3 WorldPosition(this Room room, Vector3 offset) => room.Transform.TransformPoint(offset);
    }
}