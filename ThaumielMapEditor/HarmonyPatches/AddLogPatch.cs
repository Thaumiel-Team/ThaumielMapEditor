// -----------------------------------------------------------------------
// <copyright file="AddLogPatch.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using HarmonyLib;
using System;
using System.Text.RegularExpressions;
using ThaumielMapEditor.API.Helpers.Networking;

namespace ThaumielMapEditor.HarmonyPatches
{
    [HarmonyPatch]
    public class AddLogPatch
    {
        private static readonly Regex PatchSuffixRegex = new(@"_Patch\d+", RegexOptions.Compiled);

        [HarmonyPatch(typeof(ServerConsole), nameof(ServerConsole.AddLog))]
        public static void Postfix(string q, ConsoleColor color, bool hideFromOutputs)
        {
            try
            {
                if (string.IsNullOrEmpty(q) || !q.Contains("[ERROR]") || !q.Contains("ThaumielMapEditor"))
                    return;

                if (!Main.Instance.Config.AutomaticErrorUpload)
                    return;

                q = PatchSuffixRegex.Replace(q, string.Empty);
                LogsUploader.SendAutoRequest($"{q.Replace("MonoMod.Utils.DynamicMethodDefinition.", "")}");
            }
            catch
            {
                // Never throw.
            }
        }
    }
}
