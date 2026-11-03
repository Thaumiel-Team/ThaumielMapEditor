// -----------------------------------------------------------------------
// <copyright file="DoorBlocks.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU Lesser General Public License v3.0 (LGPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Interactables.Interobjects.DoorUtils;
using ThaumielMapEditor.API.Blocks.Definitions;
using ThaumielMapEditor.API.Data;
using ThaumielMapEditor.API.Enums;

namespace ThaumielMapEditor.API.Helpers.BlockParser
{
    public static class DoorCache
    {
        public static Dictionary<SchematicData, Door.Server> DoorNameCache = [];
    }

    public class DoorGetPropertyBlock : BlockBase
    {
        private enum Properties
        {
            DoorType,
            Permissions,
            IsOpen,
            IsLocked,
            RequireAllPermissions,
            Bypass2176,
            Name,
            Position,
            Rotation,
            Scale,
            ExactState,
        }

        public string Property { get; set; } = string.Empty;

        public override object? ReturnExecute()
        {
            return ResolveTarget() is Door.Server door ? GetProperty(door) : null!;
        }

        public override object? ReturnExecute(object obj)
        {
            if (ResolveTarget(obj) is not Door.Server door)
                return null!;

            if (string.IsNullOrEmpty(Property))
                return null!;

            return GetProperty(door);
        }

        private object GetProperty(Door.Server door)
        {
            return Property switch
            {
                "DoorType" => door.DoorType,
                "Permissions" => door.Permissions,
                "IsOpen" => door.IsOpen,
                "IsLocked" => door.IsLocked,
                "RequireAllPermissions" => door.RequireAllPermissions,
                "Bypass2176" => door.Bypass2176,
                "Name" => door.Name,
                "Position" => door.Position,
                "Rotation" => door.Rotation,
                "Scale" => door.Scale,
                "ExactState" => door.Base != null ? door.Base.GetExactState() : 0f,
                _ => LogUnknownProperty(Property)
            };
        }

        private static object LogUnknownProperty(string property)
        {
            LogManager.Warn($"Unknown property '{property}'.");
            return null!;
        }
    }

    public class FindDoorByNameBlock : BlockBase
    {
        public string Name { get; set; } = string.Empty;

        public override object? ReturnExecute()
        {
            if (!DoorCache.DoorNameCache.TryGetValue(Executor?.Schematic!, out var cache))
            {
                foreach (Door.Server door in Executor?.Schematic.GetServerObject<Door.Server>()!)
                {
                    if (string.Equals(Name, door.Name, StringComparison.InvariantCultureIgnoreCase))
                    {
                        DoorCache.DoorNameCache.Add(Executor?.Schematic!, door);
                        return door;
                    }
                }
            }

            return cache;
        }
    }

    public class CreateDoorBlock : BlockBase
    {
        public DoorType DoorType { get; set; }
        public DoorPermissionFlags DoorPermissionFlags { get; set; }
        public bool IsOpen { get; set; }
        public bool IsLocked { get; set; }
        public bool RequireAllPermissions { get; set; }
        public bool Bypass2176 { get; set; }

        public override object? ReturnExecute()
        {
            Door.Server door = new()
            {
                DoorType = DoorType,
                Permissions = DoorPermissionFlags,
                IsOpen = IsOpen,
                IsLocked = IsLocked,
                RequireAllPermissions = RequireAllPermissions,
                Bypass2176 = Bypass2176
            };

            door.SpawnObject(Executor!.Schematic);
            return door;
        }
    }

    public class SetDoorPermsBlock : BlockBase
    {
        public DoorPermissionFlags Perms { get; set; }

        public override void Execute(object obj)
        {
            if (obj is not Door.Server door)
                return;

            door.Permissions = Perms;
        }
    }

    public class OpenDoorBlock : BlockBase
    {
        public override void Execute(object obj)
        {
            if (obj is not Door.Server door)
                return;

            door.IsOpen = true;
        }
    }

    public class CloseDoorBlock : BlockBase
    {
        public override void Execute(object obj)
        {
            if (obj is not Door.Server door)
                return;

            door.IsOpen = false;
        }
    }

    public class LockDoorBlock : BlockBase
    {
        public override void Execute(object obj)
        {
            if (obj is not Door.Server door)
                return;

            door.IsLocked = true;
        }
    }

    public class UnlockDoorBlock : BlockBase
    {
        public override void Execute(object obj)
        {
            if (obj is not Door.Server door)
                return;

            door.IsLocked = false;
        }
    }

    public class DoorIsOpenBlock : BlockBase
    {
        public override object? ReturnExecute()
            => ResolveTarget() as Door.Server is { } door && door.IsOpen;

        public override object? ReturnExecute(object obj)
            => ResolveTarget(obj) as Door.Server is { } door && door.IsOpen;
    }

    public class DoorIsClosedBlock : BlockBase
    {
        public override object? ReturnExecute()
            => ResolveTarget() as Door.Server is { IsOpen: false };

        public override object? ReturnExecute(object obj)
            => ResolveTarget(obj) as Door.Server is { IsOpen: false };
    }

    public class DoorIsOpeningBlock : BlockBase
    {
        public override object? ReturnExecute()
            => IsMoving(ResolveTarget() as Door.Server);

        public override object? ReturnExecute(object obj)
            => IsMoving(ResolveTarget(obj) as Door.Server);

        private static bool IsMoving(Door.Server? door)
        {
            if (door?.Base == null)
                return false;

            float exact = door.Base.GetExactState();
            return exact is > 0f and < 1f;
        }
    }
}