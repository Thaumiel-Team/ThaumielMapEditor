// -----------------------------------------------------------------------
// <copyright file="GetCreditsRequest.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using ThaumielMapEditor.API.Enums;

namespace ThaumielMapEditor.API.Helpers.Networking
{
    public class GetCreditsRequest : NetworkRequest
    {
        public override string Name => nameof(GetCreditsRequest);

        public override RequestType Method => RequestType.Get;

        public override string CustomUrl => "https://raw.githubusercontent.com/Thaumiel-Team/ThaumielMapEditor-Credits/master/credits.yml";

        public override bool UseCustomUrl => true;
    }
}