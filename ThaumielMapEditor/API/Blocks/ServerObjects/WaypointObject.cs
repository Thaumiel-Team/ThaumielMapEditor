// -----------------------------------------------------------------------
// <copyright file="WaypointObject.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using AdminToys;
using Mirror;
using ThaumielMapEditor.API.Data;
using ThaumielMapEditor.API.Enums;
using ThaumielMapEditor.API.Helpers;
using ThaumielMapEditor.API.Serialization;
using YamlDotNet.Serialization;

namespace ThaumielMapEditor.API.Blocks.ServerObjects
{
    public class WaypointObject : ServerObject
    {
        /// <summary>
        /// Reference to the instantiated <see cref="WaypointToy"/> on the server.
        /// </summary>
        [YamlIgnore]
#pragma warning disable CS8618
        public WaypointToy Base { get; private set; }
#pragma warning restore CS8618

        /// <inheritdoc/>
        public override ObjectType ObjectType { get; set; } = ObjectType.Waypoint;

        /// <summary>
        /// Whether the waypoint's bounds are visualized in the editor/runtime.
        /// Setting this property updates the underlying <see cref="WaypointToy.VisualizeBounds"/>
        /// when the toy instance is available.
        /// </summary>
        [YamlMember(Alias = "VisualizeBounds")]
        public bool VisualizeBounds
        {
            get;

            set
            {
                if (field == value)
                    return;

                field = value;
                Base?.VisualizeBounds = value;
            }
        }

        /// <summary>
        /// Priority value for the waypoint. Higher values can be used to influence
        /// ordering or selection logic that consumes waypoint priorities.
        /// Setting this property updates the underlying <see cref="WaypointToy.Priority"/>
        /// when the toy instance is available.
        /// </summary>
        [YamlMember(Alias = "Priority")]
        public float Priority
        {
            get;
            
            set
            {
                if (field == value)
                    return;

                field = value;
                Base?.Priority = value;
            }
        }

        /// <summary>
        /// Size of the waypoint bounds as a <see cref="Vector3"/>
        /// Setting this property updates the underlying <see cref="WaypointToy.BoundsSize"/>
        /// when the toy instance is available.
        /// </summary>
        [YamlMember(Alias = "BoundsSize")]
        public Vector3 BoundsSize
        {
            get;

            set
            {
                if (field == value)
                    return;

                field = value;
                Base?.BoundsSize = value;
            }
        }

        /// <summary>
        /// Identifier assigned to this waypoint instance.
        /// </summary>
        [YamlIgnore]
        public byte WaypointId { get; private set; }

        /// <inheritdoc/>
        public override void SpawnObject(SchematicData schematic, SerializableObject serializable)
        {
            if (PrefabHelper.WaypointToy == null)
            {
                LogManager.Warn($"Failed to spawn Waypoint. Prefab is null");
                return;
            }

            WaypointToy toy = UnityEngine.Object.Instantiate(PrefabHelper.WaypointToy);
            NetworkServer.UnSpawn(toy.gameObject);
            toy.VisualizeBounds = VisualizeBounds;
            toy.Priority = Priority;
            toy.BoundsSize = BoundsSize;
            Object = toy.gameObject;
            SetWorldTransform(schematic);
            NetworkServer.Spawn(toy.gameObject);
            NetId = toy.netId;

            base.SpawnObject(schematic, serializable);
        }

        public void SpawnObject(SchematicData schematic)
        {
            if (PrefabHelper.WaypointToy == null)
            {
                LogManager.Warn($"Failed to spawn Waypoint. Prefab is null");
                return;
            }

            WaypointToy toy = UnityEngine.Object.Instantiate(PrefabHelper.WaypointToy);
            NetworkServer.UnSpawn(toy.gameObject);
            toy.VisualizeBounds = VisualizeBounds;
            toy.Priority = Priority;
            toy.BoundsSize = BoundsSize;
            Object = toy.gameObject;
            SetWorldTransform(schematic);
            NetworkServer.Spawn(toy.gameObject);
            NetId = toy.netId;
        }
    }
}