// -----------------------------------------------------------------------
// <copyright file="Config.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using System.ComponentModel;
using ThaumielMapEditor.API.Attributes;

namespace ThaumielMapEditor
{
    [DoNotParse]
    public class Config
    {
        public bool Debug { get; set; }

        [Description("If true whenever you get an error from TME it will be automatically uploaded to our API. No identifiable information is uploaded alongside this besides whats in the log.")]
        public bool AutomaticErrorUpload { get; set; } = true;

        [Description("If true whenever you upload logs using the command **tmelogs** the localadmin log for that round will be uploaded alongside the logs.")]
        public bool AllowLocalAdminLogUpload { get; set; }

        public bool EnableCreditTags { get; set; } = true;
        public bool EnableServerTracking { get; set; } = true;

        [Description("If set to true schematic and map files will be read on a background thread. This is for testing currently.")]
        public bool ReadFilesInBackground { get; set; } = true;

        [Description("The github token to use when checking for updates or updating.")]
        public string GithubToken { get; set; } = string.Empty;

        [Description("The path to the audio directory.")]
        public string AudioPath { get; set; } = string.Empty;
        
        [Description(
            "\n" +
            "# ------------------------------Map Loading Logic------------------------------\n" +
            "# This method parses input strings to load/unload maps using operators:\n" +
            "# - || (Random): Loads exactly ONE random map from a list.\n" +
            "# - && (Batch): Loads ALL maps specified in the line simultaneously.\n" +
            "# ----------------------------------------------------------------------------\n" +
            "# \n" +
            "# Supported syntax examples:\n" +
            "#   Load::MapName\n" +
            "#   Load::MapA||MapB\n" +
            "#   Load::MapA&&MapB\n" +
            "#   Unload::MapName\n" +
            "#   Unload::MapA||MapB\n" +
            "#   Unload::MapA&&MapB\n" +
            "#   LoadIf::MapName::IsLoaded::ConditionMap\n" +
            "#   LoadIf::MapName::IsNotLoaded::ConditionMap\n" +
            "#   UnloadIf::MapName::IsLoaded::ConditionMap\n" +
            "#   UnloadIf::MapName::IsNotLoaded::ConditionMap\n"
        )]
        public List<string> WaitingForPlayers { get; set; } = [];
        public List<string> RoundStarted { get; set; } = [];
        public List<string> DecontaminationStarted { get; set; } = [];
        public List<string> WarheadStarted { get; set; } = [];
        public List<string> WarheadDetonated { get; set; } = [];

        [Description("These schematics when loaded will play the specified animation by its name. Key: Schematic name. Value: Animation name")]
        public Dictionary<string, string> SchematicAnimationPlayOnLoad { get; set; } = new()
        {
            ["ExampleSchematicName"] = "ExampleAnimationName"
        };
    }
}