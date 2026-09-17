// -----------------------------------------------------------------------
// <copyright file="DoorBlocks.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Interactables.Interobjects.DoorUtils;
using ThaumielMapEditor.API.Blocks.ServerObjects;
using ThaumielMapEditor.API.Data;
using ThaumielMapEditor.API.Enums;

namespace ThaumielMapEditor.API.Helpers.BlockParser
{
    public static class DoorCache
    {
        public static Dictionary<SchematicData, DoorObject> DoorNameCache = [];
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

        public object? Door { get; set; }

        public override object ReturnExecute()
        {
            return ResolveDoor() is DoorObject door ? GetProperty(door) : null!;
        }

        public override object ReturnExecute(object obj)
        {
            if (ResolveDoor(obj) is not DoorObject door)
                return null!;

            if (string.IsNullOrEmpty(Property))
                return null!;

            return GetProperty(door);
        }

        private DoorObject? ResolveDoor(object? fallback = null)
        {
            object? target = Door is BlockBase block ? block.ReturnExecute() : Door ?? fallback;
            return target as DoorObject;
        }

        private object GetProperty(DoorObject door)
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

        public override object ReturnExecute()
        {
            if (!DoorCache.DoorNameCache.TryGetValue(Executor?.Schematic!, out var cache))
            {
                foreach (DoorObject door in Executor?.Schematic.GetServerObject<DoorObject>()!)
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

        public override object ReturnExecute()
        {
            DoorObject door = new()
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
            if (obj is not DoorObject door)
                return;

            door.Permissions = Perms;
        }
    }

    public class OpenDoorBlock : BlockBase
    {
        public override void Execute(object obj)
        {
            if (obj is not DoorObject door)
                return;

            door.IsOpen = true;
        }
    }

    public class CloseDoorBlock : BlockBase
    {
        public override void Execute(object obj)
        {
            if (obj is not DoorObject door)
                return;

            door.IsOpen = false;
        }
    }

    public class LockDoorBlock : BlockBase
    {
        public override void Execute(object obj)
        {
            if (obj is not DoorObject door)
                return;

            door.IsLocked = true;
        }
    }

    public class UnlockDoorBlock : BlockBase
    {
        public override void Execute(object obj)
        {
            if (obj is not DoorObject door)
                return;

            door.IsLocked = false;
        }
    }

    public class DoorIsOpenBlock : BlockBase
    {
        public object? Door { get; set; }

        public override object ReturnExecute()
            => ResolveDoor()?.IsOpen ?? false;

        public override object ReturnExecute(object obj)
            => ResolveDoor(obj)?.IsOpen ?? false;

        private DoorObject? ResolveDoor(object? fallback = null)
        {
            object? target = Door is BlockBase block ? block.ReturnExecute() : Door ?? fallback;
            return target as DoorObject;
        }
    }

    public class DoorIsClosedBlock : BlockBase
    {
        public object? Door { get; set; }

        public override object ReturnExecute()
            => ResolveDoor()?.IsOpen == false;

        public override object ReturnExecute(object obj)
            => ResolveDoor(obj)?.IsOpen == false;

        private DoorObject? ResolveDoor(object? fallback = null)
        {
            object? target = Door is BlockBase block ? block.ReturnExecute() : Door ?? fallback;
            return target as DoorObject;
        }
    }

    public class DoorIsOpeningBlock : BlockBase
    {
        public object? Door { get; set; }

        public override object ReturnExecute()
            => IsMoving(ResolveDoor());

        public override object ReturnExecute(object obj)
            => IsMoving(ResolveDoor(obj));

        private DoorObject? ResolveDoor(object? fallback = null)
        {
            object? target = Door is BlockBase block ? block.ReturnExecute() : Door ?? fallback;
            return target as DoorObject;
        }

        private static bool IsMoving(DoorObject? door)
        {
            if (door?.Base == null)
                return false;

            float exact = door.Base.GetExactState();
            return exact is > 0f and < 1f;
        }
    }
}