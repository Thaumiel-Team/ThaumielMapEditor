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
    [GitBookPage("Blocks/Server/CameraObject")]
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
        /// The camera prefab type.
        /// Setting this on a spawned camera respawns it in place with the new prefab.
        /// </summary>
        [YamlMember(Alias = "CameraType")]
        public CameraType Type
        {
            get;
            set
            {
                if (field == value)
                    return;

                field = value;

                if (Base == null || Object == null)
                    return;

                NetworkServer.Destroy(Object);
                Respawn();
            }
        }

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
                if (field == value)
                    return;

                field = value;

                if (Base == null)
                    return;

                Base.NetworkLabel = value;
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
                if (field == value)
                    return;

                field = value;

                if (Base == null || value == null)
                    return;

                Base.NetworkRoom = value.Base;
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
                if (field == value)
                    return;

                field = value;

                if (Base == null)
                    return;

                Base.NetworkVerticalConstraint = value;
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
                if (field == value)
                    return;

                field = value;

                if (Base == null)
                    return;

                Base.NetworkHorizontalConstraint = value;
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
                if (field == value)
                    return;

                field = value;

                if (Base == null)
                    return;

                Base.NetworkZoomConstraint = value;
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
            Object.transform.localScale = Scale;
            ApplyProperties(camera);
            NetworkServer.Spawn(camera.gameObject);
            NetId = camera.netId;
            base.SpawnObject(schematic, serializable);
        }

        /// <summary>
        /// Respawns this camera in place with the current <see cref="Type"/> prefab,
        /// </summary>
        public void Respawn()
        {
            Scp079CameraToy camera = UnityEngine.Object.Instantiate(GetCameraPrefab(Type));
            NetworkServer.UnSpawn(camera.gameObject);
            Base = camera;
            Object = camera.gameObject;
            Object.transform.SetPositionAndRotation(Position, Rotation);
            Object.transform.localScale = Scale;
            ApplyProperties(camera);
            NetworkServer.Spawn(camera.gameObject);
            NetId = camera.netId;
        }

        /// <summary>
        /// Applies all current property values to the given camera toy.
        /// </summary>
        /// <param name="camera">The <see cref="Scp079CameraToy"/> to apply properties to.</param>
        public void ApplyProperties(Scp079CameraToy camera)
        {
            camera.NetworkLabel = Label;
            if (Room != null)
                camera.NetworkRoom = Room.Base;

            camera.NetworkVerticalConstraint = VerticalConstraint;
            camera.NetworkHorizontalConstraint = HorizontalConstraint;
            camera.NetworkZoomConstraint = ZoomConstraint;
        }
    }
}