// -----------------------------------------------------------------------
// <copyright file="ToolGunUI.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using System.Text;
using LabApi.Features.Wrappers;
using ThaumielMapEditor.API.Blocks;
using ThaumielMapEditor.API.Blocks.ClientSide;
using ThaumielMapEditor.API.Enums;
using ThaumielMapEditor.ToolGun.Utilities.Settings;
using UnityEngine;
using static ThaumielMapEditor.API.Extensions.RoomExtensions;

namespace ThaumielMapEditor.ToolGun.Utilities
{
    public class ToolGunUI : MonoBehaviour
    {
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
        public Player Player { get; set; }
        public ToolGun ToolGun { get; set; }
        public ToolGunSettings Settings { get; set; }
        private StringBuilder Builder { get; set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
        private bool UpdateNeeded { get; set; } = true;

        private Vector3 PrevPos { get; set; }
        private Quaternion PrevRot { get; set; }
        private Room? PrevRoom { get; set; }
        private ObjectType PrevType { get; set; }
        private ServerObject? PrevSelectedServer { get; set; }
        private ClientObject? PrevSelectedClient { get; set; }

        private const float HintInterval = 0.5f;
        private const float HintDuration = 0.6f;
        private const int MaxHintNameLength = 64;
        private float HintTimer { get; set; }

        public void Init(Player player, ToolGun toolgun, ToolGunSettings settings)
        {
            Player = player;
            ToolGun = toolgun;
            Settings = settings;
            PrevType = settings.SelectedObjectType;
            PrevSelectedServer = settings.SelectedServer;
            PrevSelectedClient = settings.SelectedClient;
            Builder = new();
            UpdateNeeded = true;
            HintTimer = 0f;
        }

        private void Update()
        {
            if (Player.CurrentItem == null)
                return;

            if (!Player.IsAlive)
                return;

            if (!UpdateNeeded)
                UpdateNeeded = Player.Position != PrevPos || Player.Rotation != PrevRot || PrevRoom == null || (Player.Room != null && Player.Room != PrevRoom) || Settings.SelectedObjectType != PrevType || Settings.SelectedServer != PrevSelectedServer || Settings.SelectedClient != PrevSelectedClient;

            bool rebuilt = false;

            if (UpdateNeeded)
            {
                UpdateNeeded = false;
                PrevPos = Player.Position;
                PrevRot = Player.Rotation;
                PrevRoom = Player.Room;
                PrevType = Settings.SelectedObjectType;
                PrevSelectedServer = Settings.SelectedServer;
                PrevSelectedClient = Settings.SelectedClient;
                Builder.Clear();

                if (PrevRoom != null)
                    Builder.AppendLine($"<pos=-10em>Room Local Pos - <color=yellow>{PrevRoom.LocalPosition(PrevPos)}</color>");

                AppendSelection();
                Builder.AppendLine($"<pos=-10em>Spawning Type - <color=yellow>{Settings.SelectedObjectType}{Settings.SpawnVariantLabel()}</color>");
                rebuilt = true;
                HintTimer = HintInterval;
            }

            if (!rebuilt)
            {
                HintTimer -= Time.deltaTime;

                if (HintTimer > 0f)
                    return;

                HintTimer = HintInterval;
            }

            string text = Builder.ToString();

            if (string.IsNullOrEmpty(text))
                return;

            
            Player.SendHint(text, HintDuration);
        }

        private static string FormatFloat(float value) =>
            float.IsNaN(value) || float.IsInfinity(value) ? "---" : value.ToString("F1");

        private void AppendSelection()
        {
            if (Settings.SelectedServer is { } server)
            {
                string name = string.IsNullOrEmpty(server.Name) ? $"net {server.NetId}" : ToolGunMenu.SanitizeDisplayText(server.Name, MaxHintNameLength);

                if (string.IsNullOrEmpty(name))
                    name = $"net {server.NetId}";

                Builder.AppendLine($"<pos=-10em>Selected [Server] <color=yellow>{server.ObjectType}</color> <color=yellow>{name}</color>");
                AppendObjectPosition(server.Position);
            }
            else if (Settings.SelectedClient is { } client)
            {
                string name = client switch
                {
                    PrimitiveObject prim when !string.IsNullOrEmpty(prim.Name) => ToolGunMenu.SanitizeDisplayText(prim.Name, MaxHintNameLength),
                    CapybaraObject capy when !string.IsNullOrEmpty(capy.Name) => ToolGunMenu.SanitizeDisplayText(capy.Name, MaxHintNameLength),
                    _ => $"id {client.ObjectId}",
                };

                if (string.IsNullOrEmpty(name))
                    name = $"id {client.ObjectId}";

                Builder.AppendLine($"<pos=-10em>Selected [Client] <color=yellow>{client.ObjectType}</color> <color=yellow>{name}</color>");
                AppendObjectPosition(client.Position);
            }
            else
                Builder.AppendLine("<pos=-10em>Selected - <color=grey>none</color>");
        }

        private void AppendObjectPosition(Vector3 position)
        {
            float distance;

            try
            {
                distance = Vector3.Distance(Player.Position, position);
            }
            catch
            {
                distance = float.NaN;
            }

            Builder.AppendLine($"<pos=-10em>Pos - <color=yellow>{FormatFloat(position.x)}, {FormatFloat(position.y)}, {FormatFloat(position.z)}</color> (<color=yellow>{FormatFloat(distance)}m</color>)");
        }
    }
}