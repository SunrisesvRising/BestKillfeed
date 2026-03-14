using HarmonyLib;
using ProjectM;
using ProjectM.Network;
using Unity.Entities;
using BestKillfeed.Services;
using Unity.Collections;
using System;
using ProjectM.Gameplay.Systems;
namespace BestKillfeed.Patches
{
    [HarmonyPatch]
    public static class PlayerLevelUpdatePatch
    {
        private static bool _bloodBoundInitialized = false;
        [HarmonyPatch(typeof(WeaponLevelSystem_Spawn), nameof(WeaponLevelSystem_Spawn.OnUpdate))]
        [HarmonyPostfix]
        public static void WeaponLevelPatch(WeaponLevelSystem_Spawn __instance)
        {
            TryInitBloodBound();
            UpdateAllOnlinePlayers(__instance.EntityManager);
        }
        [HarmonyPatch(typeof(ArmorLevelSystem_Spawn), nameof(ArmorLevelSystem_Spawn.OnUpdate))]
        [HarmonyPostfix]
        public static void ArmorLevelPatch(ArmorLevelSystem_Spawn __instance)
        {
            TryInitBloodBound();
            UpdateAllOnlinePlayers(__instance.EntityManager);
        }
        private static void TryInitBloodBound()
        {
            if (_bloodBoundInitialized) return;
            try
            {
                BloodBoundService.SetRewardItemBloodBound(KillfeedSettings.RewardBloodBound.Value);
                _bloodBoundInitialized = true;
                Plugin.Logger.LogInfo("[BestKillfeed] BloodBound successfully initialized.");
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogWarning($"[BestKillfeed] BloodBound not ready yet, retrying next tick: {ex.Message}");
            }
        }
        private static void UpdateAllOnlinePlayers(EntityManager entityManager)
        {
            if (DatabaseService.DB == null) return;
            var userQuery = entityManager.CreateEntityQuery(ComponentType.ReadOnly<User>());
            var userEntities = userQuery.ToEntityArray(Allocator.Temp);
            try
            {
                foreach (var userEntity in userEntities)
                {
                    var user = entityManager.GetComponentData<User>(userEntity);
                    if (user.IsConnected)
                    {
                        PlayerService.Instance.UpdatePlayerLevel(userEntity);
                    }
                }
            }
            catch { }
            finally
            {
                userEntities.Dispose();
            }
        }
    }
}