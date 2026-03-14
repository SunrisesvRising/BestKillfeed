using ProjectM;
using ProjectM.Network;
using Stunlock.Core;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using BestKillfeed;
using BestKillfeed.Utils;

namespace BestKillfeed.Services
{
    public static class ClaimZoneManager
    {
        private static readonly PrefabGUID IconPrefab = new PrefabGUID(139106998);
        private static readonly float3 ZoneCenter = new float3(-1585f, 0f, -936f);

        private static Entity _iconEntity = Entity.Null;
        private static readonly BepInEx.Logging.ManualLogSource Log =
            BepInEx.Logging.Logger.CreateLogSource("ClaimZoneManager");

        public static void SpawnIfNotExists()
        {
            if (!KillfeedSettings.EnableBounty.Value) return;

            var em = VWorld.ServerEntityManager;

            if (_iconEntity != Entity.Null && em.Exists(_iconEntity))
                return;

            if (!Core.PrefabCollection._PrefabGuidToEntityMap.TryGetValue(IconPrefab, out var iconPrefabEntity))
            {
                Log.LogWarning($"PrefabGUID {IconPrefab.GuidHash} not found in _PrefabGuidToEntityMap.");
                return;
            }

            var icon = em.Instantiate(iconPrefabEntity);

            if (em.HasComponent<MapIconTargetEntity>(icon))
                em.RemoveComponent<MapIconTargetEntity>(icon);

            if (em.HasComponent<SyncToUserBitMask>(icon))
                em.RemoveComponent<SyncToUserBitMask>(icon);

            if (em.HasComponent<SyncToUserBuffer>(icon))
                em.RemoveComponent<SyncToUserBuffer>(icon);

            if (em.HasComponent<OnlySyncToUsersTag>(icon))
                em.RemoveComponent<OnlySyncToUsersTag>(icon);

            if (em.HasComponent<Translation>(icon))
                em.SetComponentData(icon, new Translation { Value = ZoneCenter });

            if (em.HasComponent<LocalToWorld>(icon))
                em.SetComponentData(icon, new LocalToWorld
                {
                    Value = float4x4.TRS(ZoneCenter, quaternion.identity, new float3(1f))
                });

            _iconEntity = icon;
            Log.LogInfo($"Claim zone map icon spawned at {ZoneCenter}. Entity: {icon}");
        }

        public static void DestroyIcon()
        {
            var em = VWorld.ServerEntityManager;
            if (_iconEntity == Entity.Null || !em.Exists(_iconEntity)) return;

            var users = em.CreateEntityQuery(ComponentType.ReadOnly<User>())
                          .ToEntityArray(Unity.Collections.Allocator.Temp);
            var userEntity = users.Length > 0 ? users[0] : _iconEntity;
            users.Dispose();

            StatChangeUtility.KillOrDestroyEntity(em, _iconEntity, userEntity, userEntity, 0, StatChangeReason.Any, true);
            _iconEntity = Entity.Null;
        }
    }
}