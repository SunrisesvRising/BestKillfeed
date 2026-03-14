using ProjectM;
using ProjectM.Network;
using Unity.Entities;
using VampireCommandFramework;
using Unity.Collections;
using System.Collections.Generic;
using BestKillfeed.Services;
using BestKillfeed.Utils;
using System;
using System.Linq;

#nullable enable
namespace BestKillfeed.Commands
{
    internal class PlayerInfo
    {
        [Command("playerinfo", "pi", description: "Displays detailed information about a player.", adminOnly: false)]
        public static void HandleCommand(ChatCommandContext ctx, string targetName)
        {
            var entityManager = VWorld.ServerEntityManager;

            var userQuery = entityManager.CreateEntityQuery(ComponentType.ReadOnly<User>());
            var userEntities = userQuery.ToEntityArray(Allocator.Temp);

            Entity targetUserEntity = Entity.Null;
            User targetUserData = default;
            bool targetIsOnline = false;

            foreach (var entity in userEntities)
            {
                var userData = entityManager.GetComponentData<User>(entity);
                if (userData.CharacterName.ToString().Equals(targetName, StringComparison.OrdinalIgnoreCase))
                {
                    targetUserEntity = entity;
                    targetUserData = userData;
                    targetIsOnline = userData.IsConnected;
                    break;
                }
            }

            if (targetUserEntity == Entity.Null)
            {
                ctx.Reply($"<color=#ff5555>Player '{targetName}' not found.</color>");
                userEntities.Dispose();
                return;
            }

            var stats = PlayerService.Instance.GetPlayerData(targetName);
            string clanName = GetClanName(entityManager, targetUserData);
            var targetClanEntity = targetUserData.ClanEntity._Entity;

            ctx.Reply("<color=#FFD700>-- Player Info --</color>");
            string nameColor = targetIsOnline ? "#00FF00" : "#FF0000";
            ctx.Reply($"<color=#FFFF00>Name :</color> <color={nameColor}>{targetName}</color> <color=#00ffff>[{stats.MaxLevel}]</color>");
            ctx.Reply($"<color=#ffaa00>K:</color> <color=#55ff55>{stats.Kills}</color> | <color=#ffaa00>D:</color> <color=#ff5555>{stats.Deaths}</color> | <color=#ffaa00>KD:</color> <color=#ffffff>{CalculateKD(stats.Kills, stats.Deaths)}</color> | <color=#ffaa00>S:</color> <color=#55aaff>{stats.CurrentStreak}</color>");
            ctx.Reply($"<color=#FFFF00>Clan:</color> <color=#FFFFFF>{clanName}</color>");

            if (targetClanEntity != Entity.Null)
            {
                List<string> clanMembersInfo = new List<string>();
                int clanKills = 0, clanDeaths = 0;

                foreach (var entity in userEntities)
                {
                    var uData = entityManager.GetComponentData<User>(entity);
                    if (uData.ClanEntity._Entity == targetClanEntity)
                    {
                        var memberName = uData.CharacterName.ToString();
                        var mStats = PlayerService.Instance.GetPlayerData(memberName);
                        string mColor = uData.IsConnected ? "#00FF00" : "#FF0000";
                        clanMembersInfo.Add($"<color={mColor}>{memberName}</color> <color=#00ffff>[{mStats.MaxLevel}]</color>");
                        clanKills += mStats.Kills;
                        clanDeaths += mStats.Deaths;
                    }
                }

                ctx.Reply($"<color=#FFFF00>Clan members :</color> {string.Join(", ", clanMembersInfo)}");
                ctx.Reply($"<color=#FFFF00>Clan Stats :</color> <color=#ffaa00>K:</color> <color=#55ff55>{clanKills}</color> | <color=#ffaa00>D:</color> <color=#ff5555>{clanDeaths}</color> | <color=#ffaa00>KD:</color> <color=#ffffff>{CalculateKD(clanKills, clanDeaths)}</color>");
            }

            userEntities.Dispose();
        }

        private static string GetClanName(EntityManager entityManager, User user)
        {
            var clanEntity = user.ClanEntity._Entity;
            if (clanEntity != Entity.Null && entityManager.HasComponent<ClanTeam>(clanEntity))
            {
                return entityManager.GetComponentData<ClanTeam>(clanEntity).Name.ToString();
            }
            return "No Clan";
        }

        private static string CalculateKD(int kills, int deaths)
        {
            if (deaths == 0) return kills.ToString("F1");
            return ((double)kills / deaths).ToString("F2");
        }
    }
}