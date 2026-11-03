// -----------------------------------------------------------------------
// <copyright file="ServerObject.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU Lesser General Public License v3.0 (LGPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using LabApi.Features.Wrappers;
using MapGeneration.Distributors;
using Mirror;
using System;
using System.Collections.Generic;
using ThaumielMapEditor.API.Components.Tools;
using ThaumielMapEditor.API.Data;
using ThaumielMapEditor.API.Enums;
using ThaumielMapEditor.API.Helpers;
using ThaumielMapEditor.API.Serialization;
using ThaumielMapEditor.Events.EventArgs.Handlers;
using UnityEngine;

namespace ThaumielMapEditor.API.Blocks
{
    [GitBookPage("Components/Tools/ServerObject")]
    public class ServerObject : ObjectBase
    {
        /// <summary>
        /// Gets every <see cref="ServerObject"/> that is currently spawned.
        /// </summary>
        public static List<ServerObject> SpawnedObjects { get; set; } = [];

        /// <summary>
        /// Fired when a <see cref="ServerObject"/> is spawned into the server.
        /// </summary>
        public static event Action<ServerObject>? OnObjectCreated;

        /// <summary>
        /// Fired when a <see cref="ServerObject"/> is being destroyed and removed from the server.
        /// </summary>
        public static event Action<ServerObject>? OnObjectDestroying;

        /// <summary>
        /// Fired when a <see cref="ServerObject"/> has its world transform updated.
        /// </summary>
        public static event Action<ServerObject, bool>? OnObjectUpdated;

        /// <summary>
        /// Gets the <see cref="ToolBase"/> tools that are attached to this <see cref="ServerObject"/>.
        /// </summary>
        public IEnumerable<ToolBase> Tools { get; internal set; } = [];

        /// <summary>
        /// Gets or sets the associated game object instance.
        /// </summary>
        public virtual GameObject? Object { get; internal set; }

        /// <summary>
        /// Gets a value indicating whether this object should be parented to the transform of the <see cref="SerializableObject.ParentId"/> after it has been spawned.
        /// </summary>
        public virtual bool ParentToSpawnTransform => false;

        /// <summary>
        /// Gets the value returned by the loader after spawning this object.
        /// </summary>
        public virtual uint SpawnResult => NetId;

        private AdminToy? adminToy;

        private StructurePositionSync? positionSync;

        /// <summary>
        /// Gets or sets the position of the object.
        /// </summary>
        public override Vector3 Position
        {
            get => base.Position;
            set
            {
                if (base.Position == value)
                    return;

                base.Position = value;
                if (Object != null)
                {
                    Object.transform.position = value;
                    MarkSyncNeeded(SyncFlags.Position);
                }

                positionSync?.Network_position = base.Position;
            }
        }

        /// <summary>
        /// Gets or sets the scale of the object.
        /// </summary>
        public override Vector3 Scale
        {
            get => base.Scale;
            set
            {
                if (base.Scale == value)
                    return;

                base.Scale = value;
                if (Object != null)
                {
                    Object.transform.localScale = value;
                    MarkSyncNeeded(SyncFlags.Scale);
                }
            }
        }

        /// <summary>
        /// Gets or sets the rotation of the object.
        /// </summary>
        public override Quaternion Rotation
        {
            get => base.Rotation;
            set
            {
                if (base.Rotation == value)
                    return;

                base.Rotation = value;
                if (Object != null)
                {
                    Object.transform.rotation = value;
                    MarkSyncNeeded(SyncFlags.Rotation);
                }

                positionSync?.Network_rotationY = (sbyte)Mathf.RoundToInt(base.Rotation.eulerAngles.y / 5.625f);
            }
        }

        /// <summary>
        /// Gets or sets how many ticks the server waits between movement updates.
        /// </summary>
        public override byte MovementSmoothing
        {
            get => base.MovementSmoothing;
            set
            {
                if (base.MovementSmoothing == value)
                    return;

                base.MovementSmoothing = value;

                if (Object == null)
                    return;

                adminToy ??= Object.GetComponent<AdminToy>();
                if (adminToy == null)
                    return;

                adminToy.MovementSmoothing = value;
                MarkSyncNeeded(SyncFlags.MovementSmoothing);
            }
        }

        /// <inheritdoc/>
        protected override void RegisterForSync() => SyncManager.RegisterForSync(this);

        /// <summary>
        /// Applies the specified schematic's transformation to the current object's world position, rotation, and scale.
        /// </summary>
        /// <param name="schematic">The schematic data to apply.</param>
        public void SetWorldTransform(SchematicData schematic)
        {
            Position = schematic.Primitive?.Transform.TransformPoint(Position) ?? schematic.Position + (schematic.Rotation * Vector3.Scale(Position, schematic.Scale));
            if (schematic.Rotation.eulerAngles != default)
                Rotation = schematic.Rotation * Rotation;

            Scale = Vector3.Scale(Scale, schematic.Scale);
        }

        /// <summary>
        /// Spawns this object into the server and registers it with the schematic it belongs to.
        /// </summary>
        /// <param name="schematic">The schematic this object belongs to.</param>
        /// <param name="serializable">The serialized data this object was created from.</param>
        public virtual void SpawnObject(SchematicData schematic, SerializableObject serializable)
        {
            OnObjectCreated?.Invoke(this);
            ObjectHandler.OnServerObjectSpawned(new(this));
            SpawnedObjects.Add(this);
            schematic.SpawnedServerObjects.Add(this);
            Name = serializable.Name;
            ObjectId = serializable.ObjectId;
            ParentId = serializable.ParentId;
        }

        /// <summary>
        /// Called by the loader once the object has been spawned and fully registered.
        /// </summary>
        /// <param name="schematic">The schematic this object belongs to.</param>
        public virtual void OnSpawned(SchematicData schematic)
        {
        }

        /// <summary>
        /// Updates the object's world transform and network state based on the specified schematic data.
        /// </summary>
        /// <param name="schematic">The schematic data used to update the object's position and rotation.</param>
        /// <param name="respawn">Whether the object should be respawned on the network.</param>
        public void UpdateObject(SchematicData schematic, bool respawn = true)
        {
            if (Object == null)
            {
                LogManager.Warn($"Failed to update Object. Object is null.");
                return;
            }

            if (respawn)
                NetworkServer.UnSpawn(Object);

            positionSync ??= Object.GetComponent<StructurePositionSync>();

            SetWorldTransform(schematic);

            OnObjectUpdated?.Invoke(this, respawn);

            if (respawn && Object != null)
            {
                NetworkServer.Spawn(Object);
            }
            else
                MarkSyncNeeded(SyncFlags.Position | SyncFlags.Rotation | SyncFlags.Scale);
        }

        /// <summary>
        /// Destroys the associated networked object and removes it from the schematic's collection.
        /// </summary>
        /// <param name="schematic">The schematic to remove this object from.</param>
        public virtual void DestroyObject(SchematicData schematic)
        {
            DrawableLinesHelper.StopDraw(this, null);
            OnObjectDestroying?.Invoke(this);
            ObjectHandler.OnServerObjectDestroyed(new(this));
            schematic.SpawnedServerObjects.Remove(this);
            SpawnedObjects.Remove(this);

            if (Object == null)
                return;

            NetworkServer.Destroy(Object);
        }

        /// <summary>
        /// Hides this object on the specified client.
        /// </summary>
        /// <param name="player">The player to hide the object for.</param>
        public void HideForPlayer(Player player)
        {
            if (Object == null)
                return;

            if (!Object.TryGetComponent<NetworkIdentity>(out var network))
                return;

            NetworkServer.HideForConnection(network, player.Connection);
        }

        /// <summary>
        /// Shows this object on the specified client.
        /// </summary>
        /// <param name="player">The player to show the object for.</param>
        public void ShowForPlayer(Player player)
        {
            if (Object == null || player == null || player.IsDestroyed)
                return;

            if (player.Connection == null || !player.Connection.isReady)
                return;

            if (!Object.TryGetComponent<NetworkIdentity>(out var network))
                return;

            if (network.observers.ContainsKey(player.Connection.connectionId))
                return;

            NetworkServer.ShowForConnection(network, player.Connection);
        }
    }
}