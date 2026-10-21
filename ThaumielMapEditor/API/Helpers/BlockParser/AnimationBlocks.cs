// -----------------------------------------------------------------------
// <copyright file="AnimationBlocks.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using ThaumielMapEditor.API.Data;

namespace ThaumielMapEditor.API.Helpers.BlockParser
{
    public class AnimationPlayBlock : BlockBase
    {
        public string AnimationName { get; set; } = string.Empty;

        public override void Execute()
        {
            Executor?.Schematic.AnimationController.Play(AnimationName);
        }
    }

    public class SchematicAnimationPlayBlock : BlockBase
    {
        public string AnimationName { get; set; } = string.Empty;

        public override void Execute(object obj)
        {
            if (ResolveTarget(obj) is not SchematicData schematic)
            {
                LogManager.Warn("Animation block expected a Schematic input.");
                return;
            }

            schematic.AnimationController.Play(AnimationName);
        }
    }

    public class SchematicAnimationPlayIndexBlock : BlockBase
    {
        public string AnimationName { get; set; } = string.Empty;
        public int AnimatorIndex { get; set; }

        public override void Execute(object obj)
        {
            if (ResolveTarget(obj) is not SchematicData schematic)
            {
                LogManager.Warn("Animation index block expected a Schematic input.");
                return;
            }

            schematic.AnimationController.Play(AnimationName, AnimatorIndex);
        }
    }

    public class AnimationPauseBlock : BlockBase
    {
        public string AnimationName { get; set; } = string.Empty;

        public override void Execute()
        {
            Executor?.Schematic.AnimationController.Pause(AnimationName);
        }
    }

    public class SchematicAnimationPauseBlock : BlockBase
    {
        public string AnimationName { get; set; } = string.Empty;

        public override void Execute(object obj)
        {
            if (ResolveTarget(obj) is not SchematicData schematic)
            {
                LogManager.Warn("Animation pause block expected a Schematic input.");
                return;
            }

            schematic.AnimationController.Pause(AnimationName);
        }
    }

    public class SchematicAnimationPauseIndexBlock : BlockBase
    {
        public string AnimationName { get; set; } = string.Empty;
        public int AnimatorIndex { get; set; }

        public override void Execute(object obj)
        {
            if (ResolveTarget(obj) is not SchematicData schematic)
            {
                LogManager.Warn("Animation pause index block expected a Schematic input.");
                return;
            }

            LogManager.Debug($"Pausing animation '{AnimationName}' at animator index {AnimatorIndex} on '{schematic.FileName}'.");
            schematic.AnimationController.Pause(AnimatorIndex);
        }
    }
}
