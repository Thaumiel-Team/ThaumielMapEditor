// -----------------------------------------------------------------------
// <copyright file="SchematicHandler.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU Lesser General Public License v3.0 (LGPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using ThaumielMapEditor.Events.EventArgs.Schematic;

namespace ThaumielMapEditor.Events
{
    /// <summary>
    /// This is a example of how to setup animations to run after spawning a schematic.
    /// </summary>
    internal class SchematicHandler
    {
        public static void Register()
        {
            EventArgs.Handlers.SchematicHandler.SchematicSpawned += OnSchematicSpawned;
        }

        public static void Unregister()
        {
            EventArgs.Handlers.SchematicHandler.SchematicSpawned -= OnSchematicSpawned;
        }

        private static void OnSchematicSpawned(SchematicSpawnedEventArgs ev)
        {
            ev.Schematic.AnimationController.Play("Idle");
        }
    }
}