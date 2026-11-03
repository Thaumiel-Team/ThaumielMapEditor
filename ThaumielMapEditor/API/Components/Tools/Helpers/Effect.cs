// -----------------------------------------------------------------------
// <copyright file="Effect.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU Lesser General Public License v3.0 (LGPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using System;
using ThaumielMapEditor.API.Enums;

namespace ThaumielMapEditor.API.Components.Tools.Helpers
{
    [Serializable]
    public class GiveEffect
    {
        public int Intensity { get; set; }
        public float Duration { get; set; }
        public EffectType Effect { get; set; }
    }

    [Serializable]
    public class RemoveEffect
    {
        public int Intensity { get; set; }
        public EffectType Effect { get; set; }
    }
}