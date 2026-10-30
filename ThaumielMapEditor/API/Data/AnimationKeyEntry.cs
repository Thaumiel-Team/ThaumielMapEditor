// -----------------------------------------------------------------------
// <copyright file="AnimationKeyEntry.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using System;
using ThaumielMapEditor.API.Serialization;

namespace ThaumielMapEditor.API.Data
{
    [Serializable]
    public class AnimationKeyEntry
    {
        public string StateName { get; set; } = string.Empty;

        public int KeyIndex { get; set; } = -1;

        public float KeyTime { get; set; }

        public BlockyPayload? Payload { get; set; }
    }
}
