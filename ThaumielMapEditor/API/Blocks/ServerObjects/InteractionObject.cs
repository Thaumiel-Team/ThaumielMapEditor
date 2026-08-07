// -----------------------------------------------------------------------
// <copyright file="InteractionObject.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using AdminToys;
using Mirror;
using ThaumielMapEditor.API.Data;
using ThaumielMapEditor.API.Enums;
using ThaumielMapEditor.API.Extensions;
using ThaumielMapEditor.API.Helpers;
using ThaumielMapEditor.API.Serialization;
using static AdminToys.InvisibleInteractableToy;
using System;
using LabApi.Features.Wrappers;
using static ThaumielMapEditor.API.Extensions.PlayerExtensions;
using YamlDotNet.Serialization;
using Interactables.Interobjects.DoorUtils;
using System.Linq;
using ThaumielMapEditor.Events.EventArgs.Handlers;
using PlayerRoles;
using System.Collections.Generic;

namespace ThaumielMapEditor.API.Blocks.ServerObjects
{
    public class InteractionObject : ServerObject
    {
        private static readonly Dictionary<InvisibleInteractableToy, InteractionObject> InteractionCache = [];

        public static event Action<InteractionObject, Player>? OnInteracted;
        public static event Action<InteractionObject, Player>? OnSearching;
        public static event Action<InteractionObject, Player>? OnSearched;
        public static event Action<InteractionObject, Player>? OnSearchAborted;

        /// <summary>
        /// The runtime instance of the underlying <see cref="InvisibleInteractableToy"/>.
        /// </summary>
        [YamlIgnore]
#pragma warning disable CS8618
        public InvisibleInteractableToy Base { get; private set; }
#pragma warning restore CS8618

        public override ObjectType ObjectType { get; set; } = ObjectType.Interactable;

        [YamlMember(Alias = "Shape")]
        public ColliderShape Shape
        {
            get;
            set
            {
                if (field == value) return;
                field = value;
                Base?.Shape = value;
            }
        }

        [YamlMember(Alias = "Duration")]
        public float InteractionDuration
        {
            get;
            set
            {
                if (field == value) return;
                field = value;
                Base?.InteractionDuration = value;
            }
        }

        [YamlMember(Alias = "Locked")]
        public bool IsLocked
        {
            get;
            set
            {
                if (field == value) return;
                field = value;
                Base?.IsLocked = value;
            }
        }

        [YamlMember(Alias = "Permissions")]
        public DoorPermissionFlags Permissions { get; set; }

        public override Vector3 Scale
        {
            get;
            set
            {
                field = value;
                Base?.Scale = value;
            }
        }

        [YamlIgnore]
        public bool CanSearch => Base?.CanSearch ?? false;

        [YamlMember(Alias = "AllowedRoles")]
        public List<RoleTypeId> AllowedRoles { get; set; } = [];

        public override void SpawnObject(SchematicData schematic, SerializableObject serializable)
        {
            if (PrefabHelper.Interactable == null)
            {
                LogManager.Warn($"Failed to spawn Interact Object. Prefab is null.");
                return;
            }

            InvisibleInteractableToy toy = UnityEngine.Object.Instantiate(PrefabHelper.Interactable);
            NetworkServer.UnSpawn(toy.gameObject);
            Base = toy;
            Object = toy.gameObject;
            SetWorldTransform(schematic);
            Base?.Shape = Shape;
            Base?.InteractionDuration = InteractionDuration;
            Base?.IsLocked = IsLocked;
            Base?.Scale = Scale;
            NetworkServer.Spawn(toy.gameObject);
            NetId = toy.netId;

            toy.OnInteracted += HandleInteracted;
            toy.OnSearching += HandleSearching;
            toy.OnSearched += HandleSearched;
            toy.OnSearchAborted += HandleSearchAborted;
            InteractionCache[toy] = this;
            base.SpawnObject(schematic, serializable);
        }

        public void SpawnObject(SchematicData schematic)
        {
            if (PrefabHelper.Interactable == null)
            {
                LogManager.Warn($"Failed to spawn Interact Object. Prefab is null.");
                return;
            }

            InvisibleInteractableToy toy = UnityEngine.Object.Instantiate(PrefabHelper.Interactable);
            NetworkServer.UnSpawn(toy.gameObject);
            Base = toy;
            Object = toy.gameObject;
            SetWorldTransform(schematic);
            Base.Shape = Shape;
            Base.InteractionDuration = InteractionDuration;
            Base.IsLocked = IsLocked;
            Base.Scale = Scale;
            NetworkServer.Spawn(toy.gameObject);
            NetId = toy.netId;

            toy.OnInteracted += HandleInteracted;
            toy.OnSearching += HandleSearching;
            toy.OnSearched += HandleSearched;
            toy.OnSearchAborted += HandleSearchAborted;
            InteractionCache[toy] = this;
            schematic.SpawnedServerObjects.Add(this);
            ObjectHandler.OnServerObjectSpawned(new(this));
            SpawnedObjects.Add(this);
        }

        /// <inheritdoc/>
        public override void DestroyObject(SchematicData schematic)
        {
            if (Base != null)
            {
                InteractionCache.Remove(Base);
                Base.OnInteracted -= HandleInteracted;
                Base.OnSearching -= HandleSearching;
                Base.OnSearched -= HandleSearched;
                Base.OnSearchAborted -= HandleSearchAborted;
            }

            base.DestroyObject(schematic);
        }

        private void HandleInteracted(ReferenceHub hub)
        {
            if (!hub.TryGet(out var player))
                return;

            OnInteracted?.Invoke(this, player);
        }

        private void HandleSearching(ReferenceHub hub)
        {
            if (!hub.TryGet(out var player))
                return;

            OnSearching?.Invoke(this, player);
        }

        private void HandleSearched(ReferenceHub hub)
        {
            if (!hub.TryGet(out var player))
                return;

            OnSearched?.Invoke(this, player);
        }

        private void HandleSearchAborted(ReferenceHub hub)
        {
            if (!hub.TryGet(out var player))
                return;

            OnSearchAborted?.Invoke(this, player);
        }

        public static bool TryGetInteractionObject(InvisibleInteractableToy toy, out InteractionObject interactionobj)
            => InteractionCache.TryGetValue(toy, out interactionobj!);
    }
}