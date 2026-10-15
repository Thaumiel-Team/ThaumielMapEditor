// -----------------------------------------------------------------------
// <copyright file="CullingParentObject.cs" company="Thaumiel Team">
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
using UnityEngine;
using YamlDotNet.Serialization;

namespace ThaumielMapEditor.API.Blocks.ServerObjects
{
    [GitBookPage("Blocks/Server/CullingParentObject")]
    public class CullingParentObject : ServerObject
    {
        public override ObjectType ObjectType { get; set; } = ObjectType.CullingParent;

        /// <summary>
        /// Gets the underlying <see cref="SpawnableCullingParent"/> component.
        /// </summary>
        [YamlIgnore]
        public SpawnableCullingParent? Base { get; private set; }

        /// <summary>
        /// Gets or sets the world space size.
        /// </summary>
        [YamlMember(Alias = "BoundsSize")]
        public Vector3 BoundsSize
        {
            get => Base != null ? Base.BoundsSize : field;
            set
            {
                field = value;
                Base?.NetworkBoundsSize = value;
            }
        }

        /// <summary>
        /// Gets or sets the world position.
        /// </summary>
        public new Vector3 Position
        {
            get => Base != null ? Base.BoundsPosition : field;
            set
            {
                field = value;
                Base?.BoundsPosition = value;
            }
        }

        /// <summary>
        /// Gets or sets the rotation.
        /// </summary>
        public new Quaternion Rotation
        {
            get;
            set
            {
                field = value;
                Object?.transform.rotation = value;
            }
        }

        /// <summary>
        /// Gets or sets the scale.
        /// </summary>
        public new Vector3 Scale
        {
            get;
            set
            {
                field = value;
                Object?.transform.localScale = Vector3.one;
            }
        }

        /// <inheritdoc/>
        public override void SpawnObject(SchematicData schematic, SerializableObject serializable)
        {
            if (PrefabHelper.CullingParent == null)
            {
                LogManager.Warn($"Skipping culling parent '{Name}': culling parent prefab not registered.");
                return;
            }

            GameObject instance = UnityEngine.Object.Instantiate(PrefabHelper.CullingParent.gameObject);
            Object = instance;

            SetWorldTransform(schematic);
            Position = base.Position;
            Rotation = base.Rotation;
            Scale = base.Scale;

            if (!PrepareInstance())
                return;

            base.SpawnObject(schematic, serializable);
            ClearDirtyFlags();
        }

        private bool PrepareInstance()
        {
            if (Object == null)
                return false;

            Object.transform.SetPositionAndRotation(Position, Rotation);
            Object.transform.localScale = Vector3.one;

            Base = Object.GetComponent<SpawnableCullingParent>();
            if (Base == null)
            {
                LogManager.Warn($"Skipping culling parent '{Name}': spawned instance has no SpawnableCullingParent component.");
                UnityEngine.Object.Destroy(Object);
                Object = null;
                return false;
            }

            Base.NetworkBoundsPosition = Position;
            Base.NetworkBoundsSize = BoundsSize;

            NetworkServer.Spawn(Object);

            if (Object.TryGetComponent<NetworkIdentity>(out var network))
                NetId = network.netId;

            return true;
        }
    }
}
