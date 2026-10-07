// -----------------------------------------------------------------------
// <copyright file="SerializableLOD.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using UnityEngine;

namespace ThaumielMapEditor.API.Serialization
{
    [GitBookPage("Serialization/SerializableLOD")]
    public class SerializableLOD
    {
        public Vector3 Bounds { get; set; }
        public List<PrimitiveType> Primitives { get; set; } = [];
    }
}