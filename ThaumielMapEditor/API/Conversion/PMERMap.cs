// -----------------------------------------------------------------------
// <copyright file="PMERMap.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using YamlDotNet.Serialization;

namespace ThaumielMapEditor.API.Conversion
{
    public class PMERMapRoot
    {
        [YamlIgnore]
        public string Name = string.Empty;

        public Dictionary<string, PMERMapPrimitive> Primitives { get; set; } = [];

        public Dictionary<string, PMERMapLight> Lights { get; set; } = [];

        public Dictionary<string, PMERMapDoor> Doors { get; set; } = [];

        public Dictionary<string, PMERMapWorkstation> Workstations { get; set; } = [];

        public Dictionary<string, PMERMapItemSpawnpoint> ItemSpawnpoints { get; set; } = [];

        public Dictionary<string, PMERMapPlayerSpawnpoint> PlayerSpawnpoints { get; set; } = [];

        public Dictionary<string, PMERMapCapybara> Capybaras { get; set; } = [];

        public Dictionary<string, PMERMapText> Texts { get; set; } = [];

        public Dictionary<string, PMERMapInteractable> Interactables { get; set; } = [];

        public Dictionary<string, PMERMapScp079Camera> Scp079Cameras { get; set; } = [];

        public Dictionary<string, PMERMapShootingTarget> ShootingTargets { get; set; } = [];

        public Dictionary<string, PMERMapSchematicRef> Schematics { get; set; } = [];

        public Dictionary<string, PMERMapTeleport> Teleports { get; set; } = [];

        public Dictionary<string, PMERMapLocker> Lockers { get; set; } = [];
    }

    public class PMERMapObject
    {
        public virtual Vector3 Position { get; set; } = Vector3.zero;

        public virtual Vector3 Rotation { get; set; } = Vector3.zero;

        public virtual Vector3 Scale { get; set; } = Vector3.one;

        public virtual string Room { get; set; } = "Unknown";

        public virtual int Index { get; set; } = -1;
    }

    public class PMERMapPrimitive : PMERMapObject
    {
        public string PrimitiveType { get; set; } = "Cube";

        public string Color { get; set; } = "#FF0000";

        public string PrimitiveFlags { get; set; } = "Visible, Collidable";
    }

    public class PMERMapLight : PMERMapObject
    {
        public string Color { get; set; } = "white";

        public float Intensity { get; set; } = 1f;

        public float Range { get; set; } = 1f;

        public string Shadows { get; set; } = "Hard";

        public float Strength { get; set; }

        public string LightType { get; set; } = "Point";

        public string Shape { get; set; } = "Cone";

        public float SpotAngle { get; set; } = 30f;

        public float InnerSpotAngle { get; set; }
    }

    public class PMERMapDoor : PMERMapObject
    {
        public string DoorType { get; set; } = "Lcz";

        public bool IsOpen { get; set; }

        public bool IsLocked { get; set; }

        public string RequiredPermissions { get; set; } = "None";

        public bool RequireAll { get; set; } = true;
    }

    public class PMERMapWorkstation : PMERMapObject
    {
        public bool IsInteractable { get; set; } = true;
    }

    public class PMERMapItemSpawnpoint : PMERMapObject
    {
        public string ItemType { get; set; } = "Lantern";

        public float Weight { get; set; } = -1f;

        public string AttachmentsCode { get; set; } = "-1";

        public uint NumberOfItems { get; set; } = 1;

        public int NumberOfUses { get; set; } = 1;

        public bool UseGravity { get; set; } = true;

        public bool CanBePickedUp { get; set; } = true;
    }

    public class PMERMapPlayerSpawnpoint : PMERMapObject
    {
        public List<string> Roles { get; set; } = [];
    }

    public class PMERMapCapybara : PMERMapObject
    {
    }

    public class PMERMapText : PMERMapObject
    {
        public string Text { get; set; } = "Custom Text";

        public Vector3 DisplaySize { get; set; } = new(10f, 5f, 0f);
    }

    public class PMERMapInteractable : PMERMapObject
    {
        public string ColliderShape { get; set; } = "Box";

        public float InteractionDuration { get; set; }

        public bool IsLocked { get; set; }
    }

    public class PMERMapScp079Camera : PMERMapObject
    {
        public string CameraType { get; set; } = "Lcz";

        public string Label { get; set; } = "CustomCamera";
    }

    public class PMERMapShootingTarget : PMERMapObject
    {
        public string TargetType { get; set; } = "ClassD";
    }

    public class PMERMapSchematicRef : PMERMapObject
    {
        public string SchematicName { get; set; } = "None";
    }

    public class PMERMapTeleport : PMERMapObject
    {
        public List<string> Targets { get; set; } = [];

        public float Cooldown { get; set; } = 5f;
    }

    public class PMERMapLocker : PMERMapObject
    {
        public string LockerType { get; set; } = "PedestalScp500";

        public List<PMERMapLockerLoot> Loot { get; set; } = [];

        public List<PMERMapLockerChamber> Chambers { get; set; } = [];
    }

    public class PMERMapLockerLoot
    {
        public string TargetItem { get; set; } = string.Empty;

        public int RemainingUses { get; set; }

        public int MaxPerChamber { get; set; }

        public int ProbabilityPoints { get; set; }

        public int MinPerChamber { get; set; } = 1;
    }

    public class PMERMapLockerChamber
    {
        public List<string> AcceptableItems { get; set; } = [];

        public bool IsOpen { get; set; }

        public string RequiredPermissions { get; set; } = "None";
    }
}
