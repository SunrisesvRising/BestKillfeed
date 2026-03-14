using System;
using HarmonyLib;
using ProjectM;
using ProjectM.Network;
using Unity.Entities;
using Unity.Collections;
using BestKillfeed.Services;
using BestKillfeed.Utils;
using Stunlock.Network;
using UnityEngine;

namespace BestKillfeed.Patches
{
    [HarmonyPatch(typeof(ServerBootstrapSystem), nameof(ServerBootstrapSystem.OnUserConnected))]
    public static class PlayerConnectPatch
    {
        public static void Postfix(ServerBootstrapSystem __instance, NetConnectionId netConnectionId)
        {
            try
            {
                var entityManager = __instance.EntityManager;

                if (!__instance._NetEndPointToApprovedUserIndex.TryGetValue(netConnectionId, out var userIndex))
                    return;

                var serverClients = __instance._ApprovedUsersLookup;
                if (userIndex < 0 || userIndex >= serverClients.Length)
                    return;

                var userEntity = serverClients[userIndex].UserEntity;

                if (!entityManager.HasComponent<User>(userEntity)) return;

                var userData = entityManager.GetComponentData<User>(userEntity);
                var playerName = userData.CharacterName.ToString();

                Debug.Log($"[BestKillfeed] Player connected: {playerName}");

                PlayerService.Instance.UpdatePlayerLevel(userEntity);

                if (!KillfeedSettings.EnableBounty.Value) return;

                var data = PlayerService.Instance.GetPlayerData(playerName);
                if (data == null || !data.HasBounty) return;

                var characterEntity = userData.LocalCharacter._Entity;

                if (characterEntity != Entity.Null)
                    BountyService.Instance.AddBountyIcon(characterEntity);

                SendBountyReminder(entityManager, userData);
                StartHourlyReminder(playerName, userEntity);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[BestKillfeed] Error in PlayerConnectPatch: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private static void SendBountyReminder(EntityManager em, User user)
        {
            var msg1 = (FixedString512Bytes)"<color=#ff0000>⚠ You have an active bounty on your head!</color>";
            var msg2 = (FixedString512Bytes)"<color=#ffaa00>Head to the Sacrificial Site and use <color=#ffffff>.bc</color><color=#ffaa00> to clear it and claim your reward.</color>";
            ServerChatUtils.SendSystemMessageToClient(em, user, ref msg1);
            ServerChatUtils.SendSystemMessageToClient(em, user, ref msg2);
        }

        private static void StartHourlyReminder(string playerName, Entity userEntity)
        {
            string channelKey = $"bounty_reminder_{playerName.ToLower()}";

            ChannelManager.CancelChannel(channelKey);

            ChannelManager.StartChannel(
                playerKey: channelKey,
                durationSeconds: 3600,
                onTick: remaining =>
                {
                    if (remaining != 1) return;

                    var em = VWorld.ServerEntityManager;
                    if (!em.Exists(userEntity) || !em.HasComponent<User>(userEntity)) return;

                    var user = em.GetComponentData<User>(userEntity);
                    if (!user.IsConnected) return;

                    var latestData = PlayerService.Instance.GetPlayerData(user.CharacterName.ToString());
                    if (latestData == null || !latestData.HasBounty || !KillfeedSettings.EnableBounty.Value) return;

                    SendBountyReminder(em, user);
                },
                onComplete: () =>
                {
                    var em = VWorld.ServerEntityManager;
                    if (!em.Exists(userEntity) || !em.HasComponent<User>(userEntity)) return;

                    var user = em.GetComponentData<User>(userEntity);
                    if (!user.IsConnected) return;

                    var latestData = PlayerService.Instance.GetPlayerData(user.CharacterName.ToString());
                    if (latestData == null || !latestData.HasBounty || !KillfeedSettings.EnableBounty.Value) return;

                    StartHourlyReminder(user.CharacterName.ToString(), userEntity);
                }
            );
        }
    }
}