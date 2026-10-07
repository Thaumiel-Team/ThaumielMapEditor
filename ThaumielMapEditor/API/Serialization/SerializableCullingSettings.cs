// -----------------------------------------------------------------------
// <copyright file="SerializableCullingSettings.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using System;

namespace ThaumielMapEditor.API.Serialization
{
    [Serializable]
    [GitBookPage("Serialization/CullingSettings")]
    public class SerializableCullingSettings
    {
        public Vector3 Bounds { get; set; }
    }
}