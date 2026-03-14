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
            var entityManager = VWorld.ServerEntityManager;
            var downedEvents = __instance.__query_1174204813_0.ToEntityArray(Allocator.Temp);

            foreach (var entity in downedEvents)
            {
                if (!VampireDownedServerEventSystem.TryFindRootOwner(entity, 1, entityManager, out var victimEntity))
                    continue;

                var downBuff = entityManager.GetComponentData<VampireDownedBuff>(entity);

                if (!VampireDownedServerEventSystem.TryFindRootOwner(downBuff.Source, 1, entityManager, out var killerEntity))
                    continue;

                if (!entityManager.HasComponent<PlayerCharacter>(victimEntity) || !entityManager.HasComponent<PlayerCharacter>(killerEntity))
                    continue;
                KillCache.SetDowner(victimEntity, killerEntity);
            }

            downedEvents.Dispose();
        }
    }
}