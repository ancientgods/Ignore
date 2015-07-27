using System;
using System.Collections.Generic;
using Terraria;
using TShockAPI;
using TerrariaApi.Server;
using System.Reflection;
using System.Linq;
using System.Text;


namespace ignoreplugin
{
    [ApiVersion(1, 20)]
    public class IgnorePlugin : TerrariaPlugin
    {
        public static readonly string Permission_Ignore = "ignore.use";
        public static readonly string Permission_NoIgnore = "ignore.admin";

        public static bool[] _IgnoreAll = new bool[255];
        public static Dictionary<int, List<string>> IgnoreList = new Dictionary<int, List<string>>();
        public override Version Version
        {
            get { return Assembly.GetExecutingAssembly().GetName().Version; }
        }
        public override string Author
        {
            get { return "Ancientgods"; }
        }
        public override string Name
        {
            get { return "Ignore"; }
        }

        public override string Description
        {
            get { return "Adds the ability to ignore certain players"; }
        }

        public override void Initialize()
        {
            ServerApi.Hooks.ServerChat.Register(this, OnChat);
            ServerApi.Hooks.ServerLeave.Register(this, OnLeave);

            Commands.ChatCommands.Add(new Command(Ignore, "ignore"));
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ServerApi.Hooks.ServerChat.Deregister(this, OnChat);
                ServerApi.Hooks.ServerLeave.Deregister(this, OnLeave);
            }
            base.Dispose(disposing);
        }

        public IgnorePlugin(Main game)
            : base(game)
        {
            Order = 0;
        }

        public void OnChat(ServerChatEventArgs args)
        {
            TSPlayer sender = TShock.Players[args.Who];
            if (!args.Text.StartsWith("/") && !sender.mute && sender.Group.HasPermission("tshock.canchat"))
            {
                for (int i = 0; i < TShock.Players.Length; i++)
                {
                    TSPlayer ts = TShock.Players[i];

                    if (ts == null)
                        continue;

                    if (_IgnoreAll[i])
                    {
                        if (!sender.Group.HasPermission(Permission_NoIgnore))
                            continue;
                    }

                    if (IgnoreList.ContainsKey(i))
                    {
                        if (IgnoreList[i].Contains(sender.IP) && !sender.Group.HasPermission(Permission_NoIgnore))
                            continue;
                    }
                    args.Handled = true;
                    var text = String.Format(TShock.Config.ChatFormat, sender.Group.Name, sender.Group.Prefix, sender.Name, sender.Group.Suffix, args.Text);
                    ts.SendMessage(text, new Color(sender.Group.R, sender.Group.G, sender.Group.B));
                }
            }
        }

        public static void Ignore(CommandArgs args)
        {
            string cmd = args.Parameters.Count > 0 ? args.Parameters[0] : "help";
            switch (cmd.ToLower())
            {
                case "add":
                    {
                        if (args.Parameters.Count < 2)
                        {
                            args.Player.SendErrorMessage("Invalid syntax! Proper syntax: /ignore add <player>");
                            return;
                        }
                        if (!args.Player.Group.HasPermission(Permission_Ignore))
                        {
                            args.Player.SendErrorMessage("You don't have permission to ignore other players");
                            return;
                        }
                        string plrstr = string.Join(" ", args.Parameters.GetRange(1, args.Parameters.Count-1));
                        var players = TShock.Utils.FindPlayer(plrstr);
                        if (players.Count == 0)
                        {
                            args.Player.SendErrorMessage("Invalid player!");
                            return;
                        }
                        else if (players.Count > 1)
                        {
                            TShock.Utils.SendMultipleMatchError(args.Player, players.Select(p => p.Name));
                            return;
                        }
                        if (players[0].Index == args.Player.Index)
                        {
                            args.Player.SendErrorMessage("You cannot ignore yourself!");
                            return;
                        }
                        if (players[0].Group.HasPermission(Permission_NoIgnore))
                        {
                            args.Player.SendErrorMessage("You cannot ignore this player!");
                            return;
                        }
                        if (!IgnoreList.ContainsKey(args.Player.Index))
                            IgnoreList.Add(args.Player.Index, new List<string>());

                        if (!IgnoreList[args.Player.Index].Contains(players[0].IP))
                        {
                            IgnoreList[args.Player.Index].Add(players[0].IP);
                            args.Player.SendInfoMessage(string.Format("You have added {0} to your ignore list", players[0].Name));
                        }
                        else
                            args.Player.SendErrorMessage(players[0].Name + " is already on your ignore list!");
                    }
                    break;
                case "del":
                    {
                        if (args.Parameters.Count < 2)
                        {
                            args.Player.SendErrorMessage("Invalid syntax! Proper syntax: /ignore del <player>");
                            return;
                        }

                        string plrstr = string.Join(" ", args.Parameters.GetRange(1, args.Parameters.Count - 1));
                        var players = TShock.Utils.FindPlayer(plrstr);
                        if (players.Count == 0)
                        {
                            args.Player.SendErrorMessage("Invalid player!");
                            return;
                        }
                        if (IgnoreList.ContainsKey(args.Player.Index))
                        {
                            if (IgnoreList[args.Player.Index].Contains(players[0].IP))
                            {
                                IgnoreList[args.Player.Index].Remove(players[0].IP);
                                args.Player.SendInfoMessage(string.Format("You have removed {0} from your ignore list", players[0].Name));

                                if (IgnoreList[args.Player.Index].Count == 0)
                                    IgnoreList.Remove(args.Player.Index);
                            }
                            else
                                args.Player.SendErrorMessage(players[0].Name + " is not on your ignore list!");
                        }
                        else
                            args.Player.SendErrorMessage("You don't have anyone on your ignore list!");
                    }
                    break;
                case "*":
                case "all":
                    {
                        if (!args.Player.Group.HasPermission(Permission_Ignore))
                        {
                            args.Player.SendErrorMessage("You don't have permission to ignore other players");
                            return;
                        }
                        _IgnoreAll[args.Player.Index] = !_IgnoreAll[args.Player.Index];
                        args.Player.SendInfoMessage(string.Format("You have {0} ignoring all players!", _IgnoreAll[args.Player.Index] ? "enabled" : "disabled"));
                    }
                    break;
                case "list":
                    if(!IgnoreList.ContainsKey(args.Player.Index))
                    {
                        args.Player.SendInfoMessage("You currently don't have any players on your ignore list!");
                        return;
                    }
                    args.Player.SendInfoMessage("Currently Ignored players:");
                    {
                        StringBuilder sb = new StringBuilder();
                        for (int i = 0; i < TShock.Players.Length; i++)
                        {
                            if (TShock.Players[i] != null)
                            {
                                if (IgnoreList[args.Player.Index].Contains(TShock.Players[i].IP))
                                {
                                    sb.Append(TShock.Players[i].Name);
                                    if (i < TShock.Players.Length - 1)
                                        sb.Append(", ");
                                }
                            }
                        }
                        args.Player.SendInfoMessage(sb.ToString());
                    }
                    break;
                case "help":
                default:
                    args.Player.SendErrorMessage("Invalid subcommand! Valid subcommands:");
                    args.Player.SendInfoMessage("/ignore add <name> - ignore a specific player");
                    args.Player.SendInfoMessage("/ignore del <name> - stop ignoring a specific player");
                    args.Player.SendInfoMessage("/ignore all (or *) - ignore everyone");
                    args.Player.SendInfoMessage("/ignore list - list currently ignored players");
                    break;
            }
        }

        public void OnLeave(LeaveEventArgs e)
        {
            _IgnoreAll[e.Who] = false;
            if (IgnoreList.ContainsKey(e.Who))
            {
                IgnoreList.Remove(e.Who);
            }
        }
    }
}
