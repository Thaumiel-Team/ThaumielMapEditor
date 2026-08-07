// -----------------------------------------------------------------------
// <copyright file="TextToyObject.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using AdminToys;
using Mirror;
using ThaumielMapEditor.API.Data;
using ThaumielMapEditor.API.Enums;
using ThaumielMapEditor.API.Helpers;
using ThaumielMapEditor.API.Serialization;
using UnityEngine;
using YamlDotNet.Serialization;

namespace ThaumielMapEditor.API.Blocks.ServerObjects
{
    public class TextToyObject : ServerObject
    {
        /// <summary>
        /// The instantiated runtime <see cref="TextToy"/> associated with this server object.
        /// </summary>
        /// <remarks>
        /// It will be null until <see cref="SpawnObject(SchematicData, SerializableObject)"/> successfully instantiates the prefab.
        /// </remarks>
        [YamlIgnore]
#pragma warning disable CS8618
        public TextToy Base { get; private set; }
#pragma warning restore CS8618

        /// <summary>
        /// The text format string used by the <see cref="TextToy"/> for rendering text.
        /// </summary>
        /// <remarks>
        /// Setting this property updates the underlying <see cref="Base"/> instance's <c>TextFormat</c> if the
        /// runtime object has already been created.
        /// </remarks>
        [YamlMember(Alias = "Text")]
        public string Text
        {
            get;
            set
            {
                if (field == value)
                    return;

                field = value;
                Base?.TextFormat = value;
            }
        } = string.Empty;

        /// <summary>
        /// The display size (width, height) used by the <see cref="TextToy"/> when rendering text.
        /// </summary>
        /// <remarks>
        /// Setting this property updates the underlying <see cref="Base"/> instance's <c>DisplaySize</c> if the
        /// runtime object has already been created.
        /// </remarks>
        [YamlMember(Alias = "DisplaySize")]
        public Vector2 DisplaySize
        {
            get;
            set
            {
                if (field == value)
                    return;

                field = value;
                Base?.DisplaySize = value;
            }
        }

        /// <inheritdoc/>
        public override ObjectType ObjectType { get; set; } = ObjectType.TextToy;

        /// <inheritdoc/>
        public override void SpawnObject(SchematicData schematic, SerializableObject serializable)
        {
            if (PrefabHelper.TextToy == null)
            {
                LogManager.Warn($"Failed to spawn TextToy. Prefab is null.");
                return;
            }

            TextToy textToy = UnityEngine.Object.Instantiate(PrefabHelper.TextToy);
            Base = textToy;
            Object = textToy.gameObject;
            NetworkServer.UnSpawn(Base.gameObject);
            SetWorldTransform(schematic);

            Base.TextFormat = Text;
            Base.DisplaySize = DisplaySize;
            NetworkServer.Spawn(Base.gameObject);
            NetId = textToy.netId;
            base.SpawnObject(schematic, serializable);
        }

        public void SpawnObject(SchematicData schematic)
        {
            if (PrefabHelper.TextToy == null)
            {
                LogManager.Warn($"Failed to spawn TextToy. Prefab is null.");
                return;
            }

            TextToy textToy = UnityEngine.Object.Instantiate(PrefabHelper.TextToy);
            Base = textToy;
            Object = textToy.gameObject;
            NetworkServer.UnSpawn(Base.gameObject);
            SetWorldTransform(schematic);

            Base.TextFormat = Text;
            Base.DisplaySize = DisplaySize;
            NetworkServer.Spawn(Base.gameObject);
            NetId = textToy.netId;
        }
    }
}