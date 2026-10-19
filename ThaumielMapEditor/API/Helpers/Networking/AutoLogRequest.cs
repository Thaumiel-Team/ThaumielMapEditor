// -----------------------------------------------------------------------
// <copyright file="AutoLogRequest.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using System.Text.Json.Serialization;
using ThaumielMapEditor.API.Enums;

namespace ThaumielMapEditor.API.Helpers.Networking
{
    public class AutoLogRequest : NetworkRequest
    {
        public AutoLogRequest(string log)
        {
            Payload = new()
            {
                ["plugin_version"] = Main.Instance.Version,
                ["log_data"] = log,
            };
        }

        public override string Name => nameof(AutoLogRequest);

        public override RequestType Method => RequestType.Post;

        public override string Endpoint => "auto/upload";

        public override Dictionary<string, string> Headers { get; set; } = new()
        {
            ["Content-Type"] = "application/json"
        };
    }
}