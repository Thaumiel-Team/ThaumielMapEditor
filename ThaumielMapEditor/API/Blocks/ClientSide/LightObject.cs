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

namespace ThaumielMapEditor.API.Blocks.ClientSide
{
    public class LightObject : ClientObject
    {
        /// <summary>
        /// Gets or sets the intensity of the light.
        /// </summary>
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

        /// <summary>
        /// Deserializes and applies light specific values from a <see cref="SerializableObject"/>.
        /// </summary>
        /// <param name="serializable">The serialized object containing light data.</param>
        public void DeserializeValues(SerializableObject serializable)
        {
            if (serializable.ObjectType != ObjectType.Light)
            {
                LogManager.Warn($"Tried to parse {serializable.ObjectType} as Light");
                return;
            }

            if (!serializable.Values.TryConvertValue<float>("LightIntensity", out var intensity))
            {
                LogManager.Warn("Failed to parse LightIntensity");
            }

            if (!serializable.Values.TryConvertValue<float>("LightRange", out var range))
            {
                LogManager.Warn("Failed to parse LightRange");
            }

            if (!serializable.Values.TryConvertValue<Color>("LightColor", out var color))
            {
                LogManager.Warn("Failed to parse LightColor");
            }

            if (!serializable.Values.TryConvertValue<LightShadows>("ShadowType", out var shadowType))
            {
                LogManager.Warn("Failed to parse ShadowType");
            }

            if (!serializable.Values.TryConvertValue<float>("ShadowStrength", out var shadowStrength))
            {
                LogManager.Warn("Failed to parse ShadowStrength");
            }

            if (!serializable.Values.TryConvertValue<LightType>("LightType", out var lightType))
            {
                LogManager.Warn("Failed to parse LightType");
            }
#pragma warning disable CS0618 // Type or member is obsolete

            if (!serializable.Values.TryConvertValue<LightShape>("LightShape", out var lightShape))
            {
                LogManager.Warn("Failed to parse LightShape");
            }
#pragma warning restore CS0618 // Type or member is obsolete

            if (!serializable.Values.TryConvertValue<float>("SpotAngle", out var spotAngle))
            {
                LogManager.Warn("Failed to parse SpotAngle");
            }

            if (!serializable.Values.TryConvertValue<float>("InnerSpotAngle", out var innerSpotAngle))
            {
                LogManager.Warn("Failed to parse InnerSpotAngle");
            }

            Intensity = intensity;
            Range = range;
            Color = color;
            Shadows = shadowType;
            ShadowStrength = shadowStrength;
            Type = lightType;
            Shape = lightShape;
            SpotAngle = spotAngle;
            InnerSpotAngle = innerSpotAngle;

            ObjectId = serializable.ObjectId;
            ParentId = serializable.ParentId;
        }
    }
}