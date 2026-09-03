// -----------------------------------------------------------------------
// <copyright file="ObjectHealth.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using MEC;
using ThaumielMapEditor.API.Blocks;
using ThaumielMapEditor.API.Data;
using ThaumielMapEditor.API.Helpers;
using ThaumielMapEditor.API.Extensions;
using UnityEngine;
using ThaumielMapEditor.API.Enums;
using PlayerStatsSystem;
using PlayerRoles.PlayableScps.Scp939;
using InventorySystem.Items.Scp1509;
using PlayerRoles.PlayableScps.Scp1507;
using System.Linq;
using ThaumielMapEditor.API.Serialization;
using YamlDotNet.Serialization;

namespace ThaumielMapEditor.API.Components.Tools
{
    public class ObjectHealth : ToolBase, IDestructible
    {
        /// <summary>
        /// Defines the types that <see cref="ObjectHealth"/> will use.
        /// </summary>
        public enum DestroyState
        {
            Animate,
            ApplyPhysics,
            Destroy
        }

        /// <inheritdoc/>
        public override ToolType Type => ToolType.Health;

        /// <summary>
        /// Gets the <see cref="DestroyState"/> that the <see cref="ObjectHealth"/> instance will use when destroyed.
        /// </summary>
        [YamlMember(Alias = "State")]
        public DestroyState State { get; set; } = DestroyState.Destroy;

        /// <summary>
        /// Gets or sets the max health that the <see cref="ObjectHealth"/> instance has.
        /// </summary>
        [YamlMember(Alias = "MaxHealth")]
        public float HealthMax { get; set; } = 100f;

        private bool _destroyed;

        private static readonly DamageType[] FirearmDamageTypes = [DamageType.Shot];
        private static readonly DamageType[] ExplosionDamageTypes = [DamageType.Explosion];
        private static readonly DamageType[] Scp939DamageTypes = [DamageType.Scp939Lunge, DamageType.Scp939Swipe];
        private static readonly DamageType[] Scp096DamageTypes = [DamageType.Scp096Charge, DamageType.Scp096Swipe];
        private static readonly DamageType[] JailbirdDamageTypes = [DamageType.JailbirdCharge, DamageType.JailbirdHit];
        private static readonly DamageType[] DisruptorDamageTypes = [DamageType.DisruptorBurst, DamageType.DisruptorCharge];
        private static readonly DamageType[] MicroHidDamageTypes = [DamageType.MicroHidQuick, DamageType.MicroHidFullCharge, DamageType.MicroHidBroken];
        private static readonly DamageType[] Scp1509DamageTypes = [DamageType.Scp1509];
        private static readonly DamageType[] MarshmallowDamageTypes = [DamageType.Marshmallow];
        private static readonly DamageType[] Scp1507DamageTypes = [DamageType.Scp1507];
        private static readonly DamageType[] NoDamageTypes = [];

        /// <summary>
        /// Gets or sets the current health that the <see cref="ObjectHealth"/> instance has.
        /// </summary>
        public float Health
        {
            get;
            set
            {
                field = Mathf.Max(0f, value);
                if (field <= 0 && !_destroyed)
                    Destroy();
            }
        } = 100f;

        /// <summary>
        /// Gets or sets the allowed <see cref="DamageType"/>s that the <see cref="ObjectHealth"/> instance can be damaged by.
        /// </summary>
        [YamlMember(Alias = "AllowedDamage")]
        public List<DamageType> AllowedDamage { get; set; } = [];

        /// <summary>
        /// Gets or sets the amount of time in seconds that the <see cref="ObjectHealth"/> instance will have untill it despawns after being destroyed.
        /// </summary>
        [YamlMember(Alias = "Despawn")]
        public float DespawnTime { get; set; } = 5f;

        /// <summary>
        /// Gets or sets the animation name that the <see cref="ObjectHealth"/> instance will play if the <see cref="State"/> is <see cref="DestroyState.Animate"/>.
        /// </summary>
        [YamlMember(Alias = "StateName")]
        public string StateName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the launch force of the <see cref="ObjectHealth"/> instance that will be applied to the <see cref="Rigidbody"/> if the <see cref="State"/> is <see cref="DestroyState.ApplyPhysics"/>.
        /// </summary>
        public Vector3 Force { get; set; } = Vector3.zero;

        /// <summary>
        /// Gets the NetworkId.
        /// Required by <see cref="IDestructible"/>
        /// </summary>
        public uint NetworkId => Object!.NetId;

        /// <summary>
        /// Gets the center of mass of the <see cref="ObjectHealth"/> instance.
        /// </summary>
        public Vector3 CenterOfMass => Vector3.zero;

        /// <inheritdoc/>
        public override void Init(ServerObject obj, SchematicData schem, Dictionary<string, object> properties)
        {
            base.Init(obj, schem, properties);
            Health = HealthMax;
        }

        /// <summary>
        /// Destroys the <see cref="ObjectHealth"/> instance and applies the destroy state from <see cref="State"/>
        /// </summary>
        public void Destroy()
        {
            if (_destroyed)
                return;

            _destroyed = true;

            switch (State)
            {
                case DestroyState.Animate:
                    Schematic?.AnimationController.Play(StateName, Object!.Name);
                    Timing.CallDelayed(DespawnTime, () => Object?.DestroyObject(Schematic!));
                    break;

                case DestroyState.ApplyPhysics:
                    Object?.MovementSmoothing = 0;
                    if (!gameObject.TryGetComponent<Rigidbody>(out var rigidbody))
                        rigidbody = gameObject.AddComponent<Rigidbody>();

                    rigidbody.isKinematic = false;
                    rigidbody.collisionDetectionMode = CollisionDetectionMode.Continuous;
                    if (Force != Vector3.zero)
                        rigidbody.AddForce(Force, ForceMode.Impulse);

                    Timing.CallDelayed(DespawnTime, () => Object?.DestroyObject(Schematic!));
                    break;

                case DestroyState.Destroy:
                    Object?.DestroyObject(Schematic!);
                    break;
            }
        }

        /// <summary>
        /// Attempts to damage the <see cref="ObjectHealth"/> instance if the <paramref name="handler"/> matches an allowed <see cref="DamageType"/>.
        /// </summary>
        /// <param name="damage">The amount of health to subtract from <see cref="Health"/>.</param>
        /// <param name="handler">The source of the damage.</param>
        /// <param name="exactHitPos">The position of the hit.</param>
        /// <returns><see langword="true"/> if the damage was applied otherwise <see langword="false"/>.</returns>
        public bool Damage(float damage, DamageHandlerBase handler, Vector3 exactHitPos)
        {
            DamageType[] types = handler switch
            {
                FirearmDamageHandler => FirearmDamageTypes,
                ExplosionDamageHandler => ExplosionDamageTypes,
                Scp939DamageHandler => Scp939DamageTypes,
                Scp096DamageHandler => Scp096DamageTypes,
                JailbirdDamageHandler => JailbirdDamageTypes,
                DisruptorDamageHandler => DisruptorDamageTypes,
                MicroHidDamageHandler => MicroHidDamageTypes,
                Scp1509DamageHandler => Scp1509DamageTypes,
                MarshmallowDamageHandler => MarshmallowDamageTypes,
                Scp1507DamageHandler => Scp1507DamageTypes,
                _ => NoDamageTypes
            };

            bool allowed = false;
            for (int i = 0; i < types.Length; i++)
            {
                if (AllowedDamage.Contains(types[i]))
                {
                    allowed = true;
                    break;
                }
            }

            if (!allowed)
                return false;

            Health -= damage;
            return true;
        }
    }
}