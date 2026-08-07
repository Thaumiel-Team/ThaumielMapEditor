// -----------------------------------------------------------------------
// <copyright file="SpeakerObject.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using System.IO;
using LabApi.Features.Wrappers;
using LabApiExtensions.FakeExtension;
using SecretLabNAudio.Core;
using SecretLabNAudio.Core.Extensions;
using ThaumielMapEditor.API.Data;
using ThaumielMapEditor.API.Enums;
using ThaumielMapEditor.API.Serialization;
using YamlDotNet.Serialization;

namespace ThaumielMapEditor.API.Blocks.ServerObjects
{
    public class SpeakerObject : ServerObject
    {
        /// <summary>
        /// Gets the <see cref="AudioPlayer"/> instance associated with this speaker.
        /// </summary>
#pragma warning disable CS8618
        [YamlIgnore]
        public AudioPlayer Player { get; private set; }
#pragma warning restore CS8618

        /// <summary>
        /// Gets or sets the volume of the speaker, expressed as a percentage between 0 and 100.
        /// </summary>
        /// <value>A <see cref="float"/> representing the volume percentage.</value>
        private float _volume = 100f;

        [YamlMember(Alias = "Volume")]
        public float Volume
        {
            get => _volume;
            set
            {
                _volume = value;
                if (Player != null)
                    Player.Speaker.Volume = value / 100;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the speaker uses spatial audio.
        /// When <see langword="true"/>, audio volume and panning are affected by the listener's position.
        /// </summary>
        /// <value><see langword="true"/> if spatial audio is enabled; otherwise, <see langword="false"/>.</value>
        private bool _isSpatial;

        [YamlMember(Alias = "IsSpatial")]
        public bool IsSpatial
        {
            get => _isSpatial;
            set
            {
                _isSpatial = value;
                if (Player != null)
                    Player.Speaker.IsSpatial = value;
            }
        }

        /// <summary>
        /// Gets or sets the minimum distance at which the speaker begins to attenuate for spatial audio.
        /// Within this distance, audio plays at full volume.
        /// </summary>
        /// <value>A <see cref="float"/> representing the minimum distance in world units.</value>
        private float _minDistance = 1f;

        [YamlMember(Alias = "MinDistance")]
        public float MinDistance
        {
            get => _minDistance;
            set
            {
                _minDistance = value;
                if (Player != null)
                    Player.Speaker.MinDistance = value;
            }
        }

        /// <summary>
        /// Gets or sets the maximum distance at which the speaker can be heard for spatial audio.
        /// Beyond this distance, audio is inaudible.
        /// </summary>
        /// <value>A <see cref="float"/> representing the maximum distance in world units.</value>
        private float _maxDistance = 10f;

        [YamlMember(Alias = "MaxDistance")]
        public float MaxDistance
        {
            get => _maxDistance;
            set
            {
                _maxDistance = value;
                if (Player != null)
                    Player.Speaker.MaxDistance = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the speaker loops its audio.
        /// Setting this property updates the underlying <see cref="AudioPlayer"/> loop state.
        /// </summary>
        /// <value><see langword="true"/> if the audio should loop; otherwise, <see langword="false"/>.</value>
        [YamlMember(Alias = "Loop")]
        public bool Loop { get; set; }

        /// <summary>
        /// Gets or sets the ID of the underlying <see cref="AudioPlayer"/>.
        /// </summary>
        /// <value>A <see cref="byte"/> representing the speaker's ID.</value>
        [YamlIgnore]
        public byte Id
        {
            get => Player.Id;
            set => Player.Id = value;
        }

        /// <summary>
        /// Gets or sets the file path of the audio file this speaker will play.
        /// Used as the default path when calling <see cref="Play(string)"/> with no argument.
        /// </summary>
        /// <value>A <see cref="string"/> containing the absolute path to the audio file.</value>
        [YamlMember(Alias = "Path")]
        public string Path { get; set; } = string.Empty;

        /// <summary>
        /// Sets an individual player's perceived volume for this speaker using a fake sync var,
        /// without affecting the volume heard by other players.
        /// </summary>
        /// <param name="player">The <see cref="Player"/> to set the volume for.</param>
        /// <param name="volume">The volume as a percentage (0 - 100) to send to the player.</param>
        public void SetPlayerVolume(Player player, float volume)
            => player.SendFakeSyncVar<float>(Player.Speaker.Base, 128ul, volume / 100);

        /// <summary>
        /// Begins audio playback on this speaker. If no <paramref name="filepath"/> is provided,
        /// playback uses the value of <see cref="Path"/>. If a filepath is provided, it must point
        /// to an existing file.
        /// </summary>
        /// <param name="filepath">The absolute path to the audio file to play. If <see langword="null"/> or empty, falls back to <see cref="Path"/>.</param>
        public void Play(string filepath = null!)
        {
            if (string.IsNullOrEmpty(filepath))
            {
                Player.UseFile(Path, Loop, Volume / 100);
            }
            else if (File.Exists(filepath))
            {
                Player.UseFile(filepath, Loop, Volume / 100);
            }
        }

        /// <summary>
        /// Pauses audio playback on this speaker.
        /// </summary>
        public void Pause()
            => Player.IsPaused = true;

        /// <summary>
        /// Resumes audio playback on this speaker if it was previously paused.
        /// </summary>
        public void Unpause()
            => Player.IsPaused = false;

        internal bool IsLocalFile(string path = null!)
        {
            if (string.IsNullOrEmpty(path))
            {
                if (System.IO.Path.IsPathRooted(Path))
                    return false;
            }
            else if (System.IO.Path.IsPathRooted(path))
                return false;

            return true;
        }

        /// <inheritdoc/>
        public override ObjectType ObjectType { get; set; } = ObjectType.Speaker;

        /// <inheritdoc/>
        public override void SpawnObject(SchematicData schematic, SerializableObject serializable)
        {
            SpeakerSettings settings = new()
            {
                IsSpatial = IsSpatial,
                Volume = Volume / 100,
                MaxDistance = MaxDistance,
                MinDistance = MinDistance
            };

            Player = AudioPlayer.Create(1, settings);
            if (IsLocalFile(Path))
            {
                Player.UseFile(System.IO.Path.Combine(Main.Instance.Config?.AudioPath, Path), Loop, Volume / 100);
            }
            else
                Player.UseFile(Path, Loop, Volume / 100);

            base.SpawnObject(schematic, serializable);
            SetWorldTransform(schematic);
        }

        public void SpawnObject(SchematicData schematic)
        {
            SpeakerSettings settings = new()
            {
                IsSpatial = IsSpatial,
                Volume = Volume / 100,
                MaxDistance = MaxDistance,
                MinDistance = MinDistance
            };

            Player = AudioPlayer.Create(1, settings);
            if (IsLocalFile(Path))
            {
                Player.UseFile(System.IO.Path.Combine(Main.Instance.Config?.AudioPath, Path), Loop, Volume / 100);
            }
            else
                Player.UseFile(Path, Loop, Volume / 100);

            SetWorldTransform(schematic);
        }
    }
}