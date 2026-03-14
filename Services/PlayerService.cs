using BestKillfeed.Database;
using BestKillfeed.Utils;
using LiteDB;
using ProjectM;
using ProjectM.Network;
using Unity.Entities;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace BestKillfeed.Services
{
    public class PlayerService
    {
        private static PlayerService _instance;
        public static PlayerService Instance => _instance ??= new PlayerService();

        private ILiteCollection<PlayersData> _collection
        {
            get
            {
                if (DatabaseService.DB == null) DatabaseService.Initialize();
                return DatabaseService.DB.GetCollection<PlayersData>("players");
            }
        }

        public PlayersData GetPlayerData(string playerName)
        {
            if (string.IsNullOrEmpty(playerName)) return null;
            try
            {
                return _collection.FindOne(x => x.Name == playerName) ?? new PlayersData { Name = playerName };
            }
            catch
            {
                return new PlayersData { Name = playerName };
            }
        }

        public void SavePlayerData(PlayersData data)
        {
            if (data == null) return;
            try { _collection.Upsert(data); } catch { }
        }

        public List<PlayersData> GetAllPlayers() => _collection.FindAll().ToList();

        public void UpdatePlayerLevel(Entity userEntity)
        {
            var em = VWorld.ServerEntityManager;
            if (!em.HasComponent<User>(userEntity)) return;

            var user = em.GetComponentData<User>(userEntity);
            var charEntity = user.LocalCharacter._Entity;

            if (charEntity == Entity.Null || !em.HasComponent<Equipment>(charEntity)) return;

            var eq = em.GetComponentData<Equipment>(charEntity);
            int currentLevel = Mathf.RoundToInt(eq.ArmorLevel + eq.SpellLevel + eq.WeaponLevel);

            var data = GetPlayerData(user.CharacterName.ToString());
            data.SteamID = user.PlatformId;

            if (currentLevel > data.MaxLevel)
            {
                data.MaxLevel = currentLevel;
                SavePlayerData(data);
            }
        }

        public void OnPlayerKill(string killerName)
        {
            var data = GetPlayerData(killerName);
            data.Kills++;
            data.CurrentStreak++;
            if (data.CurrentStreak > data.MaxStreak) data.MaxStreak = data.CurrentStreak;
            SavePlayerData(data);
        }

        public void OnPlayerDeath(string victimName)
        {
            var data = GetPlayerData(victimName);
            data.Deaths++;
            data.CurrentStreak = 0;
            SavePlayerData(data);
        }

        // --- Bounty Management Methods ---

        public bool CheckAndSetBounty(string playerName, int currentStreak, Entity characterEntity)
        {
            var data = GetPlayerData(playerName);
            if (currentStreak >= KillfeedSettings.BountyStreakThreshold.Value && !data.HasBounty)
            {
                data.HasBounty = true;
                SavePlayerData(data);
                BountyService.Instance.AddBountyIcon(characterEntity);
                return true;
            }
            return false;
        }

        public bool RemoveBounty(string playerName, Entity characterEntity)
        {
            var data = GetPlayerData(playerName);
            if (data.HasBounty)
            {
                data.HasBounty = false;
                SavePlayerData(data);
                BountyService.Instance.RemoveBountyIcon(characterEntity);
                return true;
            }
            return false;
        }
    }
}