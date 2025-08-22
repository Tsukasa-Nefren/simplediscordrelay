using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Discord;

namespace SimpleDiscordRelay
{
    public static class ChatRelay
    {
        public static void Init(BasePlugin plugin)
        {
            if (Main.SDRConfig.EnableChatRelay)
            {
                plugin.AddCommand("say", "Public chat", (player, info) => OnChat(player, info, false));
                plugin.AddCommand("say_team", "Team chat", (player, info) => OnChat(player, info, true));
            }
        }

        private static void OnChat(CCSPlayerController? player, CounterStrikeSharp.API.Modules.Commands.CommandInfo info, bool isTeamChat)
        {
            if (player == null || !player.IsValid || player.IsBot || string.IsNullOrEmpty(player.PlayerName))
                return;

            if (Main.Discord == null)
            {
                Console.WriteLine("[SDR] Discord is not initialized, cannot relay chat message.");
                return;
            }

            string message = info.ArgString.Trim();

            if (string.IsNullOrWhiteSpace(message) || message == "\"\"")
                return;
            
            if (message.StartsWith("\"") && message.EndsWith("\""))
            {
                message = message.Substring(1, message.Length - 2);
            }

            string steamId64 = player.SteamID.ToString();
            string playerName = player.PlayerName;
            string prefix = isTeamChat ? "(Team) " : "";

            string flag = Events.GetPlayerFlag(player.Slot);

            string description = $"{prefix}{flag} [{playerName}](https://steamcommunity.com/profiles/{steamId64}): {message}";

            var embed = new EmbedBuilder()
                .WithDescription(description)
                .WithColor(new Color(Main.SDRConfig.ChatColor))
                .Build();
            
            DiscordMessageQueue.QueueMessage(Main.SDRConfig.ChatRelayChannelId, embed);
        }
    }
}