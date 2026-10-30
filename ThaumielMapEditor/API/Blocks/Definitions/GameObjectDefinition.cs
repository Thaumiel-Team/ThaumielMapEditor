// -----------------------------------------------------------------------
// <copyright file="GameObjectDefinition.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using AdminToys;
using LabApi.Features.Wrappers;
using ThaumielMapEditor.API.Blocks.ClientSide;
using ThaumielMapEditor.API.Components;
using ThaumielMapEditor.API.Data;
using ThaumielMapEditor.API.Enums;
using ThaumielMapEditor.API.Helpers;
using UnityEngine;

namespace ThaumielMapEditor.API.Blocks.Definitions
{
    /// <summary>
    /// The definition of a transform-only game object node.
    /// </summary>
    /// <remarks>
    /// Game object nodes do not render anything by themselves; they only exist so other objects can be
    /// parented to them. The server representation uses an invisible primitive so the node still has a
    /// network identity.
    /// </remarks>
    [GitBookPage("Blocks/Definitions/GameObject")]
    public sealed class GameObjectDefinition : PrimitiveDefinition
    {
        /// <inheritdoc/>
        public override string Name => "GameObject";

        /// <inheritdoc/>
        public override ObjectType ObjectType => ObjectType.GameObject;

        /// <inheritdoc/>
        protected override bool ApplyServerTransform => false;

        /// <inheritdoc/>
        public override global::ThaumielMapEditor.API.Blocks.ServerObject? ServerObject(ObjectBase data)
            => new Server(data, this);

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

        /// <summary>
        /// The server-side representation of a game object node.
        /// </summary>
        public new sealed class Server : PrimitiveDefinition.Server
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="Server"/> class.
            /// </summary>
            /// <param name="data">The shared object state.</param>
            /// <param name="definition">The definition that owns the serialized values.</param>
            public Server(ObjectBase data, GameObjectDefinition definition)
                : base(data, definition)
            {
            }

            /// <inheritdoc/>
            public override void OnSpawned(SchematicData schematic)
            {
                if (Serialization != null)
                    Loader.EnsureGameObjectNode(Serialization, schematic);
            }
        }

        /// <summary>
        /// The client-side representation of a game object node.
        /// </summary>
        public new sealed class Client : PrimitiveDefinition.Client
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="Client"/> class.
            /// </summary>
            /// <param name="data">The shared object state.</param>
            /// <param name="definition">The definition that owns the serialized values.</param>
            public Client(ObjectBase data, GameObjectDefinition definition)
                : base(data, definition)
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
