using Discord;
using Discord.WebSocket;
using System;
using System.Threading.Tasks;

namespace SimpleDiscordRelay
{
    public class DiscordHelper : IDisposable
    {
        private readonly DiscordSocketClient _client;
        public event Action<SocketMessage>? OnDiscordMessageReceived;
        public event Action? OnBotReady;

        public DiscordHelper(string token)
        {
            _client = new DiscordSocketClient(new DiscordSocketConfig
            {
                GatewayIntents = GatewayIntents.Guilds | GatewayIntents.GuildMessages | GatewayIntents.MessageContent
            });

            _client.Log += LogAsync;
            _client.MessageReceived += MessageReceivedHandler;
            _client.Ready += OnClientReady;
            
            Task.Run(async () => {
                try
                {
                    await _client.LoginAsync(TokenType.Bot, token);
                    await _client.StartAsync();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Discord Exception] Login/Start failed: {ex.Message}");
                }
            });
        }

        private Task OnClientReady()
        {
            OnBotReady?.Invoke();
            return Task.CompletedTask;
        }

        private Task MessageReceivedHandler(SocketMessage message)
        {
            OnDiscordMessageReceived?.Invoke(message);
            return Task.CompletedTask;
        }

        private Task LogAsync(LogMessage msg)
        {
            Console.WriteLine($"[Discord] {msg.Severity}: {msg.Message} (Source: {msg.Source})");
            if (msg.Exception != null)
            {
                Console.WriteLine($"[Discord Exception] {msg.Exception}");
            }
            return Task.CompletedTask;
        }

        public async Task SetActivityAsync(string text, BotActivityType activityType)
        {
            if (_client.ConnectionState != ConnectionState.Connected)
                return;
            
            await _client.SetActivityAsync(new Game(text, (ActivityType)activityType));
        }

        public async Task SendMessage(ulong channelId, string message)
        {
            if (channelId == 0 || _client.ConnectionState != ConnectionState.Connected) return;
            var channel = _client.GetChannel(channelId) as IMessageChannel;
            if (channel != null) await channel.SendMessageAsync(message);
        }

        public async Task SendEmbed(ulong channelId, Embed embed)
        {
            if (channelId == 0 || _client.ConnectionState != ConnectionState.Connected) return;
            var channel = _client.GetChannel(channelId) as IMessageChannel;
            if (channel != null) await channel.SendMessageAsync(embed: embed);
        }

        public void Dispose()
        {
            if (_client != null)
            {
                _client.Ready -= OnClientReady;
                _client.MessageReceived -= MessageReceivedHandler;
            }
            _client?.StopAsync();
            _client?.Dispose();
        }
    }
}
