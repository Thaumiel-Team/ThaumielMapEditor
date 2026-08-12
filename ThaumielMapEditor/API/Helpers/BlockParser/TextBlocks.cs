// -----------------------------------------------------------------------
// <copyright file="TextBlocks.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using System.Text;

namespace ThaumielMapEditor.API.Helpers.BlockParser
{
    public class TextLengthBlock : BlockBase
    {
        public object? Text { get; set; }

        public override object ReturnExecute()
            => ResolveString(Text).Length;
    }

    public class TextJoinBlock : BlockBase
    {
        public List<string> Strings { get; set; } = [];

        public override object ReturnExecute()
        {
            StringBuilder sb = new();

            foreach (string text in Strings)
            {
                sb.AppendLine(text);
            }

            return sb.ToString();
        }
    }
}