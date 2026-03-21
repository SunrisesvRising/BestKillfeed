using HarmonyLib;
using ProjectM;
using ProjectM.Network;
using Unity.Collections;
using Unity.Entities;
using BestKillfeed.Utils;
using BestKillfeed.Services;
using ProjectM.Shared;
using BestKillfeed;

namespace BestKillfeed.Patches
{
    [HarmonyPatch(typeof(DeathEventListenerSystem), nameof(DeathEventListenerSystem.OnUpdate))]
    public static class KillfeedPatch
    {
        public static void Postfix(DeathEventListenerSystem __instance)
        {
            var entityManager = VWorld.ServerEntityManager;
            var query = entityManager.CreateEntityQuery(ComponentType.ReadOnly<DeathEvent>());
            var entities = query.ToEntityArray(Allocator.Temp);

            foreach (var entity in entities)
            {
                var deathEvent = entityManager.GetComponentData<DeathEvent>(entity);
                var victim = deathEvent.Died;
                var killer = KillCache.GetDowner(victim) ?? deathEvent.Killer;
                KillCache.Clear(victim);

                if (!entityManager.HasComponent<PlayerCharacter>(victim) || !entityManager.HasComponent<PlayerCharacter>(killer)) continue;

                var victimUserEntity = entityManager.GetComponentData<PlayerCharacter>(victim).UserEntity;
                var killerUserEntity = entityManager.GetComponentData<PlayerCharacter>(killer).UserEntity;

                if (!entityManager.HasComponent<User>(victimUserEntity) || !entityManager.HasComponent<User>(killerUserEntity)) continue;

                var victimUserData = entityManager.GetComponentData<User>(victimUserEntity);
                var killerUserData = entityManager.GetComponentData<User>(killerUserEntity);

                var victimName = victimUserData.CharacterName.ToString();
                var killerName = killerUserData.CharacterName.ToString();

                if (string.IsNullOrWhiteSpace(victimName) || string.IsNullOrWhiteSpace(killerName) || victimName == killerName) continue;

                // --- Détection FFA (soft-dependency BestFFAZones) ---
                bool isFfaKill = FfaBridge.IsFfaKill(killerUserData.PlatformId, victimUserData.PlatformId);

                PlayerService.Instance.OnPlayerKill(killerName);
                var killerData = PlayerService.Instance.GetPlayerData(killerName);

                bool announcedBounty = false;
                bool victimHadBounty = false;

                // Sauvegarde le streak de la victime AVANT le reset (pour le scaling du bounty reward)
                var victimDataBeforeDeath = PlayerService.Instance.GetPlayerData(victimName);
                int victimStreakBeforeDeath = victimDataBeforeDeath.CurrentStreak;

                if (KillfeedSettings.EnableBounty.Value)
                {
                    announcedBounty = PlayerService.Instance.CheckAndSetBounty(killerName, killerData.CurrentStreak, killerUserData.LocalCharacter._Entity);
                    victimHadBounty = PlayerService.Instance.RemoveBounty(victimName, victimUserData.LocalCharacter._Entity);
                }

                PlayerService.Instance.OnPlayerDeath(victimName);
                var victimData = PlayerService.Instance.GetPlayerData(victimName);

                // En zone FFA, le gap de niveau est toujours ignoré
                bool killAllowed = isFfaKill || IsKillAllowed(killerData.MaxLevel, victimData.MaxLevel);

                string levelColor = killAllowed ? KillfeedSettings.AllowedLevelColor.Value : KillfeedSettings.ForbiddenLevelColor.Value;
                string warningIndicator = killAllowed ? "" : $"<color={KillfeedSettings.ForbiddenLevelColor.Value}>[!] </color>";

                string streakInfo = "";
                if (KillfeedSettings.EnableBounty.Value && killerData.CurrentStreak > 1)
                {
                    streakInfo = $" <color=#ffaa00>[Streak: {killerData.CurrentStreak}]</color>";
                }

                string diffDisplay = "";
                if (!killAllowed)
                {
                    int diffValue = killerData.MaxLevel - victimData.MaxLevel;
                    diffDisplay = $" ({(diffValue >= 0 ? "Diff +" : "")}{diffValue})";
                }

                var killerClan = TruncateClan(GetClanName(entityManager, killerUserData));
                var victimClan = TruncateClan(GetClanName(entityManager, victimUserData));

                // Tag FFA affiché uniquement si les deux joueurs sont en zone FFA
                string ffaTag = isFfaKill
                    ? $"<color={KillfeedSettings.FfaTagColor.Value}>[FFA]</color> "
                    : "";

                var msgString = KillfeedSettings.KillMessageFormat.Value
                    .Replace("{FfaTag}", ffaTag)
                    .Replace("{Warning}", warningIndicator)
                    .Replace("{Killer}", killerName)
                    .Replace("{Victim}", victimName)
                    .Replace("{Streak}", streakInfo)
                    .Replace("{KillerLevel}", killerData.MaxLevel.ToString())
                    .Replace("{VictimLevel}", victimData.MaxLevel.ToString())
                    .Replace("{Diff}", diffDisplay)
                    .Replace("{KillerClan}", killerClan)
                    .Replace("{VictimClan}", victimClan)
                    .Replace("{LevelColor}", levelColor)
                    .Replace("{KillerNameColor}", KillfeedSettings.KillerNameColor.Value)
                    .Replace("{VictimNameColor}", KillfeedSettings.VictimNameColor.Value)
                    .Replace("{ClanTagColor}", KillfeedSettings.ClanTagColor.Value);

                Unity.Collections.FixedString512Bytes messageToSend = msgString;
                ServerChatUtils.SendSystemMessageToAllClients(entityManager, ref messageToSend);

                if (announcedBounty)
                {
                    var announce = $"<color=#ff0000>[!] A BOUNTY HAS BEEN PLACED ON {killerName} ({killerData.CurrentStreak} Kills) [!]</color>";
                    Unity.Collections.FixedString512Bytes fAnnounce = announce;
                    ServerChatUtils.SendSystemMessageToAllClients(entityManager, ref fAnnounce);
                    Unity.Collections.FixedString512Bytes pm2 = "<color=#ffaa00>Head to the Sacrificial Site and use <color=#ffffff>.bc</color><color=#ffaa00> to clear it and claim your reward.</color>";
                    ServerChatUtils.SendSystemMessageToClient(entityManager, killerUserData, ref pm2);
                }

                if (victimHadBounty)
                {
                    var announce = $"<color=#00ff00>★ {killerName} has claimed the bounty on {victimName}! ★</color>";
                    Unity.Collections.FixedString512Bytes fAnnounce = announce;
                    ServerChatUtils.SendSystemMessageToAllClients(entityManager, ref fAnnounce);
                    // Passe le streak de la victime avant le reset pour le scaling
                    RewardService.Instance.GiveBountyReward(killer, victimName, victimStreakBeforeDeath);
                }

                if (killAllowed && KillfeedSettings.EnableKillRewards.Value)
                {
                    RewardService.Instance.GiveKillReward(killer, killerData.CurrentStreak);
                }
            }
            entities.Dispose();
        }

        private static string GetClanName(EntityManager em, User user)
        {
            var clanEntity = user.ClanEntity._Entity;
            if (clanEntity != Entity.Null && em.HasComponent<ClanTeam>(clanEntity))
                return em.GetComponentData<ClanTeam>(clanEntity).Name.ToString();
            return "-";
        }

        private static string TruncateClan(string name)
        {
            if (string.IsNullOrEmpty(name) || name == "-") return "---";
            return name.Length <= 3 ? name : name.Substring(0, 3).ToUpper();
        }

        private static bool IsKillAllowed(int killerLevel, int victimLevel)
        {
            int diff = killerLevel - victimLevel;
            if (killerLevel >= KillfeedSettings.MaxLevel.Value)
                return diff <= KillfeedSettings.MaxLevelGapHigh.Value;
            return diff <= KillfeedSettings.MaxLevelGapNormal.Value;
        }
    }
}