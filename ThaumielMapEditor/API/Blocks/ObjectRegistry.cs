// -----------------------------------------------------------------------
// <copyright file="ObjectRegistry.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU Lesser General Public License v3.0 (LGPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using ThaumielMapEditor.API.Blocks.Definitions;
using ClutterDef = ThaumielMapEditor.API.Blocks.Definitions.Clutter;
using ThaumielMapEditor.API.Enums;

namespace ThaumielMapEditor.API.Blocks
{
    [GitBookPage("Blocks/ObjectRegistry")]
    public static class ObjectRegistry
    {
        private static readonly Dictionary<ObjectType, Func<ObjectDefinition>> Factories = new()
        {
            [ObjectType.Primitive] = static () => new Primitive(),
            [ObjectType.GameObject] = static () => new GameObject(),
            [ObjectType.Light] = static () => new Light(),
            [ObjectType.Capybara] = static () => new Capybara(),
            [ObjectType.Clutter] = static () => new ClutterDef(),
            [ObjectType.TextToy] = static () => new TextToy(),
            [ObjectType.CullingParent] = static () => new CullingParent(),
            [ObjectType.Door] = static () => new Door(),
            [ObjectType.Workstation] = static () => new Workstation(),
            [ObjectType.Interactable] = static () => new Interactable(),
            [ObjectType.Pickup] = static () => new Pickup(),
            [ObjectType.Camera] = static () => new Camera(),
            [ObjectType.Waypoint] = static () => new Waypoint(),
            [ObjectType.Locker] = static () => new Locker(),
            [ObjectType.Target] = static () => new Target(),
            [ObjectType.Teleporter] = static () => new Teleporter(),
            [ObjectType.Speaker] = static () => new Speaker(),
            [ObjectType.PlayerSpawnPoint] = static () => new PlayerSpawnPoint(),
            [ObjectType.RagdollSpawner] = static () => new RagdollSpawner(),
        };

        private static readonly Dictionary<Type, ObjectType> RuntimeTypes = BuildRuntimeTypes();

        /// <summary>
        /// Determines whether a definition is registered for the given <paramref name="type"/>.
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <returns><see langword="true"/> if a definition exists.</returns>
        public static bool IsRegistered(ObjectType type) => Factories.ContainsKey(type);

        /// <summary>
        /// Gets every <see cref="ObjectType"/> that has a registered definition.
        /// </summary>
        public static IEnumerable<ObjectType> RegisteredTypes => Factories.Keys;

        /// <summary>
        /// Creates a new definition for the given <paramref name="type"/>.
        /// </summary>
        /// <param name="type">The type to create a definition for.</param>
        /// <returns>The new definition, or <see langword="null"/> when the type is not registered.</returns>
        public static ObjectDefinition? Create(ObjectType type)
            => Factories.TryGetValue(type, out Func<ObjectDefinition>? factory) ? factory() : null;

        /// <summary>
        /// Creates a new definition for every registered <see cref="ObjectType"/>.
        /// </summary>
        /// <returns>The created definitions.</returns>
        public static IEnumerable<ObjectDefinition> CreateAll()
        {
            foreach (Func<ObjectDefinition> factory in Factories.Values)
                yield return factory();
        }

        /// <summary>
        /// Gets the display name of the given <paramref name="type"/>.
        /// </summary>
        /// <param name="type">The type to look up.</param>
        /// <returns>The definition name, or the enum name when the type is not registered.</returns>
        public static string GetName(ObjectType type) => Create(type)?.Name ?? type.ToString();

        /// <summary>
        /// Gets the <see cref="ObjectType"/> of the given runtime object type.
        /// </summary>
        /// <typeparam name="T">The runtime object type to look up.</typeparam>
        /// <returns>The matching <see cref="ObjectType"/>, or <see langword="null"/> when no definition creates it.</returns>
        public static ObjectType? GetObjectType<T>() where T : ObjectBase => GetObjectType(typeof(T));

        /// <summary>
        /// Registers or replaces the definition factory for an <see cref="ObjectType"/>.
        /// </summary>
        /// <param name="type">The type to register.</param>
        /// <param name="factory">The factory that creates the definition.</param>
        public static void Register(ObjectType type, Func<ObjectDefinition> factory)
        {
            Factories[type] = factory ?? throw new ArgumentNullException(nameof(factory));

            foreach (Type nested in factory().GetType().GetNestedTypes())
            {
                if (nested.IsAbstract || !typeof(ObjectBase).IsAssignableFrom(nested))
                    continue;

                RuntimeTypes[nested] = type;
            }
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

        /// <summary>
        /// Gets the <see cref="ObjectType"/> that creates the given runtime object type.
        /// </summary>
        /// <param name="runtimeType">The runtime object type to look up.</param>
        /// <returns>The matching <see cref="ObjectType"/>, or <see langword="null"/> when no definition creates it.</returns>
        public static ObjectType? GetObjectType(Type runtimeType)
        {
            if (runtimeType == null)
                throw new ArgumentNullException(nameof(runtimeType));

            return RuntimeTypes.TryGetValue(runtimeType, out ObjectType type) ? type : null;
        }

        /// <summary>
        /// Tries to get the <see cref="ObjectType"/> that creates the given runtime object type.
        /// </summary>
        /// <param name="runtimeType">The runtime object type to look up.</param>
        /// <param name="type">The matching <see cref="ObjectType"/>.</param>
        /// <returns><see langword="true"/> when a definition creates the runtime type.</returns>
        public static bool TryGetObjectType(Type runtimeType, out ObjectType type)
        {
            if (runtimeType == null)
                throw new ArgumentNullException(nameof(runtimeType));

            return RuntimeTypes.TryGetValue(runtimeType, out type);
        }

        /// <summary>
        /// Gets the runtime type that creates the server side object of the given <paramref name="type"/>.
        /// </summary>
        /// <param name="type">The object type to look up.</param>
        /// <returns>The server object type, or <see langword="null"/> when no definition creates it.</returns>
        public static Type? GetServerType(ObjectType type) => GetRuntimeType(type, typeof(ServerObject));

        /// <summary>
        /// Gets the runtime type that creates the client side object of the given <paramref name="type"/>.
        /// </summary>
        /// <param name="type">The object type to look up.</param>
        /// <returns>The client object type, or <see langword="null"/> when no definition creates it.</returns>
        public static Type? GetClientType(ObjectType type) => GetRuntimeType(type, typeof(ClientObject));

        private static Type? GetRuntimeType(ObjectType type, Type baseType)
        {
            ObjectDefinition? definition = Create(type);
            if (definition == null)
                return null;

            foreach (Type nested in definition.GetType().GetNestedTypes())
            {
                if (!nested.IsAbstract && baseType.IsAssignableFrom(nested))
                    return nested;
            }

            return null;
        }

        private static Dictionary<Type, ObjectType> BuildRuntimeTypes()
        {
            Dictionary<Type, ObjectType> types = [];

            foreach (KeyValuePair<ObjectType, Func<ObjectDefinition>> factory in Factories)
            {
                foreach (Type nested in factory.Value().GetType().GetNestedTypes())
                {
                    if (nested.IsAbstract || !typeof(ObjectBase).IsAssignableFrom(nested))
                        continue;

                    types[nested] = factory.Key;
                }
            }

            return types;
        }
    }
}