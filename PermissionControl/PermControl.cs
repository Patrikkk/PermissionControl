using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;

namespace PermissionControl
{
    [ApiVersion(1,25)]
    public class PermControl : TerrariaPlugin
    {
        public override string Name { get { return "PermControl"; } }
        public override string Author { get { return "Zaicon"; } }
        public override string Description { get { return "Searches for commands/permissions within groups."; } }
        public override Version Version { get { return Assembly.GetExecutingAssembly().GetName().Version; } }

        public PermControl(Main game)
            : base(game)
        {
            base.Order = 1;
        }

        #region Initialize/Dispose
        public override void Initialize()
        {
            ServerApi.Hooks.GameInitialize.Register(this, OnInitialize);
        }

        protected override void Dispose(bool Disposing)
        {
            if (Disposing)
            {
                ServerApi.Hooks.GameInitialize.Deregister(this, OnInitialize);
            }
            base.Dispose(Disposing);
        }
        #endregion

        #region Hooks
        private void OnInitialize(EventArgs args)
        {
            Commands.ChatCommands.Add(new Command("permcontrol", SCommand, "searchcommand") { HelpText = "Searches for a specified command." });
            Commands.ChatCommands.Add(new Command("permcontrol", SearchPerm, "searchperm") { HelpText = "Searches for a specified permission." });
            Commands.ChatCommands.Add(new Command("permcontrol", SearchCommandInGroup, "searchgcommand") { HelpText = "Provides a list of groups with a certain command." });
            Commands.ChatCommands.Add(new Command("permcontrol", SearchPermInGroup, "searchgperm") { HelpText = "Provides a list of groups with a certain permission." });
            Commands.ChatCommands.Add(new Command("permcontrol", findPlugins, "pluginlist") { HelpText = "Provides a list of permissions from plugin-provided commands." });
        }
        #endregion

        #region Search Commands
        public void SCommand(CommandArgs args)
        {
            if (args.Parameters.Count > 0)
            {
                List<string> commandNameList = new List<string>();

                foreach (Command command in Commands.ChatCommands)
                {
                    for (int i = 0; i < command.Permissions.Count; i++)
                    {
                        if (args.Player.Group.HasPermission(command.Permissions[i]))
                            foreach (string commandName in command.Names)
                            {
                                bool showCommand = true;
                                foreach (string searchParameter in args.Parameters)
                                {
                                    if (!commandName.Contains(searchParameter))
                                    {
                                        showCommand = false;
                                        break;
                                    }
                                }
                                if (showCommand && !commandNameList.Contains(commandName))
                                    commandNameList.Add(command.Name);
                            }
                    }
                }
                if (commandNameList.Count > 0)
                {
                    args.Player.SendInfoMessage("The following commands matched your search:");
                    for (int i = 0; i < commandNameList.Count && i < 6; i++)
                    {
                        string returnLine = "";

                        for (int j = 0; j < commandNameList.Count - i * 5 && j < 5; j++)
                        {
                            if (i * 5 + j + 1 < commandNameList.Count)
                                returnLine += commandNameList[i * 5 + j] + ", ";

                            else
                                returnLine += commandNameList[i * 5 + j] + ".";
                        }
                        args.Player.SendInfoMessage(returnLine);
                    }
                }
                else
                    args.Player.SendErrorMessage("No Commands matched your search term(s).");
            }
            else
                args.Player.SendErrorMessage("Invalid syntax: {0}searchcommand <command>", (args.Silent ? TShock.Config.CommandSilentSpecifier : TShock.Config.CommandSpecifier));
        }

        public void SearchPerm(CommandArgs args)
        {
            if (args.Parameters.Count > 0)
            {
                foreach (Command cmd in TShockAPI.Commands.ChatCommands)
                {
                    if (cmd.Names.Contains(args.Parameters[0]))
                    {
                        args.Player.SendInfoMessage(string.Format("Permission to use {0}: {1}",
                            cmd.Name, cmd.Permissions.Count > 0 ? cmd.Permissions[0] : "Nothing"));
                        return;
                    }
                }
                args.Player.SendErrorMessage("Command not found.");
            }
            else
            {
				args.Player.SendErrorMessage("Invalid syntax: {0}searchperm <command>", (args.Silent ? TShock.Config.CommandSilentSpecifier : TShock.Config.CommandSpecifier));
            }
        }

        public void SearchCommandInGroup(CommandArgs args)
        {
            if (args.Parameters.Count == 0)
            {
				args.Player.SendErrorMessage("Invalid syntax: {0}searchgcommand <command>", (args.Silent ? TShock.Config.CommandSilentSpecifier : TShock.Config.CommandSpecifier));
                return;
            }

            var thecommand = String.Join(" ", args.Parameters);
            List<Command> therealcommand = new List<Command>();

            foreach(Command command in Commands.ChatCommands)
            {
                if (command.Name.Contains(thecommand))
                {
                    therealcommand.Add(command);
                }
                if (command.Name == thecommand)
                {
                    therealcommand.Clear();
                    therealcommand.Add(command);
                    break;
                }
            }

            if (therealcommand.Count < 1)
            {
                args.Player.SendErrorMessage("No commands found.");
                return;
            }
            else if (therealcommand.Count > 1)
            {
				args.Player.SendMultipleMatchError(therealcommand.Select(p => p.Name));
            }
            else
            {
				var perms = (from thegroup in TShock.Groups where (therealcommand[0].Permissions.Count > 0 ? thegroup.HasPermission(therealcommand[0].Permissions[0]) : true) select thegroup.Name);

				args.Player.SendInfoMessage("Groups with the " + therealcommand[0].Name + " command:");
				args.Player.SendInfoMessage(string.Join(", ", perms));

				/*
                Command therealrealcommand = therealcommand[0];
                List<string> groupswithperm = new List<string>();
                string permname;
                foreach (TShockAPI.Group group in TShock.Groups)
                {
                    try
                    {
                        permname = therealrealcommand.Permissions[0];
                        if (group.HasPermission(permname))
                        {
                            groupswithperm.Add(group.Name);
                        }
                    }
                    catch
                    {
                        groupswithperm.Add(group.Name);
                    }
                }

                string outputgroupswithperm = "";
                for (int i = 0; i < groupswithperm.Count; i++)
                {
                    outputgroupswithperm += groupswithperm[i];
                    if ((i + 1) < groupswithperm.Count)
                        outputgroupswithperm += " ";
                }
                args.Player.SendInfoMessage("Groups with the " + therealcommand[0].Name + " command:");
                args.Player.SendInfoMessage(outputgroupswithperm);
				 */
            }
        }

        private void SearchPermInGroup(CommandArgs args)
        {
            if (args.Parameters.Count != 1)
            {
				args.Player.SendErrorMessage("Invalid syntax: {0}searchgperm <permission>", (args.Silent ? TShock.Config.CommandSilentSpecifier : TShock.Config.CommandSpecifier));
                return;
            }

            string perms = args.Parameters[0];

			var glist = (from thegroup in TShock.Groups where thegroup.HasPermission(perms) select thegroup.Name);

			args.Player.SendInfoMessage("Groups with the " + perms + " permission:");
			args.Player.SendInfoMessage(string.Join(", ", glist));
			/*
            List<string> groupswithperms = new List<string>();

            foreach (TShockAPI.Group group in TShock.Groups)
            {
                if (group.HasPermission(perms))
                    groupswithperms.Add(group.Name);
            }

            string outputgroupswithperms = "";
            for (int i = 0; i < groupswithperms.Count; i++)
            {
                outputgroupswithperms += groupswithperms[i];
                if (i + 1 < groupswithperms.Count)
                    outputgroupswithperms += " ";
            }

            args.Player.SendInfoMessage("Groups with the " + perms + " permission:");
            args.Player.SendInfoMessage(outputgroupswithperms);
			 */
        }
        #endregion

        #region FindPlugin
        private void findPlugins(CommandArgs args)
        {
            List<string> plugincommands = new List<string>();

            foreach (Command command in Commands.ChatCommands)
            {
                if (command.Permissions.Count > 0)
                {
                    var perm = command.Permissions[0].Split('.');

                    if (perm[0] != "tshock" && !plugincommands.Contains(perm[0]))
                        plugincommands.Add(perm[0]);
                }
            }

            args.Player.SendInfoMessage("Command permissions that do not start with tshock:");
			args.Player.SendInfoMessage(string.Join(", ", plugincommands));
        }
        #endregion
    }
}
