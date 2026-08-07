// -----------------------------------------------------------------------
// <copyright file="BlockBase.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using System.Collections;
using System.Reflection;
using LabApi.Features.Wrappers;
using ThaumielMapEditor.API.Data;

namespace ThaumielMapEditor.API.Helpers.BlockParser
{
    public abstract class BlockBase
    {
        public BlockExecutor? Executor { get; internal set; }

        /// <summary>
        /// Assigns the executor to this block and recursively to any nested blocks exposed through its properties.
        /// </summary>
        internal void SetExecutorRecursive(BlockExecutor executor)
        {
            Executor = executor;

            foreach (PropertyInfo property in GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!property.CanRead)
                    continue;

                object? value = property.GetValue(this);

                if (value is BlockBase block)
                {
                    block.SetExecutorRecursive(executor);
                }
                else if (value is IEnumerable enumerable && value is not string)
                {
                    foreach (object? item in enumerable)
                    {
                        if (item is BlockBase inner)
                            inner.SetExecutorRecursive(executor);
                    }
                }
            }
        }

        public virtual void Execute()
        {

        }

        public virtual void Execute(Player player)
        {

        }

        public virtual void Execute(object obj)
        {

        }

        public virtual object ReturnExecute()
        {
            return null!;
        }

        public virtual object ReturnExecute(object obj)
        {
            return null!;
        }

        public virtual object ReturnExecute(SchematicData schematic)
        {
            return null!;
        }

        /// <summary>
        /// Resolves a value to a float. If the value is a <see cref="BlockBase"/>, its <see cref="ReturnExecute()"/> is called first.
        /// </summary>
        protected static float ResolveFloat(object? value, float defaultVal = 0f)
        {
            if (value is BlockBase block)
                value = block.ReturnExecute();
 
            if (value is float f)
                return f;
 
            if (value is double d)
                return (float)d;
 
            if (value is int i)
                return i;
 
            if (float.TryParse(value?.ToString(), out float parsed))
                return parsed;
 
            return defaultVal;
        }
    }
}