// -----------------------------------------------------------------------
// <copyright file="CapybaraObjectServer.cs" company="Thaumiel Team">
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
using YamlDotNet.Serialization;

namespace ThaumielMapEditor.API.Blocks.ServerObjects
{
    public class CapybaraObjectServer : ServerObject
    {
#pragma warning disable CS8618
        public CapybaraToy Base { get; private set; }
#pragma warning restore CS8618

        [YamlMember(Alias = "Collisions")]
        public bool CollisionsEnabled
        {
            get;
            set
            {
                if (field == value)
                    return;

                field = value;
                Base.CollisionsEnabled = value;
            }
        } = true;

        /// <inheritdoc/>
        public override ObjectType ObjectType { get; set; } = ObjectType.Capybara;

        /// <inheritdoc/>
        public override void SpawnObject(SchematicData schematic, SerializableObject serializable)
        {
            CapybaraToy? capybara = UnityEngine.Object.Instantiate(PrefabHelper.Capybara);
            if (capybara == null)
                return;

            NetworkServer.UnSpawn(capybara.gameObject);
            Base = capybara;
            Object = capybara.gameObject;
            capybara.CollisionsEnabled = CollisionsEnabled;
            capybara.gameObject.transform.position = Position;
            capybara.gameObject.transform.rotation = Rotation;
            capybara.gameObject.transform.localScale = Scale;
            NetworkServer.Spawn(capybara.gameObject);
            NetId = capybara.netId;
            base.SpawnObject(schematic, serializable);
        }
    }
}