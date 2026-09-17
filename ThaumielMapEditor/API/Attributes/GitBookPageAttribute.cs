// -----------------------------------------------------------------------
// <copyright file="GitBookPageAttribute.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThaumielMapEditor.API.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Struct | AttributeTargets.Enum)]
    internal class GitBookPageAttribute : Attribute
    {
        public string Path { get; }

        public GitBookPageAttribute(string pagepath)
        {
            Path = pagepath;
        }
    }
}