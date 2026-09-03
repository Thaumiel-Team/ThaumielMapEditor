// -----------------------------------------------------------------------
// <copyright file="TeleporterObject.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using DrawableLine;
using PlayerRoles;
using ThaumielMapEditor.API.Components;
using ThaumielMapEditor.API.Data;
using ThaumielMapEditor.API.Enums;
using ThaumielMapEditor.API.Serialization;
using UnityEngine;
using YamlDotNet.Serialization;

namespace ThaumielMapEditor.API.Blocks.ServerObjects
{
    public class TeleporterObject : ServerObject
    {
        private static readonly Dictionary<Guid, TeleporterObject> RegisteredTeleporters = [];

        /// <summary>
        /// Gets the <see cref="TeleporterObject"/>s currently registered by their <see cref="Id"/>.
        /// </summary>
        public static IReadOnlyDictionary<Guid, TeleporterObject> Teleporters => RegisteredTeleporters;

        /// <summary>
        /// Gets the Id for this <see cref="TeleporterObject"/> instance
        /// </summary>
        [YamlMember(Alias = "Id")]
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the Id to teleport to for this <see cref="TeleporterObject"/> instance
        /// </summary>
        [YamlMember(Alias = "Target")]
        public List<Guid> Targets { get; set; } = [];

        /// <summary>
        /// Gets or sets the cooldown time for this <see cref="TeleporterObject"/> instance
        /// </summary>
        [YamlMember(Alias = "CoolDown")]
        public float CoolDown { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="RoleTypeId"/>s allowed for this <see cref="TeleporterObject"/> instance
        /// </summary>
        [YamlMember(Alias = "AllowedRoles")]
        public List<RoleTypeId> AllowedRoles { get; set; } = [];

        /// <summary>
        /// Gets or sets whether this <see cref="TeleporterObject"/> instance uses perplayer cooldowns or global cooldowns.
        /// </summary>
        [YamlMember(Alias = "PerPlayerCooldown")]
        public bool PerPlayerCooldown { get; set; }

        /// <summary>
        /// Gets the <see cref="TeleporterFlags"/> of this <see cref="TeleporterObject"/> instance
        /// </summary>
        [YamlMember(Alias = "Flags")]
        public TeleporterFlags Flags { get; set; }

        /// <inheritdoc/>
        public override ObjectType ObjectType { get; set; } = ObjectType.Teleporter;

        [YamlIgnore]
#pragma warning disable CS8618
        public TeleporterHandler TeleporterHandler { get; private set; }
#pragma warning restore CS8618

        /// <inheritdoc/>
        public override void SpawnObject(SchematicData schematic, SerializableObject serializable)
        {
            SetWorldTransform(schematic);

            GameObject triggerObj = new($"Teleporter_{Id}");
            triggerObj.transform.position = Position;
            triggerObj.transform.rotation = Rotation;
            triggerObj.transform.localScale = Scale;

            BoxCollider collider = triggerObj.AddComponent<BoxCollider>();
            collider.isTrigger = true;

            TeleporterHandler = triggerObj.AddComponent<TeleporterHandler>();
            TeleporterHandler.Init(this);
            RegisteredTeleporters[Id] = this;

            base.SpawnObject(schematic, serializable);
        }

        /// <inheritdoc/>
        public override void DestroyObject(SchematicData schematic)
        {
            RegisteredTeleporters.Remove(Id);

            if (TeleporterHandler != null && TeleporterHandler.gameObject != null)
                UnityEngine.Object.Destroy(TeleporterHandler.gameObject);

            base.DestroyObject(schematic);
        }

        public void DrawLines()
        {
            if (TeleporterHandler.Collider == null)
                return;

            DrawableLines.GenerateBounds(TeleporterHandler.Collider.bounds);
        }
    }
}