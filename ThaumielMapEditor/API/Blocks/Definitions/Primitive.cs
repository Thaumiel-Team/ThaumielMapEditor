// -----------------------------------------------------------------------
// <copyright file="Primitive.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using AdminToys;
using LabApi.Features.Wrappers;
using Mirror;
using ThaumielMapEditor.API.Blocks.ClientSide;
using ThaumielMapEditor.API.Components;
using ThaumielMapEditor.API.Data;
using ThaumielMapEditor.API.Enums;
using ThaumielMapEditor.API.Helpers;
using ThaumielMapEditor.API.Serialization;
using UnityEngine;
using YamlDotNet.Serialization;
using PrimitiveObjectToy = AdminToys.PrimitiveObjectToy;

namespace ThaumielMapEditor.API.Blocks.Definitions
{
    /// <summary>
    /// The definition of a primitive object, shared by the client and server runtime representations.
    /// </summary>
    [GitBookPage("Blocks/Definitions/Primitive")]
    public class Primitive : ObjectDefinition<Primitive>
    {
        /// <summary>
        /// Gets or sets the material color.
        /// </summary>
        [YamlMember(Alias = "Color")]
        public Color Color { get; set; } = Color.white;

        /// <summary>
        /// Gets or sets the primitive shape.
        /// </summary>
        [YamlMember(Alias = "PrimitiveType")]
        public PrimitiveType PrimitiveType { get; set; } = PrimitiveType.Cube;

        /// <summary>
        /// Gets or sets the primitive flags.
        /// </summary>
        [YamlMember(Alias = "PrimitiveFlags")]
        public PrimitiveFlags PrimitiveFlags { get; set; } = PrimitiveFlags.None;

        /// <inheritdoc/>
        public override string Name => "Primitive";

        /// <inheritdoc/>
        public override ObjectType ObjectType => ObjectType.Primitive;

        /// <summary>
        /// Gets a value indicating whether the server representation should apply the object's transform.
        /// </summary>
        protected virtual bool ApplyServerTransform => true;

        /// <inheritdoc/>
        public override global::ThaumielMapEditor.API.Blocks.ServerObject? ServerObject(ObjectBase data)
            => new Server(data, this);

        /// <inheritdoc/>
        public override ClientObject? ClientObject(ObjectBase data)
        {
            if (PrefabHelper.PrimitiveObject?.netIdentity == null)
            {
                LogManager.Warn($"Skipping primitive '{data.Name}': Primitive prefab not registered.");
                return null;
            }

            return new Client(data, this)
            {
                AssetId = PrefabHelper.PrimitiveObject.netIdentity.assetId,
            };
        }

        /// <summary>
        /// The server-side representation of a primitive.
        /// </summary>
        public class Server : global::ThaumielMapEditor.API.Blocks.ServerObject
        {
            private readonly Primitive definition;

            /// <summary>
            /// Initializes a new instance of the <see cref="Server"/> class.
            /// </summary>
            /// <param name="data">The shared object state.</param>
            /// <param name="definition">The definition that owns the serialized values.</param>
            public Server(ObjectBase data, Primitive definition)
            {
                CopyStateFrom(data);
                this.definition = definition;
            }

            /// <summary>
            /// Gets the underlying <see cref="PrimitiveObjectToy"/>.
            /// </summary>
            public PrimitiveObjectToy? Base { get; private set; }

            /// <summary>
            /// Gets or sets the material color.
            /// </summary>
            public Color Color
            {
                get => definition.Color;
                set
                {
                    definition.Color = value;
                    if (Base != null)
                        Base.MaterialColor = value;
                }
            }

            /// <summary>
            /// Gets or sets the primitive shape.
            /// </summary>
            public PrimitiveType PrimitiveType
            {
                get => definition.PrimitiveType;
                set
                {
                    definition.PrimitiveType = value;
                    if (Base != null)
                        Base.PrimitiveType = value;
                }
            }

            /// <summary>
            /// Gets or sets the primitive flags.
            /// </summary>
            public PrimitiveFlags PrimitiveFlags
            {
                get => definition.PrimitiveFlags;
                set
                {
                    definition.PrimitiveFlags = value;
                    if (Base != null)
                        Base.PrimitiveFlags = value;
                }
            }

            /// <inheritdoc/>
            public override bool ParentToSpawnTransform => true;

            /// <inheritdoc/>
            public override void SpawnObject(SchematicData schematic, SerializableObject serializable)
            {
                PrimitiveObjectToy? primitive = UnityEngine.Object.Instantiate(PrefabHelper.PrimitiveObject);
                if (primitive == null)
                    return;

                NetworkServer.UnSpawn(primitive.gameObject);
                Base = primitive;
                Object = primitive.gameObject;
                primitive.PrimitiveFlags = PrimitiveFlags;
                primitive.PrimitiveType = PrimitiveType;
                primitive.MaterialColor = Color;
                primitive.gameObject.transform.position = ApplyTransform ? Position : Vector3.zero;
                primitive.gameObject.transform.rotation = ApplyTransform ? Rotation : Quaternion.identity;
                primitive.gameObject.transform.localScale = ApplyTransform ? Scale : Vector3.one;
                NetworkServer.Spawn(primitive.gameObject);
                NetId = primitive.netId;
                base.SpawnObject(schematic, serializable);
            }

            /// <summary>
            /// Spawns this primitive without a serialized source, used by block scripts.
            /// </summary>
            /// <param name="schematic">The schematic this primitive belongs to.</param>
            public void SpawnObject(SchematicData schematic)
            {
                PrimitiveObjectToy? primitive = UnityEngine.Object.Instantiate(PrefabHelper.PrimitiveObject);
                if (primitive == null)
                    return;

                NetworkServer.UnSpawn(primitive.gameObject);
                Base = primitive;
                Object = primitive.gameObject;
                primitive.PrimitiveFlags = PrimitiveFlags;
                primitive.PrimitiveType = PrimitiveType;
                primitive.MaterialColor = Color;
                primitive.gameObject.transform.position = Position;
                primitive.gameObject.transform.rotation = Rotation;
                primitive.gameObject.transform.localScale = Scale;
                NetworkServer.Spawn(primitive.gameObject);
                NetId = primitive.netId;
            }

            private bool ApplyTransform => definition.ApplyServerTransform;
        }

        /// <summary>
        /// The client-side representation of a primitive.
        /// </summary>
        public class Client : ClientObject
        {
            private readonly Primitive definition;

            /// <summary>
            /// Initializes a new instance of the <see cref="Client"/> class.
            /// </summary>
            /// <param name="data">The shared object state.</param>
            /// <param name="definition">The definition that owns the serialized values.</param>
            public Client(ObjectBase data, Primitive definition)
            {
                CopyStateFrom(data);
                this.definition = definition;
            }

            /// <summary>
            /// Gets or sets the server-side collider used for this primitive.
            /// </summary>
            public MeshCollider? ServerCollider { get; set; }

            /// <summary>
            /// Gets or sets the material color.
            /// </summary>
            public Color Color
            {
                get => definition.Color;
                set
                {
                    if (definition.Color == value)
                        return;

                    definition.Color = value;
                    MarkSyncNeeded(SyncFlags.Color);
                }
            }

            /// <summary>
            /// Gets or sets the primitive shape.
            /// </summary>
            public PrimitiveType PrimitiveType
            {
                get => definition.PrimitiveType;
                set
                {
                    if (definition.PrimitiveType == value)
                        return;

                    definition.PrimitiveType = value;
                    MarkSyncNeeded(SyncFlags.PrimitiveType);
                }
            }

            /// <summary>
            /// Gets or sets the primitive flags.
            /// </summary>
            public PrimitiveFlags PrimitiveFlags
            {
                get => definition.PrimitiveFlags;
                set
                {
                    if (definition.PrimitiveFlags == value)
                        return;

                    definition.PrimitiveFlags = value;
                    MarkSyncNeeded(SyncFlags.PrimitiveFlags);
                }
            }

            /// <inheritdoc/>
            public override ObjectType ObjectType => ObjectType.Primitive;

            /// <inheritdoc/>
            public override bool ShouldSpawnFor(Player player, LODZone[] zones)
            {
                if (zones.Length == 0)
                    return true;

                foreach (LODZone zone in zones)
                {
                    if (zone == null || zone.Collider == null || !zone.PrimitivestoUnload.Contains(PrimitiveType))
                        continue;

                    if (zone.Collider.bounds.Contains(player.Position))
                        return true;
                }

                return false;
            }

            /// <inheritdoc/>
            public override void OnAddedToSchematic(SchematicData schematic)
                => ColliderHelper.CreateCollisionMesh(this);

            /// <inheritdoc/>
            protected override void WriteSyncVars(NetworkWriter payloadWriter)
            {
                base.WriteSyncVars(payloadWriter);
                payloadWriter.WriteInt((int)PrimitiveType);
                payloadWriter.WriteColor(Color);
                payloadWriter.WriteByte((byte)PrimitiveFlags);
            }

            /// <inheritdoc/>
            protected override ulong GetDerivedDirtyBits(SyncFlags flags)
            {
                ulong mask = 0UL;
                if (flags.HasFlagFast(SyncFlags.PrimitiveType))
                    mask |= 0x20UL;

                if (flags.HasFlagFast(SyncFlags.Color))
                    mask |= 0x40UL;

                if (flags.HasFlagFast(SyncFlags.PrimitiveFlags))
                    mask |= 0x80UL;

                return mask;
            }

            /// <inheritdoc/>
            protected override void WriteDerivedSyncVars(NetworkWriter writer, SyncFlags flags)
            {
                if (flags.HasFlagFast(SyncFlags.PrimitiveType))
                    writer.WriteInt((int)PrimitiveType);

                if (flags.HasFlagFast(SyncFlags.Color))
                    writer.WriteColor(Color);

                if (flags.HasFlagFast(SyncFlags.PrimitiveFlags))
                    writer.WriteByte((byte)PrimitiveFlags);
            }
        }
    }
}
