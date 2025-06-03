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

namespace BestKillfeed.Services
{
    public class BountyService
    {
        private static BountyService _instance;
        public static BountyService Instance => _instance ??= new BountyService();
        private readonly Dictionary<Entity, Entity> _bountyIcons = new();

        private readonly HashSet<string> activeBounties = new();

        public bool HasBounty(string playerName) => activeBounties.Contains(playerName);

        private static Entity mapIconProxyPrefab;
        public static PrefabGUID mapIconPrefab = new PrefabGUID(-1060998155);
        public static EntityQuery mapIconProxyQuery;

        public static void AddIcon(Entity targetPlayerEntity)
        {
            if (!Core.PrefabCollection._PrefabGuidToEntityMap.TryGetValue(mapIconPrefab, out mapIconProxyPrefab))
            {
                Plugin.Logger.LogError("Failed to find MapIcon_ProxyObject_POI_Unknown Prefab entity");
                return;
            }

            var mapIconProxy = Core.EntityManager.Instantiate(mapIconProxyPrefab);

            Core.EntityManager.AddComponent<MapIconTargetEntity>(mapIconProxy);
            Core.EntityManager.AddBuffer<AttachMapIconsToEntity>(mapIconProxy);

            var networkId = Core.EntityManager.GetComponentData<NetworkId>(targetPlayerEntity);

            var mapIconTargetEntity = new MapIconTargetEntity
            {
                TargetEntity = targetPlayerEntity,
                TargetNetworkId = networkId
            };
            Core.EntityManager.SetComponentData(mapIconProxy, mapIconTargetEntity);

            Core.EntityManager.RemoveComponent<SyncToUserBitMask>(mapIconProxy);
            Core.EntityManager.RemoveComponent<SyncToUserBuffer>(mapIconProxy);
            Core.EntityManager.RemoveComponent<OnlySyncToUsersTag>(mapIconProxy);

            var attachMapIconsToEntity = Core.EntityManager.GetBuffer<AttachMapIconsToEntity>(mapIconProxy);
            attachMapIconsToEntity.Clear();
            attachMapIconsToEntity.Add(new AttachMapIconsToEntity { Prefab = mapIconPrefab });
            Instance._bountyIcons[targetPlayerEntity] = mapIconProxy;
        }
        public static void RemoveIcon(Entity targetPlayerEntity)
        {
            if (Instance._bountyIcons.TryGetValue(targetPlayerEntity, out var iconEntity))
            {
                if (Core.EntityManager.Exists(iconEntity))
                {
                    Core.EntityManager.DestroyEntity(iconEntity);
                }

                Instance._bountyIcons.Remove(targetPlayerEntity);
            }
        }
        public void RestoreBountyIconIfNeeded(string playerName, Entity playerEntity)
        {
            if (HasBounty(playerName))
            {
                AddIcon(playerEntity);
                Debug.Log($"[BestKillfeed] Icon restored for {playerName}");
            }
        }

        public void PlaceBounty(string playerName)
        {
            if (activeBounties.Contains(playerName)) return;

            activeBounties.Add(playerName);

            var message = $"<color=#ff5555>A bounty has been placed on {playerName} for a 5 killstreak!</color>";
            FixedString512Bytes chatMsg = message;
            ServerChatUtils.SendSystemMessageToAllClients(VWorld.Server.EntityManager, ref chatMsg);

            Debug.Log($"[BestKillfeed] Bounty placed on {playerName}");
        }

        public void RemoveBounty(string playerName)
        {
            if (activeBounties.Remove(playerName))
            {
                var message = $"<color=#ffaa55>The bounty on {playerName} has been lifted!</color>";
                FixedString512Bytes chatMsg = message;
                ServerChatUtils.SendSystemMessageToAllClients(VWorld.Server.EntityManager, ref chatMsg);

                Debug.Log($"[BestKillfeed] Bounty removed from {playerName}");
            }
        }
    }
}
