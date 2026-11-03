// -----------------------------------------------------------------------
// <copyright file="DoorType.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU Lesser General Public License v3.0 (LGPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using ThaumielMapEditor.API.Blocks.Definitions;

namespace ThaumielMapEditor.API.Enums
{
    /// <summary>
    /// Defines the types that <see cref="Door.Server"/> can use.
    /// </summary>
    [GitBookPage("Enums/DoorType")]
    public enum DoorType
    {
        /// <summary>
        /// Light containment zone
        /// </summary>
        Lcz = 1,

        /// <summary>
        /// Heavy containment zone
        /// </summary>
        Hcz = 2,

        /// <summary>
        /// Entrance zone
        /// </summary>
        Ez = 3,

        /// <summary>
        /// The gates used at Gate A, and Gate B
        /// </summary>
        Gate = 4,

        /// <summary>
        /// The containment bulkheads in heavy containment zone
        /// </summary>
        BulkHead = 5
    }
}