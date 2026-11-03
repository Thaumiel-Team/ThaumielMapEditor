// -----------------------------------------------------------------------
// <copyright file="SchematicHandler.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU Lesser General Public License v3.0 (LGPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using ThaumielMapEditor.API.Data;
using ThaumielMapEditor.API.Helpers;

namespace ThaumielMapEditor.Events
{
    /// <summary>
    /// This is a example of how to setup animations to run after spawning a schematic.
    /// </summary>
    internal class SchematicHandler
    {
        public static void Register()
        {
            Loader.SchematicSpawned += OnSchematicSpawned;
        }

        public static void Unregister()
        {
            Loader.SchematicSpawned -= OnSchematicSpawned;
        }

        private static void OnSchematicSpawned(SchematicData schematic)
        {
            schematic.AnimationController.Play("Idle");
        }
    }
}