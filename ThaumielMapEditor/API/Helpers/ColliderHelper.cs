// -----------------------------------------------------------------------
// <copyright file="ColliderHelper.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using AdminToys;
using ThaumielMapEditor.API.Blocks.ClientSide;
using ThaumielMapEditor.API.Blocks.ServerObjects;
using ThaumielMapEditor.API.Data;
using ThaumielMapEditor.API.Enums;
using UnityEngine;

namespace ThaumielMapEditor.API.Helpers
{
    [GitBookPage("ColliderHelper")]
    public static class ColliderHelper
    {
        /// <summary>
        /// Collection of all the <see cref="Collider"/>s in a schematic.
        /// </summary>
        public static Dictionary<SchematicData, HashSet<Collider>> SchematicColliders = [];

        /// <summary>
        /// Creates the colliders on the server for the client side objects.
        /// </summary>
        /// <param name="clientObject">The <see cref="ClientObject"/> to create colliders for.</param>
        /// <param name="schematic">The <see cref="SchematicData"/> that the colliders will be parented to.</param>
        public static void CreateClientObjectColliders(ClientObject clientObject, SchematicData schematic)
        {
            if (!SchematicColliders.ContainsKey(schematic))
                SchematicColliders.Add(schematic, []);

            List<Collider> colliders = [];
            GameObject? prefab = GetPrefabForClientObject(clientObject);

            if (prefab == null)
            {
                LogManager.Warn($"Could not find prefab for client object of type '{clientObject.ObjectType}', creating selection sphere as fallback.");
                CreateSelectionSphere(clientObject, schematic);
                return;
            }

            Collider[] prefabColliders = prefab.GetComponentsInChildren<Collider>(includeInactive: true);
            if (prefabColliders.Length == 0)
            {
                LogManager.Debug($"No colliders found on prefab '{prefab.name}', creating selection sphere as fallback.");
                CreateSelectionSphere(clientObject, schematic);
                return;
            }

            Transform? targetParent = ResolveServerParentTransform(clientObject.ParentId, schematic);
            if (targetParent == null)
            {
                LogManager.Warn($"Failed to resolve parent for colliders of {clientObject.ObjectType} - {clientObject.ObjectId}, falling back to selection sphere.");
                CreateSelectionSphere(clientObject, schematic);
                return;
            }

            bool anyCopied = false;
            foreach (Collider col in prefabColliders)
            {
                if (!col.enabled)
                    continue;

                Vector3 localOffset = col.transform.localPosition;
                Quaternion localRotation = col.transform.localRotation;
                Vector3 localScale = col.transform.localScale;

                GameObject colGo = new($"[ThaumielMapEditor] Collider_{clientObject.ObjectType}_{col.GetType().Name}");

                colGo.transform.SetParent(targetParent, worldPositionStays: false);
                colGo.transform.localPosition = clientObject.Position + clientObject.Rotation * Vector3.Scale(localOffset, clientObject.Scale);
                colGo.transform.localRotation = clientObject.Rotation * localRotation;
                colGo.transform.localScale = Vector3.Scale(clientObject.Scale, localScale);

                if (TryCopyCollider(col, colGo))
                {
                    Collider collider = colGo.GetComponent<Collider>();
                    colliders.Add(collider);
                    SchematicColliders[schematic].Add(collider);
                    anyCopied = true;
                }
                else
                {
                    LogManager.Warn($"Unhandled collider type '{col.GetType().Name}' on prefab '{prefab.name}', skipping.");
                    UnityEngine.Object.Destroy(colGo);
                }
            }

            if (!anyCopied)
            {
                LogManager.Debug($"No usable colliders on prefab '{prefab.name}', creating selection sphere as fallback.");
                CreateSelectionSphere(clientObject, schematic);
                return;
            }

            clientObject.ServerColliders = colliders.ToArray();
        }

        /// <summary>
        /// Moves the server side colliders of the specified <see cref="ClientObject"/> to its current transform.
        /// </summary>
        /// <param name="clientObject">The <see cref="ClientObject"/> whose colliders will be synced.</param>
        /// <param name="schematic">The <see cref="SchematicData"/> used to resolve the parent transform.</param>
        public static void SyncClientObjectColliders(ClientObject clientObject, SchematicData? schematic)
        {
            if (clientObject?.ServerColliders == null || clientObject.ServerColliders.Length == 0 || schematic == null)
                return;

            if (!schematic.ServerSideTransforms.TryGetValue(clientObject.ParentId, out Transform parent) || parent == null)
                parent = schematic.Primitive?.Transform!;

            if (parent == null)
                return;

            GameObject? prefab = GetPrefabForClientObject(clientObject);
            List<Collider> sources = [];
            if (prefab != null)
            {
                foreach (Collider source in prefab.GetComponentsInChildren<Collider>(includeInactive: true))
                {
                    if (!source.enabled || !IsHandledColliderType(source))
                        continue;

                    sources.Add(source);
                }
            }

            for (int i = 0; i < clientObject.ServerColliders.Length; i++)
            {
                Collider collider = clientObject.ServerColliders[i];
                if (collider == null || collider.gameObject == null)
                    continue;

                if (collider.gameObject.name.StartsWith("[ThaumielMapEditor] Selection_", StringComparison.Ordinal))
                {
                    if (collider.transform.parent != parent)
                        collider.transform.SetParent(parent, worldPositionStays: false);

                    collider.transform.localPosition = clientObject.Position;
                    collider.transform.localRotation = clientObject.Rotation;
                    continue;
                }

                if (i >= sources.Count)
                {
                    if (collider.transform.parent != parent)
                        collider.transform.SetParent(parent, worldPositionStays: false);

                    collider.transform.localPosition = clientObject.Position;
                    collider.transform.localRotation = clientObject.Rotation;
                    continue;
                }

                Collider sourceCollider = sources[i];
                if (collider.transform.parent != parent)
                    collider.transform.SetParent(parent, worldPositionStays: false);

                collider.transform.localPosition = clientObject.Position + clientObject.Rotation * Vector3.Scale(sourceCollider.transform.localPosition, clientObject.Scale);
                collider.transform.localRotation = clientObject.Rotation * sourceCollider.transform.localRotation;
                collider.transform.localScale = Vector3.Scale(clientObject.Scale, sourceCollider.transform.localScale);
            }
        }

        private static bool IsHandledColliderType(Collider source) =>
            source is BoxCollider or SphereCollider or CapsuleCollider or MeshCollider;

        public static void CreateSelectionSphere(ClientObject clientObject, SchematicData schematic, float radius = 0.5f)
        {
            if (clientObject == null || schematic == null)
                return;

            Transform? targetParent = ResolveServerParentTransform(clientObject.ParentId, schematic);
            if (targetParent == null)
            {
                LogManager.Warn($"Failed to get parent for selection sphere of {clientObject.ObjectType} - {clientObject.ObjectId}");
                return;
            }

            GameObject go = new($"[ThaumielMapEditor] Selection_{clientObject.ObjectType}");
            go.transform.SetParent(targetParent, worldPositionStays: false);
            go.transform.localPosition = clientObject.Position;
            go.transform.localRotation = clientObject.Rotation;

            SphereCollider sphere = go.AddComponent<SphereCollider>();
            sphere.isTrigger = true;
            sphere.radius = radius;

            clientObject.ServerColliders = [.. clientObject.ServerColliders, sphere];

            if (!SchematicColliders.TryGetValue(schematic, out var value))
            {
                value = [];
                SchematicColliders.Add(schematic, value);
            }

            value.Add(sphere);
            LogManager.Debug($"Created selection sphere for {clientObject.ObjectType} {clientObject.ObjectId}.");
        }

        /// <summary>
        /// Moves the selection spheres of the specified <see cref="ClientObject"/> to its current transform.
        /// </summary>
        /// <param name="clientObject">The <see cref="ClientObject"/> whose selection spheres will be synced.</param>
        /// <param name="schematic">The <see cref="SchematicData"/> used to resolve the parent transform.</param>
        public static void SyncSelectionSpheres(ClientObject clientObject, SchematicData? schematic)
        {
            if (clientObject?.ServerColliders == null || clientObject.ServerColliders.Length == 0 || schematic?.Primitive?.Transform == null)
                return;

            Transform root = schematic.ServerSideTransforms.TryGetValue(clientObject.ParentId, out Transform transform) && transform != null ? transform : schematic.Primitive.Transform;
            foreach (Collider collider in clientObject.ServerColliders)
            {
                if (collider == null)
                    continue;

                collider.transform.position = root.TransformPoint(clientObject.Position);
                collider.transform.rotation = root.rotation * clientObject.Rotation;
            }
        }

        /// <summary>
        /// Destroys the server side colliders of the specified <see cref="ClientObject"/>.
        /// </summary>
        /// <param name="clientObject">The <see cref="ClientObject"/> whose colliders will be destroyed.</param>
        /// <param name="schematic">The <see cref="SchematicData"/> the colliders were tracked under.</param>
        public static void DestroyClientObjectColliders(ClientObject clientObject, SchematicData schematic)
        {
            if (clientObject?.ServerColliders == null)
                return;

            foreach (Collider collider in clientObject.ServerColliders)
            {
                if (collider == null)
                    continue;

                if (schematic != null && SchematicColliders.TryGetValue(schematic, out HashSet<Collider>? set))
                    set.Remove(collider);

                if (collider.gameObject != null)
                    UnityEngine.Object.Destroy(collider.gameObject);
            }

            clientObject.ServerColliders = [];

            if (clientObject is PrimitiveObject primitive && primitive.ServerCollider != null)
            {
                if (schematic != null && SchematicColliders.TryGetValue(schematic, out HashSet<Collider>? primitiveSet))
                    primitiveSet.Remove(primitive.ServerCollider);

                if (primitive.ServerCollider.gameObject != null)
                    UnityEngine.Object.Destroy(primitive.ServerCollider.gameObject);

                primitive.ServerCollider = null;
            }
            else if (clientObject is PrimitiveObject primitiveNull)
                primitiveNull.ServerCollider = null;
        }

        /// <summary>
        /// Disables all the colliders on the specified <see cref="ClientObject"/>.
        /// </summary>
        /// <param name="clientObject">The client side object to disable the server side colliders on.</param>
        public static void DisableColliders(ClientObject clientObject)
        {
            if (clientObject.ServerColliders.IsEmpty())
                return;

            foreach (Collider collider in clientObject.ServerColliders)
            {
                if (!collider.enabled)
                    continue;

                collider.enabled = false;
            }
        }

        /// <summary>
        /// If <paramref name="enabled"/> is <see langword="true"/> colliders will be enabled else if <paramref name="enabled"/> is <see langword="false"/> colliders will be disabled. 
        /// </summary>
        /// <param name="clientObject"></param>
        /// <param name="enabled"></param>
        public static void SetColliders(ClientObject clientObject, bool enabled)
        {
            if (enabled)
            {
                EnableColliders(clientObject);
            }
            else
                DisableColliders(clientObject);
        }

        /// <summary>
        /// Enables all the colliders on the specified <see cref="ClientObject"/>.
        /// </summary>
        /// <param name="clientObject">The client side object to enable the server side colliders on.</param>
        public static void EnableColliders(ClientObject clientObject)
        {
            if (clientObject.ServerColliders.IsEmpty())
            {
                LogManager.Warn($"Failed to enable colliders for object {clientObject.AssetId} - {clientObject.ObjectType} there are no colliders");
                return;
            }

            foreach (Collider collider in clientObject.ServerColliders)
            {
                if (collider.enabled)
                    continue;

                collider.enabled = true;
            }
        }

        /// <summary>
        /// Copies a <see cref="Collider"/> component from a prefab onto the target <see cref="GameObject"/>.
        /// Returns <see langword="true"/> if the collider type was handled.
        /// </summary>
        private static bool TryCopyCollider(Collider source, GameObject target)
        {
            switch (source)
            {
                case BoxCollider box:
                    BoxCollider newBox = target.AddComponent<BoxCollider>();
                    newBox.center = box.center;
                    newBox.size = box.size;
                    newBox.isTrigger = box.isTrigger;
                    return true;

                case SphereCollider sphere:
                    SphereCollider newSphere = target.AddComponent<SphereCollider>();
                    newSphere.center = sphere.center;
                    newSphere.radius = sphere.radius;
                    newSphere.isTrigger = sphere.isTrigger;
                    return true;

                case CapsuleCollider capsule:
                    CapsuleCollider newCapsule = target.AddComponent<CapsuleCollider>();
                    newCapsule.center = capsule.center;
                    newCapsule.radius = capsule.radius;
                    newCapsule.height = capsule.height;
                    newCapsule.direction = capsule.direction;
                    newCapsule.isTrigger = capsule.isTrigger;
                    return true;

                case MeshCollider mesh:
                    MeshCollider newMesh = target.AddComponent<MeshCollider>();
                    newMesh.sharedMesh = mesh.sharedMesh;
                    newMesh.convex = mesh.convex;
                    newMesh.isTrigger = mesh.isTrigger;
                    return true;

                default:
                    return false;
            }
        }

        /// <summary>
        /// Resolves the source prefab <see cref="GameObject"/> for a given client side object.
        /// </summary>
        private static GameObject? GetPrefabForClientObject(ClientObject clientObject)
        {
            return clientObject.ObjectType switch
            {
                ObjectType.Primitive => PrefabHelper.PrimitiveObject?.gameObject,
                ObjectType.Capybara => PrefabHelper.Capybara?.gameObject,
                ObjectType.Clutter => clientObject is ClientClutterObject clutter ? TryGetClutterPrefab(clutter) : null,
                _ => null
            };
        }

        /// <summary>
        /// Resolves the clutter prefab for the given <see cref="ClientClutterObject"/>, returning <see langword="null"/> when its type is unknown.
        /// </summary>
        private static GameObject? TryGetClutterPrefab(ClientClutterObject clutter)
        {
            try
            {
                return ClutterObject.GetClutterPrefab(clutter.Type);
            }
            catch (ArgumentOutOfRangeException ex)
            {
                LogManager.Warn($"Could not resolve clutter prefab for type '{clutter.Type}': {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Creates the colliders for primitives
        /// </summary>
        /// <param name="primitive"></param>
        public static void CreateCollisionMesh(PrimitiveObject primitive)
        {
            if ((primitive.PrimitiveFlags & PrimitiveFlags.Collidable) == 0 || primitive.Schematic == null)
                return;

            if (primitive.ServerCollider != null)
                return;

            GameObject collider = new();
            collider.transform.name = $"[ThaumielMapEditor] Collider_{primitive.Name}";

            Transform? targetParent = ResolveServerParentTransform(primitive.ParentId, primitive.Schematic);
            if (targetParent == null)
            {
                LogManager.Warn($"Failed to get parent for primitive {primitive.Name} - {primitive.ObjectId}");
                return;
            }

            collider.transform.SetParent(targetParent, worldPositionStays: false);
            collider.transform.localPosition = primitive.Position;
            collider.transform.localRotation = primitive.Rotation;
            collider.transform.localScale = new Vector3(Math.Abs(primitive.Scale.x), Math.Abs(primitive.Scale.y), Math.Abs(primitive.Scale.z));

            MeshCollider mesh = collider.AddComponent<MeshCollider>();
            mesh.sharedMesh = PrimitiveObjectToy.PrimitiveTypeToMesh[primitive.PrimitiveType];

            if (mesh != null)
            {
                primitive.ServerCollider = mesh;
                if (!SchematicColliders.TryGetValue(primitive.Schematic, out var value))
                {
                    value = [];
                    SchematicColliders.Add(primitive.Schematic, value);
                }

                value.Add(mesh);
                LogManager.Debug($"Created collider for primitive {primitive.Name} with type {primitive.PrimitiveType} at {collider.transform.position}.");
            }
            else
            {
                LogManager.Warn($"Failed to get mesh for primitive {primitive.Name}, skipping collider.");
                UnityEngine.Object.Destroy(collider);
            }
        }

        internal static Transform ResolveServerParentTransform(int parentId, SchematicData schematic)
        {
            if (schematic.ServerSideTransforms.TryGetValue(parentId, out Transform transform))
                return transform;

            if (schematic?.Primitive?.Transform != null)
            {
                if (parentId != schematic.RootObjectId)
                    LogManager.Warn($"Could not find parent transform for id {parentId}, falling back to schematic root.");
                    
                return schematic.Primitive.Transform;
            }

            LogManager.Error($"Could not resolve parent transform for id {parentId} and schematic root is null.");
            return null!;
        }
    }
}