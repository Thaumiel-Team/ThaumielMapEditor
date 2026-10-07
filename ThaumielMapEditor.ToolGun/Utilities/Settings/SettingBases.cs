// -----------------------------------------------------------------------
// <copyright file="SettingBases.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using System;
using SecretAPI.Features.UserSettings;
using static UserSettings.ServerSpecific.SSDropdownSetting;

namespace ThaumielMapEditor.ToolGun.Utilities.Settings
{
    public abstract class EnumDropdownSetting<T> : CustomDropdownSetting where T : struct, Enum
    {
        protected static readonly string[] Names = Enum.GetNames(typeof(T));

        protected static readonly T[] Values = (T[])Enum.GetValues(typeof(T));

        protected EnumDropdownSetting(int id, string label) : base(id, label, Names, 0, DropdownEntryType.HybridLoop)
        {
        }

        protected static int IndexOf(T value) => Math.Max(Array.IndexOf(Values, value), 0);
    }
}
