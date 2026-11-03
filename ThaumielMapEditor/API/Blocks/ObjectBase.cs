// -----------------------------------------------------------------------
// <copyright file="ObjectBase.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU Lesser General Public License v3.0 (LGPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using ThaumielMapEditor.API.Data;
using ThaumielMapEditor.API.Enums;
using ThaumielMapEditor.API.Extensions;
using ThaumielMapEditor.API.Serialization;

namespace ThaumielMapEditor.API.Blocks
{
    [GitBookPage("Blocks/ObjectBase")]
    public class ObjectBase
    {
        /// <summary>
        /// Gets or sets the display name of this object.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets the id of the <see cref="SerializableObject"/> this instance was generated from.
        /// </summary>
        public int ObjectId { get; internal set; }

        /// <summary>
        /// Gets or sets the id of the <see cref="SerializableObject"/> that acts as this object's parent.
        /// </summary>
        public virtual int ParentId { get; set; }

        /// <summary>
        /// Gets or sets the local position of this object relative to its parent.
        /// </summary>
        public virtual Vector3 Position { get; set; }

        /// <summary>
        /// Gets or sets the rotation of this object.
        /// </summary>
        public virtual Quaternion Rotation { get; set; } = Quaternion.identity;

        /// <summary>
        /// Gets or sets the local scale of this object.
        /// </summary>
        public virtual Vector3 Scale { get; set; } = Vector3.one;

        /// <summary>
        /// Gets or sets whether this object should be treated as static.
        /// </summary>
        public virtual bool IsStatic { get; set; }

        /// <summary>
        /// Gets or sets how many ticks the server waits between movement updates.
        /// </summary>
        public virtual byte MovementSmoothing { get; set; }

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
        public uint NetId { get; internal set; }

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

            Name = source.Name;
            ObjectId = source.ObjectId;
            ParentId = source.ParentId;
            Position = source.Position;
            Rotation = source.Rotation;
            Scale = source.Scale;
            IsStatic = source.IsStatic;
            MovementSmoothing = source.MovementSmoothing;
            NetId = source.NetId;
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
