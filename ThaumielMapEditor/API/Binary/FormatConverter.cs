// -----------------------------------------------------------------------
// <copyright file="FormatConverter.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.IO;
using ThaumielMapEditor.API.Helpers;
using ThaumielMapEditor.API.Serialization;

namespace ThaumielMapEditor.API.Binary
{
    public enum FileFormat
    {
        Binary,
        Yaml
    }

    public static class FormatConverter
    {
        public static bool TryConvert(string directoryName, string name, FileFormat target, out string response)
        {
            string directory = ThaumFileManager.Dir([directoryName]);
            if (!Directory.Exists(directory))
            {
                response = $"Directory '{directory}' does not exist.";
                return false;
            }

            string safeName = Path.GetFileName(name.Trim());
            if (string.IsNullOrWhiteSpace(safeName) || safeName.Contains("..") || safeName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                response = "Invalid file name.";
                return false;
            }

            string extension = Path.GetExtension(safeName);
            if (extension.Equals(".bin", StringComparison.OrdinalIgnoreCase) || extension.Equals(".yml", StringComparison.OrdinalIgnoreCase) || extension.Equals(".yaml", StringComparison.OrdinalIgnoreCase))
                safeName = Path.GetFileNameWithoutExtension(safeName);

            string? sourcePath = FindSource(directory, safeName, target);
            if (sourcePath == null)
            {
                response = $"No file named '{safeName}' was found in '{directoryName}'.";
                return false;
            }

            bool sourceIsBinary = IsBinaryFile(sourcePath);
            if (target == FileFormat.Binary == sourceIsBinary)
            {
                response = $"'{Path.GetFileName(sourcePath)}' is already a {target.ToString().ToLowerInvariant()} file.";
                return false;
            }

            string baseName = Path.GetFileNameWithoutExtension(sourcePath);
            string targetPath = GetTargetPath(directory, baseName, target);
            bool replacedExistingFile = File.Exists(targetPath);

            bool isSchematic = directoryName.Equals("Schematics", StringComparison.OrdinalIgnoreCase);
            bool converted = isSchematic ? ConvertSchematic(sourcePath, targetPath, target, baseName, out string error) : ConvertMap(sourcePath, targetPath, target, baseName, out error);

            if (!converted)
            {
                response = error;
                return false;
            }

            response = $"Converted '{Path.GetFileName(sourcePath)}' to '{Path.GetFileName(targetPath)}'";
            response += replacedExistingFile ? ", replacing the existing file." : ".";
            return true;
        }

        private static bool ConvertSchematic(string sourcePath, string targetPath, FileFormat target, string name, out string error)
            => ConvertFile<SerializableSchematic>(sourcePath, targetPath, target, name, static (schematic, fileName) => schematic.FileName = fileName, out error);

        private static bool ConvertMap(string sourcePath, string targetPath, FileFormat target, string name, out string error)
            => ConvertFile<SerializableMap>(sourcePath, targetPath, target, name, static (map, fileName) => map.FileName = fileName, out error);

        private static bool ConvertFile<T>(string sourcePath, string targetPath, FileFormat target, string name, Action<T, string> assignFileName, out string error) where T : class
        {
            error = string.Empty;

            T? value;
            try
            {
                value = IsBinaryFile(sourcePath) ? BinarySerializer.DeserializeFromFile<T>(sourcePath) : Loader.Deserializer.Deserialize<T>(File.ReadAllText(sourcePath));
            }
            catch (Exception ex)
            {
                error = $"Failed to read '{Path.GetFileName(sourcePath)}': {ex.Message}";
                return false;
            }

            if (value is null)
            {
                error = $"Failed to deserialize '{Path.GetFileName(sourcePath)}'.";
                return false;
            }

            assignFileName(value, name);

            try
            {
                if (target == FileFormat.Binary)
                {
                    BinarySerializer.SerializeToFile(targetPath, value);
                }
                else
                    File.WriteAllText(targetPath, Loader.Serializer.Serialize(value));
            }
            catch (Exception ex)
            {
                error = $"Failed to write '{Path.GetFileName(targetPath)}': {ex.Message}";
                return false;
            }

            return true;
        }

        private static string? FindSource(string directory, string name, FileFormat target)
        {
            string[] files = ThaumFileManager.GetFilesInDirectory(directory);
            string[] extensions = target == FileFormat.Binary ? [".yml", ".yaml", ".bin"] : [".bin", ".yml", ".yaml"];

            foreach (string extension in extensions)
            {
                foreach (string path in files)
                {
                    if (Path.GetExtension(path).Equals(extension, StringComparison.OrdinalIgnoreCase) && Path.GetFileNameWithoutExtension(path).Equals(name, StringComparison.OrdinalIgnoreCase))
                        return path;
                }
            }

            return null;
        }

        private static string GetTargetPath(string directory, string name, FileFormat target)
        {
            if (target == FileFormat.Binary)
                return Path.Combine(directory, name + ".bin");

            string ymlPath = Path.Combine(directory, name + ".yml");
            if (File.Exists(ymlPath))
                return ymlPath;

            string yamlPath = Path.Combine(directory, name + ".yaml");
            return File.Exists(yamlPath) ? yamlPath : ymlPath;
        }

        private static bool IsBinaryFile(string path)
            => Path.GetExtension(path).Equals(".bin", StringComparison.OrdinalIgnoreCase);
    }
}
