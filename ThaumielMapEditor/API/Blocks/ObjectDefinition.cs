// -----------------------------------------------------------------------
// <copyright file="ObjectDefinition.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU Lesser General Public License v3.0 (LGPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using ThaumielMapEditor.API.Enums;
using ThaumielMapEditor.API.Extensions;
using ThaumielMapEditor.API.Serialization;

namespace ThaumielMapEditor.API.Blocks
{
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
        /// Gets a value indicating whether this definition can create both a server and a client object.
        /// </summary>
        public bool SupportsBoth => SupportsServer && SupportsClient;

        /// <summary>
        /// Determines whether this definition can create an object for the given side.
        /// </summary>
        /// <param name="serverSide">Whether the server side variant is requested.</param>
        /// <returns><see langword="true"/> if the side is supported.</returns>
        public bool CanCreate(bool serverSide) => serverSide ? SupportsServer : SupportsClient;

        /// <summary>
        /// Reads the serialized values of <paramref name="serializable"/> into this definition.
        /// </summary>
        /// <param name="serializable">The serialized object to read from.</param>
        public virtual void Load(SerializableObject serializable) =>
            serializable.Values.PopulateFrom(this);

        /// <summary>
        /// Creates the server runtime object for this definition.
        /// </summary>
        /// <param name="data">The shared object state that was read from the map file.</param>
        /// <returns>The server object, or <see langword="null"/> when the object cannot be spawned.</returns>
        public abstract ServerObject? ServerObject(ObjectBase data);

        /// <summary>
        /// Creates the client runtime object for this definition.
        /// </summary>
        /// <param name="data">The shared object state that was read from the map file.</param>
        /// <returns>The client object, or <see langword="null"/> when the object cannot be spawned.</returns>
        public abstract ClientObject? ClientObject(ObjectBase data);
    }

    public abstract class ObjectDefinition<TSelf> : ObjectDefinition where TSelf : ObjectDefinition<TSelf>, new()
    {
        /// <inheritdoc/>
        public override void Load(SerializableObject serializable)
        {
            serializable.Values.PopulateFrom(this);
        }
    }

    public abstract class ServerDefinition<TServer> : ObjectDefinition where TServer : ServerObject, new()
    {
        private SerializableObject? source;

        /// <inheritdoc/>
        public override bool SupportsClient => false;

        /// <inheritdoc/>
        public override void Load(SerializableObject serializable)
        {
            source = serializable;
        }

        /// <inheritdoc/>
        public override ServerObject? ServerObject(ObjectBase data)
        {
            if (source == null)
                return null;

            TServer server = source.Values.Count == 0 ? new TServer() : source.Values.ConvertTo<TServer>() ?? new TServer();
            server.CopyStateFrom(data);
            return server;
        }

        /// <inheritdoc/>
        public override ClientObject? ClientObject(ObjectBase data) => null;
    }
}