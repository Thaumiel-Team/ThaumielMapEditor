// -----------------------------------------------------------------------
// <copyright file="SilentCommandSender.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU Lesser General Public License v3.0 (LGPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

namespace ThaumielMapEditor.Commands
{
    public class SilentCommandSender : CommandSender
    {
        public override string SenderId => "ThaumielMapEditor";

        public override string Nickname => "ThaumielMapEditor";

        public override ulong Permissions => ulong.MaxValue;

        public override byte KickPower => byte.MaxValue;
        
        public override bool FullPermissions => true;

        public override void RaReply(string text, bool success, bool logToConsole, string overrideDisplay) { }

        public override void Print(string text) { }

        public override bool Available() => true;

        public override void Respond(string message, bool success = true) { }
    }
}