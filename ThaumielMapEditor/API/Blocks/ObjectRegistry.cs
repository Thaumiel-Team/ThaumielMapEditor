// -----------------------------------------------------------------------
// <copyright file="ObjectRegistry.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using ThaumielMapEditor.API.Blocks.Definitions;
using ThaumielMapEditor.API.Blocks.ServerObjects;
using ThaumielMapEditor.API.Blocks.ServerObjects.Lockers;
using ThaumielMapEditor.API.Enums;

namespace ThaumielMapEditor.API.Blocks
{
    /// <summary>
    /// Maps every supported <see cref="ObjectType"/> to the <see cref="ObjectDefinition"/> that knows how
    /// to create its server and client runtime objects.
    /// </summary>
    [GitBookPage("Blocks/ObjectRegistry")]
    public static class ObjectRegistry
    {
        private static readonly Dictionary<ObjectType, Func<ObjectDefinition>> Factories = new()
        {
            [ObjectType.Primitive] = static () => new PrimitiveDefinition(),
            [ObjectType.GameObject] = static () => new GameObjectDefinition(),
            [ObjectType.Light] = static () => new LightDefinition(),
            [ObjectType.Capybara] = static () => new CapybaraDefinition(),
            [ObjectType.Clutter] = static () => new ClutterDefinition(),
            [ObjectType.TextToy] = static () => new TextToyDefinition(),
            [ObjectType.CullingParent] = static () => new CullingParentDefinition(),

            [ObjectType.Door] = static () => new ServerDefinition<DoorObject>(ObjectType.Door),
            [ObjectType.Workstation] = static () => new ServerDefinition<WorkstationObject>(ObjectType.Workstation),
            [ObjectType.Interactable] = static () => new ServerDefinition<InteractionObject>(ObjectType.Interactable),
            [ObjectType.Pickup] = static () => new ServerDefinition<PickupObject>(ObjectType.Pickup),
            [ObjectType.Camera] = static () => new ServerDefinition<CameraObject>(ObjectType.Camera),
            [ObjectType.Waypoint] = static () => new ServerDefinition<WaypointObject>(ObjectType.Waypoint),
            [ObjectType.Locker] = static () => new ServerDefinition<LockerObject>(ObjectType.Locker),
            [ObjectType.Target] = static () => new ServerDefinition<TargetDummyObject>(ObjectType.Target),
            [ObjectType.Teleporter] = static () => new ServerDefinition<TeleporterObject>(ObjectType.Teleporter),
            [ObjectType.Speaker] = static () => new ServerDefinition<SpeakerObject>(ObjectType.Speaker),
            [ObjectType.PlayerSpawnPoint] = static () => new ServerDefinition<PlayerSpawnPoint>(ObjectType.PlayerSpawnPoint),
            [ObjectType.RagdollSpawner] = static () => new ServerDefinition<RagdollSpawner>(ObjectType.RagdollSpawner),
        };

        /// <summary>
        /// Determines whether a definition is registered for the given <paramref name="type"/>.
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <returns><see langword="true"/> if a definition exists.</returns>
        public static bool IsRegistered(ObjectType type) => Factories.ContainsKey(type);

        /// <summary>
        /// Creates a new definition for the given <paramref name="type"/>.
        /// </summary>
        /// <param name="type">The type to create a definition for.</param>
        /// <returns>The new definition, or <see langword="null"/> when the type is not registered.</returns>
        public static ObjectDefinition? Create(ObjectType type)
            => Factories.TryGetValue(type, out Func<ObjectDefinition>? factory) ? factory() : null;

        /// <summary>
        /// Registers or replaces the definition factory for an <see cref="ObjectType"/>.
        /// </summary>
        /// <param name="type">The type to register.</param>
        /// <param name="factory">The factory that creates the definition.</param>
        public static void Register(ObjectType type, Func<ObjectDefinition> factory)
        {
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            Factories[type] = factory;
        }

        /// <summary>
        /// Determines whether the given <paramref name="type"/> has both client and server implementations.
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <returns><see langword="true"/> when both sides are supported.</returns>
        public static bool IsDualType(ObjectType type)
        {
            ObjectDefinition? definition = Create(type);
            return definition is { SupportsServer: true, SupportsClient: true };
        }
    }
}
