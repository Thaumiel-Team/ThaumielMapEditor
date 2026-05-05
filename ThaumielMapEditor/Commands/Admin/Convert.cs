// -----------------------------------------------------------------------
// <copyright file="Convert.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.IO;
using System.Threading.Tasks;
using CommandSystem;
using LabApi.Loader.Features.Paths;
using ThaumielMapEditor.API.Attributes;
using ThaumielMapEditor.API.Conversion;
using ThaumielMapEditor.API.Helpers;
using ThaumielMapEditor.API.Interfaces;
using ThaumielMapEditor.API.Serialization;

namespace ThaumielMapEditor.Commands.Admin
{
#pragma warning disable CS1591
    [DoNotParse]
    public class Convert : ISubCommand
    {
        public override string Name => "convert";
        public override string VisibleArgs => "<Schematic Name>";
        public override int RequiredArgsCount => 1;
        public override string Description => "Converts the PMER schematic with the specified name";
        public override string[] Aliases => ["cv"];
        public override string RequiredPermission => "tme.convert";

        public override bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            if (arguments == null || arguments.Count == 0)
            {
                response = "No schematic name provided.";
                return false;
            }

            string merDir = Path.Combine(PathManager.Configs.ToString(), "ProjectMER", "Schematics");

            foreach (string file in Directory.GetFiles(merDir, "*.json", SearchOption.AllDirectories))
            {
                string filename = Path.GetFileNameWithoutExtension(file);

                if (filename.Contains("-Rigidbodies"))
                    continue;
                if (!filename.Equals(arguments.At(0), StringComparison.OrdinalIgnoreCase))
                    continue;

                string content = File.ReadAllText(file);
                if (!content.TrimStart().StartsWith("{"))
                    continue;

                string schematicName = arguments.At(0);

                Task.Run(async () =>
                {
                    try
                    {
                        PMERRoot root = PMERLoader.Load(file);
                        root.Name = filename;
                        SerializableSchematic schematic = await PMERConverter.ConvertSchematicAsync(root);
                        string yaml = Loader.Serializer.Serialize(schematic);
                        string outputPath = ThaumFileManager.Dir(["Schematics", $"{schematicName}.yml"]);
                        Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
                        File.WriteAllText(outputPath, yaml);

                        LogManager.Info($"Conversion of '{schematicName}' completed successfully.");
                        Loader.ReloadSchematics();
                    }
                    catch (Exception e)
                    {
                        LogManager.Error($"Conversion of '{schematicName}' failed: {e}");
                    }
                });

                response = $"Conversion of '{schematicName}' started. Check logs for completion.";
                return true;
            }

            response = $"Failed to find file with name {arguments.At(0)}";
            return true;
        }
    }
}