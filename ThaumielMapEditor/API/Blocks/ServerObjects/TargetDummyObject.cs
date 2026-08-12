// -----------------------------------------------------------------------
// <copyright file="TargetDummyObject.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using System;
using AdminToys;
using Mirror;
using ThaumielMapEditor.API.Data;
using ThaumielMapEditor.API.Enums;
using ThaumielMapEditor.API.Helpers;
using ThaumielMapEditor.API.Serialization;
using YamlDotNet.Serialization;

namespace ThaumielMapEditor.API.Blocks.ServerObjects
{
    public class TargetDummyObject : ServerObject
    {
        /// <summary>
        /// The instantiated <see cref="ShootingTarget"/> component for the spawned object.
        /// This value is null until <see cref="SpawnObject(SchematicData, SerializableObject)"/> is called.
        /// </summary>
        [YamlIgnore]
#pragma warning disable CS8618
        public ShootingTarget Base { get; private set; }
#pragma warning restore CS8618

        /// <summary>
        /// The configured <see cref="TargetType"/> for this target.
        /// Determined by parsing serialized data before spawning.
        /// </summary>
        [YamlMember(Alias = "TargetType")]
        public TargetType Type
        {
            get;
            set
            {
                if (field == value)
                    return;

                field = value;
                NetworkServer.Destroy(Object);
                SpawnObject();
            }
        }

        /// <inheritdoc/>
        public override ObjectType ObjectType { get; set; } = ObjectType.Target;

        /// <summary>
        /// Returns the prefab <see cref="ShootingTarget"/> that corresponds to the provided <paramref name="type"/>.
        /// </summary>
        /// <param name="type">The target variant to resolve a prefab for.</param>
        /// <returns>The matching <see cref="ShootingTarget"/> prefab.</returns>
        /// <exception cref="InvalidOperationException">Thrown when an unsupported <paramref name="type"/> is provided.</exception>
        public ShootingTarget? GetPrefab(TargetType type)
        {
			ShootingTarget? prefab = type switch
			{
				TargetType.Binary => PrefabHelper.ShootingTargetBinary,
				TargetType.ClassD => PrefabHelper.ShootingTargetDBoy,
				TargetType.Sport => PrefabHelper.ShootingTargetSport,
				_ => throw new InvalidOperationException(),
			};

            return prefab;
        }

        /// <inheritdoc/>
        public override void SpawnObject(SchematicData schematic, SerializableObject serializable)
        {
            ShootingTarget? prefab = GetPrefab(Type);
            if (prefab == null) 
                return;
                
            ShootingTarget? target = UnityEngine.Object.Instantiate(prefab);
            NetworkServer.UnSpawn(target.gameObject);
            Object = target.gameObject;
            Base = target;
            SetWorldTransform(schematic);
            NetworkServer.Spawn(target.gameObject);
            NetId = target.netId;

            base.SpawnObject(schematic, serializable);
        }

        private void SpawnObject()
        {
            ShootingTarget? prefab = GetPrefab(Type);
            if (prefab == null) 
                return;
                
            ShootingTarget? target = UnityEngine.Object.Instantiate(prefab);
            NetworkServer.UnSpawn(target.gameObject);
            Object = target.gameObject;
            Base = target;
            Base.transform.SetPositionAndRotation(Position, Rotation);
            NetworkServer.Spawn(target.gameObject);
            NetId = target.netId;
        }
    }
}