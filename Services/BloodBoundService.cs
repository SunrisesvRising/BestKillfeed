using BestKillfeed.Utils;
using ProjectM;
using Stunlock.Core;
using Unity.Entities;
namespace BestKillfeed.Services
{
    public static class BloodBoundService
    {
        public static void SetRewardItemBloodBound(bool value)
        {
            try
            {
                var id = new PrefabGUID(KillfeedSettings.RewardItemId.Value);
                if (!Core.PrefabCollection._PrefabGuidToEntityMap.TryGetValue(id, out var entity))
                {
                    Plugin.Logger.LogWarning($"[BloodBoundService] PrefabGUID {id.GuidHash} not found in _PrefabGuidToEntityMap.");
                    return;
                }
                var em = VWorld.ServerEntityManager;
                if (!em.HasComponent<ItemData>(entity))
                {
                    Plugin.Logger.LogWarning($"[BloodBoundService] Prefab entity {id.GuidHash} has no ItemData component.");
                    return;
                }
                var itemData = em.GetComponentData<ItemData>(entity);
                bool hasFlag = itemData.ItemCategory.HasFlag(ItemCategory.BloodBound);
                if (hasFlag == value)
                {
                    Plugin.Logger.LogInfo($"[BloodBoundService] Item {id.GuidHash} is already BloodBound={value}. No change.");
                    return;
                }
                if (value)
                    itemData.ItemCategory |= ItemCategory.BloodBound;
                else
                    itemData.ItemCategory &= ~ItemCategory.BloodBound;
                em.SetComponentData(entity, itemData);
                var itemMap = Core.GameDataSystem.ItemHashLookupMap;
                if (itemMap.ContainsKey(id))
                    itemMap[id] = itemData;
                Plugin.Logger.LogInfo($"[BloodBoundService] Item {id.GuidHash} → BloodBound={value}.");
            }
            catch (System.Exception e)
            {
                Plugin.Logger.LogError($"[BloodBoundService] Error: {e.Message}\n{e.StackTrace}");
            }
        }
    }
}