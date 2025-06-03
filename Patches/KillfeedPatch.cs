using HarmonyLib;
using ProjectM;
using ProjectM.Network;
using Unity.Collections;
using Unity.Entities;
using BestKillfeed.Utils;
using BestKillfeed.Services;
using UnityEngine;
using BestKillfeed;

namespace BestKillfeed.Patches
{
    [HarmonyPatch(typeof(DeathEventListenerSystem), nameof(DeathEventListenerSystem.OnUpdate))]
    public static class KillfeedPatch
    {
            public static void Postfix(DeathEventListenerSystem __instance)
        {
            var entityManager = Core.Server.EntityManager;
            var query = entityManager.CreateEntityQuery(ComponentType.ReadOnly<DeathEvent>());
            var entities = query.ToEntityArray(Allocator.Temp);

            foreach (var entity in entities)
            {
                if (!entityManager.HasComponent<DeathEvent>(entity)) continue;

                var deathEvent = entityManager.GetComponentData<DeathEvent>(entity);
                var victim = deathEvent.Died;
                var killer = KillCache.GetDowner(victim) ?? deathEvent.Killer;
                KillCache.Clear(victim);

                if (!entityManager.HasComponent<PlayerCharacter>(victim) || !entityManager.HasComponent<PlayerCharacter>(killer)) continue;

                var victimUser = entityManager.GetComponentData<PlayerCharacter>(victim).UserEntity;
                var killerUser = entityManager.GetComponentData<PlayerCharacter>(killer).UserEntity;

                if (!entityManager.HasComponent<User>(victimUser) || !entityManager.HasComponent<User>(killerUser)) continue;

                var victimUserData = entityManager.GetComponentData<User>(victimUser);
                var killerUserData = entityManager.GetComponentData<User>(killerUser);

                var victimName = victimUserData.CharacterName.ToString();
                var killerName = killerUserData.CharacterName.ToString();

                if (string.IsNullOrWhiteSpace(victimName) || string.IsNullOrWhiteSpace(killerName)) continue;
                if (victimName == killerName) continue;

                // Update levels
                LevelService.Instance.UpdatePlayerLevel(killerUser);
                LevelService.Instance.UpdatePlayerLevel(victimUser);

                // Register kill and death
                StatsService.Instance.IncrementKill(killerName, killer);
                StatsService.Instance.IncrementDeath(victimName, victim);

                var killerLevel = LevelService.Instance.GetMaxLevel(killerName);
                var victimLevel = LevelService.Instance.GetMaxLevel(victimName);

                var killerClan = TruncateClan(GetClanName(entityManager, killerUserData));
                var victimClan = TruncateClan(GetClanName(entityManager, victimUserData));

                // Determine kill validity
                var killAllowed = IsKillAllowed(killerLevel, victimLevel);

                // Colors from settings
                var killerLevelColor = KillfeedSettings.AllowedLevelColor.Value;
                var victimLevelColor = killAllowed
                    ? KillfeedSettings.AllowedLevelColor.Value
                    : KillfeedSettings.ForbiddenLevelColor.Value;

                var killerNameColor = KillfeedSettings.KillerNameColor.Value;
                var victimNameColor = KillfeedSettings.VictimNameColor.Value;
                var clanColor = KillfeedSettings.ClanTagColor.Value;

                // Format message using template
                var msg = KillfeedSettings.KillMessageFormat.Value
                    .Replace("{Killer}", killerName)
                    .Replace("{Victim}", victimName)
                    .Replace("{KillerClan}", killerClan)
                    .Replace("{VictimClan}", victimClan)
                    .Replace("{KillerLevel}", killerLevel.ToString())
                    .Replace("{VictimLevel}", victimLevel.ToString())
                    .Replace("{LevelColor}", victimLevelColor) // used for both in default template
                    .Replace("{KillerNameColor}", killerNameColor)
                    .Replace("{VictimNameColor}", victimNameColor)
                    .Replace("{ClanTagColor}", clanColor);

                FixedString512Bytes message = msg;
                ServerChatUtils.SendSystemMessageToAllClients(entityManager, ref message);
            }

            entities.Dispose();
        }

        private static string GetClanName(EntityManager entityManager, User user)
        {
            var clanEntity = user.ClanEntity._Entity;
            if (clanEntity != Entity.Null && entityManager.HasComponent<ClanTeam>(clanEntity))
            {
                var clanData = entityManager.GetComponentData<ClanTeam>(clanEntity);
                return clanData.Name.ToString();
            }
            return "-";
        }

        private static string TruncateClan(string name)
        {
            return string.IsNullOrEmpty(name) ? "---" :
                   name.Length <= 3 ? name :
                   name.Substring(0, 3).ToUpper();
        }

        private static bool IsKillAllowed(int killerLevel, int victimLevel)
        {
            int diff = killerLevel - victimLevel;
            if (killerLevel >= 91)
                return diff <= KillfeedSettings.MaxLevelGapHigh.Value;
            return diff <= KillfeedSettings.MaxLevelGapNormal.Value;
        }
    }
}
