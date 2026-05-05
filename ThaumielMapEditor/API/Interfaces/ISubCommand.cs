// -----------------------------------------------------------------------
// <copyright file="ISubCommand.cs" company="Thaumiel Team">
// Copyright (c) Thaumiel Team. All rights reserved.
// Licensed under the GNU General Public License v3.0 (GPL-3.0).
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using CommandSystem;
using ThaumielMapEditor.API.Data;

namespace ThaumielMapEditor.API.Interfaces
{
    public abstract class ISubCommand
    {
        public abstract string Name { get; }

        public abstract string RequiredPermission { get; }

        public abstract string Description { get; }

        public virtual string VisibleArgs => string.Empty;

        public virtual int RequiredArgsCount => 0;

        public virtual string[] Aliases => [];

        public virtual List<ISubCommand> SubCommands { get; set; } = [];

        public virtual void PopulateSubCommands() { }

        public virtual bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            response = string.Empty;
            return false;
        }

        public virtual bool SubCommandExecute(ArraySegment<string> arguments, ICommandSender sender, SchematicData schematic, StringBuilder sb, out string response)
        {
            response = string.Empty;
            return false;
        }

        public void RunInBackground(Action run, Action onComplete)
        {
            Task.Run(() =>
            {
                run.Invoke();
                MainThreadDispatcher.Dispatch(onComplete);
            });
        }

        public override string ToString() => Name;
    }
}