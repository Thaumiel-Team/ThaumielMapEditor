// -----------------------------------------------------------------------
// <copyright file="Cassie.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU Lesser General Public License v3.0 (LGPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using System;
using LabApi.Features.Wrappers;
using ThaumielMapEditor.API.Helpers;

namespace ThaumielMapEditor.API.Components.Tools.Helpers
{
    [Serializable]
    public class SendCassieMessage
    {
        public string Message { get; set; } = string.Empty;
        public string CustomSubtitles { get; set; } = string.Empty;
        public bool PlayBackground { get; set; }
        public float Priority { get; set; }
        public float GlitchScale { get; set; }

        public void ValidateLines()
        {
            foreach (string word in Message.Split(' '))
            {
                if (!Announcer.IsValid(word))
                    LogManager.Warn($"Invalid Cassie word '{word}' this will not be played");
            }
        }
    }
}