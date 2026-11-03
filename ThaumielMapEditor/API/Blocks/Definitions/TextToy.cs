// -----------------------------------------------------------------------
// <copyright file="TextToy.cs" company="Thaumiel Team">
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
using AdminTextToy = AdminToys.TextToy;

namespace ThaumielMapEditor.API.Blocks.Definitions
{
    [GitBookPage("Blocks/Definitions/TextToy")]
    public sealed class TextToy : ObjectDefinition<TextToy>
    {
        /// <summary>
        /// Gets or sets the text format string.
        /// </summary>
        [YamlMember(Alias = "Text")]
        public string Text { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the display size.
        /// </summary>
        [YamlMember(Alias = "DisplaySize")]
        public Vector2 DisplaySize { get; set; }

        /// <inheritdoc/>
        public override string Name => "TextToy";

        /// <inheritdoc/>
        public override ObjectType ObjectType => ObjectType.TextToy;

        /// <inheritdoc/>
        public override ServerObject? ServerObject(ObjectBase data)
            => new Server(data, this);

        /// <inheritdoc/>
        public override ClientObject? ClientObject(ObjectBase data)
        {
            if (PrefabHelper.TextToy?.netIdentity == null)
            {
                LogManager.Warn($"Skipping text toy '{data.Name}': TextToy prefab not registered.");
                return null;
            }

            return new Client(data, this)
            {
                AssetId = PrefabHelper.TextToy.netIdentity.assetId,
            };
        }

        public sealed class Server : ServerObject
        {
            private readonly TextToy definition;

            /// <summary>
            /// Initializes a new instance of the <see cref="Server"/> class.
            /// </summary>
            /// <param name="data">The shared object state.</param>
            /// <param name="definition">The definition that owns the serialized values.</param>
            public Server(ObjectBase data, TextToy definition)
            {
                CopyStateFrom(data);
                this.definition = definition;
            }

            /// <summary>
            /// Gets the underlying <see cref="AdminToys.TextToy"/>.
            /// </summary>
            public AdminTextToy? Base { get; private set; }

            /// <summary>
            /// Gets or sets the text format string.
            /// </summary>
            public string Text
            {
                get => definition.Text;
                set
                {
                    definition.Text = value;
                    Base?.TextFormat = value;
                }
            }

            /// <summary>
            /// Gets or sets the display size.
            /// </summary>
            public Vector2 DisplaySize
            {
                get => definition.DisplaySize;
                set
                {
                    definition.DisplaySize = value;
                    Base?.DisplaySize = value;
                }
            }

            /// <inheritdoc/>
            public override void SpawnObject(SchematicData schematic, SerializableObject serializable)
            {
                if (PrefabHelper.TextToy == null)
                {
                    LogManager.Warn($"Failed to spawn TextToy. Prefab is null.");
                    return;
                }

                AdminTextToy textToy = UnityEngine.Object.Instantiate(PrefabHelper.TextToy);
                Base = textToy;
                Object = textToy.gameObject;
                NetworkServer.UnSpawn(Base.gameObject);
                SetWorldTransform(schematic);

                Base.TextFormat = Text;
                Base.DisplaySize = DisplaySize;
                NetworkServer.Spawn(Base.gameObject);
                NetId = textToy.netId;
                base.SpawnObject(schematic, serializable);
            }

            /// <summary>
            /// Spawns this text toy without a serialized source, used by block scripts.
            /// </summary>
            /// <param name="schematic">The schematic this text toy belongs to.</param>
            public void SpawnObject(SchematicData schematic)
            {
                if (PrefabHelper.TextToy == null)
                {
                    LogManager.Warn($"Failed to spawn TextToy. Prefab is null.");
                    return;
                }

                AdminTextToy textToy = UnityEngine.Object.Instantiate(PrefabHelper.TextToy);
                Base = textToy;
                Object = textToy.gameObject;
                NetworkServer.UnSpawn(Base.gameObject);
                SetWorldTransform(schematic);

                Base.TextFormat = Text;
                Base.DisplaySize = DisplaySize;
                NetworkServer.Spawn(Base.gameObject);
                NetId = textToy.netId;
            }
        }

        public sealed class Client : ClientObject
        {
            private readonly TextToy definition;

            /// <summary>
            /// Initializes a new instance of the <see cref="Client"/> class.
            /// </summary>
            /// <param name="data">The shared object state.</param>
            /// <param name="definition">The definition that owns the serialized values.</param>
            public Client(ObjectBase data, TextToy definition)
            {
                CopyStateFrom(data);
                this.definition = definition;
            }

            /// <summary>
            /// Gets or sets the text format string.
            /// </summary>
            public string Text
            {
                get => definition.Text;
                set
                {
                    if (definition.Text == value)
                        return;

                    definition.Text = value;
                    MarkSyncNeeded(SyncFlags.TextFormat);
                }
            }

            /// <summary>
            /// Gets or sets the display size.
            /// </summary>
            public Vector2 DisplaySize
            {
                get => definition.DisplaySize;
                set
                {
                    if (definition.DisplaySize == value)
                        return;

                    definition.DisplaySize = value;
                    MarkSyncNeeded(SyncFlags.DisplaySize);
                }
            }

            /// <inheritdoc/>
            public override ObjectType ObjectType => ObjectType.TextToy;

            /// <inheritdoc/>
            protected override void WriteSyncVars(NetworkWriter payloadWriter)
            {
                base.WriteSyncVars(payloadWriter);
                payloadWriter.WriteVector2(DisplaySize);
                payloadWriter.WriteString(Text);
            }

            /// <inheritdoc/>
            protected override void WriteSyncObjects(NetworkWriterPooled payloadWriter)
            {
                payloadWriter.WriteUInt(0);
                payloadWriter.WriteUInt(0);
            }

            /// <inheritdoc/>
            protected override ulong GetDerivedDirtyBits(SyncFlags flags)
            {
                ulong mask = 0UL;
                if (flags.HasFlagFast(SyncFlags.DisplaySize))
                    mask |= 0x20UL;

                if (flags.HasFlagFast(SyncFlags.TextFormat))
                    mask |= 0x40UL;

                return mask;
            }

            /// <inheritdoc/>
            protected override void WriteDerivedSyncVars(NetworkWriter writer, SyncFlags flags)
            {
                if (flags.HasFlagFast(SyncFlags.DisplaySize))
                    writer.WriteVector2(DisplaySize);

                if (flags.HasFlagFast(SyncFlags.TextFormat))
                    writer.WriteString(Text);
            }
        }
    }
}
