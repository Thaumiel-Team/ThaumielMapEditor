// -----------------------------------------------------------------------
// <copyright file="UnityTransformBlocks.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using UnityEngine;

namespace ThaumielMapEditor.API.Helpers.BlockParser
{
    public class UnityGetPosBlock : BlockBase
    {
        public object OBJ { get; set; } = null!;

        public override object? ReturnExecute(object obj)
        {
            if (OBJ is not Transform t)
            {
                LogManager.Warn("OBJ is not a Transform.");
                return null!;
            }

            return t.position;
        }
    }

    public class UnitySetPosBlock : BlockBase
    {
        public object OBJ { get; set; } = null!;
        public Vector3 POS { get; set; }

        public override void Execute(object obj)
        {
            if (OBJ is not Transform t)
            {
                LogManager.Warn("OBJ is not a Transform.");
                return;
            }

            t.position = POS;
        }
    }

    public class UnityTranslateBlock : BlockBase
    {
        public object OBJ { get; set; } = null!;
        public Vector3 V { get; set; }

        public override void Execute(object obj)
        {
            if (OBJ is not Transform t)
            {
                LogManager.Warn("OBJ is not a Transform.");
                return;
            }

            t.Translate(V);
        }
    }

    public class UnityRotateBlock : BlockBase
    {
        public object OBJ { get; set; } = null!;
        public Vector3 V { get; set; }

        public override void Execute(object obj)
        {
            if (OBJ is not Transform t)
            {
                LogManager.Warn("OBJ is not a Transform.");
                return;
            }

            t.Rotate(V);
        }
    }

    public class UnitySetScaleBlock : BlockBase
    {
        public object OBJ { get; set; } = null!;
        public Vector3 S { get; set; }

        public override void Execute(object obj)
        {
            if (OBJ is not Transform t)
            {
                LogManager.Warn("OBJ is not a Transform.");
                return;
            }

            t.localScale = S;
        }
    }

    public class UnityLookAtBlock : BlockBase
    {
        public object OBJ { get; set; } = null!;
        public object T { get; set; } = null!;

        public override void Execute(object obj)
        {
            if (OBJ is not Transform t)
            {
                LogManager.Warn("OBJ is not a Transform.");
                return;
            }

            switch (T)
            {
                case Transform target:
                    t.LookAt(target);
                    break;

                case Vector3 pos:
                    t.LookAt(pos);
                    break;

                default:
                    LogManager.Warn("Invalid target.");
                    break;
            }
        }
    }
}