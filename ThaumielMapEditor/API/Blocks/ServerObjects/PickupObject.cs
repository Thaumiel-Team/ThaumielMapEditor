// -----------------------------------------------------------------------
// <copyright file="PickupObject.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using LabApi.Features.Wrappers;
using Mirror;
using ThaumielMapEditor.API.Data;
using ThaumielMapEditor.API.Enums;
using ThaumielMapEditor.API.Helpers;
using ThaumielMapEditor.API.Serialization;
using YamlDotNet.Serialization;

namespace ThaumielMapEditor.API.Blocks.ServerObjects
{
    [GitBookPage("Blocks/Server/PickupObject")]
    public class PickupObject : ServerObject
    {
        [YamlMember(Alias = "ItemToSpawn")]
        public ItemType ItemToSpawn
        {
            get;
            set
            {
                if (field == value)
                    return;

                field = value;

                if (Object == null)
                    return;

                NetworkServer.Destroy(Object);
                Respawn();
            }
        }

        [YamlMember(Alias = "SpawnPercentage")]
        public float SpawnPercentage { get; set; }

        [YamlMember(Alias = "MaxAmount")]
        public uint MaxAmount { get; set; }

        [YamlMember(Alias = "IsInfinite")]
        public bool IsInfinite { get; set; }

        public override ObjectType ObjectType { get; set; } = ObjectType.Pickup;

        public override void SpawnObject(SchematicData schematic, SerializableObject serializable)
        {
            SetWorldTransform(schematic);
            
            if (SpawnPercentage < 100f && UnityEngine.Random.Range(0f, 100f) > SpawnPercentage)
                return;

            Pickup? pickup = Pickup.Create(ItemToSpawn, Position, Rotation);
            if (pickup == null)
            {
                LogManager.Warn($"Failed to create pickup of type {ItemToSpawn}.");
                return;
            }

            Object = pickup.GameObject;
            Object.transform.localScale = Scale;

            pickup.Spawn();
            NetId = pickup.Base.netId;

            LogManager.Debug($"Spawned pickup {ItemToSpawn} at {Position}");
            base.SpawnObject(schematic, serializable);
        }

        /// <summary>
        /// Respawns this pickup in place with the current <see cref="ItemToSpawn"/>.
        /// </summary>
        public void Respawn()
        {
            Pickup? pickup = Pickup.Create(ItemToSpawn, Position, Rotation);
            if (pickup == null)
            {
                LogManager.Warn($"Failed to respawn pickup of type {ItemToSpawn}.");
                return;
            }

            Object = pickup.GameObject;
            pickup.Spawn();
            NetId = pickup.Base.netId;
        }
    }
}
