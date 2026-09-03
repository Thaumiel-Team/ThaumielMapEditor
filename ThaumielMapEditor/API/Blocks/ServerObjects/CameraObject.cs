// -----------------------------------------------------------------------
// <copyright file="CameraObject.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using AdminToys;
using LabApi.Features.Wrappers;
using MapGeneration;
using Mirror;
using System;
using System.Linq;
using ThaumielMapEditor.API.Data;
using ThaumielMapEditor.API.Enums;
using ThaumielMapEditor.API.Helpers;
using ThaumielMapEditor.API.Serialization;
using UnityEngine;
using YamlDotNet.Serialization;
using CameraType = ThaumielMapEditor.API.Enums.CameraType;

namespace ThaumielMapEditor.API.Blocks.ServerObjects
{
    public class CameraObject : ServerObject
    {
        /// <summary>
        /// The underlying in game camera toy instance.
        /// </summary>
#pragma warning disable CS8618
        [YamlIgnore]
        public Scp079CameraToy Base { get; internal set; }
#pragma warning restore CS8618

        /// <summary>
        /// The camera prefab type (mapped to a specific prefab via <see cref="GetCameraPrefab"/>).
        /// </summary>
        [YamlMember(Alias = "CameraType")]
        public CameraType Type { get; set; }

        /// <summary>
        /// Display label for the camera. Setting this property updates the networked label on
        /// the underlying <see cref="Base"/> when available.
        /// </summary>
        [YamlMember(Alias = "Label")]
        public string Label
        {
            get;
            set
            {
                if (field == value || Base == null)
                    return;

                Base.NetworkLabel = value;
                field = value;
            }
        } = string.Empty;

        /// <summary>
        /// The <see cref="Room"/> that the camera belongs to.
        /// Setting this property updates <see cref="Scp079CameraToy.NetworkRoom"/> on <see cref="Base"/>.
        /// </summary>
        [YamlIgnore]
        public Room Room
        {
            get;
            set
            {
                if (field == value || Base == null)
                    return;

                Base.NetworkRoom = value.Base;
                field = value;
            }
        } = Room.Get(RoomName.Outside).First();

        /// <summary>
        /// Proxy property for <see cref="Room"/> to allow serialization as <see cref="RoomName"/>.
        /// </summary>
        [YamlMember(Alias = "Room")]
        public RoomName RoomName
        {
            get => Room?.Name ?? RoomName.Outside;
            set => Room = Room.Get(value).First();
        }

        /// <summary>
        /// Vertical rotation constraint applied to the camera.
        /// When set, the value is copied to <see cref="Scp079CameraToy.NetworkVerticalConstraint"/>.
        /// </summary>
        [YamlMember(Alias = "VerticalConstraint")]
        public Vector2 VerticalConstraint
        {
            get;
            set
            {
                if (field == value || Base == null)
                    return;

                Base.NetworkVerticalConstraint = value;
                field = value;
            }
        }

        /// <summary>
        /// Horizontal rotation constraint applied to the camera.
        /// When set, the value is copied to <see cref="Scp079CameraToy.NetworkHorizontalConstraint"/>.
        /// </summary>
        [YamlMember(Alias = "HorizontalConstraint")]
        public Vector2 HorizontalConstraint
        {
            get;
            set
            {
                if (field == value || Base == null)
                    return;

                Base.NetworkHorizontalConstraint = value;
                field = value;
            }
        }
        
        /// <summary>
        /// Zoom constraint for the camera (min/max).
        /// When set, the value is copied to <see cref="Scp079CameraToy.NetworkZoomConstraint"/>.
        /// </summary>
        [YamlMember(Alias = "ZoomConstraint")]
        public Vector2 ZoomConstraint
        {
            get;
            set
            {
                if (field == value || Base == null)
                    return;
                
                Base.NetworkZoomConstraint = value;
                field = value;
            }
        }

        /// <inheritdoc/>
        public override ObjectType ObjectType { get; set; } = ObjectType.Camera;

        /// <summary>
        /// Returns the corresponding camera prefab instance for the provided <paramref name="type"/>.
        /// </summary>
        /// <param name="type">The camera type to resolve to a prefab.</param>
        /// <returns>The matching <see cref="Scp079CameraToy"/> prefab from <see cref="PrefabHelper"/>.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when an unknown <paramref name="type"/> is provided.</exception>
        public static Scp079CameraToy GetCameraPrefab(CameraType type)
        {
#pragma warning disable CS8603 // Possible null reference return.
            return type switch
            {
                CameraType.Ez => PrefabHelper.CameraEz,
                CameraType.EzArm => PrefabHelper.CameraEzArm,
                CameraType.Hcz => PrefabHelper.CameraHcz,
                CameraType.Lcz => PrefabHelper.CameraLcz,
                CameraType.Sz => PrefabHelper.CameraSz,
                _ => throw new ArgumentOutOfRangeException(nameof(type), $"Unknown CameraType: {type}")
            };
#pragma warning restore CS8603 // Possible null reference return.
        }

        /// <inheritdoc/>
        public override void SpawnObject(SchematicData schematic, SerializableObject serializable)
        {
            Scp079CameraToy camera = UnityEngine.Object.Instantiate(GetCameraPrefab(Type));
            NetworkServer.UnSpawn(camera.gameObject);
            Base = camera;
            Object = camera.gameObject;
            SetWorldTransform(schematic);
            NetworkServer.Spawn(camera.gameObject);
        }
    }
}