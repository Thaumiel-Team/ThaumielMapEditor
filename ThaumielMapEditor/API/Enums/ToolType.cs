// -----------------------------------------------------------------------
// <copyright file="ToolType.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

namespace ThaumielMapEditor.API.Enums
{
    [GitBookPage("Enums/ToolType")]
    public enum ToolType
    {
        Custom = 0, // Todo: setup custom.
        Health = 1,
        Physics = 2,
        Doorlink = 3,
        ColliderTrigger = 4,
        InteractableTrigger = 5,
        BlockyRuntime = 6,
    }
}