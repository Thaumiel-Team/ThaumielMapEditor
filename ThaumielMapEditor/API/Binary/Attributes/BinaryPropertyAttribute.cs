// -----------------------------------------------------------------------
// <copyright file="BinaryPropertyAttribute.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using System;

namespace ThaumielMapEditor.API.Binary.Attributes
{
    /// <summary>
    /// Controls the serialized name, order and requiredness of a property or field.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public sealed class BinaryPropertyAttribute : Attribute
    {
        /// <summary>
        /// Gets or sets the name used in the binary file.
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Gets or sets the order in which the member is written.
        /// </summary>
        public int Order { get; set; }

        /// <summary>
        /// Gets or sets whether the member must be present when reading.
        /// </summary>
        public bool Required { get; set; }
    }
}
