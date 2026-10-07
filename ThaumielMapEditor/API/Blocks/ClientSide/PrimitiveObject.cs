// -----------------------------------------------------------------------
// <copyright file="PrimitiveObject.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using AdminToys;

using Mirror;
using ThaumielMapEditor.API.Data;
using ThaumielMapEditor.API.Enums;

using UnityEngine;

namespace ThaumielMapEditor.API.Blocks.ClientSide
{
    [GitBookPage("Blocks/Client/TextObject")]
    public class PrimitiveObject : ClientObject
    {
        public string Name { get; set; } = string.Empty;

        public Color Color
        {
            get;
            set
            {
                if (field == value)
                    return;

                field = value;
                MarkSyncNeeded(SyncFlags.Color);
            }
        }

        public PrimitiveType PrimitiveType
        {
            get;
            set
            {
                if (field == value)
                    return;

                field = value;
                MarkSyncNeeded(SyncFlags.PrimitiveType);
            }
        }

        public PrimitiveFlags PrimitiveFlags
        {
            get;
            set
            {
                if (field == value)
                    return;

                field = value;
                MarkSyncNeeded(SyncFlags.PrimitiveFlags);
            }
        }

        public MeshCollider? ServerCollider { get; set; }
        public SchematicData? Schematic { get; set; }

        /// <inheritdoc/>
        public override ObjectType ObjectType => ObjectType.Primitive;

        protected override void WriteSyncVars(NetworkWriter payloadWriter)
        {
            base.WriteSyncVars(payloadWriter);
            payloadWriter.WriteInt((int)PrimitiveType);
            payloadWriter.WriteColor(Color);
            payloadWriter.WriteByte((byte)PrimitiveFlags);
        }

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