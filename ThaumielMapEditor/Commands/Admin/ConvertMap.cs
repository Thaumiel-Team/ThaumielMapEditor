// -----------------------------------------------------------------------
// <copyright file="ConvertMap.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.IO;
using System.Threading.Tasks;
using CommandSystem;
using LabApi.Loader.Features.Paths;
using ThaumielMapEditor.API.Binary;
using ThaumielMapEditor.API.Conversion;
using ThaumielMapEditor.API.Helpers;
using ThaumielMapEditor.API.Interfaces;
using ThaumielMapEditor.API.Serialization;

namespace ThaumielMapEditor.Commands.Admin
{
    [DoNotParse]
    public class ConvertMap : SubCommand
    {
        public override string Name => "convertmap";
        public override string VisibleArgs => "<Map Name>";
        public override int RequiredArgsCount => 1;
        public override string Description => "Converts the PMER map with the specified name";
        public override string[] Aliases => ["cvm"];
        public override string RequiredPermission => "tme.convert";

        public override bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            if (arguments == null || arguments.Count == 0)
            {
                response = "No map name provided.";
                return false;
            }

            string merDir = Path.Combine(PathManager.Configs.ToString(), "ProjectMER", "Maps");

            foreach (string file in Directory.GetFiles(merDir, "*.yml", SearchOption.AllDirectories))
            {
                string filename = Path.GetFileNameWithoutExtension(file);

                if (!filename.Equals(arguments.At(0), StringComparison.OrdinalIgnoreCase))
                    continue;

                string schematicName = Path.GetFileName(arguments.At(0));
                if (string.IsNullOrEmpty(schematicName))
                {
                    response = "Invalid map name provided.";
                    return false;
                }

                Task.Run(async () =>
                {
                    try
                    {
                        PMERMapRoot root = PMERLoader.LoadMap(file);
                        root.Name = filename;
                        SerializableSchematic schematic = await PMERConverter.ConvertMapAsync(root);
                        string outputPath = ThaumFileManager.Dir(["Schematics", $"{schematicName}.bin"]);
                        Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
                        BinarySerializer.SerializeToFile(outputPath, schematic);

                        LogManager.Info($"Conversion of map '{schematicName}' completed successfully.");
                        Loader.ReloadSchematics();
                    }
                    catch (Exception e)
                    {
                        LogManager.Error($"Conversion of map '{schematicName}' failed: {e}");
                    }
                });

                response = $"Conversion of map '{schematicName}' started. Check logs for completion.";
                return true;
            }

            response = $"Failed to find map with name {arguments.At(0)}";
            return true;
        }
    }
}
