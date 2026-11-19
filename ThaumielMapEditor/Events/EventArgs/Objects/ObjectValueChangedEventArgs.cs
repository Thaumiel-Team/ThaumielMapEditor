// -----------------------------------------------------------------------
// <copyright file="ObjectValueChangedEventArgs.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU Lesser General Public License v3.0 (LGPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using ThaumielMapEditor.API.Blocks;

namespace ThaumielMapEditor.Events.EventArgs.Objects
{
    /// <summary>
    /// Invoked before a serialized value is written or removed.
    /// Set <see cref="CancellableEventArgs.IsAllowed"/> to false to cancel the change.
    /// </summary>
    [GitBookPage("Events/EventArgs/ObjectValueChangedEventArgs")]
    public class ObjectValueChangedEventArgs : CancellableEventArgs
    {
        /// <summary>
        /// Gets the object whose value is changing.
        /// </summary>
        public ObjectBase Object { get; }

        /// <summary>
        /// Gets the key of the value that is changing.
        /// </summary>
        public string Key { get; }

        /// <summary>
        /// Gets the value before the change.
        /// </summary>
        public object? OldValue { get; }

        /// <summary>
        /// Gets the value that will be stored.
        /// </summary>
        public object? NewValue { get; }

        /// <summary>
        /// Gets a value indicating whether the value is being removed.
        /// </summary>
        public bool IsRemoval { get; }

        public ObjectValueChangedEventArgs(ObjectBase @object, string key, object? oldValue, object? newValue, bool isRemoval)
        {
            Object = @object;
            Key = key;
            OldValue = oldValue;
            NewValue = newValue;
            IsRemoval = isRemoval;
        }
    }
}