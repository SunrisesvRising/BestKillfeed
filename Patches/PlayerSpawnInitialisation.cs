using HarmonyLib;
using ProjectM;
using Unity.Collections;
using ProjectM.Network;
using BestKillfeed.Services;
using UnityEngine;
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

            foreach (var entity in entities)
            {
                // Replace this with the correct GUID once confirmed
                var guid = entityManager.GetComponentData<PrefabGUID>(entity);
                if (guid.GuidHash != 722466953) continue;

                var owner = entityManager.GetComponentData<EntityOwner>(entity).Owner;
                if (!entityManager.HasComponent<PlayerCharacter>(owner)) continue;

                var userEntity = entityManager.GetComponentData<PlayerCharacter>(owner).UserEntity;
                if (!entityManager.HasComponent<User>(userEntity)) continue;

                var user = entityManager.GetComponentData<User>(userEntity);
                var characterName = user.CharacterName.ToString();

                if (!string.IsNullOrWhiteSpace(characterName))
                {
                    LevelService.Instance.InitPlayerLevel(characterName);
                    Debug.Log($"[BestKillfeed] New player detected: {characterName}. Level initialized to 0.");
                }
            }

            entities.Dispose();
        }
    }
}