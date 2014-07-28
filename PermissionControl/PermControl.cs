using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Terraria;
using TerrariaApi;
using TerrariaApi.Server;
using TShockAPI;

namespace PermissionControl
{
    [ApiVersion(1,16)]
    public class PermControl : TerrariaPlugin
    {
        public override string Name { get { return "PermControl"; } }
        public override string Author { get { return "Zaicon"; } }
        public override string Description { get { return "Searches for commands/permissions within groups."; } }
        public override Version Version { get { return new Version(1, 1, 0, 0); } }

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
            Commands.ChatCommands.Add(new Command("permcontrol", SearchCommandInGroup, "searchgcommand") { HelpText = "Provides a list of groups with a certain command." });
            Commands.ChatCommands.Add(new Command("permcontrol", SearchPermInGroup, "searchgperm") { HelpText = "Provides a list of groups with a certain permission." });
            Commands.ChatCommands.Add(new Command("permcontrol", findPlugins, "pluginlist") { HelpText = "Provides a list of permissions from plugin-provided commands." });
        }
        #endregion

        #region Search Commands
        public void SearchCommandInGroup(CommandArgs args)
        {
            if (args.Parameters.Count == 0)
            {
                args.Player.SendErrorMessage("Invalid syntax: /searchgcommand <command>");
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
                args.Player.SendErrorMessage("Multiple commands found:");
                string errorcommands = string.Join(", ", therealcommand.Select(p => p.Name));
                args.Player.SendErrorMessage(errorcommands);
            }
            else
            {
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

                string outputgroupswithperm = string.Join(", ", groupswithperm.Select(p => p));
                args.Player.SendInfoMessage("Groups with the " + therealcommand[0].Name + " command:");
                args.Player.SendInfoMessage(outputgroupswithperm);
            }
        }

        private void SearchPermInGroup(CommandArgs args)
        {
            if (args.Parameters.Count != 1)
            {
                args.Player.SendErrorMessage("Invalid syntax: /searchgperms <permission>");
                return;
            }

            string perms = args.Parameters[0];

            List<string> groupswithperms = new List<string>();

            foreach (TShockAPI.Group group in TShock.Groups)
            {
                if (group.HasPermission(perms))
                    groupswithperms.Add(group.Name);
            }

            string outputgroupswithperms = string.Join(", ", groupswithperms.Select(p => p));

            args.Player.SendInfoMessage("Groups with the " + perms + " permission:");
            args.Player.SendInfoMessage(outputgroupswithperms);
        }
        #endregion

        #region FindPlugin
        private void findPlugins(CommandArgs args)
        {
            List<string> plugincommands = new List<string>();

            foreach (Command command in Commands.ChatCommands)
            {
                try
                {
                    var perm = command.Permissions[0].Split('.');

                    if (!perm[0].StartsWith("tshock") && !plugincommands.Contains(perm[0]))
                        plugincommands.Add(perm[0]);
                }
                catch
                {
                }
            }

            string listofcommands = "";

            for (int i = 0; i < plugincommands.Count; i++)
            {
                listofcommands += plugincommands[i];
                if (i + 1 < plugincommands.Count)
                    listofcommands += " ";
            }

            args.Player.SendInfoMessage("Command permissions that do not start with tshock:");
            args.Player.SendInfoMessage(listofcommands);
        }
        #endregion
    }
}
