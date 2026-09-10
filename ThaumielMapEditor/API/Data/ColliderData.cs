// -----------------------------------------------------------------------
// <copyright file="ColliderData.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using ThaumielMapEditor.API.Enums;
using ThaumielMapEditor.API.Helpers;
using UnityEngine;

namespace ThaumielMapEditor.API.Data
{
    public class PrefabCollidersData
    {
#pragma warning disable CS8618
        /// <summary>
        /// Gets the prefab this collider data belongs to.
        /// </summary>
        public GameObject Prefab { get; internal set; }

        /// <summary>
        /// Gets the collection of <see cref="ColliderData"/> instances associated with this prefab.
        /// </summary>
        public IEnumerable<ColliderData> Colliders { get; internal set; }
#pragma warning restore CS8618
    }

    public class ColliderData
    {
        /// <summary>
        /// The <see cref="ColliderType"/> of this collider.
        /// </summary>
        public ColliderType Type { get; set; }

        /// <summary>
        /// Gets or sets the position of the collider local to its parent object.
        /// </summary>
        public Vector3 LocalPosition { get; set; }

        /// <summary>
        /// Gets or sets the size of the collider.
        /// </summary>
        public Vector3 Size { get; set; }

        /// <summary>
        /// Gets or sets the rotaion of the collider
        /// </summary>
        public Quaternion Rotation { get; set; }

        private static readonly List<Collider> SharedColliderBuffer = [];

        /// <summary>
        /// Determines the specific collider type for the given Unity collider instance.
        /// </summary>
        /// <param name="collider">The collider instance to evaluate. Must be a non-null instance of a supported Unity collider type.</param>
        /// <returns>A value of the ColliderType enumeration that corresponds to the type of the provided collider.</returns>
        public static ColliderType GetTypeFromCollider(Collider collider)
        {
            return collider switch
            {
                BoxCollider => ColliderType.Box,
                SphereCollider => ColliderType.Sphere,
                CapsuleCollider => ColliderType.Capsule,
                MeshCollider => ColliderType.Mesh,
                _ => throw new NotSupportedException($"Collider type '{collider.GetType().Name}' is not supported.")
            };
        }

        /// <summary>
        /// Parses all of the <see cref="Collider"/>s from a <see cref="GameObject"/> into <see cref="ColliderData"/>s
        /// </summary>
        /// <param name="gameObject">The <see cref="GameObject"/> to parse.</param>
        /// <returns>A enumerable containing all of the <see cref="Collider"/>s</returns>
        /// <exception cref="NotSupportedException">Throws when a unsupported collider is parsed by <see cref="GetTypeFromCollider"/></exception>
        public static IEnumerable<ColliderData> ParseObjectColliders(GameObject gameObject)
        {
            SharedColliderBuffer.Clear();
            gameObject.GetComponentsInChildren(true, SharedColliderBuffer);
            List<ColliderData> colliders = new(SharedColliderBuffer.Count);
            
            foreach (Collider collider in SharedColliderBuffer)
            {
                if (collider.GetType() == typeof(CharacterController))
                    continue;

                ColliderType type;
                try
                {
                    type = GetTypeFromCollider(collider);
                }
                catch (NotSupportedException)
                {
                    LogManager.Warn($"Skipping unsupported collider type '{collider.GetType().Name}' on '{collider.gameObject.name}'.");
                    continue;
                }

                ColliderData data = new()
                {
                    Type = type,
                    LocalPosition = collider.transform.localPosition,
                    Size = collider switch
                    {
                        BoxCollider box => box.size,
                        SphereCollider sphere => Vector3.one * sphere.radius * 2f,
                        CapsuleCollider capsule => new Vector3(capsule.radius * 2f, capsule.height, capsule.radius * 2f),
                        MeshCollider mesh => mesh.sharedMesh.bounds.size,
                        _ => throw new NotSupportedException()
                    },
                    Rotation = collider.transform.localRotation
                };

                colliders.Add(data);
            }

            return colliders;
        }
    }
}