// -----------------------------------------------------------------------
// <copyright file="SerializableSchematic.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU Lesser General Public License v3.0 (LGPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using System;

namespace ThaumielMapEditor.API.Serialization
{
    /// <summary>
    /// This class is used to read schematic data from yaml
    /// </summary>
    [GitBookPage("Serialization/SerializableSchematic")]
    public class SerializableSchematic
    {
        /// <summary>
        /// Gets or sets the root object id for the <see cref="SerializableSchematic"/> instance.
        /// </summary>
        public int RootObjectId { get; set; }

        /// <summary>
        /// Gets or sets file name for the <see cref="SerializableSchematic"/> instance.
        /// </summary>
        public string FileName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the rotation for the <see cref="SerializableSchematic"/> instance.
        /// </summary>
        public Quaternion Rotation { get; set; }

        /// <summary>
        /// Gets or sets the scale for the <see cref="SerializableSchematic"/> instance.
        /// </summary>
        public Vector3 Scale { get; set; }

        /// <summary>
        /// Gets or sets the objects for the <see cref="SerializableSchematic"/> instance.
        /// </summary>
        public List<SerializableObject> Objects {get; set; } = [];

        /// <summary>
        /// Gets or sets the server-side objects for the <see cref="SerializableSchematic"/> instance.
        /// </summary>
        public List<SerializableObject> ServerSideObjects { get; set; } = [];
        
        /// <summary>
        /// Gets or sets the LOD Zones for the <see cref="SerializableSchematic"/> instance.
        /// </summary>
        public List<SerializableLOD> LOD { get; set; } = [];

        /// <summary>
        /// Gets the number of serialized objects in this schematic.
        /// </summary>
        public int ObjectCount => Objects.Count + ServerSideObjects.Count;

        /// <summary>
        /// Gets every serialized object in this schematic.
        /// </summary>
        /// <returns>An <see cref="IEnumerable{T}"/> containing the client and server side objects.</returns>
        public IEnumerable<SerializableObject> GetObjects()
        {
            foreach (SerializableObject obj in Objects)
                yield return obj;

            foreach (SerializableObject obj in ServerSideObjects)
                yield return obj;
        }

        /// <summary>
        /// Gets the serialized object with the given object id.
        /// </summary>
        /// <param name="objectId">The object id to find.</param>
        /// <returns>The object, or <see langword="null"/> if it was not found.</returns>
        public SerializableObject? GetObject(int objectId)
        {
            foreach (SerializableObject obj in GetObjects())
            {
                if (obj.ObjectId == objectId)
                    return obj;
            }

            return null;
        }

        /// <summary>
        /// Tries to get the serialized object with the given object id.
        /// </summary>
        /// <param name="objectId">The object id to find.</param>
        /// <param name="obj">The found object, or <see langword="null"/>.</param>
        /// <returns><see langword="true"/> if the object was found.</returns>
        public bool TryGetObject(int objectId, out SerializableObject? obj)
        {
            obj = GetObject(objectId);
            return obj != null;
        }

        /// <summary>
        /// Gets the first serialized object with the given name.
        /// </summary>
        /// <param name="name">The object name to find.</param>
        /// <returns>The object, or <see langword="null"/> if it was not found.</returns>
        public SerializableObject? GetObjectByName(string name)
        {
            foreach (SerializableObject obj in GetObjects())
            {
                if (string.Equals(obj.Name, name, StringComparison.OrdinalIgnoreCase))
                    return obj;
            }

            return null;
        }

        /// <summary>
        /// Gets every serialized object that is parented to the given object id.
        /// </summary>
        /// <param name="parentId">The parent object id.</param>
        /// <returns>An <see cref="IEnumerable{T}"/> containing the child objects.</returns>
        public IEnumerable<SerializableObject> GetChildren(int parentId)
        {
            foreach (SerializableObject obj in GetObjects())
            {
                if (obj.ParentId == parentId)
                    yield return obj;
            }
        }

        /// <summary>
        /// Gets every serialized object that is parented directly to the root object.
        /// </summary>
        /// <returns>An <see cref="IEnumerable{T}"/> containing the root objects.</returns>
        public IEnumerable<SerializableObject> GetRootObjects() => GetChildren(RootObjectId);

        /// <summary>
        /// Gets the next free object id for this schematic.
        /// </summary>
        /// <returns>The next free object id.</returns>
        public int GetNextObjectId()
        {
            int id = RootObjectId;

            foreach (SerializableObject obj in GetObjects())
                id = Math.Max(id, obj.ObjectId);

            return id + 1;
        }

        /// <summary>
        /// Determines whether this schematic contains an object with the given object id.
        /// </summary>
        /// <param name="objectId">The object id to find.</param>
        /// <returns><see langword="true"/> if the object exists.</returns>
        public bool Contains(int objectId) => GetObject(objectId) != null;
    }
}