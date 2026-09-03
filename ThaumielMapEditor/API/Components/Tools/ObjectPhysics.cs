// -----------------------------------------------------------------------
// <copyright file="ObjectPhysics.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using ThaumielMapEditor.API.Blocks;
using ThaumielMapEditor.API.Data;
using ThaumielMapEditor.API.Enums;
using UnityEngine;
using YamlDotNet.Serialization;

namespace ThaumielMapEditor.API.Components.Tools
{
    public class ObjectPhysics : ToolBase
    {
        /// <inheritdoc/>
        public override ToolType Type => ToolType.Physics;

        /// <summary>
        /// Gets the <see cref="UnityEngine.Rigidbody"/> for this <see cref="ObjectPhysics"/> instance.
        /// Null until <see cref="SetupRigidbody()"/> is called by <see cref="Init(ServerObject, SchematicData, Dictionary{string, object})"/>.
        /// </summary>
        public Rigidbody? Rigidbody { get; private set; }

        /// <summary>
        /// Gets or sets the mass for the <see cref="Rigidbody"/>.
        /// </summary>
        [YamlMember(Alias = "Weight")]
        public float Weight
        {
            get;
            set
            {
                Rigidbody?.mass = value;
                field = value;
            }
        }

        /// <summary>
        /// Gets or sets the drag for the <see cref="Rigidbody"/>.
        /// </summary>
        [YamlMember(Alias = "Drag")]
        public float Drag
        {
            get;
            set
            {
                Rigidbody?.linearDamping = value;
                field = value;
            }
        }

        /// <summary>
        /// Gets or sets the rotation resistance for the <see cref="Rigidbody"/>.
        /// </summary>
        [YamlMember(Alias = "AngularDrag")]
        public float AngularDrag
        {
            get;
            set
            {
                Rigidbody?.angularDamping = value;
                field = value;
            }
        }

        /// <summary>
        /// Gets or sets the <see cref="CollisionDetectionMode"/> for the <see cref="Rigidbody"/>.
        /// </summary>
        [YamlMember(Alias = "CollisionMode")]
        public CollisionDetectionMode CollisionMode
        {
            get;
            set
            {
                Rigidbody?.collisionDetectionMode = value;
                field = value;
            }
        }

        /// <summary>
        /// Gets or sets whether the <see cref="Rigidbody"/> applies physics or not
        /// </summary>
        [YamlMember(Alias = "Enabled")]
        public bool Enabled
        {
            get;
            set
            {
                Rigidbody?.isKinematic = !value;
                field = value;
            }
        }

        /// <inheritdoc/>
        public override void Init(ServerObject obj, SchematicData schem, Dictionary<string, object> properties)
        {
            base.Init(obj, schem, properties);
            SetupRigidbody();
        }

        /// <summary>
        /// Adds the specified force to the <see cref="Rigidbody"/>
        /// </summary>
        /// <param name="force">The amount of <see cref="Vector3"/> force to add.</param>
        /// <param name="mode">The <see cref="ForceMode"/> that will be used.</param>
        public void AddForce(Vector3 force, ForceMode mode = ForceMode.Impulse)
        {
            if (Rigidbody == null || !Enabled)
                return;

            Rigidbody.AddForce(force, mode);
        }

        private void SetupRigidbody()
        {
            if (!TryGetComponent<Rigidbody>(out var body))
                body = gameObject.AddComponent<Rigidbody>();

            Rigidbody = body;
            Rigidbody.mass = Weight;
            Rigidbody.isKinematic = !Enabled;
            Rigidbody.linearDamping = Drag;
            Rigidbody.angularDamping = AngularDrag;
            Rigidbody.collisionDetectionMode = CollisionMode;
        }
    }
}