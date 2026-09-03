// -----------------------------------------------------------------------
// <copyright file="ToolBase.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using ThaumielMapEditor.API.Blocks;
using ThaumielMapEditor.API.Data;
using ThaumielMapEditor.API.Enums;
using ThaumielMapEditor.API.Extensions;
using ThaumielMapEditor.API.Helpers;
using UnityEngine;
using YamlDotNet.Serialization;

namespace ThaumielMapEditor.API.Components.Tools
{
    public class ToolBase : MonoBehaviour
    {
        /// <summary>
        /// Gets the <see cref="ServerObject"/> for this <see cref="ToolBase"/> instance.
        /// Null until <see cref="Init(ServerObject, SchematicData, Dictionary{string, object})"/> is called.
        /// </summary>
        [YamlIgnore]
        public ServerObject? Object { get; internal set; }

        /// <summary>
        /// Gets the <see cref="ServerObject"/> for this <see cref="ToolBase"/> instance.
        /// Null until <see cref="Init(ServerObject, SchematicData, Dictionary{string, object})"/> is called.
        /// </summary>
        [YamlIgnore]
        public SchematicData? Schematic { get; internal set; }

        /// <summary>
        /// The <see cref="ToolType"/> this <see cref="ToolBase"/> instance uses. 
        /// </summary>
        [YamlIgnore]
        public virtual ToolType Type { get; }

        /// <summary>
        /// Initializes the component.
        /// </summary>
        /// <param name="obj">The <see cref="ServerObject"/> instance this was added to.</param>
        /// <param name="schem">The <see cref="SchematicData"/> that the <see cref="ServerObject"/> was spawned from.</param>
        /// <param name="properties">The serialized properties.</param>
        public virtual void Init(ServerObject obj, SchematicData schem, Dictionary<string, object> properties)
        {
            Object = obj;
            Schematic = schem;
            properties.PopulateFrom(this);
        }

        protected virtual void OnDestroy()
        {
            if (Object == null)
                return;

            Object.Tools = Object.Tools.Where(t => t != this).ToList();
        }

        public T? MapToObject<T>(object data)
        {
            if (data is T alreadyType)
                return alreadyType;

            try
            {
                return JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(data));
            }
            catch (Exception ex)
            {
                LogManager.Warn($"Failed to map object to {typeof(T).Name}: {ex.Message}");
                return default;
            }
        }

        public bool IsLocalFile(string path)
        {
            if (Path.IsPathRooted(path))
                return false;

            return true;
        }
    }
}