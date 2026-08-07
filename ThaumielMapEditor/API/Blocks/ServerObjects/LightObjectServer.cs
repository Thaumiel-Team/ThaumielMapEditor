// -----------------------------------------------------------------------
// <copyright file="LightObjectServer.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
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

namespace ThaumielMapEditor.API.Blocks.ServerObjects
{
    public class LightObjectServer : ServerObject
    {
        [YamlIgnore]
#pragma warning disable CS8618
        public LightSourceToy Base { get; private set; }
#pragma warning restore CS8618

        [YamlMember(Alias = "LightIntensity")]
        public float Intensity
        {
            get;
            set
            {
                if (field == value)
                    return;

                field = value;
                Base?.LightIntensity = value;
            }
        } = 1f;

        [YamlMember(Alias = "LightRange")]
        public float Range
        {
            get;
            set
            {
                if (field == value)
                    return;

                field = value;
                Base?.LightRange = value;
            }
        } = 10f;

        [YamlMember(Alias = "LightColor")]
        public Color Color
        {
            get;
            set
            {
                if (field == value)
                    return;

                field = value;
                Base?.LightColor = value;
            }
        } = Color.white;

        [YamlMember(Alias = "ShadowType")]
        public LightShadows Shadows
        {
            get;
            set
            {
                if (field == value)
                    return;

                field = value;
                Base?.ShadowType = value;
            }
        } = LightShadows.None;

        [YamlMember(Alias = "ShadowStrength")]
        public float ShadowStrength
        {
            get;
            set
            {
                if (field == value)
                    return;

                field = value;
                Base?.ShadowStrength = value;
            }
        } = 1f;

        [YamlMember(Alias = "LightType")]
        public LightType Type
        {
            get;
            set
            {
                if (field == value)
                    return;

                field = value;
                Base?.LightType = value;
            }
        } = LightType.Point;

        [YamlMember(Alias = "SpotAngle")]
        public float SpotAngle
        {
            get;
            set
            {
                if (field == value)
                    return;

                field = value;
                Base?.SpotAngle = value;
            }
        } = 30f;

        public override ObjectType ObjectType { get; set; } = ObjectType.Light;

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
            NetworkServer.Spawn(light.gameObject);
            NetId = light.netId;
            base.SpawnObject(schematic, serializable);
        }
    }
}