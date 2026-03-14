using BestKillfeed.Services;
using VampireCommandFramework;
using ProjectM.Network;
using Unity.Entities;
using BestKillfeed.Utils;
using ProjectM;
using System.Collections.Generic;
using Unity.Collections;
using System;
using System.Linq;

namespace BestKillfeed.Commands
{
    internal class MatchmakingOfflineCommand
    {
        [Command("targetsoffline", "toff", description: "Displays unique offline clans and solo players within level gap.", adminOnly: false)]
        public static void HandleCommand(ChatCommandContext ctx, string arg = null)
        {
            int pageSize = 5;
            int page = 1;
            if (!string.IsNullOrWhiteSpace(arg) && int.TryParse(arg, out var parsedPage))
            {
                page = Math.Max(1, parsedPage);
            }

            var entityManager = VWorld.ServerEntityManager;
            var callerUser = ctx.Event.User;
            var callerName = callerUser.CharacterName.ToString();
            var callerData = PlayerService.Instance.GetPlayerData(callerName);
            int callerLevel = callerData.MaxLevel;

            var allPlayersInDb = PlayerService.Instance.GetAllPlayers();
            var userQuery = entityManager.CreateEntityQuery(ComponentType.ReadOnly<User>());
            var onlineUserEntities = userQuery.ToEntityArray(Allocator.Temp);

            List<string> uniqueTargets = new List<string>();
            HashSet<Entity> processedClans = new HashSet<Entity>();

            try
            {
                foreach (var targetData in allPlayersInDb)
                {
                    string targetName = targetData.Name;
                    if (targetName.Equals(callerName, StringComparison.OrdinalIgnoreCase)) continue;

                    // On ne garde que les joueurs hors-ligne
                    if (IsPlayerOnline(targetName, onlineUserEntities, entityManager)) continue;

                    if (IsTargetCompatible(callerLevel, targetData.MaxLevel))
                    {
                        var targetUserEnt = GetUserEntity(targetName, onlineUserEntities, entityManager);

                        if (targetUserEnt != Entity.Null)
                        {
                            var targetUser = entityManager.GetComponentData<User>(targetUserEnt);
                            var clanEntity = targetUser.ClanEntity._Entity;

                            if (clanEntity != Entity.Null)
                            {
                                // GESTION CLAN : Un seul affichage par clan
                                if (processedClans.Contains(clanEntity)) continue;
                                processedClans.Add(clanEntity);

                                uniqueTargets.Add(GetClanLine(entityManager, clanEntity, onlineUserEntities));
                            }
                            else
                            {
                                // JOUEUR SOLO (Pas de clan)
                                string bountyPrefix = targetData.HasBounty ? "<color=#ff0000>[BOUNTY] </color>" : "";
                                uniqueTargets.Add($"{bountyPrefix}<color=#777777>{targetName}</color> <color=#00ffff>[Lvl {targetData.MaxLevel}]</color> <color=#777777>(No Clan)</color>");
                            }
                        }
                    }
                }

                int total = uniqueTargets.Count;
                int totalPages = (int)Math.Ceiling((double)total / pageSize);

                if (total == 0)
                {
                    ctx.Reply($"<color=#ffaa00>--- Offline Targets (Your Lvl: {callerLevel}) ---</color>");
                    ctx.Reply("<color=#aaaaaa>No offline targets found within range.</color>");
                    return;
                }

                page = Math.Clamp(page, 1, totalPages);
                int startIndex = (page - 1) * pageSize;
                int count = Math.Min(pageSize, total - startIndex);

                ctx.Reply($"<color=#ffaa00>--- Offline Targets (Page {page}/{totalPages}) ---</color>");
                for (int i = 0; i < count; i++)
                {
                    ctx.Reply(uniqueTargets[startIndex + i]);
                }

                if (totalPages > 1) ctx.Reply("<color=#aaaaaa>Use .toff [page] to see more.</color>");
            }
            finally
            {
                onlineUserEntities.Dispose();
            }
        }

        private static bool IsTargetCompatible(int myLevel, int targetLevel)
        {
            int diff = Math.Abs(myLevel - targetLevel);
            if (myLevel >= KillfeedSettings.MaxLevel.Value)
                return diff <= KillfeedSettings.MaxLevelGapHigh.Value;
            return diff <= KillfeedSettings.MaxLevelGapNormal.Value;
        }

        private static bool IsPlayerOnline(string name, NativeArray<Entity> onlineUsers, EntityManager em)
        {
            foreach (var ent in onlineUsers)
            {
                var userData = em.GetComponentData<User>(ent);
                if (userData.CharacterName.ToString().Equals(name, StringComparison.OrdinalIgnoreCase))
                    return userData.IsConnected;
            }
            return false;
        }

        private static Entity GetUserEntity(string name, NativeArray<Entity> onlineUsers, EntityManager em)
        {
            foreach (var ent in onlineUsers)
            {
                if (em.GetComponentData<User>(ent).CharacterName.ToString().Equals(name, StringComparison.OrdinalIgnoreCase))
                    return ent;
            }
            return Entity.Null;
        }

        private static string GetClanLine(EntityManager em, Entity clanEntity, NativeArray<Entity> onlineUsers)
        {
            string clanName = em.HasComponent<ClanTeam>(clanEntity)
                ? em.GetComponentData<ClanTeam>(clanEntity).Name.ToString()
                : "Clan";

            List<string> members = new List<string>();

            foreach (var userEnt in onlineUsers)
            {
                var uData = em.GetComponentData<User>(userEnt);
                if (uData.ClanEntity._Entity == clanEntity)
                {
                    string mName = uData.CharacterName.ToString();
                    var mStats = PlayerService.Instance.GetPlayerData(mName);
                    string color = uData.IsConnected ? "#00FF00" : "#FF0000";
                    members.Add($"<color={color}>{mName}</color> ({mStats.MaxLevel})");
                }
            }

            return $"<color=#FFFF00>Clan: [{clanName}]</color> {string.Join(", ", members)}";
        }
    }
}