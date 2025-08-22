using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Timers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Timer = CounterStrikeSharp.API.Modules.Timers.Timer;

namespace SimpleDiscordRelay
{
    public class HudManager
    {
        private readonly Main _plugin;
        private readonly ConcurrentQueue<string> _messageQueue = new();
        private bool _isHudBusy = false;

        public HudManager(Main plugin)
        {
            _plugin = plugin;
        }

        public void EnqueueMessage(string html)
        {
            _messageQueue.Enqueue(html);
            if (!_isHudBusy)
            {
                Server.NextFrame(ProcessQueue);
            }
        }

        private void ProcessQueue()
        {
            if (!_messageQueue.TryDequeue(out var htmlToShow))
            {
                _isHudBusy = false;
                return;
            }

            _isHudBusy = true;
            
            var targetPlayers = Utilities.GetPlayers().Where(p => p.IsValid && !p.IsBot).ToList();
            
            if (!targetPlayers.Any())
            {
                _isHudBusy = false;
                ProcessQueue();
                return;
            }
            
            var timerState = new HudTimerState(this, htmlToShow, targetPlayers);
            timerState.StartTimer(_plugin);
        }

        public void Clear()
        {
            _isHudBusy = false;
            _messageQueue.Clear();
        }
        
        private class HudTimerState
        {
            private readonly HudManager _manager;
            private readonly string _htmlToShow;
            private readonly List<CCSPlayerController> _targetPlayers;
            private readonly int _repeatCount;
            private int _currentRepeat = 0;
            private Timer? _timer;

            public HudTimerState(HudManager manager, string html, List<CCSPlayerController> players)
            {
                _manager = manager;
                _htmlToShow = html;
                _targetPlayers = players;

                float duration = Main.SDRConfig.HudEmojiDuration;
                float interval = 0.1f;
                _repeatCount = (int)(duration / interval);
            }

            public void StartTimer(Main plugin)
            {
                _timer = plugin.AddTimer(0.1f, Tick, TimerFlags.REPEAT);
            }

            private void Tick()
            {
                if (_currentRepeat >= _repeatCount)
                {
                    _timer?.Kill();
                    foreach (var player in _targetPlayers)
                    {
                        if(player.IsValid)
                        {
                            player.PrintToCenterHtml(" ");
                        }
                    }
                    _manager._isHudBusy = false;
                    _manager.ProcessQueue();
                    return;
                }

                foreach (var player in _targetPlayers)
                {
                    if (player.IsValid)
                    {
                        player.PrintToCenterHtml(_htmlToShow);
                    }
                }
                _currentRepeat++;
            }
        }
    }
}
