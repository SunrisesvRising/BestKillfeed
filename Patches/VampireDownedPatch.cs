using BestKillfeed.Utils;
using HarmonyLib;
using ProjectM;
using ProjectM.Network;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace BestKillfeed.Patches
{
    [HarmonyPatch(typeof(VampireDownedServerEventSystem), nameof(VampireDownedServerEventSystem.OnUpdate))]
    public static class VampireDownedPatch
    {
        public static void Prefix(VampireDownedServerEventSystem __instance)
        {
            var entityManager = Core.Server.EntityManager;
            var downedEvents = __instance.__query_1174204813_0.ToEntityArray(Allocator.Temp); // downed players

            foreach (var entity in downedEvents)
            {
                if (!VampireDownedServerEventSystem.TryFindRootOwner(entity, 1, entityManager, out var victimEntity))
                    continue;

                var downBuff = entityManager.GetComponentData<VampireDownedBuff>(entity);

                if (!VampireDownedServerEventSystem.TryFindRootOwner(downBuff.Source, 1, entityManager, out var killerEntity))
                    continue;

                if (!entityManager.HasComponent<PlayerCharacter>(victimEntity) || !entityManager.HasComponent<PlayerCharacter>(killerEntity))
                    continue;

                var victimUserEntity = entityManager.GetComponentData<PlayerCharacter>(victimEntity).UserEntity;
                var killerUserEntity = entityManager.GetComponentData<PlayerCharacter>(killerEntity).UserEntity;

                if (!entityManager.HasComponent<User>(victimUserEntity) || !entityManager.HasComponent<User>(killerUserEntity))
                    continue;

                var victimName = entityManager.GetComponentData<User>(victimUserEntity).CharacterName.ToString();
                var killerName = entityManager.GetComponentData<User>(killerUserEntity).CharacterName.ToString();

                KillCache.SetDowner(victimEntity, killerEntity);
            }

            downedEvents.Dispose();
        }
    }
}
