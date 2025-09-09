using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;
using Discord;
using Discord.WebSocket;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using Timer = CounterStrikeSharp.API.Modules.Timers.Timer;

namespace SimpleDiscordRelay
{
    public class Main : BasePlugin
    {
        public override string ModuleName => "Simple Discord Relay";
        public override string ModuleVersion => "1.0.1";
        public override string ModuleAuthor => "Tsukasa";

        public static Config SDRConfig { get; private set; } = null!;
        public static DiscordHelper? Discord { get; private set; }
        public static Main? Instance { get; private set; }
        
        public static string CurrentMapName = "Lobby";
        private Timer? _statusUpdateTimer;
        private HudManager? _hudManager;
        private Timer? _inviteAnnouncerTimer;

        private static readonly Regex EmojiRegex = new Regex("<a?:\\w+:\\d+>", RegexOptions.Compiled);

        public override void Load(bool hotReload)
        {
            Instance = this;
            
            _hudManager = new HudManager(this);

            Config.SetPluginDirectory(ModuleDirectory);
            SDRConfig = Config.LoadConfig();
            
            Events.SetMapBlacklist(SDRConfig.MapBlacklist);
            
            string geoIpDbPath = Path.Combine(ModuleDirectory, "GeoIP", "GeoLite2-Country.mmdb");
            GeoIP.Init(geoIpDbPath);

            DiscordMessageQueue.Start();
            
            if (string.IsNullOrEmpty(SDRConfig.BotToken) || SDRConfig.BotToken == "YOUR_DISCORD_BOT_TOKEN")
            {
                Console.WriteLine("[Simple-Discord-Relay] ERROR: Discord Bot Token is not configured.");
                return;
            }
            
            Discord = new DiscordHelper(SDRConfig.BotToken);
            Discord.OnBotReady += OnBotReady;

            if (SDRConfig.EnableDiscordToGameRelay)
                Discord.OnDiscordMessageReceived += OnDiscordMessage;

            ChatRelay.Init(this);
            Events.Init(this);
            
            StartInviteAnnouncerTimer();
            
            Console.WriteLine("[Simple-Discord-Relay] Loaded successfully.");
        }
        
        private void OnBotReady()
        {
            Server.NextFrame(StartStatusUpdateTimer);
        }

        private void StartStatusUpdateTimer()
        {
            if (SDRConfig.BotStatus.Enabled)
            {
                _statusUpdateTimer?.Kill();
                _statusUpdateTimer = AddTimer(SDRConfig.BotStatus.UpdateInterval, UpdateBotStatus, TimerFlags.REPEAT);
            }
        }

        private void UpdateBotStatus()
        {
            if (Discord == null) return;

            int playerCount = Utilities.GetPlayers().Count(p => p.IsValid && !p.IsBot && p.Connected == PlayerConnectedState.PlayerConnected);
            
            var statusBuilder = new StringBuilder(SDRConfig.BotStatus.Format);
            statusBuilder.Replace("{map}", CurrentMapName);
            statusBuilder.Replace("{players}", playerCount.ToString());
            statusBuilder.Replace("{max_players}", Server.MaxPlayers.ToString());
            
            _ = Discord.SetActivityAsync(statusBuilder.ToString(), SDRConfig.BotStatus.ActivityType);
        }
        
        private void OnDiscordMessage(SocketMessage message)
        {
            if (message.Author.IsBot || message.Channel.Id != SDRConfig.ChatRelayChannelId) return;

            var sticker = message.Stickers.FirstOrDefault();
            var customEmojis = message.Tags.Where(t => t.Type == TagType.Emoji).Select(v => v.Value as Emote).Where(e => e != null).ToList();

            string displayName = (message.Author as SocketGuildUser)?.DisplayName ?? message.Author.Username;
            string username = message.Author.Username;
            string rawContent = message.Content;

            if (SDRConfig.EnableHudMessageForSticker && sticker != null)
            {
                if (sticker.Format == StickerFormatType.Lottie)
                {
                }
                else
                {
                    string stickerUrl = $"https://cdn.discordapp.com/stickers/{sticker.Id}.png";
                
                    var hudBuilder = new StringBuilder(SDRConfig.DiscordToGameHudStickerFormat);
                    hudBuilder.Replace("{user}", HttpUtility.HtmlEncode(displayName));
                    hudBuilder.Replace("{username}", username);
                    hudBuilder.Replace("{user_id}", message.Author.Id.ToString());
                    hudBuilder.Replace("{sticker_url}", stickerUrl);

                    _hudManager?.EnqueueMessage(hudBuilder.ToString());
                    return; 
                }
            }
            
            if (SDRConfig.EnableHudMessageForEmoji && customEmojis.Any())
            {
                string emojiImagesHtml = string.Join(" ", customEmojis.Select(e => $"<img src='{e!.Url}'>"));
                string messageWithoutEmojiCodes = EmojiRegex.Replace(rawContent, "").Trim();

                string safeDisplayName = HttpUtility.HtmlEncode(displayName);
                string safeMessage = HttpUtility.HtmlEncode(messageWithoutEmojiCodes).Replace("\n", "<br>");

                var hudBuilder = new StringBuilder(SDRConfig.DiscordToGameHudFormat);
                hudBuilder.Replace("{user}", safeDisplayName);
                hudBuilder.Replace("{username}", username);
                hudBuilder.Replace("{user_id}", message.Author.Id.ToString());
                hudBuilder.Replace("{emojis}", emojiImagesHtml);
                hudBuilder.Replace("{message}", safeMessage);
                
                _hudManager?.EnqueueMessage(hudBuilder.ToString());
            }
            else if (!string.IsNullOrWhiteSpace(rawContent))
            {
                var chatBuilder = new StringBuilder(SDRConfig.DiscordToGameFormat);
                chatBuilder.Replace("{user}", displayName);
                chatBuilder.Replace("{username}", username);
                chatBuilder.Replace("{user_id}", message.Author.Id.ToString());
                chatBuilder.Replace("{message}", rawContent);

                Server.NextFrame(() => Server.PrintToChatAll(chatBuilder.ToString()));
            }
        }

        private void StartInviteAnnouncerTimer()
        {
            _inviteAnnouncerTimer?.Kill();
            var announcerConfig = SDRConfig.DiscordInviteAnnouncer;

            if (announcerConfig.Enabled && 
                !string.IsNullOrWhiteSpace(announcerConfig.InviteLink) && 
                announcerConfig.InviteLink != "https://discord.gg/your-invite-code")
            {
                _inviteAnnouncerTimer = AddTimer(announcerConfig.AnnounceIntervalSeconds, AnnounceDiscordLink, TimerFlags.REPEAT);
            }
        }

        private void AnnounceDiscordLink()
        {
            if (Utilities.GetPlayers().Any(p => p.IsValid && !p.IsBot))
            {
                var announcerConfig = SDRConfig.DiscordInviteAnnouncer;
                string message = announcerConfig.AnnounceMessageFormat.Replace("{link}", announcerConfig.InviteLink);
                Server.PrintToChatAll(message);
            }
        }

        public override void Unload(bool hotReload)
        {
            _hudManager?.Clear();
            DiscordMessageQueue.Stop();

            if (Discord != null)
            {
                Discord.OnBotReady -= OnBotReady;
                Discord.OnDiscordMessageReceived -= OnDiscordMessage;
                Discord.Dispose();
            }
            
            _statusUpdateTimer?.Kill();
            _inviteAnnouncerTimer?.Kill();
            Events.Dispose();
            GeoIP.Dispose();
            Instance = null;
        }
    }
}
