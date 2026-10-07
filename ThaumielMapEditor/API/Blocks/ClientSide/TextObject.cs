// -----------------------------------------------------------------------
// <copyright file="TextObject.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using Mirror;
using ThaumielMapEditor.API.Data;
using ThaumielMapEditor.API.Enums;
using UnityEngine;
using YamlDotNet.Serialization;

namespace ThaumielMapEditor.API.Blocks.ClientSide
{
    [GitBookPage("Blocks/Client/TextObject")]
    public class TextObject : ClientObject
    {
        /// <summary>
        /// Gets or sets the text format string used for rendering text.
        /// </summary>
        [YamlMember(Alias = "Text")]
        public string Text
        {
            get;
            set
            {
                if (field == value)
                    return;

                field = value;
                MarkSyncNeeded(SyncFlags.TextFormat);
            }
        } = string.Empty;

        /// <summary>
        /// Gets or sets the display size (width, height) used when rendering text.
        /// </summary>
        [YamlMember(Alias = "DisplaySize")]
        public Vector2 DisplaySize
        {
            get;
            set
            {
                if (field == value)
                    return;

                field = value;
                MarkSyncNeeded(SyncFlags.DisplaySize);
            }
        }

        /// <summary>
        /// Gets or sets the schematic data associated with this text object.
        /// </summary>
        public SchematicData? Schematic { get; set; }

        /// <inheritdoc/>
        public override ObjectType ObjectType => ObjectType.TextToy;

        /// <inheritdoc />
        protected override void WriteSyncVars(NetworkWriter payloadWriter)
        {
            base.WriteSyncVars(payloadWriter);
            payloadWriter.WriteVector2(DisplaySize);
            payloadWriter.WriteString(Text);
        }

        protected override void WriteSyncObjects(NetworkWriterPooled payloadWriter)
        {
            payloadWriter.WriteUInt(0);
            payloadWriter.WriteUInt(0);
        }

        protected override ulong GetDerivedDirtyBits(SyncFlags flags)
        {
            ulong mask = 0UL;
            if (flags.HasFlagFast(SyncFlags.DisplaySize))
                mask |= 0x20UL;

            if (flags.HasFlagFast(SyncFlags.TextFormat))
                mask |= 0x40UL;

            return mask;
        }

        protected override void WriteDerivedSyncVars(NetworkWriter writer, SyncFlags flags)
        {
            if (flags.HasFlagFast(SyncFlags.DisplaySize))
                writer.WriteVector2(DisplaySize);

            if (flags.HasFlagFast(SyncFlags.TextFormat))
                writer.WriteString(Text);
        }
    }
}
