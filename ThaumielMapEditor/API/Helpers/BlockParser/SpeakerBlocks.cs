// -----------------------------------------------------------------------
// <copyright file="SpeakerBlocks.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using ThaumielMapEditor.API.Blocks.ServerObjects;
using ThaumielMapEditor.API.Data;

namespace ThaumielMapEditor.API.Helpers.BlockParser
{
    public class SpeakerCreateBlock : BlockBase
    {
        public string Name { get; set; } = string.Empty;

        public override object ReturnExecute(SchematicData schematic)
        {
            SpeakerObject server = new();
            server.SpawnObject(schematic);
            LogManager.Debug($"Created Speaker '{Name}'.");
            return server;
        }
    }

    public class SpeakerSetVolumeBlock : BlockBase
    {
        public float Volume { get; set; } = 100f;

        public override void Execute(object obj)
        {
            if (obj is not SpeakerObject server)
            {
                LogManager.Warn("obj is not a SpeakerObject.");
                return;
            }

            server.Volume = Volume;
        }
    }

    public class SpeakerSetIsSpatialBlock : BlockBase
    {
        public bool IsSpatial { get; set; }

        public override void Execute(object obj)
        {
            if (obj is not SpeakerObject server)
            {
                LogManager.Warn("obj is not a SpeakerObject.");
                return;
            }

            server.IsSpatial = IsSpatial;
        }
    }

    public class SpeakerSetMinDistanceBlock : BlockBase
    {
        public float MinDistance { get; set; } = 1f;

        public override void Execute(object obj)
        {
            if (obj is not SpeakerObject server)
            {
                LogManager.Warn("obj is not a SpeakerObject.");
                return;
            }

            server.MinDistance = MinDistance;
        }
    }

    public class SpeakerSetMaxDistanceBlock : BlockBase
    {
        public float MaxDistance { get; set; } = 10f;

        public override void Execute(object obj)
        {
            if (obj is not SpeakerObject server)
            {
                LogManager.Warn("obj is not a SpeakerObject.");
                return;
            }

            server.MaxDistance = MaxDistance;
        }
    }

    public class SpeakerSetLoopBlock : BlockBase
    {
        public bool Loop { get; set; }

        public override void Execute(object obj)
        {
            if (obj is not SpeakerObject server)
            {
                LogManager.Warn("obj is not a SpeakerObject.");
                return;
            }

            server.Loop = Loop;
        }
    }

    public class SpeakerSetPathBlock : BlockBase
    {
        public string Path { get; set; } = string.Empty;

        public override void Execute(object obj)
        {
            if (obj is not SpeakerObject server)
            {
                LogManager.Warn("obj is not a SpeakerObject.");
                return;
            }

            server.Path = Path;
        }
    }

    public class SpeakerPlayBlock : BlockBase
    {
        public string FilePath { get; set; } = string.Empty;

        public override void Execute(object obj)
        {
            if (obj is not SpeakerObject server)
            {
                LogManager.Warn("obj is not a SpeakerObject.");
                return;
            }

            string path = string.IsNullOrEmpty(FilePath) ? server.Path : FilePath;
            LogManager.Debug($"Playing audio on speaker. Path='{path}'.");
            server.Play(path);
        }
    }

    public class SpeakerPauseBlock : BlockBase
    {
        public override void Execute(object obj)
        {
            if (obj is not SpeakerObject server)
            {
                LogManager.Warn("obj is not a SpeakerObject.");
                return;
            }

            LogManager.Debug("Pausing speaker.");
            server.Pause();
        }
    }

    public class SpeakerUnpauseBlock : BlockBase
    {
        public override void Execute(object obj)
        {
            if (obj is not SpeakerObject server)
            {
                LogManager.Warn("obj is not a SpeakerObject.");
                return;
            }

            LogManager.Debug("Unpausing speaker.");
            server.Unpause();
        }
    }

    public class SpeakerGetPropertyBlock : BlockBase
    {
        public string Property { get; set; } = string.Empty;

        public object? Speaker { get; set; }

        public override object ReturnExecute()
        {
            return ResolveSpeaker() is SpeakerObject server ? GetProperty(server) : null!;
        }

        public override object ReturnExecute(object obj)
        {
            if (ResolveSpeaker(obj) is not SpeakerObject server)
            {
                LogManager.Warn("obj is not a SpeakerObject.");
                return null!;
            }

            return GetProperty(server);
        }

        private SpeakerObject? ResolveSpeaker(object? fallback = null)
        {
            object? target = Speaker is BlockBase block ? block.ReturnExecute() : Speaker ?? fallback;
            return target as SpeakerObject;
        }

        private object GetProperty(SpeakerObject server)
        {
            return Property switch
            {
                "Position" => server.Position,
                "Rotation" => server.Rotation,
                "Scale" => server.Scale,
                "Player" => server.Player,
                "Volume" => server.Volume,
                "IsSpatial" => server.IsSpatial,
                "MinDistance" => server.MinDistance,
                "MaxDistance" => server.MaxDistance,
                "Loop" => server.Loop,
                "Id" => server.Id,
                "Path" => server.Path,
                "IsPlaying" => server.Player != null && server.Player.Speaker.IsPlaying,
                _ => LogUnknownProperty(Property)
            };
        }

        private static object LogUnknownProperty(string property)
        {
            LogManager.Warn($"SpeakerGetPropertyBlock: Unknown property '{property}'.");
            return null!;
        }
    }
}