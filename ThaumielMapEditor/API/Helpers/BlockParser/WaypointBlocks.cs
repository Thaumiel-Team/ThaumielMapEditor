// -----------------------------------------------------------------------
// <copyright file="WaypointBlocks.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using ThaumielMapEditor.API.Blocks.ServerObjects;
using ThaumielMapEditor.API.Data;

namespace ThaumielMapEditor.API.Helpers.BlockParser
{
    public class WaypointCreateBlock : BlockBase
    {
        public string Name { get; set; } = string.Empty;

        public override object ReturnExecute() => ReturnExecute(Executor!.Schematic);

        public override object ReturnExecute(SchematicData schematic)
        {
            WaypointObject server = new();
            server.SpawnObject(schematic);
            LogManager.Debug($"Created Waypoint '{Name}'.");
            return server;
        }
    }

    public class WaypointSetVisualizeBoundsBlock : BlockBase
    {
        public bool VisualizeBounds { get; set; }

        public override void Execute(object obj)
        {
            if (obj is not WaypointObject server)
            {
                LogManager.Warn("obj is not a WaypointObject.");
                return;
            }

            server.VisualizeBounds = VisualizeBounds;
        }
    }

    public class WaypointSetPriorityBlock : BlockBase
    {
        public float Priority { get; set; }

        public override void Execute(object obj)
        {
            if (obj is not WaypointObject server)
            {
                LogManager.Warn("obj is not a WaypointObject.");
                return;
            }

            server.Priority = Priority;
        }
    }

    public class WaypointSetBoundsSizeBlock : BlockBase
    {
        public float X { get; set; } = 1f;
        public float Y { get; set; } = 1f;
        public float Z { get; set; } = 1f;

        public override void Execute(object obj)
        {
            if (obj is not WaypointObject server)
            {
                LogManager.Warn("obj is not a WaypointObject.");
                return;
            }

            server.BoundsSize = new Vector3(X, Y, Z);
        }
    }

    public class WaypointGetPropertyBlock : BlockBase
    {
        public string Property { get; set; } = string.Empty;

        public override object ReturnExecute()
            => GetProperty(ResolveTarget());

        public override object ReturnExecute(object obj)
            => GetProperty(ResolveTarget(obj));

        private object GetProperty(object? obj)
        {
            if (obj is not WaypointObject server)
            {
                LogManager.Warn("obj is not a WaypointObject.");
                return null!;
            }

            return Property switch
            {
                "Position" => server.Position,
                "Rotation" => server.Rotation,
                "Scale" => server.Scale,
                "Base" => server.Base!,
                "VisualizeBounds" => server.VisualizeBounds,
                "Priority" => server.Priority,
                "BoundsSize" => server.BoundsSize,
                "WaypointId" => server.WaypointId,
                _ => LogUnknownProperty(Property)
            };
        }

        private static object LogUnknownProperty(string property)
        {
            LogManager.Warn($"Unknown property '{property}'.");
            return null!;
        }
    }
}