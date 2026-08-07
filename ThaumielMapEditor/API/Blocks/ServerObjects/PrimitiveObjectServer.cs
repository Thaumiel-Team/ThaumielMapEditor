// -----------------------------------------------------------------------
// <copyright file="PrimitiveObjectServer.cs" company="Thaumiel Team">
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
    public class PrimitiveObjectServer : ServerObject
    {
        [YamlIgnore]
#pragma warning disable CS8618
        public PrimitiveObjectToy Base { get; private set; }
#pragma warning restore CS8618

        [YamlMember(Alias = "Color")]
        public Color Color
        {
            get;
            set
            {
                if (field == value)
                    return;

                field = value;
                Base?.MaterialColor = value;
            }
        } = Color.white;

        [YamlMember(Alias = "PrimitiveType")]
        public PrimitiveType PrimitiveType
        {
            get;
            set
            {
                if (field == value)
                    return;

                field = value;
                Base?.PrimitiveType = value;
            }
        } = PrimitiveType.Cube;

        [YamlMember(Alias = "PrimitiveFlags")]
        public PrimitiveFlags PrimitiveFlags
        {
            get;
            set
            {
                if (field == value)
                    return;

                field = value;
                Base?.PrimitiveFlags = value;
            }
        } = PrimitiveFlags.None;


        public override ObjectType ObjectType { get; set; } = ObjectType.Primitive;

        public override void SpawnObject(SchematicData schematic, SerializableObject serializable)
        {
            PrimitiveObjectToy? primitive = UnityEngine.Object.Instantiate(PrefabHelper.PrimitiveObject);
            if (primitive == null)
                return;

            NetworkServer.UnSpawn(primitive.gameObject);
            Base = primitive;
            Object = primitive.gameObject;
            primitive.PrimitiveFlags = PrimitiveFlags;
            primitive.PrimitiveType = PrimitiveType;
            primitive.MaterialColor = Color;
            primitive.gameObject.transform.position = Position;
            primitive.gameObject.transform.rotation = Rotation;
            primitive.gameObject.transform.localScale = Scale;
            NetworkServer.Spawn(primitive.gameObject);
            NetId = primitive.netId;
            base.SpawnObject(schematic, serializable);
        }

        public void SpawnObject(SchematicData schematic)
        {
            PrimitiveObjectToy? primitive = UnityEngine.Object.Instantiate(PrefabHelper.PrimitiveObject);
            if (primitive == null)
                return;

            NetworkServer.UnSpawn(primitive.gameObject);
            Base = primitive;
            Object = primitive.gameObject;
            primitive.PrimitiveFlags = PrimitiveFlags;
            primitive.PrimitiveType = PrimitiveType;
            primitive.MaterialColor = Color;
            primitive.gameObject.transform.position = Position;
            primitive.gameObject.transform.rotation = Rotation;
            primitive.gameObject.transform.localScale = Scale;
            NetworkServer.Spawn(primitive.gameObject);
            NetId = primitive.netId;
        }
    }
}