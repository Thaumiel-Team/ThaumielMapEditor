// -----------------------------------------------------------------------
// <copyright file="SchematicBlocks.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using ThaumielMapEditor.API.Data;
using ThaumielMapEditor.API.Serialization;
using UnityEngine;

namespace ThaumielMapEditor.API.Helpers.BlockParser
{
    public enum SchematicSpawnSource
    {
        Name,
        Path,
        Directory,
        Serialized
    }

    public class SchematicSpawnBlock : BlockBase
    {
        public SchematicSpawnSource Source { get; set; }
        public string SchematicName { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public string Directory { get; set; } = string.Empty;
        public object? SerializedSchematic { get; set; }
        public object? Position { get; set; }
        public object? Rotation { get; set; }
        public object? Scale { get; set; }

        public override object? ReturnExecute()
        {
            Vector3 position = ResolveVector3(Position, Vector3.zero);
            bool hasRotation = Rotation != null;
            bool hasScale = Scale != null;
            Quaternion rotation = hasRotation ? ResolveQuaternion(Rotation) : default;
            Vector3 scale = hasScale ? ResolveVector3(Scale, default) : default;

            SchematicData? schematic = Source switch
            {
                SchematicSpawnSource.Name => SpawnByName(position, hasRotation, hasScale, rotation, scale),
                SchematicSpawnSource.Path => SpawnFromPath(position, hasRotation, hasScale, rotation, scale),
                SchematicSpawnSource.Directory => SpawnFromDirectory(position, hasRotation, hasScale, rotation, scale),
                SchematicSpawnSource.Serialized => SpawnSerialized(position, hasRotation, hasScale, rotation, scale),
                _ => null
            };

            if (schematic == null)
                LogManager.Warn($"Schematic spawn failed for source '{Source}'.");

            return schematic!;
        }

        public override object? ReturnExecute(object obj) => ReturnExecute();

        public override object? ReturnExecute(SchematicData schematic) => ReturnExecute();

        private SchematicData? SpawnByName(Vector3 position, bool hasRotation, bool hasScale, Quaternion rotation, Vector3 scale)
        {
            if (hasScale)
                return Loader.SpawnSchematic(SchematicName, position, rotation, scale);

            if (hasRotation)
                return Loader.SpawnSchematic(SchematicName, position, rotation);

            return Loader.SpawnSchematic(SchematicName, position);
        }

        private SchematicData? SpawnFromPath(Vector3 position, bool hasRotation, bool hasScale, Quaternion rotation, Vector3 scale)
        {
            if (hasScale)
                return Loader.SpawnSchematicFromPath(Path, position, rotation, scale);

            if (hasRotation)
                return Loader.SpawnSchematicFromPath(Path, position, rotation);

            return Loader.SpawnSchematicFromPath(Path, position);
        }

        private SchematicData? SpawnFromDirectory(Vector3 position, bool hasRotation, bool hasScale, Quaternion rotation, Vector3 scale)
        {
            if (hasScale)
                return Loader.SpawnSchematicFromDirectory(Directory, SchematicName, position, rotation, scale);

            if (hasRotation)
                return Loader.SpawnSchematicFromDirectory(Directory, SchematicName, position, rotation);

            return Loader.SpawnSchematicFromDirectory(Directory, SchematicName, position);
        }

        private SchematicData? SpawnSerialized(Vector3 position, bool hasRotation, bool hasScale, Quaternion rotation, Vector3 scale)
        {
            if (ResolveValue(SerializedSchematic) is not SerializableSchematic schematic)
            {
                LogManager.Warn("Schematic spawn expected a SerializableSchematic input.");
                return null;
            }

            if (hasScale)
                return Loader.SpawnSchematic(schematic, position, rotation, scale);

            if (hasRotation)
                return Loader.SpawnSchematic(schematic, position, rotation);

            return Loader.SpawnSchematic(schematic, position);
        }

        private static Vector3 ResolveVector3(object? value, Vector3 fallback)
        {
            if (value == null)
                return fallback;

            object? resolved = ResolveValue(value);

            if (resolved is Vector3 vector)
                return vector;

            LogManager.Warn($"Expected Vector3 but got '{resolved?.GetType().Name ?? "null"}'.");
            return fallback;
        }

        private static Quaternion ResolveQuaternion(object? value)
        {
            if (value == null)
                return default;

            object? resolved = ResolveValue(value);

            if (resolved is Quaternion quaternion)
                return quaternion;

            LogManager.Warn($"Expected Quaternion but got '{resolved?.GetType().Name ?? "null"}'.");
            return default;
        }
    }
}
