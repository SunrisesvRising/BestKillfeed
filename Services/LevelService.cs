using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Unity.Entities;
using ProjectM;
using ProjectM.Network;
using UnityEngine;
using BestKillfeed.Utils;

namespace BestKillfeed.Services
{
    public class LevelService
    {
        public static LevelService Instance = new();

        private static readonly string ConfigPath = Path.Combine(BepInEx.Paths.ConfigPath, "Bestkillfeed");
        private static readonly string MaxLevelsPath = Path.Combine(ConfigPath, "MaxLevels.json");

        internal Dictionary<string, int> maxPlayerLevels = new();

        public LevelService()
        {
            Load();
        }

        public int GetMaxLevel(string playerName)
        {
            return maxPlayerLevels.TryGetValue(playerName, out var level) ? level : 0;
        }

        public void UpdatePlayerLevel(Entity userEntity)
        {
            var entityManager = Core.Server.EntityManager;

            if (!entityManager.HasComponent<User>(userEntity)) return;

            var user = entityManager.GetComponentData<User>(userEntity);
            var charEntity = user.LocalCharacter._Entity;

            if (!entityManager.HasComponent<Equipment>(charEntity)) return;

            var equipment = entityManager.GetComponentData<Equipment>(charEntity);
            var currentLevel = Mathf.RoundToInt(equipment.ArmorLevel + equipment.SpellLevel + equipment.WeaponLevel);

            var charName = user.CharacterName.ToString();

            if (!maxPlayerLevels.TryGetValue(charName, out var savedLevel) || currentLevel > savedLevel)
            {
                maxPlayerLevels[charName] = currentLevel;
                Save();
                Debug.Log($"[BestKillfeed] New max level detected for {charName}: {currentLevel}");
            }
        }

        private void Load()
        {
            if (!File.Exists(MaxLevelsPath)) return;

            try
            {
                var json = File.ReadAllText(MaxLevelsPath);
                maxPlayerLevels = JsonSerializer.Deserialize<Dictionary<string, int>>(json) ?? new();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LevelService] Failed to load max levels: {ex}");
            }
        }

        internal void Save()
        {
            try
            {
                if (!Directory.Exists(ConfigPath))
                    Directory.CreateDirectory(ConfigPath);

                var json = JsonSerializer.Serialize(maxPlayerLevels, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(MaxLevelsPath, json);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LevelService] Failed to save max levels: {ex}");
            }
        }

        public bool HasEntry(string playerName)
        {
            return maxPlayerLevels.ContainsKey(playerName);
        }

        public void InitPlayerLevel(string playerName)
        {
            if (maxPlayerLevels.Count == 0)
            {
                Load();
            }

            if (!maxPlayerLevels.ContainsKey(playerName))
            {
                maxPlayerLevels[playerName] = 0;
                Save();
            }
        }
    }
}
