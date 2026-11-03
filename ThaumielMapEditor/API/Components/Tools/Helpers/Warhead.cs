// -----------------------------------------------------------------------
// <copyright file="Warhead.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU Lesser General Public License v3.0 (LGPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using System;
using ThaumielMapEditor.API.Enums;

namespace ThaumielMapEditor.API.Components.Tools.Helpers
{
    [Serializable]
    public class Warhead
    {
        public WarheadAction Action { get; set; }
        public bool SuppressSubtitles { get; set; }
    }
}