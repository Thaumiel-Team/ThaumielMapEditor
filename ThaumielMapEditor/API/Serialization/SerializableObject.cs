// -----------------------------------------------------------------------
// <copyright file="SerializableObject.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU Lesser General Public License v3.0 (LGPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using ThaumielMapEditor.API.Enums;
using ThaumielMapEditor.API.Extensions;

namespace ThaumielMapEditor.API.Serialization
{
    /// <summary>
    /// This class is used to read object data from yaml
    /// </summary>

    [GitBookPage("Serialization/SerializableObject")]
    public class SerializableObject
    {
        /// <summary>
        /// Gets or sets the object id for the <see cref="SerializableObject"/> instance.
        /// </summary>
        public int ObjectId { get; set; }

        /// <summary>
        /// Gets or sets the parent id for the <see cref="SerializableObject"/> instance.
        /// </summary>
        public int ParentId { get; set; }

        /// <summary>
        /// Gets or sets the file name for the <see cref="SerializableObject"/> instance.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the position for the <see cref="SerializableObject"/> instance.
        /// </summary>
        public Vector3 Position { get; set; }

        /// <summary>
        /// Gets or sets the scale for the <see cref="SerializableObject"/> instance.
        /// </summary>
        public Vector3 Scale { get; set; }

        /// <summary>
        /// Gets or sets the rotation for the <see cref="SerializableObject"/> instance.
        /// </summary>
        public Quaternion Rotation { get; set; }

        /// <summary>
        /// Gets or sets whether the <see cref="SerializableObject"/> instance is static.
        /// </summary>
        public bool IsStatic { get; set; }

        /// <summary>
        /// Gets or sets the sync interval for the <see cref="SerializableObject"/> instance.
        /// </summary>
        public byte MovementSmoothing { get; set; }

        /// <summary>
        /// Gets or sets the object type for the <see cref="SerializableObject"/> instance.
        /// </summary>
        public ObjectType ObjectType { get; set; }

        /// <summary>
        /// Gets or sets the values for the <see cref="SerializableObject"/> instance.
        /// </summary>
        public Dictionary<string, object> Values
        {
            get;
            set => field = value ?? [];
        } = [];

        public List<SerializableTool> Tools
        {
            get;
            set => field = value ?? [];
        } = [];

        public string AnimatorName { get; set; } = string.Empty;

        /// <summary>
        /// Gets a value from <see cref="Values"/> and converts it to the specified type.
        /// </summary>
        /// <typeparam name="T">The type to convert the value to.</typeparam>
        /// <param name="key">The key to look up.</param>
        /// <returns>The converted value.</returns>
        public T GetValue<T>(string key) => Values.GetConvertValue<T>(key);

        /// <summary>
        /// Tries to get a value from <see cref="Values"/> and convert it to the specified type.
        /// </summary>
        /// <typeparam name="T">The type to convert the value to.</typeparam>
        /// <param name="key">The key to look up.</param>
        /// <param name="value">The converted value.</param>
        /// <returns><see langword="true"/> if the value was found and converted.</returns>
        public bool TryGetValue<T>(string key, out T value) => Values.TryConvertValue(key, out value);

        /// <summary>
        /// Gets a value from <see cref="Values"/> or returns the given default.
        /// </summary>
        /// <typeparam name="T">The type to convert the value to.</typeparam>
        /// <param name="key">The key to look up.</param>
        /// <param name="defaultValue">The value to return when the key is missing.</param>
        /// <returns>The converted value or the default.</returns>
        public T GetValueOrDefault<T>(string key, T defaultValue = default!) =>
            Values.TryConvertValue(key, out T value) ? value : defaultValue;

        /// <summary>
        /// Determines whether <see cref="Values"/> contains the given key.
        /// </summary>
        /// <param name="key">The key to look up.</param>
        /// <returns><see langword="true"/> if the key exists.</returns>
        public bool HasValue(string key) => Values.ContainsKey(key);

        /// <summary>
        /// Stores a value in <see cref="Values"/>.
        /// </summary>
        /// <param name="key">The key to store the value under.</param>
        /// <param name="value">The value to store.</param>
        public void SetValue(string key, object value) => Values[key] = value;

        /// <summary>
        /// Removes a value from <see cref="Values"/>.
        /// </summary>
        /// <param name="key">The key to remove.</param>
        /// <returns><see langword="true"/> if the value was removed.</returns>
        public bool RemoveValue(string key) => Values.Remove(key);
    }
}