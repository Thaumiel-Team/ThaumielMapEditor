// -----------------------------------------------------------------------
// <copyright file="GameObject.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU Lesser General Public License v3.0 (LGPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using AdminToys;
using LabApi.Features.Wrappers;
using ThaumielMapEditor.API.Components;
using ThaumielMapEditor.API.Data;
using ThaumielMapEditor.API.Enums;
using ThaumielMapEditor.API.Helpers;
using UnityEngine;

namespace ThaumielMapEditor.API.Blocks.Definitions
{
    [GitBookPage("Blocks/Definitions/GameObject")]
    public sealed class GameObject : Primitive
    {
        /// <inheritdoc/>
        public override string Name => "GameObject";

        /// <inheritdoc/>
        public override ObjectType ObjectType => ObjectType.GameObject;

        /// <inheritdoc/>
        protected override bool ApplyServerTransform => false;

        /// <inheritdoc/>
        public override ServerObject? ServerObject(ObjectBase data) =>
            new Server(data, this);

        /// <inheritdoc/>
        public override ClientObject? ClientObject(ObjectBase data)
        {
            if (PrefabHelper.PrimitiveObject?.netIdentity == null)
            {
                LogManager.Warn($"Skipping game object '{data.Name}': Primitive prefab not registered.");
                return null;
            }

            return new Client(data, this)
            {
                AssetId = PrefabHelper.PrimitiveObject.netIdentity.assetId,
            };
        }

        public new sealed class Server : Primitive.Server
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="Server"/> class.
            /// </summary>
            /// <param name="data">The shared object state.</param>
            /// <param name="definition">The definition that owns the serialized values.</param>
            public Server(ObjectBase data, GameObject definition) : base(data, definition)
            {
            }

            /// <inheritdoc/>
            public override void OnSpawned(SchematicData schematic)
            {
                if (Serialization != null)
                    Loader.EnsureGameObjectNode(Serialization, schematic);
            }
        }
        
        public new sealed class Client : Primitive.Client
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="Client"/> class.
            /// </summary>
            /// <param name="data">The shared object state.</param>
            /// <param name="definition">The definition that owns the serialized values.</param>
            public Client(ObjectBase data, GameObject definition) : base(data, definition)
            {
                definition.Color = Color.white;
                definition.PrimitiveType = PrimitiveType.Cube;
                definition.PrimitiveFlags = PrimitiveFlags.None;
            }

            /// <inheritdoc/>
            public override void OnAddedToSchematic(SchematicData schematic)
            {
                if (Serialization != null)
                    Loader.EnsureGameObjectNode(Serialization, schematic);
            }

            /// <inheritdoc/>
            public override bool ShouldSpawnFor(Player player, LODZone[] zones) => true;
        }
    }
}