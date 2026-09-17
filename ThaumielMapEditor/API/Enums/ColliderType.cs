// -----------------------------------------------------------------------
// <copyright file="ColliderType.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using UnityEngine;

namespace ThaumielMapEditor.API.Enums
{
    /// <summary>
    /// Defines the types of <see cref="Collider"/> as a enum.
    /// </summary>
    [GitBookPage("Enums/ColliderType")]
    public enum ColliderType
    {
        /// <summary>
        /// <see cref="BoxCollider"/>
        /// </summary>
        Box,

        /// <summary>
        /// <see cref="SphereCollider"/>
        /// </summary>
        Sphere,

        /// <summary>
        /// <see cref="CapsuleCollider"/>
        /// </summary>
        Capsule,

        /// <summary>
        /// <see cref="MeshCollider"/>
        /// </summary>
        Mesh
    }
}
