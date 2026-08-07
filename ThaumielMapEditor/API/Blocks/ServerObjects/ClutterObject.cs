// -----------------------------------------------------------------------
// <copyright file="ClutterObject.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using System;
using MapGeneration.RoomConnectors;
using Mirror;
using ThaumielMapEditor.API.Data;
using ThaumielMapEditor.API.Enums;
using ThaumielMapEditor.API.Helpers;
using ThaumielMapEditor.API.Serialization;
using UnityEngine;
using YamlDotNet.Serialization;

namespace ThaumielMapEditor.API.Blocks.ServerObjects
{
    public class ClutterObject : ServerObject
    {
        public override ObjectType ObjectType { get; set; } = ObjectType.Clutter;

        /// <summary>
        /// Gets the <see cref="ClutterType"/> of this clutter object.
        /// </summary>
        [YamlMember(Alias = "ClutterType")]
        public ClutterType Type { get; internal set; }
        
        /// <summary>
        /// Gets the underlying <see cref="GameObject"/> of this clutter object.
        /// </summary>
        [YamlIgnore]
        public GameObject? ClutterGameObject { get; private set; }

        /// <summary>
        /// Gets the <see cref="SpawnableClutterConnector"/> component attached to the <see cref="ClutterGameObject"/>.
        /// </summary>
        /// <remarks>
        /// This will be <see langword="null"/> if the clutter type does not have a <see cref="SpawnableClutterConnector"/> component,
        /// such as <see cref="ClutterType.BrokenElectricalBox"/>.
        /// </remarks>
        [YamlIgnore]
        public SpawnableClutterConnector? Base { get; private set; }

        /// <summary>
        /// Gets the <see cref="GameObject"/> prefab associated with the given <see cref="ClutterType"/>."
        /// </summary>
        /// <param name="type"></param>
        /// <returns><see cref="GameObject"/></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public static GameObject GetClutterPrefab(ClutterType type)
        {
#pragma warning disable CS8603 // Possible null reference return.
            return type switch
            {
                ClutterType.SimpleBoxes => PrefabHelper.SimpleBoxes,
                ClutterType.PipesShort => PrefabHelper.PipesShort,
                ClutterType.BoxesLadder => PrefabHelper.BoxesLadder,
                ClutterType.TankSupportedShelf => PrefabHelper.TankSupportedShelf,
                ClutterType.AngledFences => PrefabHelper.AngledFences,
                ClutterType.HugeOrangePipes => PrefabHelper.HugeOrangePipes,
                ClutterType.PipesLongOpen => PrefabHelper.PipesLong,
                ClutterType.BrokenElectricalBox => PrefabHelper.BrokenElectricalBox,
                _ => throw new ArgumentOutOfRangeException(nameof(type), $"Unknown ClutterType: {type}")
            };
#pragma warning restore CS8603 // Possible null reference return.
        }

        /// <inheritdoc/>
        public override void SpawnObject(SchematicData schematic, SerializableObject serializable)
        {
            GameObject clutterPrefab = UnityEngine.Object.Instantiate(GetClutterPrefab(Type));
            NetworkServer.UnSpawn(clutterPrefab);
            ClutterGameObject = clutterPrefab;
            Object = ClutterGameObject;
            if (ClutterGameObject.TryGetComponent<SpawnableClutterConnector>(out var connector))
                Base = connector;

            SetWorldTransform(schematic);

            NetworkServer.Spawn(clutterPrefab);

            if (clutterPrefab.TryGetComponent<NetworkBehaviour>(out var network))
            {
                NetId = network.netId;
            }

            base.SpawnObject(schematic, serializable);
        }
    }
}