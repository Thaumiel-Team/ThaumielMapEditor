// -----------------------------------------------------------------------
// <copyright file="PrimitiveHandler.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using System;
using AdminToys;
using ThaumielMapEditor.API.Blocks.ClientSide;
using ThaumielMapEditor.API.Data;

namespace ThaumielMapEditor.Events
{
    internal class PrimitiveHandler
    {

        public static void Register()
        {
            ClientObject.ScaleUpdated += OnScaleUpdated;
            ClientObject.PositionUpdated += OnPositionUpdated;
            ClientObject.RotationUpdated += OnRotationUpdated;
            SchematicData.SchematicPositionUpdated += OnSchematicPositionUpdated;
            SchematicData.SchematicRotationUpdated += OnSchematicRotationUpdated;
        }

        public static void Unregister()
        {
            ClientObject.ScaleUpdated -= OnScaleUpdated;
            ClientObject.PositionUpdated -= OnPositionUpdated;
            ClientObject.RotationUpdated -= OnRotationUpdated;
            SchematicData.SchematicPositionUpdated -= OnSchematicPositionUpdated;
            SchematicData.SchematicRotationUpdated -= OnSchematicRotationUpdated;
        }

        private static void OnScaleUpdated(Vector3 scale, ClientObject client)
        {
            if (client is not PrimitiveObject primitive)
                return;

            if (!primitive.PrimitiveFlags.HasFlag(PrimitiveFlags.Collidable) || primitive.ServerCollider == null || primitive.Schematic == null || primitive.Schematic.Primitive == null)
                return;

            primitive.ServerCollider.transform.localScale = new(Math.Abs(scale.x), Math.Abs(scale.y), Math.Abs(scale.z));
        }

        private static void OnPositionUpdated(Vector3 position, ClientObject client)
        {
            if (client is not PrimitiveObject primitive)
                return;

            if (!primitive.PrimitiveFlags.HasFlag(PrimitiveFlags.Collidable) || primitive.ServerCollider == null || primitive.Schematic == null || primitive.Schematic.Primitive == null)
                return;

            if (primitive.Schematic.ServerSideTransforms.TryGetValue(primitive.ParentId, out var transform))
            {
                primitive.ServerCollider.transform.position = transform.TransformPoint(primitive.Position);
            }
            else
                primitive.ServerCollider.transform.position = primitive.Schematic.Primitive.Transform.TransformPoint(position);
        }

        private static void OnRotationUpdated(Quaternion rotation, ClientObject client)
        {
            if (client is not PrimitiveObject primitive)
                return;

            if (!primitive.PrimitiveFlags.HasFlag(PrimitiveFlags.Collidable) || primitive.ServerCollider == null || primitive.Schematic == null || primitive.Schematic.Primitive == null)
                return;

            if (primitive.Schematic.ServerSideTransforms.TryGetValue(primitive.ParentId, out var transform))
            {
                primitive.ServerCollider.transform.rotation = transform.rotation * primitive.Rotation;
            }
            else
                primitive.ServerCollider.transform.rotation = rotation;
        }

        private static void OnSchematicPositionUpdated(SchematicData schematic)
        {
            foreach (PrimitiveObject primitive in schematic.GetClientObject<PrimitiveObject>())
            {
                if (!primitive.PrimitiveFlags.HasFlag(PrimitiveFlags.Collidable) || primitive.ServerCollider == null || schematic.Primitive == null)
                    continue;

                if (schematic.ServerSideTransforms.TryGetValue(primitive.ParentId, out var transform))
                {
                    primitive.ServerCollider.transform.position = transform.TransformPoint(primitive.Position);
                }
                else
                    primitive.ServerCollider.transform.position = schematic.Primitive.Transform.TransformPoint(primitive.Position);
            }
        }

        private static void OnSchematicRotationUpdated(SchematicData schematic)
        {
            foreach (PrimitiveObject primitive in schematic.GetClientObject<PrimitiveObject>())
            {
                if (!primitive.PrimitiveFlags.HasFlag(PrimitiveFlags.Collidable) || primitive.ServerCollider == null)
                    continue;

                if (schematic.ServerSideTransforms.TryGetValue(primitive.ParentId, out var transform))
                {
                    primitive.ServerCollider.transform.rotation = transform.rotation * primitive.Rotation;
                }
                else
                    primitive.ServerCollider.transform.rotation = schematic.Rotation * primitive.Rotation;
            }
        }
    }
}