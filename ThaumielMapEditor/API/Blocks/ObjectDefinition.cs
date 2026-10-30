// -----------------------------------------------------------------------
// <copyright file="ObjectDefinition.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using ThaumielMapEditor.API.Blocks.ClientSide;
using ThaumielMapEditor.API.Enums;
using ThaumielMapEditor.API.Extensions;
using ThaumielMapEditor.API.Serialization;

namespace ThaumielMapEditor.API.Blocks
{
    /// <summary>
    /// Describes a single kind of object that can be spawned by the map editor.
    /// </summary>
    /// <remarks>
    /// A definition owns the serialized values of one object and knows how to turn those values into the
    /// server and client runtime objects. This replaces the old pattern of maintaining a separate
    /// <c>ClientNAMEObject</c> and <c>ServerNAMEObject</c> pair per object kind.
    /// </remarks>
    [GitBookPage("Blocks/ObjectDefinition")]
    public abstract class ObjectDefinition
    {
        /// <summary>
        /// Gets the name of this object kind.
        /// </summary>
        public abstract string Name { get; }

        /// <summary>
        /// Gets the <see cref="Enums.ObjectType"/> this definition creates.
        /// </summary>
        public abstract ObjectType ObjectType { get; }

        /// <summary>
        /// Gets a value indicating whether this definition can create a server object.
        /// </summary>
        public virtual bool SupportsServer => true;

        /// <summary>
        /// Gets a value indicating whether this definition can create a client object.
        /// </summary>
        public virtual bool SupportsClient => true;

        /// <summary>
        /// Reads the serialized values of <paramref name="serializable"/> into this definition.
        /// </summary>
        /// <param name="serializable">The serialized object to read from.</param>
        public virtual void Load(SerializableObject serializable)
        {
            serializable.Values.PopulateFrom(this);
        }

        /// <summary>
        /// Creates the server runtime object for this definition.
        /// </summary>
        /// <param name="data">The shared object state that was read from the map file.</param>
        /// <returns>The server object, or <see langword="null"/> when the object cannot be spawned.</returns>
        public abstract global::ThaumielMapEditor.API.Blocks.ServerObject? ServerObject(ObjectBase data);

        /// <summary>
        /// Creates the client runtime object for this definition.
        /// </summary>
        /// <param name="data">The shared object state that was read from the map file.</param>
        /// <returns>The client object, or <see langword="null"/> when the object cannot be spawned.</returns>
        public abstract ClientObject? ClientObject(ObjectBase data);
    }

    /// <summary>
    /// An <see cref="ObjectDefinition"/> that automatically populates its serialized properties from
    /// <see cref="SerializableObject.Values"/>.
    /// </summary>
    /// <typeparam name="TSelf">The concrete definition type.</typeparam>
    public abstract class ObjectDefinition<TSelf> : ObjectDefinition where TSelf : ObjectDefinition<TSelf>, new()
    {
        /// <inheritdoc/>
        public override void Load(SerializableObject serializable)
        {
            serializable.Values.PopulateFrom(this);
        }
    }

    /// <summary>
    /// An <see cref="ObjectDefinition"/> for server-only object kinds that still live in their own
    /// <see cref="ServerObject"/> class.
    /// </summary>
    /// <typeparam name="T">The server object type.</typeparam>
    public sealed class ServerDefinition<T> : ObjectDefinition where T : ServerObject, new()
    {
        private SerializableObject? source;

        /// <summary>
        /// Initializes a new instance of the <see cref="ServerDefinition{T}"/> class.
        /// </summary>
        /// <param name="objectType">The type of object this definition creates.</param>
        public ServerDefinition(ObjectType objectType)
        {
            ObjectType = objectType;
        }

        /// <inheritdoc/>
        public override string Name => typeof(T).Name;

        /// <inheritdoc/>
        public override ObjectType ObjectType { get; }

        /// <inheritdoc/>
        public override bool SupportsClient => false;

        /// <inheritdoc/>
        public override void Load(SerializableObject serializable)
        {
            source = serializable;
        }

        /// <inheritdoc/>
        public override global::ThaumielMapEditor.API.Blocks.ServerObject? ServerObject(ObjectBase data)
        {
            if (source == null)
                return null;

            T server = source.Values.Count == 0 ? new T() : source.Values.ConvertTo<T>() ?? new T();
            server.CopyStateFrom(data);
            return server;
        }

        /// <inheritdoc/>
        public override ClientObject? ClientObject(ObjectBase data) => null;
    }
}
