// -----------------------------------------------------------------------
// <copyright file="PickupObject.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using LabApi.Features.Wrappers;
using ThaumielMapEditor.API.Binary.Attributes;
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
        [YamlIgnore]
        public static List<PickupObject> Instances = [];
        
        [YamlIgnore]
        public uint SpawnAmount { get; set; }

        [YamlIgnore]
        public HashSet<ushort> SpawnedSerials = [];

        public static void Cleanup()
        {
            foreach (PickupObject instance in Instances)
            {
                instance.SpawnedSerials.Clear();
            }

            Instances.Clear();
        }

        public static bool IsItem(Item item, out PickupObject? inst)
        {
            foreach (PickupObject instance in Instances)
            {
                if (instance.SpawnedSerials.Contains(item.Serial))
                {
                    inst = instance;
                    return true;
                }
            }

            inst = null;
            return false;
        }

        public static bool IsPickup(Pickup pickup, out PickupObject? inst)
        {
            foreach (PickupObject instance in Instances)
            {
                if (instance.SpawnedSerials.Contains(pickup.Serial))
                {
                    inst = instance;
                    return true;
                }
            }

            inst = null;
            return false;
        }

        [YamlMember(Alias = "ItemToSpawn")]
        [BinaryProperty(Name = "ItemToSpawn")]
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

                Respawn();
            }
        }

        [YamlMember(Alias = "SpawnPercentage")]
        [BinaryProperty(Name = "SpawnPercentage")]
        public float SpawnPercentage { get; set; }

        [YamlMember(Alias = "MaxAmount")]
        [BinaryProperty(Name = "MaxAmount")]
        public uint MaxAmount { get; set; }

        [YamlMember(Alias = "IsInfinite")]
        [BinaryProperty(Name = "IsInfinite")]
        public bool IsInfinite { get; set; }

        public override ObjectType ObjectType { get; set; } = ObjectType.Pickup;

        public override void SpawnObject(SchematicData schematic, SerializableObject serializable)
        {
            SetWorldTransform(schematic);
            
            if (SpawnPercentage < 100f && UnityEngine.Random.Range(0f, 100f) > SpawnPercentage)
                return;

            SpawnPickup();
            Object = new();
            Object.transform.SetPositionAndRotation(Position, Rotation);
            Object.transform.localScale = Scale;
            SelectionProxyHelper.AddTrigger(Object);

            LogManager.Debug($"Spawned pickup {ItemToSpawn} at {Position}");
            Instances.Add(this);
            base.SpawnObject(schematic, serializable);
        }

        /// <inheritdoc/>
        public override void DestroyObject(SchematicData schematic)
        {
            Instances.Remove(this);
            DestroyLivePickups();
            SelectionProxyHelper.DestroyProxy(this);
            base.DestroyObject(schematic);
        }

        /// <summary>
        /// Respawns this pickup in place with the current <see cref="ItemToSpawn"/>.
        /// </summary>
        public void Respawn()
        {
            DestroyLivePickups();
            SpawnAmount = 0;
            SpawnPickup();
        }

        public void SpawnPickup()
        {
            if (MaxAmount > 0 && SpawnAmount >= MaxAmount)
                return;

            Pickup? pickup = Pickup.Create(ItemToSpawn, Position, Rotation, Scale);
            if (pickup == null)
            {
                LogManager.Warn($"Failed to create pickup of type {ItemToSpawn}.");
                return;
            }

            pickup.Spawn();
            SpawnedSerials.Add(pickup.Serial);
            SpawnAmount++;
            NetId = pickup.Base.netId;
        }

        private void DestroyLivePickups()
        {
            if (SpawnedSerials.Count == 0)
                return;

            foreach (Pickup pickup in Pickup.List.ToArray())
            {
                if (pickup == null || pickup.IsDestroyed)
                    continue;

                if (!SpawnedSerials.Contains(pickup.Serial))
                    continue;

                pickup.Destroy();
            }

            SpawnedSerials.Clear();
        }
    }
}
