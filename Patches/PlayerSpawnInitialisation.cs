using HarmonyLib;
using ProjectM;
using Unity.Collections;
using ProjectM.Network;
using BestKillfeed.Services;
using Stunlock.Core;

namespace BestKillfeed.Patches
{
    [HarmonyPatch(typeof(Destroy_TravelBuffSystem), nameof(Destroy_TravelBuffSystem.OnUpdate))]
    public class PlayerCreationPatch
    {
        public static void Postfix(Destroy_TravelBuffSystem __instance)
        {
            var entityManager = __instance.EntityManager;
            var entities = __instance.__query_615927226_0.ToEntityArray(Allocator.Temp);

            try
            {
                foreach (var entity in entities)
                {
                    var guid = entityManager.GetComponentData<PrefabGUID>(entity);
                    if (guid.GuidHash != 722466953) continue;

                    if (!entityManager.HasComponent<EntityOwner>(entity)) continue;
                    var owner = entityManager.GetComponentData<EntityOwner>(entity).Owner;

                    if (!entityManager.HasComponent<PlayerCharacter>(owner)) continue;
                    var userEntity = entityManager.GetComponentData<PlayerCharacter>(owner).UserEntity;

                    if (!entityManager.HasComponent<User>(userEntity)) continue;
                    var user = entityManager.GetComponentData<User>(userEntity);

                    string characterName = user.CharacterName.ToString();

                    if (!string.IsNullOrWhiteSpace(characterName))
                    {
                        var data = PlayerService.Instance.GetPlayerData(characterName);
                        data.SteamID = user.PlatformId;
                        PlayerService.Instance.SavePlayerData(data);
                    }
                }
            }
            finally
            {
                entities.Dispose();
            }
        }
    }
}