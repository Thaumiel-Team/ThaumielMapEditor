// -----------------------------------------------------------------------
// <copyright file="LightObject.cs" company="Thaumiel Team">
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
    [GitBookPage("Blocks/Client/LightObject")]
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

        /// <inheritdoc />
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