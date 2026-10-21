// -----------------------------------------------------------------------
// <copyright file="CreditsHelper.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using Cryptography;
using LabApi.Features.Wrappers;
using LabApi.Loader.Features.Yaml;
using ThaumielMapEditor.API.Helpers.Networking;

namespace ThaumielMapEditor.API.Helpers
{
    public class CreditHelper
    {
        public enum TagType
        {
            None = 0,
            LeadDeveloper = 1,
            Developer = 2,
            Contributor = 3,
        }

        private static DirectoryInfo CacheDirectory { get; } = FileManager.TryCreateDirectory("CreditTags");
        private static string DatabaseCachePath { get; } = Path.Combine(CacheDirectory.FullName, TmeTagCacheName);
        private const string TmeTagCacheName = "tmetag_cache.yml";


        public static void Init()
        {
            if (!CacheDirectory.Exists)
                CacheDirectory.Create();

            if (File.Exists(DatabaseCachePath))
            {
                if (File.GetCreationTime(DatabaseCachePath) - DateTime.Now < TimeSpan.FromMinutes(10))
                {
                    Credits = YamlConfigParser.Deserializer.Deserialize<Dictionary<string, TagType>>(File.ReadAllText(DatabaseCachePath));
                }
                else
                    UpdateCache();
            }
            else
                UpdateCache();
        }

        private static void UpdateCache()
        {
            GetCreditsRequest request = new();
            request.SendRequest((complete) => 
            {
                Credits = YamlConfigParser.Deserializer.Deserialize<Dictionary<string, TagType>>(complete.downloadHandler.text);
                File.WriteAllText(DatabaseCachePath, complete.downloadHandler.text);
            });
        }

        public static string ParseId(Player player)
            => Sha.HashToString(Sha.Sha256(player.UserId));

        internal static Dictionary<string, TagType> Credits = [];
        
        internal static TagType SetTag(Player player)
        {
            if (Credits.IsEmpty())
            {
                LogManager.Warn($"Failed to get credits.");
                return TagType.None;
            }

            if (player.UserGroup != null)
                return TagType.None;

            if (!Main.Instance.Config.EnableCreditTags)
                return TagType.None;

            if (!Credits.TryGetValue(ParseId(player), out var type))
                return TagType.None;

            switch (type)
            {
                case TagType.LeadDeveloper:
                    player.GroupName = "TME Lead Developer";
                    player.GroupColor = "pumpkin";
                    break;

                case TagType.Developer:
                    player.GroupName = "TME Developer";
                    player.GroupColor = "purple";
                    break;

                case TagType.Contributor:
                    player.GroupName = "TME Contributor";
                    player.GroupColor = "red";
                    break;
            };

            return type;
        }
    }
}