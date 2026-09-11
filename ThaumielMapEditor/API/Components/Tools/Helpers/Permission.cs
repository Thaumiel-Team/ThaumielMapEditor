// -----------------------------------------------------------------------
// <copyright file="Permission.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using Interactables.Interobjects.DoorUtils;
using PlayerRoles;

namespace ThaumielMapEditor.API.Components.Tools.Helpers
{
    public class Permission
    {
        public List<RoleTypeId> AllowedRoles { get; set; } = [];

        public string LabAPIPermission { get; set; } = string.Empty;

        public DoorPermissionFlags KeycardPermissions { get; set; }
    }
}