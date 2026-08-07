// -----------------------------------------------------------------------
// <copyright file="PickupObject.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using LabApi.Features.Wrappers;
using ThaumielMapEditor.API.Data;
using ThaumielMapEditor.API.Enums;
using ThaumielMapEditor.API.Helpers;
using ThaumielMapEditor.API.Serialization;
using YamlDotNet.Serialization;

namespace ThaumielMapEditor.API.Blocks.ServerObjects
{
    public class PickupObject : ServerObject
    {
        [YamlMember(Alias = "ItemToSpawn")]
        public ItemType ItemToSpawn { get; private set; }

        [YamlMember(Alias = "SpawnPercentage")]
        public float SpawnPercentage { get; private set; }

        [YamlMember(Alias = "MaxAmount")]
        public uint MaxAmount { get; private set; }

        [YamlMember(Alias = "IsInfinite")]
        public bool IsInfinite { get; private set; }

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

            pickup.Spawn();
            NetId = pickup.Base.netId;

            LogManager.Debug($"Spawned pickup {ItemToSpawn} at {Position}");
            base.SpawnObject(schematic, serializable);
        }
    }
}
