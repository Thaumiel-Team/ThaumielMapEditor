// -----------------------------------------------------------------------
// <copyright file="BinaryIgnoreAttribute.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using System;

namespace ThaumielMapEditor.API.Binary.Attributes
{
    /// <summary>
    /// Skips the property or field during binary serialization.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public sealed class BinaryIgnoreAttribute : Attribute
    {
    }
}
