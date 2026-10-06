// -----------------------------------------------------------------------
// <copyright file="LockerSettings.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using System.Linq;
using Interactables.Interobjects.DoorUtils;
using LabApi.Features.Wrappers;
using SecretAPI.Features.UserSettings;
using ThaumielMapEditor.API.Blocks.ServerObjects;
using ThaumielMapEditor.API.Blocks.ServerObjects.Lockers;
using TmeChamber = ThaumielMapEditor.API.Blocks.ServerObjects.Lockers.LockerChamber;
using ThaumielMapEditor.API.Enums;
using static UserSettings.ServerSpecific.SSDropdownSetting;

namespace ThaumielMapEditor.ToolGun.Utilities.Settings.Objects
{
    public static class LockerChamberHelper
    {
        public static bool TryGetLocker(Player player, out LockerObject? locker) =>
            ObjectSettingsHelper.TryGetServer(player, out locker) && locker != null;

        public static int GetSelectedChamberIndex(Player player)
        {
            LockerChamberSetting? selector = CustomSetting.GetPlayerSetting<LockerChamberSetting>(LockerChamberSetting.SettingId, player);
            int index = selector != null ? selector.ValidatedSelectedIndex : 0;
            if (index < 0)
                return 0;

            if (TryGetLocker(player, out LockerObject? locker) && locker != null)
            {
                int count = locker.ChamberCount;
                if (count <= 0)
                    return 0;

                if (index >= count)
                    return count - 1;
            }

            return index;
        }

        public static bool CanViewChamber(Player player) =>
            TryGetLocker(player, out LockerObject? locker) && locker != null && locker.ChamberCount > 0;
    }

    public class LockerInfo : CustomTextAreaSetting
    {
        public static int SettingId => Main.Instance.Config.LockerInfoId;

        public LockerInfo() : base(SettingId, string.Empty)
        {
        }

        public override CustomHeader Header => ToolGunHeaders.Locker;

        protected override bool CanView(Player player) => ObjectSettingsHelper.CanViewServer<LockerObject>(player);

        protected override CustomSetting CreateDuplicate() => new LockerInfo();

        protected override void PersonalizeSetting()
        {
            if (KnownOwner != null && ObjectSettingsHelper.TryGetServer(KnownOwner, out LockerObject? locker) && locker != null)
            {
                int selected = LockerChamberHelper.GetSelectedChamberIndex(KnownOwner);
                TmeChamber? chamber = locker.GetChamber(selected);
                ChamberData? data = chamber?.Data.FirstOrDefault();
                string chamberSummary = chamber == null || data == null ? $"Chamber {selected}: empty" : $"Chamber {selected}: {data.ItemType} x{data.AmountToSpawn} ({data.SpawnPercent}%, {chamber.Permissions})";

                Content = $"Locker Settings ({locker.Type}, Chambers: {locker.Chambers.Count})\n{chamberSummary}";
            }
        }

        protected override void HandleSettingUpdate()
        {
        }
    }

    public class LockerTypeSetting : EnumDropdownSetting<LockerType>
    {
        public static int SettingId => Main.Instance.Config.LockerTypeSettingId;

        public LockerTypeSetting() : base(SettingId, "Locker Type")
        {
        }

        public override CustomHeader Header => ToolGunHeaders.Locker;

        protected override bool CanView(Player player) => ObjectSettingsHelper.CanViewServer<LockerObject>(player);

        protected override CustomSetting CreateDuplicate() => new LockerTypeSetting();

        protected override void PersonalizeSetting()
        {
            if (KnownOwner != null && ObjectSettingsHelper.TryGetServer(KnownOwner, out LockerObject? locker) && locker != null)
                Base.DefaultOptionIndex = IndexOf(locker.Type);
        }

        protected override void HandleSettingUpdate()
        {
            if (LastUpdateType == SettingResponseType.Initial)
                return;

            if (KnownOwner != null && ObjectSettingsHelper.TryGetServer(KnownOwner, out LockerObject? locker) && locker != null)
            {
                LockerType value = Values[ValidatedSelectedIndex];
                if (value == LockerType.None)
                    return;

                if (locker.Type == value)
                    return;

                locker.Type = value;
                ToolGunMenu.Refresh(KnownOwner);
            }
        }
    }

    public class LockerChamberSetting : CustomDropdownSetting
    {
        public static int SettingId => Main.Instance.Config.LockerChamberSettingId;

        public LockerChamberSetting() : base(SettingId, "Chamber", ["0"], 0, DropdownEntryType.HybridLoop)
        {
        }

        public override CustomHeader Header => ToolGunHeaders.Locker;

        protected override bool CanView(Player player) => ObjectSettingsHelper.CanViewServer<LockerObject>(player);

        protected override CustomSetting CreateDuplicate() => new LockerChamberSetting();

        protected override void PersonalizeSetting()
        {
            if (KnownOwner == null)
                return;

            if (!ObjectSettingsHelper.TryGetServer(KnownOwner, out LockerObject? locker) || locker == null)
                return;

            int count = locker.ChamberCount;
            if (count <= 0)
            {
                if (Options.Length != 1 || Options[0] != "None")
                    Options = ["None"];

                Base.DefaultOptionIndex = 0;
                return;
            }

            string[] options = new string[count];
            for (int i = 0; i < count; i++)
                options[i] = i.ToString();

            if (!options.SequenceEqual(Options))
                Options = options;

            int current = ValidatedSelectedIndex;
            if (current < 0)
                current = 0;

            if (current >= count)
                current = count - 1;

            Base.DefaultOptionIndex = current;
        }

        protected override void HandleSettingUpdate()
        {
            if (LastUpdateType == SettingResponseType.Initial || !HasValueChanged)
                return;

            if (KnownOwner == null)
                return;

            ToolGunMenu.Refresh(KnownOwner);
        }
    }

    public class LockerChamberPermissionsSetting : EnumDropdownSetting<DoorPermissionFlags>
    {
        public static int SettingId => Main.Instance.Config.LockerChamberPermissionsSettingId;

        public LockerChamberPermissionsSetting() : base(SettingId, "Chamber Permissions")
        {
        }

        public override CustomHeader Header => ToolGunHeaders.Locker;

        protected override bool CanView(Player player) => LockerChamberHelper.CanViewChamber(player);

        protected override CustomSetting CreateDuplicate() => new LockerChamberPermissionsSetting();

        protected override void PersonalizeSetting()
        {
            if (KnownOwner == null)
                return;

            if (!LockerChamberHelper.TryGetLocker(KnownOwner, out LockerObject? locker) || locker == null)
                return;

            int selected = LockerChamberHelper.GetSelectedChamberIndex(KnownOwner);
            TmeChamber? chamber = locker.GetChamber(selected);
            if (chamber != null)
            {
                Base.DefaultOptionIndex = IndexOf(chamber.Permissions);
                return;
            }

            if (locker.Base?.Chambers != null && selected >= 0 && selected < locker.Base.Chambers.Length)
                Base.DefaultOptionIndex = IndexOf(locker.Base.Chambers[selected].RequiredPermissions);
        }

        protected override void HandleSettingUpdate()
        {
            if (LastUpdateType == SettingResponseType.Initial)
                return;

            if (KnownOwner == null)
                return;

            if (!LockerChamberHelper.TryGetLocker(KnownOwner, out LockerObject? locker) || locker == null)
                return;

            int selected = LockerChamberHelper.GetSelectedChamberIndex(KnownOwner);
            DoorPermissionFlags value = Values[ValidatedSelectedIndex];

            TmeChamber chamber = locker.GetOrCreateChamber(selected);
            chamber.Permissions = value;

            if (locker.Base?.Chambers != null && selected >= 0 && selected < locker.Base.Chambers.Length)
                locker.Base.Chambers[selected].RequiredPermissions = value;
        }
    }

    public class LockerChamberItemSetting : EnumDropdownSetting<ItemType>
    {
        public static int SettingId => Main.Instance.Config.LockerChamberItemSettingId;

        public LockerChamberItemSetting() : base(SettingId, "Chamber Item")
        {
        }

        public override CustomHeader Header => ToolGunHeaders.Locker;

        protected override bool CanView(Player player) => LockerChamberHelper.CanViewChamber(player);

        protected override CustomSetting CreateDuplicate() => new LockerChamberItemSetting();

        protected override void PersonalizeSetting()
        {
            if (KnownOwner == null)
                return;

            if (!LockerChamberHelper.TryGetLocker(KnownOwner, out LockerObject? locker) || locker == null)
                return;

            int selected = LockerChamberHelper.GetSelectedChamberIndex(KnownOwner);
            ItemType current = locker.GetChamber(selected)?.Data.FirstOrDefault()?.ItemType ?? Values[0];
            Base.DefaultOptionIndex = IndexOf(current);
        }

        protected override void HandleSettingUpdate()
        {
            if (LastUpdateType == SettingResponseType.Initial)
                return;

            if (KnownOwner == null)
                return;

            if (!LockerChamberHelper.TryGetLocker(KnownOwner, out LockerObject? locker) || locker == null)
                return;

            int selected = LockerChamberHelper.GetSelectedChamberIndex(KnownOwner);
            ItemType value = Values[ValidatedSelectedIndex];

            TmeChamber chamber = locker.GetOrCreateChamber(selected);
            ChamberData entry = chamber.Data.FirstOrDefault() ?? EnsureEntry(chamber);
            entry.ItemType = value;
        }

        private static ChamberData EnsureEntry(TmeChamber chamber)
        {
            ChamberData created = new() { ItemType = Values[0], SpawnPercent = 100f, AmountToSpawn = 1 };
            chamber.Data.Add(created);
            return created;
        }
    }

    public class LockerChamberChanceSetting : CustomSliderSetting
    {
        public static int SettingId => Main.Instance.Config.LockerChamberChanceSettingId;

        public LockerChamberChanceSetting() : base(SettingId, "Chamber Chance", 0f, 100f)
        {
        }

        public override CustomHeader Header => ToolGunHeaders.Locker;

        protected override bool CanView(Player player) => LockerChamberHelper.CanViewChamber(player);

        protected override CustomSetting CreateDuplicate() => new LockerChamberChanceSetting();

        protected override void PersonalizeSetting()
        {
            if (KnownOwner == null)
                return;

            if (!LockerChamberHelper.TryGetLocker(KnownOwner, out LockerObject? locker) || locker == null)
                return;

            int selected = LockerChamberHelper.GetSelectedChamberIndex(KnownOwner);
            float chance = locker.GetChamber(selected)?.Data.FirstOrDefault()?.SpawnPercent ?? 100f;
            DefaultValue = ToolGunMenu.Clamp(chance, 0f, 100f);
        }

        protected override void HandleSettingUpdate()
        {
            if (LastUpdateType == SettingResponseType.Initial)
                return;

            if (KnownOwner == null)
                return;

            if (!LockerChamberHelper.TryGetLocker(KnownOwner, out LockerObject? locker) || locker == null)
                return;

            int selected = LockerChamberHelper.GetSelectedChamberIndex(KnownOwner);
            TmeChamber chamber = locker.GetOrCreateChamber(selected);
            ChamberData entry = chamber.Data.FirstOrDefault() ?? EnsureEntry(chamber);
            entry.SpawnPercent = SelectedValueFloat;
        }

        private static ChamberData EnsureEntry(TmeChamber chamber)
        {
            ChamberData created = new() { SpawnPercent = 100f, AmountToSpawn = 1 };
            chamber.Data.Add(created);
            return created;
        }
    }

    public class LockerChamberAmountSetting : CustomSliderSetting
    {
        public static int SettingId => Main.Instance.Config.LockerChamberAmountSettingId;

        public LockerChamberAmountSetting() : base(SettingId, "Chamber Amount", 1f, 10f, 1f, true)
        {
        }

        public override CustomHeader Header => ToolGunHeaders.Locker;

        protected override bool CanView(Player player) => LockerChamberHelper.CanViewChamber(player);

        protected override CustomSetting CreateDuplicate() => new LockerChamberAmountSetting();

        protected override void PersonalizeSetting()
        {
            if (KnownOwner == null)
                return;

            if (!LockerChamberHelper.TryGetLocker(KnownOwner, out LockerObject? locker) || locker == null)
                return;

            int selected = LockerChamberHelper.GetSelectedChamberIndex(KnownOwner);
            int amount = locker.GetChamber(selected)?.Data.FirstOrDefault()?.AmountToSpawn ?? 1;
            DefaultValue = ToolGunMenu.Clamp(amount, 1f, 10f);
        }

        protected override void HandleSettingUpdate()
        {
            if (LastUpdateType == SettingResponseType.Initial)
                return;

            if (KnownOwner == null)
                return;

            if (!LockerChamberHelper.TryGetLocker(KnownOwner, out LockerObject? locker) || locker == null)
                return;

            int selected = LockerChamberHelper.GetSelectedChamberIndex(KnownOwner);
            TmeChamber chamber = locker.GetOrCreateChamber(selected);
            ChamberData entry = chamber.Data.FirstOrDefault() ?? EnsureEntry(chamber);
            entry.AmountToSpawn = SelectedValueInt < 1 ? 1 : SelectedValueInt;
        }

        private static ChamberData EnsureEntry(TmeChamber chamber)
        {
            ChamberData created = new() { SpawnPercent = 100f, AmountToSpawn = 1 };
            chamber.Data.Add(created);
            return created;
        }
    }

    public class LockerChamberApplySetting : CustomButtonSetting
    {
        public static int SettingId => Main.Instance.Config.LockerChamberApplySettingId;

        public LockerChamberApplySetting() : base(SettingId, "Apply Chamber", "Apply")
        {
        }

        public override CustomHeader Header => ToolGunHeaders.Locker;

        protected override bool CanView(Player player) => LockerChamberHelper.CanViewChamber(player);

        protected override CustomSetting CreateDuplicate() => new LockerChamberApplySetting();

        protected override void HandleSettingUpdate()
        {
            if (KnownOwner == null)
                return;

            if (!LockerChamberHelper.TryGetLocker(KnownOwner, out LockerObject? locker) || locker == null)
                return;

            locker.RefreshChambers();
            ToolGunMenu.Refresh(KnownOwner);
        }
    }

    public class LockerChamberClearSetting : CustomButtonSetting
    {
        public static int SettingId => Main.Instance.Config.LockerChamberClearSettingId;

        public LockerChamberClearSetting() : base(SettingId, "Clear Chamber", "Clear")
        {
        }

        public override CustomHeader Header => ToolGunHeaders.Locker;

        protected override bool CanView(Player player) => LockerChamberHelper.CanViewChamber(player);

        protected override CustomSetting CreateDuplicate() => new LockerChamberClearSetting();

        protected override void HandleSettingUpdate()
        {
            if (KnownOwner == null)
                return;

            if (!LockerChamberHelper.TryGetLocker(KnownOwner, out LockerObject? locker) || locker == null)
                return;

            locker.RemoveChamber(LockerChamberHelper.GetSelectedChamberIndex(KnownOwner));
            locker.RefreshChambers();
            ToolGunMenu.Refresh(KnownOwner);
        }
    }

    public class LockerPopulateSetting : CustomButtonSetting
    {
        public static int SettingId => Main.Instance.Config.LockerPopulateSettingId;

        public LockerPopulateSetting() : base(SettingId, "Populate Chambers", "Populate")
        {
        }

        public override CustomHeader Header => ToolGunHeaders.Locker;

        protected override bool CanView(Player player) => ObjectSettingsHelper.CanViewServer<LockerObject>(player);

        protected override CustomSetting CreateDuplicate() => new LockerPopulateSetting();

        protected override void HandleSettingUpdate()
        {
            if (KnownOwner != null && ObjectSettingsHelper.TryGetServer(KnownOwner, out LockerObject? locker) && locker != null)
            {
                locker.ClearChambers();
                locker.PopulateChambers();
            }
        }
    }

    public class LockerClearSetting : CustomButtonSetting
    {
        public static int SettingId => Main.Instance.Config.LockerClearSettingId;

        public LockerClearSetting() : base(SettingId, "Clear Chambers", "Clear")
        {
        }

        public override CustomHeader Header => ToolGunHeaders.Locker;

        protected override bool CanView(Player player) => ObjectSettingsHelper.CanViewServer<LockerObject>(player);

        protected override CustomSetting CreateDuplicate() => new LockerClearSetting();

        protected override void HandleSettingUpdate()
        {
            if (KnownOwner != null && ObjectSettingsHelper.TryGetServer(KnownOwner, out LockerObject? locker) && locker != null)
                locker.ClearChambers();
        }
    }
}
