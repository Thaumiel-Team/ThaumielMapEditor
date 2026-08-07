// -----------------------------------------------------------------------
// <copyright file="PrimitiveBlocks.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using System;
using AdminToys;
using LabApi.Features.Wrappers;
using ThaumielMapEditor.API.Blocks.ClientSide;
using ThaumielMapEditor.API.Blocks.ServerObjects;
using ThaumielMapEditor.API.Data;
using UnityEngine;

namespace ThaumielMapEditor.API.Helpers.BlockParser
{
    public class PrimitiveCreateBlock : BlockBase
    {
        public string Name { get; set; } = string.Empty;
        public bool IsServerSide { get; set; }

        public override object ReturnExecute(SchematicData schematic)
        {
            if (IsServerSide)
            {
                PrimitiveObjectServer server = new();
                server.SpawnObject(schematic);
                return server;
            }
            else
            {
                PrimitiveObject client = new();
                foreach (Player player in Player.ReadyList)
                {
                    client.SpawnForPlayer(player);
                }

                ColliderHelper.CreateCollisionMesh(client);
                return client;
            }
        }
    }

    public class PrimitiveVectorBlock : BlockBase
    {
        public string TargetProperty { get; set; } = string.Empty;
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }

        public override void Execute(object obj)
        {
            PrimitiveObject? client = obj as PrimitiveObject;
            PrimitiveObjectServer? server = obj as PrimitiveObjectServer;
            if (client is null && server is null)
                return;

            Vector3 vector = new(X, Y, Z);
            switch (TargetProperty)
            {
                case "position":
                    client?.Position = vector;
                    server?.Position = vector;
                    break;
                case "rotation":
                    client?.Rotation = Quaternion.Euler(vector);
                    server?.Rotation = Quaternion.Euler(vector);
                    break;
                case "scale":
                    client?.Scale = vector;
                    server?.Scale = vector;
                    break;
                default:
                    LogManager.Warn($"Unknown vector target property: {TargetProperty}");
                    break;
            }
        }
    }

    public class PrimitiveColorBlock : BlockBase
    {
        public float R { get; set; }
        public float G { get; set; }
        public float B { get; set; }
        public float A { get; set; }

        public override void Execute(object obj)
        {
            PrimitiveObject? client = obj as PrimitiveObject;
            PrimitiveObjectServer? server = obj as PrimitiveObjectServer;
            if (client is null && server is null)
                return;

            Color color = new(R, G, B, A);
            client?.Color = color;
            server?.Color = color;
        }
    }

    public class PrimitiveStateBlock : BlockBase
    {
        public string SettingType { get; set; } = string.Empty;
        public string StringValue { get; set; } = string.Empty;
        public bool BoolValue { get; set; }
        public byte ByteValue { get; set; }

        public override void Execute(object obj)
        {
            PrimitiveObject? client = obj as PrimitiveObject;
            PrimitiveObjectServer? server = obj as PrimitiveObjectServer;
            if (client is null && server is null)
                return;

            switch (SettingType)
            {
                case "type":
                    if (Enum.TryParse(StringValue, out PrimitiveType primitiveType))
                    {
                        client?.PrimitiveType = primitiveType;
                        server?.PrimitiveType = primitiveType;
                    }
                    else
                        LogManager.Warn($"Failed to parse PrimitiveType from '{StringValue}'.");

                    break;

                case "flags":
                    if (Enum.TryParse(StringValue, out PrimitiveFlags primitiveFlags))
                    {
                        server?.PrimitiveFlags = primitiveFlags;
                        client?.PrimitiveFlags = primitiveFlags;
                    }
                    else
                        LogManager.Warn($"Failed to parse PrimitiveFlags from '{StringValue}'.");

                    break;

                case "static":
                    server?.IsStatic = BoolValue;
                    client?.IsStatic = BoolValue;
                    break;

                case "smoothing":
                    server?.MovementSmoothing = ByteValue;
                    client?.MovementSmoothing = ByteValue;
                    break;

                default:
                    LogManager.Warn($"Unknown state setting type: {SettingType}");
                    break;
            }
        }
    }
}