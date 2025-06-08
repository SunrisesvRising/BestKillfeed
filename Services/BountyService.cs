using System.Collections.Generic;
using UnityEngine;
using ProjectM;
using ProjectM.Network;
using Unity.Collections;
using BestKillfeed.Utils;
using Stunlock.Core;
using Unity.Entities;
using Stunlock.Network;
using System.Linq;
using System.Text.Json;
using System.IO;
using System;
using ProjectM.Scripting;
using ProjectM.UI;
using System.Security.AccessControl;

namespace BestKillfeed.Services
{
    public class BountyService
    {
        private static BountyService _instance;
        public static BountyService Instance => _instance ??= new BountyService();
        private readonly Dictionary<Entity, Entity> _bountyIcons = new();
        private readonly HashSet<string> activeBounties = new();
        private static Entity mapIconProxyPrefab;
        private static readonly string BountiesPath = Path.Combine(BepInEx.Paths.ConfigPath, "Bestkillfeed", "Bounties.json");
        public static PrefabGUID mapIconPrefab = new PrefabGUID(-2078904014);
        //public static PrefabGUID mapIconPrefab = new PrefabGUID(1501929529);
        public IEnumerable<string> GetAllBounties() => activeBounties;
        private BountyService()
        {
            StatsService.Instance.OnKillStreakChanged += HandleKillStreakChanged;
            LoadBounties();
        }
        private void HandleKillStreakChanged(string playerName, Entity targetEntity, int streak)
        {
            if (!KillfeedSettings.EnableBountySystem.Value) return;

            bool shouldPlace = streak == KillfeedSettings.BountyStreakThreshold.Value;
            bool hasBounty = HasBounty(playerName);

            if (shouldPlace && !hasBounty)
            {
                PlaceBounty(playerName);
                AddIcon(targetEntity);
            }
            else if (streak == 0 && hasBounty)
            {
                RemoveBounty(playerName);
                RemoveIcon(targetEntity);
            }
        }
        public bool HasBounty(string playerName) => activeBounties.Contains(playerName);
        public static void AddIcon(Entity targetPlayerEntity)
        {
            var entityManager = Core.EntityManager;

            if (!Core.PrefabCollection._PrefabGuidToEntityMap.TryGetValue(mapIconPrefab, out var mapIconProxyPrefab))
            {
                Plugin.Logger.LogError("Failed to find MapIcon prefab");
                return;
            }

            var mapIconProxy = entityManager.Instantiate(mapIconProxyPrefab);
            Plugin.Logger.LogInfo($"[AddIcon] Created MapIcon entity: {mapIconProxy.Index}:{mapIconProxy.Version}");

            entityManager.AddComponent<MapIconTargetEntity>(mapIconProxy);
            entityManager.AddBuffer<AttachMapIconsToEntity>(mapIconProxy);

            var networkId = entityManager.GetComponentData<NetworkId>(targetPlayerEntity);

            var mapIconTargetEntity = new MapIconTargetEntity
            {
                TargetEntity = targetPlayerEntity,
                TargetNetworkId = networkId
            };

            entityManager.SetComponentData(mapIconProxy, mapIconTargetEntity);
            entityManager.RemoveComponent<SyncToUserBitMask>(mapIconProxy);
            entityManager.RemoveComponent<SyncToUserBuffer>(mapIconProxy);
            entityManager.RemoveComponent<OnlySyncToUsersTag>(mapIconProxy);

            var attachMapIconsToEntity = entityManager.GetBuffer<AttachMapIconsToEntity>(mapIconProxy);
            attachMapIconsToEntity.Clear();
            attachMapIconsToEntity.Add(new AttachMapIconsToEntity { Prefab = mapIconPrefab });

            Plugin.Logger.LogInfo($"[AddIcon] Icon entity {mapIconProxy.Index}:{mapIconProxy.Version} attached to player {targetPlayerEntity.Index}:{targetPlayerEntity.Version}");

            Instance._bountyIcons[targetPlayerEntity] = mapIconProxy;
        }
        private static void DetachEntityFromAllReferences(Entity targetEntity, EntityManager em)
        {
            // Crée une query sur tous les buffers AttachMapIconsToEntity existants
            var query = em.CreateEntityQuery(ComponentType.ReadOnly<AttachMapIconsToEntity>());
            var entitiesWithBuffer = query.ToEntityArray(Allocator.TempJob);

            foreach (var entity in entitiesWithBuffer)
            {
                if (!em.Exists(entity)) continue;
                if (!em.HasComponent<AttachMapIconsToEntity>(entity)) continue;

                var buffer = em.GetBuffer<AttachMapIconsToEntity>(entity);

                // Parcours du buffer à l'envers pour retirer en toute sécurité
                for (int i = buffer.Length - 1; i >= 0; i--)
                {
                    var entry = buffer[i];

                    // Vérifie si l'entrée fait référence au même prefab que celui qu'on veut détacher
                    if (entry.Prefab == BountyService.mapIconPrefab)
                    {
                        Plugin.Logger.LogInfo($"[Detach] Removing icon prefab {entry.Prefab._Value} from buffer of entity {entity.Index}:{entity.Version} (index {i})");
                        buffer.RemoveAt(i);
                    }
                }
            }

            entitiesWithBuffer.Dispose();
            query.Dispose();
        }
        private static void DetachAllAttachedChildren(Entity parentEntity, EntityManager em)
        {
            var query = em.CreateEntityQuery(ComponentType.ReadOnly<Attached>());
            var allAttached = query.ToEntityArray(Allocator.TempJob);

            foreach (var entity in allAttached)
            {
                if (!em.Exists(entity)) continue;

                var attached = em.GetComponentData<Attached>(entity);

                if (attached.Parent == parentEntity)
                {
                    Plugin.Logger.LogInfo($"[DetachAllAttachedChildren] Detaching child entity {entity.Index}:{entity.Version} from parent {parentEntity.Index}:{parentEntity.Version}");
                    em.RemoveComponent<Attached>(entity);
                }
            }

            allAttached.Dispose();
            query.Dispose();
        }
        public static void RemoveIcon(Entity targetPlayerEntity)
        {
            var em = Core.EntityManager;

            if (Instance._bountyIcons.TryGetValue(targetPlayerEntity, out var iconEntity))
            {
                if (em.Exists(iconEntity))
                {
                    Plugin.Logger.LogInfo($"[RemoveIcon] Detaching and destroying icon {iconEntity.Index}:{iconEntity.Version} for player {targetPlayerEntity.Index}:{targetPlayerEntity.Version}");
                    DetachEntityFromAllReferences(iconEntity, em);
                    em.DestroyEntity(iconEntity);
                }
                Instance._bountyIcons.Remove(targetPlayerEntity);
            }
            else
            {
                Plugin.Logger.LogInfo($"[RemoveIcon] No icon found for player {targetPlayerEntity.Index}:{targetPlayerEntity.Version}");
            }

            var queryDesc = new EntityQueryDesc
            {
                All = new ComponentType[]
                {
            ComponentType.ReadOnly<MapIconData>(),
            ComponentType.ReadOnly<MapIconTargetEntity>(),
            ComponentType.ReadOnly<PrefabGUID>()
                }
            };

            var query = em.CreateEntityQuery(queryDesc);
            var allMapIcons = query.ToEntityArray(Allocator.TempJob);

            foreach (var icon in allMapIcons)
            {
                if (!em.Exists(icon)) continue;
                if (!em.HasComponent<MapIconTargetEntity>(icon)) continue;

                var prefabGuid = em.GetComponentData<PrefabGUID>(icon);
                if (prefabGuid._Value != mapIconPrefab._Value)
                    continue;

                var iconTarget = em.GetComponentData<MapIconTargetEntity>(icon);
                if (iconTarget.TargetEntity._Entity == targetPlayerEntity)
                {
                    Plugin.Logger.LogInfo($"[RemoveIcon] Found matching icon {icon.Index}:{icon.Version} for player {targetPlayerEntity.Index}:{targetPlayerEntity.Version}, detaching and destroying.");
                    DetachEntityFromAllReferences(iconEntity, em);
                    StatChangeUtility.KillOrDestroyEntity(em, icon, icon, Entity.Null, 0, StatChangeReason.Any, true);
                }
            }

            allMapIcons.Dispose();
            query.Dispose();
            DetachAllAttachedChildren(iconEntity, em);
            DetachEntityFromAllReferences(iconEntity, em);
            CleanupOrphanedIcons();
        }
        public static void CleanupOrphanedIcons()
        {
            var world = Utils.VWorld.Server;
            if (world == null || !world.IsCreated)
                return;

            var em = world.EntityManager;

            var queryDesc = new EntityQueryDesc
            {
                All = new ComponentType[]
                {
            ComponentType.ReadOnly<MapIconData>(),
            ComponentType.ReadOnly<MapIconTargetEntity>(),
            ComponentType.ReadOnly<PrefabGUID>()
                }
            };

            var query = em.CreateEntityQuery(queryDesc);
            var allMapIcons = query.ToEntityArray(Allocator.TempJob);

            foreach (var iconEntity in allMapIcons)
            {
                if (!em.Exists(iconEntity)) continue;

                var prefabGuid = em.GetComponentData<PrefabGUID>(iconEntity);
                if (prefabGuid._Value != mapIconPrefab._Value)
                    continue;

                var data = em.GetComponentData<MapIconData>(iconEntity);
                if (data.TargetUser == Entity.Null || !em.Exists(data.TargetUser))
                {
                    if (em.HasComponent<AttachMapIconsToEntity>(iconEntity))
                    {
                        var buffer = em.GetBuffer<AttachMapIconsToEntity>(iconEntity);
                        buffer.Clear();
                    }
                    em.DestroyEntity(iconEntity);
                }
            }

            allMapIcons.Dispose();
            query.Dispose();
        }
        public void RemoveBounty(string playerName)
        {
            if (activeBounties.Remove(playerName))
            {
                SaveBounties();
                var message = $"<color=#ffaa55>The bounty on {playerName} has been lifted!</color>";
                FixedString512Bytes chatMsg = message;
                ServerChatUtils.SendSystemMessageToAllClients(VWorld.Server.EntityManager, ref chatMsg);
                Debug.Log($"[BestKillfeed] Bounty removed from {playerName}");
            }
        }
        public void PlaceBounty(string playerName)
        {
            if (activeBounties.Contains(playerName)) return;
            activeBounties.Add(playerName);
            SaveBounties();
            var message = $"<color=#ff5555>A bounty has been placed on {playerName} for a 5 killstreak!</color>";
            FixedString512Bytes chatMsg = message;
            ServerChatUtils.SendSystemMessageToAllClients(VWorld.Server.EntityManager, ref chatMsg);
            Debug.Log($"[BestKillfeed] Bounty placed on {playerName}");
        }
        private void LoadBounties()
        {
            try
            {
                if (!File.Exists(BountiesPath))
                {
                    activeBounties.Clear();
                    return;
                }

                var json = File.ReadAllText(BountiesPath);
                var loaded = JsonSerializer.Deserialize<HashSet<string>>(json);
                activeBounties.Clear();
                if (loaded != null)
                    activeBounties.UnionWith(loaded);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[BountyService] Failed to load bounties: {ex}");
                activeBounties.Clear();
            }
        }
        private void SaveBounties()
        {
            try
            {
                var json = JsonSerializer.Serialize(activeBounties, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(BountiesPath, json);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[BountyService] Failed to save bounties: {ex}");
            }
        }
    }
}