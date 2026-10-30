// -----------------------------------------------------------------------
// <copyright file="DoorRuntime.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using ThaumielMapEditor.API.Binary.Attributes;
using ThaumielMapEditor.API.Blocks;
using ThaumielMapEditor.API.Blocks.ServerObjects;
using ThaumielMapEditor.API.Data;
using ThaumielMapEditor.API.Enums;
using ThaumielMapEditor.API.Helpers;
using ThaumielMapEditor.API.Serialization;
using YamlDotNet.Serialization;

namespace ThaumielMapEditor.API.Components.Tools
{
    [GitBookPage("Components/Tools/DoorRuntime")]
    public class DoorRuntime : ToolBase
    {
        public override ToolType Type => ToolType.DoorRuntime;

        [YamlMember(Alias = "OnDoorDestroyed")]
        [BinaryProperty(Name = "OnDoorDestroyed")]
        public BlockyPayload? OnDoorDestroyed { get; set; }

        [YamlMember(Alias = "OnDoorOpened")]
        [BinaryProperty(Name = "OnDoorOpened")]
        public BlockyPayload? OnDoorOpened { get; set; }

        [YamlMember(Alias = "OnDoorClosed")]
        [BinaryProperty(Name = "OnDoorClosed")]
        public BlockyPayload? OnDoorClosed { get; set; }

        [YamlMember(Alias = "OnDoorLocked")]
        [BinaryProperty(Name = "OnDoorLocked")]
        public BlockyPayload? OnDoorLocked { get; set; }

        [YamlMember(Alias = "OnDoorDamaged")]
        [BinaryProperty(Name = "OnDoorDamaged")]
        public BlockyPayload? OnDoorDamaged { get; set; }

        [YamlMember(Alias = "OnDoorUnlocked")]
        [BinaryProperty(Name = "OnDoorUnlocked")]
        public BlockyPayload? OnDoorUnlocked { get; set; }
        
        public void ExecuteEvent(EventType eventType)
        {
            if (Object is not DoorObject door || Schematic?.Executor == null)
                return;

            BlockyPayload? payload = eventType switch
            {
                EventType.OnDoorDestroyed => OnDoorDestroyed,
                EventType.OnDoorOpened => OnDoorOpened,
                EventType.OnDoorClosed => OnDoorClosed,
                EventType.OnDoorLocked => OnDoorLocked,
                EventType.OnDoorDamaged => OnDoorDamaged,
                EventType.OnDoorUnlocked => OnDoorUnlocked,
                _ => null
            };

            if (payload == null || string.IsNullOrEmpty(payload.Code))
                return;

            try
            {
                Schematic.Executor.Execute(ArgumentsParser.Load(payload), null!, eventType, door);
            }
            catch (Exception ex)
            {
                LogManager.Error($"DoorRuntime failed to execute {eventType} on '{door.Name}': {ex.Message}");
            }
        }
    }
}
