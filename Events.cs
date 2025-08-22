using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Utils;
using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Timer = CounterStrikeSharp.API.Modules.Timers.Timer;

namespace SimpleDiscordRelay
{
    public static class Events
    {
        private class PlayerData
        {
            public string Name { get; set; } = string.Empty;
            public string SteamId64 { get; set; } = string.Empty;
            public string IpAddress { get; set; } = string.Empty;
            public string Flag { get; set; } = string.Empty;
            public string CountryName { get; set; } = string.Empty;
        }

        private class PendingDisconnect
        {
            public PlayerData Data { get; init; } = new();
            public Timer Timer { get; set; } = null!;
            public int PlayerSlot { get; init; }
        }
        
        private static Dictionary<int, PlayerData> _playerData = new();
        private static HashSet<string> _suppressedPlayers = new();
        private static Dictionary<int, PendingDisconnect> _pendingDisconnects = new();

        private static string _lastMapChangeAnnounced = string.Empty;
        private static bool _isMapChanging = false;
        private static Timer? _cleanupTimer;
        
        private static HashSet<string> _mapBlacklistHashSet = new();

        public static void Init(BasePlugin plugin)
        {
            plugin.RegisterEventHandler<EventPlayerConnectFull>(OnPlayerConnectFull, HookMode.Post);
            plugin.RegisterEventHandler<EventPlayerDisconnect>(OnPlayerDisconnect);
            plugin.RegisterListener<Listeners.OnMapStart>(OnMapStart);
            plugin.RegisterListener<Listeners.OnMapEnd>(OnMapEnd);
        }

        public static void SetMapBlacklist(List<string> mapBlacklist)
        {
            _mapBlacklistHashSet = new HashSet<string>(mapBlacklist, StringComparer.OrdinalIgnoreCase);
        }

        private static void OnMapEnd()
        {
            _isMapChanging = true;
            foreach (var pending in _pendingDisconnects.Values)
            {
                pending.Timer.Kill();
            }
            _pendingDisconnects.Clear();
            _suppressedPlayers.Clear();
            foreach (var player in _playerData.Values)
            {
                _suppressedPlayers.Add(player.SteamId64);
            }
        }

        private static void OnMapStart(string mapName)
        {
            Main.CurrentMapName = mapName;
            _playerData.Clear();
            
            if (Main.SDRConfig.EnableMapChangeMessage && _lastMapChangeAnnounced != mapName)
            {
                if (!_mapBlacklistHashSet.Contains(mapName))
                {
                    var embed = new EmbedBuilder()
                        .WithColor(new Color(Main.SDRConfig.MapChangeColor))
                        .WithDescription(Main.SDRConfig.MapChangeMessage.Replace("{map}", mapName))
                        .Build();
                    DiscordMessageQueue.QueueMessage(Main.SDRConfig.NotificationChannelId, embed);
                }
                _lastMapChangeAnnounced = mapName;
            }

            _cleanupTimer?.Kill();
            _cleanupTimer = Main.Instance!.AddTimer(15.0f, () =>
            {
                _isMapChanging = false;
                _suppressedPlayers.Clear();
            });
        }

        private static HookResult OnPlayerConnectFull(EventPlayerConnectFull @event, GameEventInfo info)
        {
            var player = @event.Userid;
            if (player == null || !player.IsValid || player.IsBot || player.AuthorizedSteamID == null)
                return HookResult.Continue;

            if (_pendingDisconnects.Remove(player.Slot, out var pending))
            {
                pending.Timer.Kill();
            }
            
            string steamId64 = player.AuthorizedSteamID.SteamId64.ToString();
            var geoInfo = GeoIP.GetGeoInfo(player.IpAddress);

            var newPlayerData = new PlayerData 
            { 
                Name = player.PlayerName, 
                SteamId64 = steamId64, 
                IpAddress = player.IpAddress ?? "",
                Flag = geoInfo.FlagEmoji,
                CountryName = geoInfo.CountryName
            };

            if (_suppressedPlayers.Contains(steamId64))
            {
                _playerData[player.Slot] = newPlayerData;
                _suppressedPlayers.Remove(steamId64);
                return HookResult.Continue;
            }

            if (!_playerData.ContainsKey(player.Slot))
            {
                _playerData[player.Slot] = newPlayerData;
                
                SendDiscordJoinNotification(newPlayerData);
                SendInGameJoinNotification(newPlayerData);
            }
            return HookResult.Continue;
        }

        private static HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
        {
            var player = @event.Userid;
            if (player == null || !player.IsValid || player.IsBot || !_playerData.TryGetValue(player.Slot, out var playerData))
                return HookResult.Continue;

            if (_isMapChanging) return HookResult.Continue;

            var timer = Main.Instance!.AddTimer(1.5f, () =>
            {
                if (_pendingDisconnects.Remove(player.Slot))
                {
                    SendDiscordLeaveNotification(playerData);
                    SendInGameLeaveNotification(playerData);
                    
                    _playerData.Remove(player.Slot);
                }
            });
            _pendingDisconnects[player.Slot] = new PendingDisconnect { Data = playerData, Timer = timer, PlayerSlot = player.Slot };
            
            return HookResult.Continue;
        }

        private static void SendDiscordJoinNotification(PlayerData playerData)
        {
            if (!Main.SDRConfig.EnableJoinMessage) return;

            string description = $"{playerData.Flag} [**{playerData.Name}**](https://steamcommunity.com/profiles/{playerData.SteamId64}) {Main.SDRConfig.JoinMessage}";
            var embed = new EmbedBuilder()
                .WithColor(new Color(Main.SDRConfig.JoinColor))
                .WithDescription(description)
                .Build();
            DiscordMessageQueue.QueueMessage(Main.SDRConfig.NotificationChannelId, embed);
        }

        private static void SendInGameJoinNotification(PlayerData playerData)
        {
            if (!Main.SDRConfig.InGameNotifications.Enabled) return;

            var joinMsgBuilder = new StringBuilder(Main.SDRConfig.InGameNotifications.JoinMessageFormat);
            joinMsgBuilder.Replace("{player_name}", playerData.Name);
            joinMsgBuilder.Replace("{steam_id}", playerData.SteamId64);
            joinMsgBuilder.Replace("{country_name}", playerData.CountryName);
            Server.PrintToChatAll(joinMsgBuilder.ToString());
        }
        
        private static void SendDiscordLeaveNotification(PlayerData playerData)
        {
            if (!Main.SDRConfig.EnableLeaveMessage) return;

            string description = $"{playerData.Flag} [**{playerData.Name}**](https://steamcommunity.com/profiles/{playerData.SteamId64}) {Main.SDRConfig.LeaveMessage}";
            var embed = new EmbedBuilder()
                .WithColor(new Color(Main.SDRConfig.LeaveColor))
                .WithDescription(description)
                .Build();
            DiscordMessageQueue.QueueMessage(Main.SDRConfig.NotificationChannelId, embed);
        }

        private static void SendInGameLeaveNotification(PlayerData playerData)
        {
            if (!Main.SDRConfig.InGameNotifications.Enabled) return;

            var leaveMsgBuilder = new StringBuilder(Main.SDRConfig.InGameNotifications.LeaveMessageFormat);
            leaveMsgBuilder.Replace("{player_name}", playerData.Name);
            leaveMsgBuilder.Replace("{steam_id}", playerData.SteamId64);
            leaveMsgBuilder.Replace("{country_name}", playerData.CountryName);
            Server.PrintToChatAll(leaveMsgBuilder.ToString());
        }
        
        public static string GetPlayerFlag(int playerSlot)
        {
            if (_playerData.TryGetValue(playerSlot, out var data) && !string.IsNullOrEmpty(data.Flag))
            {
                return data.Flag;
            }
            return "🏳️";
        }

        public static void Dispose()
        {
            _cleanupTimer?.Kill();
            foreach(var p in _pendingDisconnects.Values) p.Timer.Kill();
            _playerData.Clear();
            _suppressedPlayers.Clear();
            _pendingDisconnects.Clear();
        }
    }
}