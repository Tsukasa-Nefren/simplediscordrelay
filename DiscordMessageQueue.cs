using Discord;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleDiscordRelay
{
    public class QueuedMessage
    {
        public ulong ChannelId { get; set; }
        public Embed? Embed { get; set; }
        public string? TextMessage { get; set; }
    }

    public static class DiscordMessageQueue
    {
        private static readonly ConcurrentQueue<QueuedMessage> _messageQueue = new();
        private static CancellationTokenSource? _cancellationTokenSource;
        private static Task? _processingTask;
        private static bool _isProcessing = false;

        public static void Start()
        {
            if (_isProcessing) return;
            
            _isProcessing = true;
            _cancellationTokenSource = new CancellationTokenSource();
            _processingTask = Task.Run(ProcessQueue, _cancellationTokenSource.Token);
            Console.WriteLine("[SDR] Message queue started.");
        }

        public static void Stop()
        {
            if (!_isProcessing) return;
            
            _cancellationTokenSource?.Cancel();
            try { _processingTask?.Wait(5000); } catch { }
            
            _isProcessing = false;
            Console.WriteLine("[SDR] Message queue stopped.");
        }

        public static void QueueMessage(ulong channelId, Embed embed)
        {
            _messageQueue.Enqueue(new QueuedMessage { ChannelId = channelId, Embed = embed });
        }

        public static void QueueMessage(ulong channelId, string textMessage)
        {
            _messageQueue.Enqueue(new QueuedMessage { ChannelId = channelId, TextMessage = textMessage });
        }

        private static async Task ProcessQueue()
        {
            while (_isProcessing && !(_cancellationTokenSource?.Token.IsCancellationRequested ?? true))
            {
                try
                {
                    if (_messageQueue.TryDequeue(out var message))
                    {
                        await SendMessage(message);
                        await Task.Delay(300, _cancellationTokenSource.Token);
                    }
                    else
                    {
                        await Task.Delay(100, _cancellationTokenSource.Token);
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[SDR] Message queue error: {ex.Message}");
                    await Task.Delay(1000);
                }
            }
        }

        private static async Task SendMessage(QueuedMessage message)
        {
            try
            {
                if (Main.Discord == null) return;
                
                if (message.Embed != null)
                {
                    await Main.Discord.SendEmbed(message.ChannelId, message.Embed);
                }
                else if (!string.IsNullOrEmpty(message.TextMessage))
                {
                    await Main.Discord.SendMessage(message.ChannelId, message.TextMessage);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SDR] Failed to send message to channel {message.ChannelId}: {ex.Message}");
            }
        }
    }
}
