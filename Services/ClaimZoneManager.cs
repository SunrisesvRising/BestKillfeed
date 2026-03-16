using ProjectM;
using ProjectM.Network;
using ProjectM.Terrain;
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

        // zoneData.Name.ToString() retourne le GUID sans tirets
        private const string SacrificialSiteNameKey = "4b1a1c9f87b54522aec856f9d0198760";

        // Fallback centre calculé depuis les vertices de la MapZone
        private static readonly float3 FallbackCenter = new float3(-1584f, 0f, -936f);

        private static Entity _iconEntity = Entity.Null;
        private static Entity _anchorEntity = Entity.Null;

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

            float3 spawnPos = GetZoneCenter(em);

            // Crée une entité ancre positionnée au centre de la zone.
            // Elle sert de cible stable au MapIconTargetEntity pour éviter
            // le spam "typeIndexInArchetype was -1" causé par une cible nulle.
            _anchorEntity = em.CreateEntity(
                ComponentType.ReadWrite<Translation>(),
                ComponentType.ReadWrite<LocalToWorld>(),
                ComponentType.ReadWrite<NetworkId>()
            );

            em.SetComponentData(_anchorEntity, new Translation { Value = spawnPos });
            em.SetComponentData(_anchorEntity, new LocalToWorld
            {
                Value = float4x4.TRS(spawnPos, quaternion.identity, new float3(1f))
            });

            // NetworkId minimal valide — l'entité est statique, pas de sync joueur
            var anchorNetworkId = em.GetComponentData<NetworkId>(_anchorEntity);

            // Instancie l'icône et la pointe vers l'ancre
            var icon = em.Instantiate(iconPrefabEntity);

            em.SetComponentData(icon, new MapIconTargetEntity
            {
                TargetEntity = new NetworkedEntity { _Entity = _anchorEntity, _WaitingForSync = false },
                TargetNetworkId = anchorNetworkId
            });

            if (em.HasComponent<Translation>(icon))
                em.SetComponentData(icon, new Translation { Value = spawnPos });

            if (em.HasComponent<LocalToWorld>(icon))
                em.SetComponentData(icon, new LocalToWorld
                {
                    Value = float4x4.TRS(spawnPos, quaternion.identity, new float3(1f))
                });

            _iconEntity = icon;
            Log.LogInfo($"Claim zone map icon spawned at {spawnPos} with anchor entity {_anchorEntity}.");
        }

        private static float3 GetZoneCenter(EntityManager em)
        {
            var query = em.CreateEntityQuery(
                ComponentType.ReadOnly<MapZoneData>(),
                ComponentType.ReadOnly<MapZonePolygonVertexElement>()
            );
            var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);

            try
            {
                foreach (var entity in entities)
                {
                    var zoneData = em.GetComponentData<MapZoneData>(entity);
                    if (zoneData.Name.ToString() != SacrificialSiteNameKey) continue;

                    // Utilise CenterPosWS si disponible
                    var center = zoneData.CenterPosWS;
                    if (math.lengthsq(center) > 0.001f)
                    {
                        Log.LogInfo($"Sacrificial Site center from CenterPosWS: {center}");
                        return new float3(center.x, 0f, center.y);
                    }

                    // Sinon calcule depuis les vertices du polygon
                    var vertices = em.GetBuffer<MapZonePolygonVertexElement>(entity);
                    if (vertices.Length > 0)
                    {
                        float2 sum = float2.zero;
                        for (int i = 0; i < vertices.Length; i++)
                            sum += vertices[i].VertexPos;
                        var avg = sum / vertices.Length;
                        Log.LogInfo($"Sacrificial Site center from vertices: {avg}");
                        return new float3(avg.x, 0f, avg.y);
                    }
                }
            }
            finally
            {
                entities.Dispose();
                query.Dispose();
            }

            Log.LogWarning($"Sacrificial Site MapZone not found, using fallback center {FallbackCenter}.");
            return FallbackCenter;
        }

        public static void DestroyIcon()
        {
            var em = VWorld.ServerEntityManager;

            if (_iconEntity != Entity.Null && em.Exists(_iconEntity))
            {
                var users = em.CreateEntityQuery(ComponentType.ReadOnly<User>())
                              .ToEntityArray(Unity.Collections.Allocator.Temp);
                var userEntity = users.Length > 0 ? users[0] : _iconEntity;
                users.Dispose();

                StatChangeUtility.KillOrDestroyEntity(em, _iconEntity, userEntity, userEntity, 0, StatChangeReason.Any, true);
                _iconEntity = Entity.Null;
            }

            // Détruit aussi l'entité ancre
            if (_anchorEntity != Entity.Null && em.Exists(_anchorEntity))
            {
                em.DestroyEntity(_anchorEntity);
                _anchorEntity = Entity.Null;
            }
        }
    }
}