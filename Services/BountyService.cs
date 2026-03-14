using System.Collections.Generic;
using ProjectM;
using ProjectM.Network;
using ProjectM.Shared;
using Unity.Entities;
using Unity.Collections;
using BestKillfeed.Utils;
using Stunlock.Core;
using UnityEngine;
using System.Linq;

namespace BestKillfeed.Services
{
    public class BountyService
    {
        private static BountyService _instance;
        public static BountyService Instance => _instance ??= new BountyService();

        private readonly Dictionary<NetworkId, Entity> _bountyIcons = new();
        public static readonly PrefabGUID BountyIconPrefab = new PrefabGUID(-2066471106);
        public static readonly PrefabGUID BountyBuffPrefab = new PrefabGUID(1516078366);

        public void AddBountyIcon(Entity playerCharacterEntity)
        {
            var entityManager = VWorld.ServerEntityManager;

            if (playerCharacterEntity == Entity.Null || !entityManager.Exists(playerCharacterEntity)) return;
            if (!entityManager.HasComponent<NetworkId>(playerCharacterEntity)) return;

            var networkId = entityManager.GetComponentData<NetworkId>(playerCharacterEntity);

            if (_bountyIcons.TryGetValue(networkId, out var existingIcon))
            {
                if (entityManager.Exists(existingIcon)) return;
                _bountyIcons.Remove(networkId);
            }

            if (!Core.PrefabCollection._PrefabGuidToEntityMap.TryGetValue(BountyIconPrefab, out var iconPrefabEntity)) return;
            var mapIconProxy = entityManager.Instantiate(iconPrefabEntity);

            if (!entityManager.HasComponent<MapIconTargetEntity>(mapIconProxy))
                entityManager.AddComponent<MapIconTargetEntity>(mapIconProxy);

            entityManager.SetComponentData(mapIconProxy, new MapIconTargetEntity
            {
                TargetEntity = new NetworkedEntity { _Entity = playerCharacterEntity, _WaitingForSync = false },
                TargetNetworkId = networkId
            });

            if (!entityManager.HasComponent<AttachMapIconsToEntity>(mapIconProxy))
                entityManager.AddBuffer<AttachMapIconsToEntity>(mapIconProxy);

            if (entityManager.HasComponent<SyncToUserBitMask>(mapIconProxy))
                entityManager.RemoveComponent<SyncToUserBitMask>(mapIconProxy);

            if (entityManager.HasComponent<SyncToUserBuffer>(mapIconProxy))
                entityManager.RemoveComponent<SyncToUserBuffer>(mapIconProxy);

            if (entityManager.HasComponent<OnlySyncToUsersTag>(mapIconProxy))
                entityManager.RemoveComponent<OnlySyncToUsersTag>(mapIconProxy);

            var buffer = entityManager.GetBuffer<AttachMapIconsToEntity>(mapIconProxy);
            buffer.Clear();
            buffer.Add(new AttachMapIconsToEntity { Prefab = BountyIconPrefab });

            _bountyIcons[networkId] = mapIconProxy;

            ApplyBountyBuff(playerCharacterEntity);
        }

        public void RemoveBountyIcon(Entity playerCharacterEntity)
        {
            var entityManager = VWorld.ServerEntityManager;
            if (!entityManager.HasComponent<NetworkId>(playerCharacterEntity)) return;

            var networkId = entityManager.GetComponentData<NetworkId>(playerCharacterEntity);

            if (_bountyIcons.TryGetValue(networkId, out var iconEntity))
            {
                DestroyIconProperly(iconEntity);
                _bountyIcons.Remove(networkId);
            }
            ForceCleanupIconForPlayer(playerCharacterEntity);

            RemoveBountyBuff(playerCharacterEntity);
        }

        private void ApplyBountyBuff(Entity playerCharacterEntity)
        {
            try
            {
                var em = VWorld.ServerEntityManager;
                if (playerCharacterEntity == Entity.Null || !em.Exists(playerCharacterEntity)) return;

                if (BuffUtility.TryGetBuff(em, playerCharacterEntity, BountyBuffPrefab, out _)) return;

                var userEntity = GetUserEntityFromCharacter(playerCharacterEntity);
                if (userEntity == Entity.Null) return;

                var debugEvents = VWorld.Server.GetExistingSystemManaged<DebugEventsSystem>();
                var applyEvent = new ApplyBuffDebugEvent { BuffPrefabGUID = BountyBuffPrefab };
                var fromChar = new FromCharacter { Character = playerCharacterEntity, User = userEntity };

                debugEvents.ApplyBuff(fromChar, applyEvent);

                if (BuffUtility.TryGetBuff(em, playerCharacterEntity, BountyBuffPrefab, out var buffEntity))
                {
                    if (em.HasComponent<CreateGameplayEventsOnSpawn>(buffEntity))
                        em.RemoveComponent<CreateGameplayEventsOnSpawn>(buffEntity);

                    if (em.HasComponent<GameplayEventListeners>(buffEntity))
                        em.RemoveComponent<GameplayEventListeners>(buffEntity);

                    if (em.HasComponent<RemoveBuffOnGameplayEvent>(buffEntity))
                        em.RemoveComponent<RemoveBuffOnGameplayEvent>(buffEntity);

                    if (em.HasComponent<RemoveBuffOnGameplayEventEntry>(buffEntity))
                        em.RemoveComponent<RemoveBuffOnGameplayEventEntry>(buffEntity);

                    if (em.HasComponent<LifeTime>(buffEntity))
                        em.RemoveComponent<LifeTime>(buffEntity);
                }
            }
            catch (System.Exception e)
            {
                BepInEx.Logging.Logger.CreateLogSource("BountyService").LogError($"ApplyBountyBuff error: {e.Message}\n{e.StackTrace}");
            }
        }

        private void RemoveBountyBuff(Entity playerCharacterEntity)
        {
            try
            {
                var em = VWorld.ServerEntityManager;
                if (playerCharacterEntity == Entity.Null || !em.Exists(playerCharacterEntity)) return;

                if (!BuffUtility.TryGetBuff(em, playerCharacterEntity, BountyBuffPrefab, out var buffEntity)) return;
                if (!em.Exists(buffEntity)) return;

                DestroyUtility.Destroy(em, buffEntity, DestroyDebugReason.None);
            }
            catch (System.Exception e)
            {
                BepInEx.Logging.Logger.CreateLogSource("BountyService").LogError($"RemoveBountyBuff error: {e.Message}\n{e.StackTrace}");
            }
        }

        private Entity GetUserEntityFromCharacter(Entity playerCharacterEntity)
        {
            var em = VWorld.ServerEntityManager;
            if (em.HasComponent<PlayerCharacter>(playerCharacterEntity))
                return em.GetComponentData<PlayerCharacter>(playerCharacterEntity).UserEntity;
            return Entity.Null;
        }

        public void ForceCleanupIconForPlayer(Entity playerCharacter)
        {
            var em = VWorld.ServerEntityManager;
            if (!em.HasComponent<NetworkId>(playerCharacter)) return;

            var networkId = em.GetComponentData<NetworkId>(playerCharacter);

            var query = em.CreateEntityQuery(ComponentType.ReadOnly<MapIconTargetEntity>(), ComponentType.ReadOnly<PrefabGUID>());
            var icons = query.ToEntityArray(Unity.Collections.Allocator.Temp);

            try
            {
                foreach (var icon in icons)
                {
                    var target = em.GetComponentData<MapIconTargetEntity>(icon);
                    var prefab = em.GetComponentData<PrefabGUID>(icon);

                    if (target.TargetNetworkId.Equals(networkId) && prefab.Equals(BountyIconPrefab))
                    {
                        DestroyIconProperly(icon);
                    }
                }
            }
            finally
            {
                icons.Dispose();
            }
        }

        private void DestroyIconProperly(Entity iconEntity)
        {
            var em = VWorld.ServerEntityManager;
            if (iconEntity == Entity.Null || !em.Exists(iconEntity)) return;

            Entity userEntity = Entity.Null;

            var users = em.CreateEntityQuery(ComponentType.ReadOnly<User>()).ToEntityArray(Allocator.Temp);
            try
            {
                if (users.Length > 0)
                    userEntity = users[0];
            }
            finally
            {
                users.Dispose();
            }

            if (userEntity == Entity.Null) userEntity = iconEntity;

            if (em.HasComponent<MapIconTargetEntity>(iconEntity))
                em.RemoveComponent<MapIconTargetEntity>(iconEntity);

            if (em.HasComponent<AttachMapIconsToEntity>(iconEntity))
                em.RemoveComponent<AttachMapIconsToEntity>(iconEntity);

            StatChangeUtility.KillOrDestroyEntity(em, iconEntity, userEntity, userEntity, 0, StatChangeReason.Any, true);
        }

        public void ClearAllBountyIcons()
        {
            var em = VWorld.ServerEntityManager;

            var keys = _bountyIcons.Keys.ToList();
            foreach (var key in keys)
            {
                if (_bountyIcons.TryGetValue(key, out var iconEntity))
                    DestroyIconProperly(iconEntity);
            }
            _bountyIcons.Clear();

            var users = em.CreateEntityQuery(ComponentType.ReadOnly<User>()).ToEntityArray(Allocator.Temp);
            try
            {
                foreach (var userEntity in users)
                {
                    var user = em.GetComponentData<User>(userEntity);
                    var characterEntity = user.LocalCharacter._Entity;
                    if (characterEntity == Entity.Null || !em.Exists(characterEntity)) continue;

                    RemoveBountyBuff(characterEntity);
                }
            }
            finally
            {
                users.Dispose();
            }
        }

        public void RestoreAllBountyIcons()
        {
            var em = VWorld.ServerEntityManager;
            var playerService = Services.PlayerService.Instance;

            var users = em.CreateEntityQuery(ComponentType.ReadOnly<User>()).ToEntityArray(Allocator.Temp);
            try
            {
                foreach (var userEntity in users)
                {
                    var user = em.GetComponentData<User>(userEntity);
                    if (!user.IsConnected) continue;

                    var characterEntity = user.LocalCharacter._Entity;
                    if (characterEntity == Entity.Null || !em.Exists(characterEntity)) continue;

                    var data = playerService.GetPlayerData(user.CharacterName.ToString());
                    if (data == null || !data.HasBounty) continue;

                    AddBountyIcon(characterEntity);
                }
            }
            finally
            {
                users.Dispose();
            }
        }
    }
}