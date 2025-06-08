using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using UnityEngine;
using BestKillfeed;
using Unity.Entities;

namespace BestKillfeed.Services
{
    public class StatsService
    {
        private static readonly string ConfigPath = Path.Combine(BepInEx.Paths.ConfigPath, "Bestkillfeed");
        private static readonly string PlayerStatsPath = Path.Combine(ConfigPath, "PlayerStats.json");

        public delegate void KillStreakChangedHandler(string playerName, Entity playerEntity, int currentStreak);
        public event KillStreakChangedHandler OnKillStreakChanged;
        private Dictionary<string, PlayerStats> playerStats = new();

        private static StatsService _instance;
        public static StatsService Instance => _instance ??= new StatsService();

        private StatsService()
        {
            Load();
        }

        public void IncrementKill(string playerName, Entity playerEntity)
        {
            if (string.IsNullOrWhiteSpace(playerName)) return;

            if (!playerStats.ContainsKey(playerName))
                playerStats[playerName] = new PlayerStats();

            var stats = playerStats[playerName];
            stats.Kills++;
            stats.CurrentStreak++;

            if (stats.CurrentStreak > stats.MaxStreak)
                stats.MaxStreak = stats.CurrentStreak;
            OnKillStreakChanged?.Invoke(playerName, playerEntity, stats.CurrentStreak);
            Save();
        }

        public void IncrementDeath(string playerName, Entity playerEntity)
        {
            if (string.IsNullOrWhiteSpace(playerName)) return;

            if (!playerStats.ContainsKey(playerName))
                playerStats[playerName] = new PlayerStats();

            var stats = playerStats[playerName];
            stats.Deaths++;
            stats.CurrentStreak = 0;
            OnKillStreakChanged?.Invoke(playerName, playerEntity, 0);
            Save();
        }

        public PlayerStats GetStats(string playerName)
        {
            return playerStats.TryGetValue(playerName, out var stats) ? stats : new PlayerStats();
        }

        public Dictionary<string, PlayerStats> GetAllStats()
        {
            return playerStats;
        }

        private void Load()
        {
            try
            {
                if (!File.Exists(PlayerStatsPath))
                {
                    playerStats = new Dictionary<string, PlayerStats>();
                    return;
                }

                var json = File.ReadAllText(PlayerStatsPath);
                playerStats = JsonSerializer.Deserialize<Dictionary<string, PlayerStats>>(json)
                              ?? new Dictionary<string, PlayerStats>();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[StatsService] Failed to load stats: {ex}");
                playerStats = new Dictionary<string, PlayerStats>();
            }
        }

        private void Save()
        {
            try
            {
                var json = JsonSerializer.Serialize(playerStats, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(PlayerStatsPath, json);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[StatsService] Failed to save stats: {ex}");
            }
        }
    }

    public class PlayerStats
    {
        public int Kills { get; set; } = 0;
        public int Deaths { get; set; } = 0;
        public int CurrentStreak { get; set; } = 0;
        public int MaxStreak { get; set; } = 0;
    }
}