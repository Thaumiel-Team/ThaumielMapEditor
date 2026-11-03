// -----------------------------------------------------------------------
// <copyright file="Capybara.cs" company="Thaumiel Team">
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
using YamlDotNet.Serialization;

namespace ThaumielMapEditor.API.Blocks.Definitions
{
    [GitBookPage("Blocks/Definitions/Capybara")]
    public sealed class Capybara : ObjectDefinition<Capybara>
    {
        /// <summary>
        /// Gets or sets whether collisions are enabled.
        /// </summary>
        [YamlMember(Alias = "Collisions")]
        public bool CollisionsEnabled { get; set; } = true;

        /// <inheritdoc/>
        public override string Name => "Capybara";

        /// <inheritdoc/>
        public override ObjectType ObjectType => ObjectType.Capybara;

        /// <inheritdoc/>
        public override ServerObject? ServerObject(ObjectBase data)
            => new Server(data, this);

        /// <inheritdoc/>
        public override ClientObject? ClientObject(ObjectBase data)
        {
            if (PrefabHelper.Capybara?.netIdentity == null)
            {
                LogManager.Warn($"Skipping capybara '{data.Name}': Capybara prefab not registered.");
                return null;
            }

            return new Client(data, this)
            {
                AssetId = PrefabHelper.Capybara.netIdentity.assetId,
            };
        }

        public sealed class Server : ServerObject
        {
            private readonly Capybara definition;

            /// <summary>
            /// Initializes a new instance of the <see cref="Server"/> class.
            /// </summary>
            /// <param name="data">The shared object state.</param>
            /// <param name="definition">The definition that owns the serialized values.</param>
            public Server(ObjectBase data, Capybara definition)
            {
                CopyStateFrom(data);
                this.definition = definition;
            }

            /// <summary>
            /// Gets the underlying <see cref="CapybaraToy"/>.
            /// </summary>
            public CapybaraToy? Base { get; private set; }

            /// <summary>
            /// Gets or sets whether collisions are enabled.
            /// </summary>
            public bool CollisionsEnabled
            {
                get => definition.CollisionsEnabled;
                set
                {
                    definition.CollisionsEnabled = value;
                    Base?.CollisionsEnabled = value;
                }
            }

            /// <inheritdoc/>
            public override bool ParentToSpawnTransform => true;

            /// <inheritdoc/>
            public override void SpawnObject(SchematicData schematic, SerializableObject serializable)
            {
                CapybaraToy? capybara = UnityEngine.Object.Instantiate(PrefabHelper.Capybara);
                if (capybara == null)
                    return;

                NetworkServer.UnSpawn(capybara.gameObject);
                Base = capybara;
                Object = capybara.gameObject;
                capybara.CollisionsEnabled = CollisionsEnabled;
                capybara.gameObject.transform.position = Position;
                capybara.gameObject.transform.rotation = Rotation;
                capybara.gameObject.transform.localScale = Scale;
                NetworkServer.Spawn(capybara.gameObject);
                NetId = capybara.netId;
                base.SpawnObject(schematic, serializable);
            }
        }

        public sealed class Client : ClientObject
        {
            private readonly Capybara definition;

            /// <summary>
            /// Initializes a new instance of the <see cref="Client"/> class.
            /// </summary>
            /// <param name="data">The shared object state.</param>
            /// <param name="definition">The definition that owns the serialized values.</param>
            public Client(ObjectBase data, Capybara definition)
            {
                CopyStateFrom(data);
                this.definition = definition;
            }

            /// <summary>
            /// Gets or sets whether collisions are enabled.
            /// </summary>
            public bool CollisionsEnabled
            {
                get => definition.CollisionsEnabled;
                set
                {
                    if (definition.CollisionsEnabled == value)
                        return;

                    definition.CollisionsEnabled = value;
                    MarkSyncNeeded(SyncFlags.Collisions);
                    ColliderHelper.SetColliders(this, value);
                }
            }

            /// <inheritdoc/>
            public override ObjectType ObjectType => ObjectType.Capybara;

            /// <inheritdoc/>
            public override void OnAddedToSchematic(SchematicData schematic)
            {
                if (CollisionsEnabled)
                    ColliderHelper.CreateClientObjectColliders(this, schematic);
            }

            /// <inheritdoc/>
            protected override void WriteSyncVars(NetworkWriter payloadWriter)
            {
                base.WriteSyncVars(payloadWriter);
                payloadWriter.WriteBool(CollisionsEnabled);
            }

            /// <inheritdoc/>
            protected override ulong GetDerivedDirtyBits(SyncFlags flags)
            {
                ulong mask = 0UL;
                if (flags.HasFlagFast(SyncFlags.Collisions))
                    mask |= 0x20UL;

                return mask;
            }

            /// <inheritdoc/>
            protected override void WriteDerivedSyncVars(NetworkWriter writer, SyncFlags flags)
            {
                if (flags.HasFlagFast(SyncFlags.Collisions))
                    writer.WriteBool(CollisionsEnabled);
            }
        }
    }
}
