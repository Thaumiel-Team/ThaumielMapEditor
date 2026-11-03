// -----------------------------------------------------------------------
// <copyright file="Audio.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU Lesser General Public License v3.0 (LGPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using System;

namespace ThaumielMapEditor.API.Components.Tools.Helpers
{
    [Serializable]
    public class PlayAudio
    {
        public string Path { get; set; } = string.Empty;
        public float Volume { get; set; }
        public float MinDistance { get; set; }
        public float MaxDistance { get; set; }
        public bool IsSpatial { get; set; }
    }
}