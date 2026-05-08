// -----------------------------------------------------------------------
// <copyright file="DoorLink.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using MEC;
using ThaumielMapEditor.API.Blocks;
using ThaumielMapEditor.API.Blocks.ServerObjects;
using ThaumielMapEditor.API.Data;
using ThaumielMapEditor.API.Enums;
using ThaumielMapEditor.API.Helpers;

namespace ThaumielMapEditor.API.Components.Tools
{
    public class DoorLink : ToolBase
    {
        private static readonly Dictionary<string, List<DoorLink>> Groups = [];
        
        private static readonly List<DoorLink> ActiveLinks = [];

        private DoorObject? door;
        private bool lastKnownState;

        public string GroupId { get; private set; } = string.Empty;

        public override ToolType Type => ToolType.Doorlink;

        public override void Init(ServerObject obj, SchematicData schem, Dictionary<string, object> properties)
        {
            base.Init(obj, schem, properties);

            if (obj is not DoorObject doorObj)
            {
                LogManager.Warn($"DoorLink tool applied to non door object '{obj.Name}' in '{schem.FileName}'. Skipping.");
                return;
            }

            if (!properties.TryGetValue("GroupId", out object? groupIdRaw) || groupIdRaw is not string groupId || string.IsNullOrEmpty(groupId))
            {
                LogManager.Warn($"DoorLink on '{obj.Name}' in '{schem.FileName}' is missing a valid GroupId property. Skipping.");
                return;
            }

            this.door = doorObj;
            GroupId = groupId;
            lastKnownState = doorObj.IsOpen;

            if (!Groups.TryGetValue(GroupId, out List<DoorLink>? group))
            {
                group = [];
                Groups[GroupId] = group;
            }

            group.Add(this);
            ActiveLinks.Add(this);
            LogManager.Debug($"DoorLink registered door '{obj.Name}' to group '{GroupId}' in '{schem.FileName}'.");
            doorObj.Base?.OnStateChanged += PropagateToGroup;
        }

        private void OnDestroy()
        {
            door?.Base?.OnStateChanged -= PropagateToGroup;
        }

        private void PropagateToGroup()
        {
            if (!Groups.TryGetValue(GroupId, out List<DoorLink>? group))
                return;

            for (int i = group.Count - 1; i >= 0; i--)
            {
                DoorLink link = group[i];

                if (link == this)
                    continue;

                if (link.door == null)
                {
                    link.Unregister();
                    continue;
                }

                try
                {
                    link.door.IsOpen = !lastKnownState;
                    link.lastKnownState = link.door.IsOpen;
                }
                catch (Exception ex)
                {
                    LogManager.Warn($"DoorLink error while syncing door '{link.door.Name}': {ex.Message}");
                }
            }
        }

        public void Unregister()
        {
            ActiveLinks.Remove(this);
            if (Groups.TryGetValue(GroupId, out List<DoorLink>? group))
            {
                if (group.Remove(this))
                {
                    if (group.Count == 0)
                    {
                        Groups.Remove(GroupId);
                        LogManager.Debug($"DoorLink group '{GroupId}' removed as it is now empty.");
                    }

                    LogManager.Debug($"DoorLink unregistered door '{door?.Name ?? "Unknown"}' from group '{GroupId}'.");
                }
            }
        }
    }
}