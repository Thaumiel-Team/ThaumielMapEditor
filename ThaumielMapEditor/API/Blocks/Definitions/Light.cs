// -----------------------------------------------------------------------
// <copyright file="Light.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU Lesser General Public License v3.0 (LGPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using AdminToys;
using Mirror;
using ThaumielMapEditor.API.Data;
using ThaumielMapEditor.API.Enums;
using ThaumielMapEditor.API.Helpers;
using ThaumielMapEditor.API.Serialization;
using UnityEngine;
using YamlDotNet.Serialization;

namespace ThaumielMapEditor.API.Blocks.Definitions
{
    [GitBookPage("Blocks/Definitions/Light")]
    public sealed class Light : ObjectDefinition<Light>
    {
        /// <summary>
        /// Gets or sets the intensity of the light.
        /// </summary>
        [YamlMember(Alias = "LightIntensity")]
        public float Intensity { get; set; } = 1f;

        /// <summary>
        /// Gets or sets the range of the light.
        /// </summary>
        [YamlMember(Alias = "LightRange")]
        public float Range { get; set; } = 10f;

        /// <summary>
        /// Gets or sets the color of the light.
        /// </summary>
        [YamlMember(Alias = "LightColor")]
        public Color Color { get; set; } = Color.white;

        /// <summary>
        /// Gets or sets the shadow type used by the light.
        /// </summary>
        [YamlMember(Alias = "ShadowType")]
        public LightShadows Shadows { get; set; } = LightShadows.None;

        /// <summary>
        /// Gets or sets the strength of the shadows cast by the light.
        /// </summary>
        [YamlMember(Alias = "ShadowStrength")]
        public float ShadowStrength { get; set; } = 1f;

        /// <summary>
        /// Gets or sets the type of the light.
        /// </summary>
        [YamlMember(Alias = "LightType")]
        public LightType Type { get; set; } = LightType.Point;

        /// <summary>
        /// Gets or sets the shape of the light.
        /// </summary>
#pragma warning disable CS0618
        [YamlMember(Alias = "LightShape")]
        public LightShape Shape { get; set; } = LightShape.Cone;
#pragma warning restore CS0618

        /// <summary>
        /// Gets or sets the outer spot angle of the light.
        /// </summary>
        [YamlMember(Alias = "SpotAngle")]
        public float SpotAngle { get; set; } = 30f;

        /// <summary>
        /// Gets or sets the inner spot angle of the light.
        /// </summary>
        [YamlMember(Alias = "InnerSpotAngle")]
        public float InnerSpotAngle { get; set; } = 20f;

        /// <inheritdoc/>
        public override string Name => "Light";

        /// <inheritdoc/>
        public override ObjectType ObjectType => ObjectType.Light;

        /// <inheritdoc/>
        public override ServerObject? ServerObject(ObjectBase data)
            => new Server(data, this);

        /// <inheritdoc/>
        public override ClientObject? ClientObject(ObjectBase data)
        {
            if (PrefabHelper.LightSource?.netIdentity == null)
            {
                LogManager.Warn($"Skipping light '{data.Name}': Light prefab not registered.");
                return null;
            }

            return new Client(data, this)
            {
                AssetId = PrefabHelper.LightSource.netIdentity.assetId,
            };
        }

        public sealed class Server : ServerObject
        {
            private readonly Light definition;

            /// <summary>
            /// Initializes a new instance of the <see cref="Server"/> class.
            /// </summary>
            /// <param name="data">The shared object state.</param>
            /// <param name="definition">The definition that owns the serialized values.</param>
            public Server(ObjectBase data, Light definition)
            {
                CopyStateFrom(data);
                this.definition = definition;
            }

            /// <summary>
            /// Gets the underlying <see cref="LightSourceToy"/>.
            /// </summary>
            public LightSourceToy? Base { get; private set; }

            /// <summary>
            /// Gets or sets the intensity of the light.
            /// </summary>
            public float Intensity
            {
                get => definition.Intensity;
                set
                {
                    definition.Intensity = value;
                    Base?.LightIntensity = value;
                }
            }

            /// <summary>
            /// Gets or sets the range of the light.
            /// </summary>
            public float Range
            {
                get => definition.Range;
                set
                {
                    definition.Range = value;
                    Base?.LightRange = value;
                }
            }

            /// <summary>
            /// Gets or sets the color of the light.
            /// </summary>
            public Color Color
            {
                get => definition.Color;
                set
                {
                    definition.Color = value;
                    Base?.LightColor = value;
                }
            }

            /// <summary>
            /// Gets or sets the shadow type used by the light.
            /// </summary>
            public LightShadows Shadows
            {
                get => definition.Shadows;
                set
                {
                    definition.Shadows = value;
                    Base?.ShadowType = value;
                }
            }

            /// <summary>
            /// Gets or sets the strength of the shadows cast by the light.
            /// </summary>
            public float ShadowStrength
            {
                get => definition.ShadowStrength;
                set
                {
                    definition.ShadowStrength = value;
                    Base?.ShadowStrength = value;
                }
            }

            /// <summary>
            /// Gets or sets the type of the light.
            /// </summary>
            public LightType Type
            {
                get => definition.Type;
                set
                {
                    definition.Type = value;
                    Base?.LightType = value;
                }
            }

            /// <summary>
            /// Gets or sets the outer spot angle of the light.
            /// </summary>
            public float SpotAngle
            {
                get => definition.SpotAngle;
                set
                {
                    definition.SpotAngle = value;
                    Base?.SpotAngle = value;
                }
            }

            /// <inheritdoc/>
            public override bool ParentToSpawnTransform => true;

            /// <inheritdoc/>
            public override void SpawnObject(SchematicData schematic, SerializableObject serializable)
            {
                LightSourceToy? light = UnityEngine.Object.Instantiate(PrefabHelper.LightSource);
                if (light == null)
                    return;

                NetworkServer.UnSpawn(light.gameObject);
                Base = light;
                Object = light.gameObject;
                Base.LightIntensity = Intensity;
                Base.LightRange = Range;
                Base.LightColor = Color;
                Base.ShadowType = Shadows;
                Base.ShadowStrength = ShadowStrength;
                Base.LightType = Type;
                Base.SpotAngle = SpotAngle;
                light.gameObject.transform.position = Position;
                light.gameObject.transform.rotation = Rotation;
                light.gameObject.transform.localScale = Scale;
                SelectionProxyHelper.EnsureChild(light.gameObject);
                NetworkServer.Spawn(light.gameObject);
                NetId = light.netId;
                base.SpawnObject(schematic, serializable);
            }
        }

        public sealed class Client : ClientObject
        {
            private readonly Light definition;

            /// <summary>
            /// Initializes a new instance of the <see cref="Client"/> class.
            /// </summary>
            /// <param name="data">The shared object state.</param>
            /// <param name="definition">The definition that owns the serialized values.</param>
            public Client(ObjectBase data, Light definition)
            {
                CopyStateFrom(data);
                this.definition = definition;
            }

            /// <summary>
            /// Gets or sets the intensity of the light.
            /// </summary>
            public float Intensity
            {
                get => definition.Intensity;
                set
                {
                    if (definition.Intensity == value)
                        return;

                    definition.Intensity = value;
                    MarkSyncNeeded(SyncFlags.LightIntensity);
                }
            }

            /// <summary>
            /// Gets or sets the range of the light.
            /// </summary>
            public float Range
            {
                get => definition.Range;
                set
                {
                    if (definition.Range == value)
                        return;

                    definition.Range = value;
                    MarkSyncNeeded(SyncFlags.LightRange);
                }
            }

            /// <summary>
            /// Gets or sets the color of the light.
            /// </summary>
            public Color Color
            {
                get => definition.Color;
                set
                {
                    if (definition.Color == value)
                        return;

                    definition.Color = value;
                    MarkSyncNeeded(SyncFlags.LightColor);
                }
            }

            /// <summary>
            /// Gets or sets the shadow type used by the light.
            /// </summary>
            public LightShadows Shadows
            {
                get => definition.Shadows;
                set
                {
                    if (definition.Shadows == value)
                        return;

                    definition.Shadows = value;
                    MarkSyncNeeded(SyncFlags.Shadows);
                }
            }

            /// <summary>
            /// Gets or sets the strength of the shadows cast by the light.
            /// </summary>
            public float ShadowStrength
            {
                get => definition.ShadowStrength;
                set
                {
                    if (definition.ShadowStrength == value)
                        return;

                    definition.ShadowStrength = value;
                    MarkSyncNeeded(SyncFlags.ShadowStrength);
                }
            }

            /// <summary>
            /// Gets or sets the type of the light.
            /// </summary>
            public LightType Type
            {
                get => definition.Type;
                set
                {
                    if (definition.Type == value)
                        return;

                    definition.Type = value;
                    MarkSyncNeeded(SyncFlags.LightType);
                }
            }

            /// <summary>
            /// Gets or sets the shape of the light.
            /// </summary>
#pragma warning disable CS0618
            public LightShape Shape
            {
                get => definition.Shape;
                set
                {
                    if (definition.Shape == value)
                        return;

                    definition.Shape = value;
                    MarkSyncNeeded(SyncFlags.LightShape);
                }
            }
#pragma warning restore CS0618

            /// <summary>
            /// Gets or sets the outer spot angle of the light.
            /// </summary>
            public float SpotAngle
            {
                get => definition.SpotAngle;
                set
                {
                    if (definition.SpotAngle == value)
                        return;

                    definition.SpotAngle = value;
                    MarkSyncNeeded(SyncFlags.SpotAngle);
                }
            }

            /// <summary>
            /// Gets or sets the inner spot angle of the light.
            /// </summary>
            public float InnerSpotAngle
            {
                get => definition.InnerSpotAngle;
                set
                {
                    if (definition.InnerSpotAngle == value)
                        return;

                    definition.InnerSpotAngle = value;
                    MarkSyncNeeded(SyncFlags.InnerSpotAngle);
                }
            }

            /// <inheritdoc/>
            public override ObjectType ObjectType => ObjectType.Light;

            /// <inheritdoc/>
            public override void OnAddedToSchematic(SchematicData schematic)
                => ColliderHelper.CreateSelectionSphere(this, schematic);

            /// <inheritdoc/>
            protected override void WriteSyncVars(NetworkWriter writer)
            {
                base.WriteSyncVars(writer);
                writer.WriteFloat(Intensity);
                writer.WriteFloat(Range);
                writer.WriteColor(Color);
                writer.WriteInt((int)Shadows);
                writer.WriteFloat(ShadowStrength);
                writer.WriteInt((int)Type);
                writer.WriteInt((int)Shape);
                writer.WriteFloat(SpotAngle);
                writer.WriteFloat(InnerSpotAngle);
            }

            /// <inheritdoc/>
            protected override ulong GetDerivedDirtyBits(SyncFlags flags)
            {
                ulong mask = 0UL;
                if (flags.HasFlagFast(SyncFlags.LightIntensity))
                    mask |= 0x20UL;

                if (flags.HasFlagFast(SyncFlags.LightRange))
                    mask |= 0x40UL;

                if (flags.HasFlagFast(SyncFlags.LightColor))
                    mask |= 0x80UL;

                if (flags.HasFlagFast(SyncFlags.Shadows))
                    mask |= 0x100UL;

                if (flags.HasFlagFast(SyncFlags.ShadowStrength))
                    mask |= 0x200UL;

                if (flags.HasFlagFast(SyncFlags.LightType))
                    mask |= 0x400UL;

                if (flags.HasFlagFast(SyncFlags.LightShape))
                    mask |= 0x800UL;

                if (flags.HasFlagFast(SyncFlags.SpotAngle))
                    mask |= 0x1000UL;

                if (flags.HasFlagFast(SyncFlags.InnerSpotAngle))
                    mask |= 0x2000UL;

                return mask;
            }

            /// <inheritdoc/>
            protected override void WriteDerivedSyncVars(NetworkWriter writer, SyncFlags flags)
            {
                if (flags.HasFlagFast(SyncFlags.LightIntensity))
                    writer.WriteFloat(Intensity);

                if (flags.HasFlagFast(SyncFlags.LightRange))
                    writer.WriteFloat(Range);

                if (flags.HasFlagFast(SyncFlags.LightColor))
                    writer.WriteColor(Color);

                if (flags.HasFlagFast(SyncFlags.Shadows))
                    writer.WriteInt((int)Shadows);

                if (flags.HasFlagFast(SyncFlags.ShadowStrength))
                    writer.WriteFloat(ShadowStrength);

                if (flags.HasFlagFast(SyncFlags.LightType))
                    writer.WriteInt((int)Type);

                if (flags.HasFlagFast(SyncFlags.LightShape))
                    writer.WriteInt((int)Shape);

                if (flags.HasFlagFast(SyncFlags.SpotAngle))
                    writer.WriteFloat(SpotAngle);

                if (flags.HasFlagFast(SyncFlags.InnerSpotAngle))
                    writer.WriteFloat(InnerSpotAngle);
            }
        }
    }
}
