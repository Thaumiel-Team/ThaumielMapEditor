// -----------------------------------------------------------------------
// <copyright file="ObjectBase.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using ThaumielMapEditor.API.Data;
using ThaumielMapEditor.API.Enums;
using ThaumielMapEditor.API.Extensions;
using ThaumielMapEditor.API.Serialization;
using UnityEngine;

namespace ThaumielMapEditor.API.Blocks
{
    /// <summary>
    /// Shared state for every object spawned by the map editor, regardless of whether the object is
    /// replicated by the server or faked on the client.
    /// </summary>
    /// <remarks>
    /// This type also acts as the data packet passed from an <see cref="ObjectDefinition"/> to the
    /// object it creates, see <see cref="CopyStateFrom(ObjectBase)"/>.
    /// </remarks>
    [GitBookPage("Blocks/ObjectBase")]
    public class ObjectBase
    {
        private protected string objectName = string.Empty;
        private protected int objectId;
        private protected int parentId;
        private protected Vector3 position;
        private protected Quaternion rotation = Quaternion.identity;
        private protected Vector3 scale = Vector3.one;
        private protected bool isStatic;
        private protected byte movementSmoothing;
        private protected uint netId;

        /// <summary>
        /// Gets or sets the display name of this object.
        /// </summary>
        public string Name
        {
            get => objectName;
            set => objectName = value;
        }

        /// <summary>
        /// Gets the id of the <see cref="SerializableObject"/> this instance was generated from.
        /// </summary>
        public int ObjectId
        {
            get => objectId;
            internal set => objectId = value;
        }

        /// <summary>
        /// Gets or sets the id of the <see cref="SerializableObject"/> that acts as this object's parent.
        /// </summary>
        public virtual int ParentId
        {
            get => parentId;
            set => parentId = value;
        }

        /// <summary>
        /// Gets or sets the local position of this object relative to its parent.
        /// </summary>
        public virtual Vector3 Position
        {
            get => position;
            set => position = value;
        }

        /// <summary>
        /// Gets or sets the rotation of this object.
        /// </summary>
        public virtual Quaternion Rotation
        {
            get => rotation;
            set => rotation = value;
        }

        /// <summary>
        /// Gets or sets the local scale of this object.
        /// </summary>
        public virtual Vector3 Scale
        {
            get => scale;
            set => scale = value;
        }

        /// <summary>
        /// Gets or sets whether this object should be treated as static.
        /// </summary>
        public virtual bool IsStatic
        {
            get => isStatic;
            set => isStatic = value;
        }

        /// <summary>
        /// Gets or sets how many ticks the server waits between movement updates.
        /// </summary>
        public virtual byte MovementSmoothing
        {
            get => movementSmoothing;
            set => movementSmoothing = value;
        }

        /// <summary>
        /// Gets or sets the movement smoothing value.
        /// </summary>
        public byte SyncInterval
        {
            get => MovementSmoothing;
            set => MovementSmoothing = value;
        }

        /// <summary>
        /// Gets the netId of the spawned object, or <c>0</c> if it has not been spawned yet.
        /// </summary>
        public uint NetId
        {
            get => netId;
            internal set => netId = value;
        }

        /// <summary>
        /// Gets or sets the kind of object this instance represents.
        /// </summary>
        public virtual ObjectType ObjectType { get; set; }

        /// <summary>
        /// Gets or sets the schematic this object belongs to.
        /// </summary>
        public SchematicData? Schematic { get; set; }

        /// <summary>
        /// Gets the serialized data this object was created from.
        /// </summary>
        public SerializableObject? Serialization { get; internal set; }

        /// <summary>
        /// Gets the values that were deserialized from the map file.
        /// </summary>
        public Dictionary<string, object> Values => Serialization?.Values ?? [];

        internal SyncFlags SyncFlags { get; private set; } = SyncFlags.None;

        /// <summary>
        /// Gets a value indicating whether this object has changes waiting to be synced.
        /// </summary>
        public bool IsDirty => SyncFlags != SyncFlags.None;

        /// <summary>
        /// Marks the given properties as dirty and schedules this object for syncing.
        /// </summary>
        /// <param name="flags">The properties that changed.</param>
        protected void MarkSyncNeeded(SyncFlags flags)
        {
            SyncFlags |= flags;
            RegisterForSync();
        }

        /// <summary>
        /// Registers this object with the <see cref="SyncManager"/> for the pending side.
        /// </summary>
        protected virtual void RegisterForSync()
        {
        }

        internal void ClearDirtyFlags() => SyncFlags = SyncFlags.None;

        /// <summary>
        /// Copies every shared value from <paramref name="source"/> without running property side effects.
        /// </summary>
        /// <param name="source">The object to copy state from.</param>
        public void CopyStateFrom(ObjectBase source)
        {
            if (source == null || ReferenceEquals(source, this))
                return;

            objectName = source.objectName;
            objectId = source.objectId;
            parentId = source.parentId;
            position = source.position;
            rotation = source.rotation;
            scale = source.scale;
            isStatic = source.isStatic;
            movementSmoothing = source.movementSmoothing;
            netId = source.netId;
            ObjectType = source.ObjectType;
            Schematic = source.Schematic;
            Serialization = source.Serialization;
        }

        /// <summary>
        /// Populates this instance with the values shared by every object type.
        /// </summary>
        /// <param name="serializable">The serialized object to read from.</param>
        /// <param name="schematic">The schematic the object belongs to.</param>
        public void LoadFrom(SerializableObject serializable, SchematicData schematic)
        {
            Name = serializable.Name;
            ObjectId = serializable.ObjectId;
            ParentId = serializable.ParentId;
            Position = serializable.Position;
            Rotation = serializable.Rotation;
            Scale = serializable.Scale;
            IsStatic = serializable.IsStatic;
            MovementSmoothing = serializable.MovementSmoothing;
            ObjectType = serializable.ObjectType;
            Schematic = schematic;
            Serialization = serializable;
        }

        /// <summary>
        /// Gets a value from <see cref="SerializableObject.Values"/> and converts it to the specified type.
        /// </summary>
        /// <typeparam name="T">The type to convert the value to.</typeparam>
        /// <param name="serializable">The <see cref="SerializableObject"/> instance.</param>
        /// <param name="key">The key to look up.</param>
        public T GetValue<T>(SerializableObject serializable, string key) =>
            serializable.Values.GetConvertValue<T>(key);

        /// <summary>
        /// Gets a value from this object's <see cref="Serialization"/> and converts it to the specified type.
        /// </summary>
        /// <typeparam name="T">The type to convert the value to.</typeparam>
        /// <param name="key">The key to look up.</param>
        public T GetValue<T>(string key) =>
            (Serialization ?? throw new InvalidOperationException($"'{Name}' has no serialized source.")).Values.GetConvertValue<T>(key);
    }
}
