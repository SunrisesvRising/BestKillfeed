using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using BestKillfeed.Services;
using HarmonyLib;
using ProjectM;
using UnityEngine;

namespace BestKillfeed.Utils
{
    public static class ChannelManager
    {
        private static readonly ConcurrentQueue<Action> _mainThreadQueue = new();

        private class ChannelState
        {
            public Timer Timer;
            public int Remaining;
            public readonly Action<int> OnTick;
            public readonly Action OnComplete;
            public volatile bool Cancelled;

            public ChannelState(int remaining, Action<int> onTick, Action onComplete)
            {
                Remaining = remaining;
                OnTick = onTick;
                OnComplete = onComplete;
                Cancelled = false;
            }
        }

        private static readonly Dictionary<string, ChannelState> _activeChannels = new();
        private static readonly object _lock = new();

        public static void StartChannel(string playerKey, int durationSeconds, Action<int> onTick, Action onComplete)
        {
            CancelChannel(playerKey);

            var state = new ChannelState(durationSeconds, onTick, onComplete);

            lock (_lock)
            {
                _activeChannels[playerKey] = state;
            }

            state.Timer = new Timer(_ =>
            {
                lock (_lock)
                {
                    if (state.Cancelled) return;

                    if (state.Remaining > 0)
                    {
                        int remaining = state.Remaining;
                        _mainThreadQueue.Enqueue(() =>
                        {
                            if (!state.Cancelled)
                                state.OnTick?.Invoke(remaining);
                        });
                        state.Remaining--;
                    }
                    else
                    {
                        state.Cancelled = true;
                        state.Timer?.Dispose();
                        _activeChannels.Remove(playerKey);

                        _mainThreadQueue.Enqueue(() =>
                        {
                            state.OnComplete?.Invoke();
                        });
                    }
                }
            },
            null,
            dueTime: 0,
            period: 1000);
        }

        public static void CancelChannel(string playerKey)
        {
            lock (_lock)
            {
                if (_activeChannels.TryGetValue(playerKey, out var state))
                {
                    state.Cancelled = true;
                    state.Timer?.Dispose();
                    _activeChannels.Remove(playerKey);
                }
            }
        }

        public static bool HasActiveChannel(string playerKey)
        {
            lock (_lock)
            {
                return _activeChannels.ContainsKey(playerKey);
            }
        }

        internal static void DrainMainThreadQueue()
        {
            while (_mainThreadQueue.TryDequeue(out var action))
            {
                try { action(); }
                catch (Exception e)
                {
                    BepInEx.Logging.Logger.CreateLogSource("ChannelManager")
                        .LogError($"Error in main thread callback: {e.Message}\n{e.StackTrace}");
                }
            }
        }
    }

    [HarmonyPatch(typeof(ServerBootstrapSystem), nameof(ServerBootstrapSystem.OnUpdate))]
    public static class ChannelManagerUpdatePatch
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            ChannelManager.DrainMainThreadQueue();
        }
    }

    [HarmonyPatch(typeof(ServerBootstrapSystem), nameof(ServerBootstrapSystem.OnUserConnected))]
    public static class GameDataInitPatch
    {
        private static bool _initialized = false;

        [HarmonyPostfix]
        public static void Postfix()
        {
            if (_initialized) return;
            _initialized = true;
            ClaimZoneManager.SpawnIfNotExists();
        }
    }
}