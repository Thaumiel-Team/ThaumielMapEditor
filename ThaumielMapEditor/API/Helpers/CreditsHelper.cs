// -----------------------------------------------------------------------
// <copyright file="CreditsHelper.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using Cryptography;
using LabApi.Features.Wrappers;

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

        public static string ParseId(Player player)
            => Sha.HashToString(Sha.Sha256(player.UserId));

        // If you contribute and want a CreditTag add your own steamid sha256 hash to this.
        // Id recommend to use https://codebeautify.org/sha256-hash-generator and set the hash to uppercase.
        internal static readonly Dictionary<string, TagType> Credits = new()
        {
            // MrBaguetter
            ["6B5D92DC3B3911F1E1680CA4C223C420F993085502F25A73F6AF84D599111797"] = TagType.LeadDeveloper,

            // Example developer badge
            ["EXAMPLE99@steam"] = TagType.Developer,

            // Example contributor badge
            ["EXAMPLE99@steam"] = TagType.Contributor
        };

        
        internal static TagType SetTag(Player player)
        {
            if (!Main.Instance.Config!.EnableCreditTags)
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