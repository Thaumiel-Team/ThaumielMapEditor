// -----------------------------------------------------------------------
// <copyright file="PrimitiveHandler.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU Lesser General Public License v3.0 (LGPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using System;
using AdminToys;
using ThaumielMapEditor.API.Blocks.Definitions;
using ClutterDef = ThaumielMapEditor.API.Blocks.Definitions.Clutter;
using ThaumielMapEditor.API.Data;
using ThaumielMapEditor.API.Helpers;
using ThaumielMapEditor.API.Blocks;

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
            if (client is Capybara.Client capy)
            {
                ColliderHelper.SyncClientObjectColliders(capy, capy.Schematic);
                return;
            }

            if (client is ClutterDef.Client clutter)
            {
                ColliderHelper.SyncClientObjectColliders(clutter, clutter.Schematic);
                return;
            }

            if (client is not Primitive.Client primitive)
                return;

            if (primitive.ServerCollider == null || primitive.Schematic?.Primitive == null || (primitive.PrimitiveFlags & PrimitiveFlags.Collidable) == 0)
                return;

            primitive.ServerCollider.transform.localScale = new(Math.Abs(scale.x), Math.Abs(scale.y), Math.Abs(scale.z));
        }

        private static void OnPositionUpdated(Vector3 position, ClientObject client)
        {
            if (client is Light.Client light)
            {
                ColliderHelper.SyncSelectionSpheres(light, light.Schematic);
                return;
            }

            if (client is Capybara.Client capy)
            {
                ColliderHelper.SyncClientObjectColliders(capy, capy.Schematic);
                return;
            }

            if (client is ClutterDef.Client clutter)
            {
                ColliderHelper.SyncClientObjectColliders(clutter, clutter.Schematic);
                return;
            }

            if (client is CullingParent.Client culling)
            {
                ColliderHelper.SyncSelectionSpheres(culling, culling.Schematic);
                return;
            }

            if (client is not Primitive.Client primitive)
                return;

            if (primitive.ServerCollider == null || primitive.Schematic?.Primitive == null || (primitive.PrimitiveFlags & PrimitiveFlags.Collidable) == 0)
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
            if (client is Light.Client light)
            {
                ColliderHelper.SyncSelectionSpheres(light, light.Schematic);
                return;
            }

            if (client is Capybara.Client capy)
            {
                ColliderHelper.SyncClientObjectColliders(capy, capy.Schematic);
                return;
            }

            if (client is ClutterDef.Client clutter)
            {
                ColliderHelper.SyncClientObjectColliders(clutter, clutter.Schematic);
                return;
            }

            if (client is CullingParent.Client culling)
            {
                ColliderHelper.SyncSelectionSpheres(culling, culling.Schematic);
                return;
            }

            if (client is not Primitive.Client primitive)
                return;

            if (primitive.ServerCollider == null || primitive.Schematic?.Primitive == null || (primitive.PrimitiveFlags & PrimitiveFlags.Collidable) == 0)
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
            foreach (Light.Client light in schematic.GetClientObject<Light.Client>())
                ColliderHelper.SyncSelectionSpheres(light, schematic);

            foreach (CullingParent.Client culling in schematic.GetClientObject<CullingParent.Client>())
                ColliderHelper.SyncSelectionSpheres(culling, schematic);

            foreach (Capybara.Client capy in schematic.GetClientObject<Capybara.Client>())
                ColliderHelper.SyncClientObjectColliders(capy, schematic);

            foreach (ClutterDef.Client clutter in schematic.GetClientObject<ClutterDef.Client>())
                ColliderHelper.SyncClientObjectColliders(clutter, schematic);

            foreach (Primitive.Client primitive in schematic.GetClientObject<Primitive.Client>())
            {
                if ((primitive.PrimitiveFlags & PrimitiveFlags.Collidable) == 0 || primitive.ServerCollider == null || schematic.Primitive == null)
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
            foreach (Light.Client light in schematic.GetClientObject<Light.Client>())
                ColliderHelper.SyncSelectionSpheres(light, schematic);

            foreach (CullingParent.Client culling in schematic.GetClientObject<CullingParent.Client>())
                ColliderHelper.SyncSelectionSpheres(culling, schematic);

            foreach (Capybara.Client capy in schematic.GetClientObject<Capybara.Client>())
                ColliderHelper.SyncClientObjectColliders(capy, schematic);

            foreach (ClutterDef.Client clutter in schematic.GetClientObject<ClutterDef.Client>())
                ColliderHelper.SyncClientObjectColliders(clutter, schematic);

            foreach (Primitive.Client primitive in schematic.GetClientObject<Primitive.Client>())
            {
                if ((primitive.PrimitiveFlags & PrimitiveFlags.Collidable) == 0 || primitive.ServerCollider == null)
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