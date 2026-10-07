// -----------------------------------------------------------------------
// <copyright file="Classes.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using ThaumielMapEditor.API.Serialization;

namespace ThaumielMapEditor.API.Components.Tools.Helpers
{
    [Obsolete("Use Classes instead. Will be removed in 1.0.0")]
    public class ColliderClasses : Classes;

    [Obsolete("Use Classes instead. Will be removed in 1.0.0")]
    public class InteractableClasses : Classes;

    [Serializable]
    public class Classes
    {
        public List<PlayAudio> PlayAudio { get; set; } = [];
        public List<RunCommand> RunCommand { get; set; } = [];
        public List<GiveEffect> GiveEffect { get; set; } = [];
        public List<RemoveEffect> RemoveEffect { get; set; } = [];
        public List<PlayAnimation> PlayAnimation { get; set; } = [];
        public List<GiveItem> GiveItem { get; set; } = [];
        public List<RemoveItem> RemoveItem { get; set; } = [];
        public List<Warhead> Warhead { get; set; } = [];
        public List<SendCassieMessage> SendCassieMessage { get; set; } = [];
        public List<BlockyPayload> Blocky { get; set; } = [];
    }
}