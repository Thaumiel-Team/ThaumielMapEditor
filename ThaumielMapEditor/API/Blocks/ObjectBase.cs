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
        private int parentId;
        private Vector3 position;
        private Quaternion rotation = Quaternion.identity;
        private Vector3 scale = Vector3.one;
        private bool isStatic;
        private byte movementSmoothing;

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
        /// Gets a value indicating whether this object can currently accept sync changes.
        /// </summary>
        protected virtual bool CanSync => true;

        /// <summary>
        /// Tracks whether this object is already queued inside the <see cref="SyncManager"/>.
        /// </summary>
        internal int PendingSync;

        /// <summary>
        /// Gets a value indicating whether this object is currently spawned in its schematic.
        /// </summary>
        public bool IsSpawned => Schematic?.Contains(this) ?? false;

        /// <summary>
        /// Gets a value indicating whether this object is parented directly to the schematic root.
        /// </summary>
        public bool IsRootObject => Schematic != null && ParentId == Schematic.RootObjectId;

        /// <summary>
        /// Gets the parent of this object.
        /// </summary>
        /// <returns>The parent object, or <see langword="null"/> if this object is parented to the schematic root.</returns>
        public ObjectBase? GetParent() => Schematic?.GetParent(this);

        /// <summary>
        /// Gets every object parented directly to this object.
        /// </summary>
        /// <returns>An <see cref="IEnumerable{T}"/> containing the child objects.</returns>
        public IEnumerable<ObjectBase> GetChildren() => Schematic?.GetChildren(this) ?? [];

        /// <summary>
        /// Gets every other object that shares this object's parent.
        /// </summary>
        /// <returns>An <see cref="IEnumerable{T}"/> containing the sibling objects.</returns>
        public IEnumerable<ObjectBase> GetSiblings()
        {
            if (Schematic == null)
                yield break;

            foreach (ObjectBase obj in Schematic.GetChildren(ParentId))
            {
                if (!ReferenceEquals(obj, this))
                    yield return obj;
            }
        }

        /// <summary>
        /// Marks the given properties as dirty and schedules this object for syncing.
        /// </summary>
        /// <param name="flags">The properties that changed.</param>
        protected void MarkSyncNeeded(SyncFlags flags)
        {
            if (!CanSync)
                return;

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

        internal void ClearDirtyFlags(SyncFlags synced) => SyncFlags &= ~synced;

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
            parentId = source.ParentId;
            position = source.Position;
            rotation = source.Rotation;
            scale = source.Scale;
            isStatic = source.IsStatic;
            movementSmoothing = source.MovementSmoothing;
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

        /// <summary>
        /// Tries to get a value from <see cref="SerializableObject.Values"/> and convert it to the specified type.
        /// </summary>
        /// <typeparam name="T">The type to convert the value to.</typeparam>
        /// <param name="key">The key to look up.</param>
        /// <param name="value">The converted value.</param>
        /// <returns><see langword="true"/> if the value was found and converted.</returns>
        public bool TryGetValue<T>(string key, out T value) => Values.TryConvertValue(key, out value);

        /// <summary>
        /// Gets a value from <see cref="SerializableObject.Values"/> or returns the given default.
        /// </summary>
        /// <typeparam name="T">The type to convert the value to.</typeparam>
        /// <param name="key">The key to look up.</param>
        /// <param name="defaultValue">The value to return when the key is missing.</param>
        public T GetValueOrDefault<T>(string key, T defaultValue = default!) =>
            Values.TryConvertValue(key, out T value) ? value : defaultValue;

        /// <summary>
        /// Determines whether this object has a serialized value with the given key.
        /// </summary>
        /// <param name="key">The key to look up.</param>
        /// <returns><see langword="true"/> if the key exists.</returns>
        public bool HasValue(string key) => Values.ContainsKey(key);

        /// <summary>
        /// Destroys this object from its schematic.
        /// </summary>
        /// <returns><see langword="true"/> if the object was destroyed.</returns>
        public bool Destroy() => Schematic?.DestroyObject(this) ?? false;
    }
}