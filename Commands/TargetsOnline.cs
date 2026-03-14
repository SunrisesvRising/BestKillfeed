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
    internal class MatchmakingCommand1
    {
        [Command("targetsonline", "ton", description: "Displays unique online clans and solo players within level gap.", adminOnly: false)]
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
            if (callerData == null)
            {
                ctx.Reply("<color=#ff0000>Error: Your player data was not found. Try reconnecting.</color>");
                return;
            }
            int callerLevel = callerData.MaxLevel;

            var userQuery = entityManager.CreateEntityQuery(ComponentType.ReadOnly<User>());
            var userEntities = userQuery.ToEntityArray(Allocator.Temp);

            List<string> uniqueTargets = new List<string>();
            HashSet<Entity> processedClans = new HashSet<Entity>();

            try
            {
                foreach (var entity in userEntities)
                {
                    var userData = entityManager.GetComponentData<User>(entity);
                    string targetName = userData.CharacterName.ToString();

                    if (string.IsNullOrEmpty(targetName) || targetName == callerName || !userData.IsConnected) continue;

                    var targetData = PlayerService.Instance.GetPlayerData(targetName);
                    if (targetData == null) continue;

                    if (IsTargetCompatible(callerLevel, targetData.MaxLevel))
                    {
                        var clanEntity = userData.ClanEntity._Entity;

                        if (clanEntity != Entity.Null)
                        {
                            if (processedClans.Contains(clanEntity)) continue;
                            processedClans.Add(clanEntity);

                            uniqueTargets.Add(GetClanLine(entityManager, clanEntity, userEntities));
                        }
                        else
                        {
                            string bountyPrefix = targetData.HasBounty ? "<color=#ff0000>[BOUNTY] </color>" : "";
                            uniqueTargets.Add($"{bountyPrefix}<color=#ffffff>{targetName}</color> <color=#00ffff>[Lvl {targetData.MaxLevel}]</color> <color=#777777>(No Clan)</color>");
                        }
                    }
                }

                int total = uniqueTargets.Count;
                int totalPages = (int)Math.Ceiling((double)total / pageSize);

                if (total == 0)
                {
                    ctx.Reply($"<color=#ffaa00>--- Online Targets (Your Lvl: {callerLevel}) ---</color>");
                    ctx.Reply("<color=#aaaaaa>No players within your level range are online.</color>");
                    return;
                }

                page = Math.Clamp(page, 1, totalPages);
                int startIndex = (page - 1) * pageSize;
                int count = Math.Min(pageSize, total - startIndex);

                ctx.Reply($"<color=#ffaa00>--- Online Targets (Page {page}/{totalPages}) ---</color>");
                for (int i = 0; i < count; i++)
                {
                    ctx.Reply(uniqueTargets[startIndex + i]);
                }

                if (totalPages > 1) ctx.Reply("<color=#aaaaaa>Use .ton [page] to see more.</color>");
            }
            catch (Exception ex)
            {
                ctx.Reply("<color=#ff0000>An error occurred while fetching targets.</color>");
                UnityEngine.Debug.LogError($"[BestKillfeed] Matchmaking Error: {ex.Message}");
            }
            finally
            {
                userEntities.Dispose();
            }
        }

        private static bool IsTargetCompatible(int myLevel, int targetLevel)
        {
            int diff = Math.Abs(myLevel - targetLevel);
            if (myLevel >= KillfeedSettings.MaxLevel.Value)
                return diff <= KillfeedSettings.MaxLevelGapHigh.Value;
            return diff <= KillfeedSettings.MaxLevelGapNormal.Value;
        }

        private static string GetClanLine(EntityManager em, Entity clanEntity, NativeArray<Entity> allUsers)
        {
            string clanName = "Clan";
            if (em.Exists(clanEntity) && em.HasComponent<ClanTeam>(clanEntity))
            {
                clanName = em.GetComponentData<ClanTeam>(clanEntity).Name.ToString();
            }

            List<string> members = new List<string>();

            foreach (var userEnt in allUsers)
            {
                var uData = em.GetComponentData<User>(userEnt);
                if (uData.ClanEntity._Entity == clanEntity)
                {
                    string mName = uData.CharacterName.ToString();
                    var mStats = PlayerService.Instance.GetPlayerData(mName);

                    if (mStats == null) continue;

                    string color = uData.IsConnected ? "#00FF00" : "#777777";
                    members.Add($"<color={color}>{mName}</color> ({mStats.MaxLevel})");
                }
            }

            return $"<color=#FFFF00>Clan: [{clanName}]</color> {string.Join(", ", members)}";
        }
    }
}