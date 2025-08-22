using CounterStrikeSharp.API.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SimpleDiscordRelay
{
    public enum BotActivityType
    {
        Playing = 0,
        Streaming = 1,
        Listening = 2,
        Watching = 3,
        Competing = 5
    }

    public class Config : BasePluginConfig
    {
        [JsonPropertyName("Bot Token")]
        public string BotToken { get; set; } = "YOUR_DISCORD_BOT_TOKEN";

        [JsonPropertyName("Chat Relay Channel ID")]
        public ulong ChatRelayChannelId { get; set; } = 0;

        [JsonPropertyName("Notification Channel ID")]
        public ulong NotificationChannelId { get; set; } = 0;

        [JsonPropertyName("Enable Chat Relay")]
        public bool EnableChatRelay { get; set; } = true;

        [JsonPropertyName("Enable Join Message")]
        public bool EnableJoinMessage { get; set; } = true;

        [JsonPropertyName("Enable Leave Message")]
        public bool EnableLeaveMessage { get; set; } = true;

        [JsonPropertyName("Enable Map Change Message")]
        public bool EnableMapChangeMessage { get; set; } = true;

        [JsonPropertyName("Enable Discord To Game Relay")]
        public bool EnableDiscordToGameRelay { get; set; } = true;
        
        [JsonPropertyName("Enable Hud Message For Emoji")]
        public bool EnableHudMessageForEmoji { get; set; } = true;

        [JsonPropertyName("Enable Hud Message For Sticker")]
        public bool EnableHudMessageForSticker { get; set; } = true;

        [JsonPropertyName("Map Notification Blacklist")]
        public List<string> MapBlacklist { get; set; } = new List<string> { "ar_barrage" };
        
        [JsonPropertyName("Bot Status Settings")]
        public BotStatusSettings BotStatus { get; set; } = new BotStatusSettings();
        
        [JsonPropertyName("Discord Invite Announcer")]
        public DiscordInviteAnnouncerSettings DiscordInviteAnnouncer { get; set; } = new DiscordInviteAnnouncerSettings();

        [JsonPropertyName("In-Game Notifications")]
        public InGameNotificationsSettings InGameNotifications { get; set; } = new InGameNotificationsSettings();

        [JsonPropertyName("Discord To Game Format")]
        public string DiscordToGameFormat { get; set; } = " [Discord] {user} ({username}): {message}";
        
        [JsonPropertyName("Discord To Game Hud Format")]
        public string DiscordToGameHudFormat { get; set; } = "<center><font color='white' size='6'>{user} ({username})</font><br>{emojis}<br><font color='lightgray' size='5'>{message}</font></center>";
        
        [JsonPropertyName("Discord To Game Hud Sticker Format")]
        public string DiscordToGameHudStickerFormat { get; set; } = "<center><font color='white' size='6'>{user} ({username})</font><br><img src='{sticker_url}' style='max-width:128px; max-height:128px;'></center>";

        [JsonPropertyName("Hud Emoji Duration")]
        public float HudEmojiDuration { get; set; } = 2.5f;

        [JsonPropertyName("Join Message")]
        public string JoinMessage { get; set; } = "has connected.";
        
        [JsonPropertyName("Leave Message")]
        public string LeaveMessage { get; set; } = "has disconnected.";
        
        [JsonPropertyName("Map Change Message")]
        public string MapChangeMessage { get; set; } = "🗺️ Map changed to **{map}**";
        
        [JsonPropertyName("Join Embed Color")]
        public uint JoinColor { get; set; } = 0x00FF00;
        
        [JsonPropertyName("Leave Embed Color")]
        public uint LeaveColor { get; set; } = 0xFF0000;
        
        [JsonPropertyName("Map Change Embed Color")]
        public uint MapChangeColor { get; set; } = 0x3498DB;
        
        [JsonPropertyName("Chat Embed Color")]
        public uint ChatColor { get; set; } = 0x5dade2;

        private static string? _pluginDirectory;
        
        public static void SetPluginDirectory(string pluginDirectory) { _pluginDirectory = pluginDirectory; }
        
        public static string GetConfigPath()
        {
            try
            {
                if (string.IsNullOrEmpty(_pluginDirectory)) return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "simplediscordrelay.json");
                
                DirectoryInfo? pluginDirInfo = new DirectoryInfo(_pluginDirectory);
                if (pluginDirInfo.Parent?.Parent != null)
                {
                    string configDirectory = Path.Combine(pluginDirInfo.Parent.Parent.FullName, "configs", "plugins", "SimpleDiscordRelay");
                    if (!Directory.Exists(configDirectory)) Directory.CreateDirectory(configDirectory);
                    return Path.Combine(configDirectory, "simplediscordrelay.json");
                }
                return Path.Combine(_pluginDirectory, "simplediscordrelay.json");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SimpleDiscordRelay] Config path error: {ex.Message}");
                return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "simplediscordrelay.json");
            }
        }

        public static Config LoadConfig()
        {
            string configPath = GetConfigPath();
            if (!File.Exists(configPath))
            {
                var cfg = new Config();
                string json = JsonSerializer.Serialize(cfg, new JsonSerializerOptions { WriteIndented = true, Converters = { new JsonStringEnumConverter() } });
                File.WriteAllText(configPath, json);
                return cfg;
            }
            string jsonContent = File.ReadAllText(configPath);
            var config = JsonSerializer.Deserialize<Config>(jsonContent, new JsonSerializerOptions { Converters = { new JsonStringEnumConverter() } });
            return config ?? new Config();
        }
    }

    public class BotStatusSettings
    {
        [JsonPropertyName("Enable Bot Status")]
        public bool Enabled { get; set; } = true;
        [JsonPropertyName("Update Interval (Seconds)")]
        public float UpdateInterval { get; set; } = 60.0f;
        [JsonPropertyName("Activity Type")]
        public BotActivityType ActivityType { get; set; } = BotActivityType.Watching;
        [JsonPropertyName("Status Text Format")]
        public string Format { get; set; } = "{map} | {players}/{max_players} players";
    }
    
    public class DiscordInviteAnnouncerSettings
    {
        [JsonPropertyName("Enable Announcer")]
        public bool Enabled { get; set; } = true;
        [JsonPropertyName("Invite Link (Must not be empty)")]
        public string InviteLink { get; set; } = "https://discord.gg/your-invite-code";
        [JsonPropertyName("Announce Interval (Seconds)")]
        public int AnnounceIntervalSeconds { get; set; } = 600;
        [JsonPropertyName("Announce Message Format")]
        public string AnnounceMessageFormat { get; set; } = " [SERVER] Join our community on Discord! {link}";
    }
    
    public class InGameNotificationsSettings
    {
        [JsonPropertyName("Enable Notifications")]
        public bool Enabled { get; set; } = true;
        [JsonPropertyName("Join Message Format")]
        public string JoinMessageFormat { get; set; } = " [+] {player_name} ({steam_id}) connected from {country_name}.";
        [JsonPropertyName("Leave Message Format")]
        public string LeaveMessageFormat { get; set; } = " [-] {player_name} ({steam_id}) from {country_name} has disconnected.";
    }
}
