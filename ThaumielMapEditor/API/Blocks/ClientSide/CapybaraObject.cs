// -----------------------------------------------------------------------
// <copyright file="CapybaraObject.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using LabApi.Features.Wrappers;
using Mirror;
using System;
using ThaumielMapEditor.API.Data;
using ThaumielMapEditor.API.Enums;
using ThaumielMapEditor.API.Helpers;
using ThaumielMapEditor.Events.EventArgs.Handlers;

namespace ThaumielMapEditor.API.Blocks.ClientSide
{
    public class CapybaraObject : ClientObject
    {
        public string Name { get; set; } = string.Empty;

        public bool CollisionsEnabled
        {
            get;

            set
            {
                if (field == value)
                    return;

                field = value;
                MarkSyncNeeded(SyncFlags.Collisions);
                ColliderHelper.SetColliders(this, value);
            }
        } = true;

        /// <summary>
        /// Gets or sets the <see cref="SchematicData"/> this <see cref="CapybaraObject"/> is spawned from.
        /// </summary>
        public SchematicData? Schematic { get; set; }

        /// <inheritdoc/>
        public override ObjectType ObjectType => ObjectType.Capybara;

        /// <inheritdoc/>
        public override void SpawnForPlayer(Player player)
        {
            if (player.IsHost)
                return;

            using NetworkWriterPooled payloadWriter = NetworkWriterPool.Get();

            payloadWriter.WriteByte(1);

            int sizePos = payloadWriter.Position;
            payloadWriter.WriteByte(0);
            int dataStart = payloadWriter.Position;

            payloadWriter.WriteVector3(Position);
            payloadWriter.WriteQuaternion(Rotation);
            payloadWriter.WriteVector3(Scale);
            payloadWriter.WriteByte(MovementSmoothing);
            payloadWriter.WriteBool(IsStatic);
            payloadWriter.WriteBool(CollisionsEnabled);
            payloadWriter.WriteUInt(ParentNetId);

            int dataEnd = payloadWriter.Position;
            payloadWriter.Position = sizePos;
            payloadWriter.WriteByte((byte)(dataEnd - dataStart));
            payloadWriter.Position = dataEnd;

            ArraySegment<byte> payload = payloadWriter.ToArraySegment();

            player.Connection.Send(new SpawnMessage
            {
                netId = NetId,
                isLocalPlayer = false,
                isOwner = false,
                sceneId = 0,
                assetId = PrefabHelper.Capybara!.netIdentity.assetId,
                position = Position,
                rotation = Rotation,
                scale = Scale,
                payload = payload
            });

            ObjectHandler.OnClientObjectSpawned(new(this, player));
            SpawnedPlayers.Add(player);
        }
    }
}
