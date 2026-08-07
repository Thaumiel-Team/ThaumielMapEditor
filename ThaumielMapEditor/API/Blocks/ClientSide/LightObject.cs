// -----------------------------------------------------------------------
// <copyright file="LightObject.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using LabApi.Features.Wrappers;
using Mirror;
using ThaumielMapEditor.API.Data;
using ThaumielMapEditor.API.Enums;
using ThaumielMapEditor.API.Extensions;
using ThaumielMapEditor.API.Helpers;
using ThaumielMapEditor.API.Serialization;
using ThaumielMapEditor.Events.EventArgs.Handlers;
using UnityEngine;
using YamlDotNet.Serialization;

namespace ThaumielMapEditor.API.Blocks.ClientSide
{
    public class LightObject : ClientObject
    {
        /// <summary>
        /// Gets or sets the intensity of the light.
        /// </summary>
        [YamlMember(Alias = "LightIntensity")]
        public float Intensity
        {
            get;
            set
            {
                if (field == value)
                    return;

                field = value;
                MarkSyncNeeded(SyncFlags.LightIntensity);
            }
        } = 1f;

        /// <summary>
        /// Gets or sets the range of the light.
        /// </summary>
        [YamlMember(Alias = "LightRange")]
        public float Range
        {
            get;
            set
            {
                if (field == value)
                    return;

                field = value;
                MarkSyncNeeded(SyncFlags.LightRange);
            }
        } = 10f;

        /// <summary>
        /// Gets or sets the color of the light.
        /// </summary>
        [YamlMember(Alias = "LightColor")]
        public Color Color
        {
            get;
            set
            {
                if (field == value)
                    return;

                field = value;
                MarkSyncNeeded(SyncFlags.LightColor);
            }
        } = Color.white;

        /// <summary>
        /// Gets or sets the shadow type used by the light.
        /// </summary>
        [YamlMember(Alias = "ShadowType")]
        public LightShadows Shadows
        {
            get;
            set
            {
                if (field == value)
                    return;

                field = value;
                MarkSyncNeeded(SyncFlags.Shadows);
            }
        } = LightShadows.None;

        /// <summary>
        /// Gets or sets the strength of the shadows cast by the light.
        /// </summary>
        public float ShadowStrength
        {
            get;
            set
            {
                if (field == value)
                    return;

                field = value;
                MarkSyncNeeded(SyncFlags.ShadowStrength);
            }
        } = 1f;

        /// <summary>
        /// Gets or sets the type of the light.
        /// </summary>
        [YamlMember(Alias = "LightType")]
        public LightType Type
        {
            get;
            set
            {
                if (field == value)
                    return;

                field = value;
                MarkSyncNeeded(SyncFlags.LightType);
            }
        } = LightType.Point;

        /// <summary>
        /// Gets or sets the shape of the light.
        /// </summary>
#pragma warning disable CS0618
        [YamlMember(Alias = "LightShape")]
        public LightShape Shape
        {
            get;
            set
            {
                if (field == value)
                    return;

                field = value;
                MarkSyncNeeded(SyncFlags.LightShape);
            }
        } = LightShape.Cone;
#pragma warning restore CS0618

        /// <summary>
        /// Gets or sets the outer spot angle of the light.
        /// </summary>
        public float SpotAngle
        {
            get;
            set
            {
                if (field == value)
                    return;

                field = value;
                MarkSyncNeeded(SyncFlags.SpotAngle);
            }
        } = 30f;

        /// <summary>
        /// Gets or sets the inner spot angle of the light.
        /// </summary>
        public float InnerSpotAngle
        {
            get;
            set
            {
                if (field == value)
                    return;

                field = value;
                MarkSyncNeeded(SyncFlags.InnerSpotAngle);
            }
        } = 20f;

        /// <summary>
        /// Gets or sets the schematic data associated with this light object.
        /// </summary>
        public SchematicData? Schematic { get; set; }

        /// <inheritdoc/>
        public override ObjectType ObjectType => ObjectType.Light;

        /// <inheritdoc/>
        public override void SpawnForPlayer(Player player)
        {
            if (player.IsHost)
                return;

            using NetworkWriterPooled writer = NetworkWriterPool.Get();

            writer.WriteByte(1);

            int sizePos = writer.Position;
            writer.WriteByte(0);
            int start = writer.Position;

            writer.WriteVector3(Position);
            writer.WriteQuaternion(Rotation);
            writer.WriteVector3(Scale);
            writer.WriteByte(MovementSmoothing);
            writer.WriteBool(IsStatic);
            writer.WriteFloat(Intensity);
            writer.WriteFloat(Range);
            writer.WriteColor(Color);
            writer.WriteInt((int)Shadows);
            writer.WriteFloat(ShadowStrength);
            writer.WriteInt((int)Type);
            writer.WriteInt((int)Shape);
            writer.WriteFloat(SpotAngle);
            writer.WriteFloat(InnerSpotAngle);
            writer.WriteUInt(ParentNetId);

            int end = writer.Position;
            writer.Position = sizePos;
            writer.WriteByte((byte)(end - start));
            writer.Position = end;

            player.Connection.Send(new SpawnMessage
            {
                netId = NetId,
                assetId = AssetId,
                position = Position,
                rotation = Rotation,
                scale = Scale,
                isLocalPlayer = false,
                isOwner = false,
                sceneId = 0,
                payload = writer.ToArraySegment()
            });

            ObjectHandler.OnClientObjectSpawned(new(this, player));
            SpawnedPlayers.Add(player);
        }
    }
}