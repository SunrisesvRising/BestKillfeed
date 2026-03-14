using BestKillfeed.Services;
using VampireCommandFramework;
using ProjectM.Network;
using Unity.Entities;
using BestKillfeed.Utils;
using ProjectM;
using System.Linq;
using Stunlock.Core;
using ProjectM.CastleBuilding;
using Unity.Mathematics;
using Unity.Transforms;

namespace BestKillfeed.Commands
{
    internal class BountyCommand
    {
        private static readonly float2[] SacrificialSitePolygon = new float2[]
        {
            new float2(-1566.3f, -946.1f),
            new float2(-1567.1f, -949.7f),
            new float2(-1571f,   -952.8f),
            new float2(-1576.2f, -954.6f),
            new float2(-1587.8f, -955.2f),
            new float2(-1593.3f, -954.3f),
            new float2(-1596.8f, -951.8f),
            new float2(-1598.9f, -948.9f),
            new float2(-1598.9f, -926.2f),
            new float2(-1596.9f, -922.5f),
            new float2(-1593.7f, -920.1f),
            new float2(-1589.1f, -917.8f),
            new float2(-1584.8f, -917.2f),
            new float2(-1580.3f, -918.8f),
            new float2(-1576.3f, -922f),
            new float2(-1573.5f, -929.1f),
        };

        private const int ChannelDurationSeconds = 30;
        private const int GracePeriodSeconds = 5;
        private static readonly int[] TickAnnounceAt = { 30, 20, 10, 5, 3, 2, 1 };
        private static readonly System.Collections.Generic.Dictionary<string, int> _graceTicks = new();

        [Command("bounty-add", "ba", description: "Manually place a bounty on a player", adminOnly: true)]
        public static void AddBounty(ChatCommandContext ctx, string targetName)
        {
            if (!KillfeedSettings.EnableBounty.Value)
            {
                ctx.Reply("<color=#ff0000>The bounty system is currently disabled.</color>");
                return;
            }
            var entityManager = VWorld.ServerEntityManager;

            Entity targetUserEntity = Entity.Null;
            var users = entityManager.CreateEntityQuery(ComponentType.ReadOnly<User>())
                                     .ToEntityArray(Unity.Collections.Allocator.Temp);

            foreach (var userEnt in users)
            {
                var userData = entityManager.GetComponentData<User>(userEnt);
                if (userData.CharacterName.ToString().ToLower() == targetName.ToLower())
                {
                    targetUserEntity = userEnt;
                    break;
                }
            }
            users.Dispose();

            if (targetUserEntity == Entity.Null)
            {
                ctx.Reply($"<color=#ff0000>Error: Player '{targetName}' not found or offline.</color>");
                return;
            }

            var user = entityManager.GetComponentData<User>(targetUserEntity);
            var characterEntity = user.LocalCharacter._Entity;
            var actualName = user.CharacterName.ToString();

            var data = PlayerService.Instance.GetPlayerData(actualName);
            if (data.HasBounty)
            {
                ctx.Reply($"<color=#ffff00>{actualName} already has an active bounty.</color>");
                return;
            }

            data.HasBounty = true;
            PlayerService.Instance.SavePlayerData(data);
            BountyService.Instance.AddBountyIcon(characterEntity);

            var announce = $"<color=#ff0000>[ ADMINISTRATION ] A bounty has been placed on {actualName}!</color>";
            Unity.Collections.FixedString512Bytes fAnnounce = announce;
            ServerChatUtils.SendSystemMessageToAllClients(entityManager, ref fAnnounce);

            ctx.Reply($"Bounty activated for {actualName}.");
        }

        [Command("bounty-remove", "br", description: "Manually remove a bounty from a player", adminOnly: true)]
        public static void RemoveBounty(ChatCommandContext ctx, string targetName)
        {
            var entityManager = VWorld.ServerEntityManager;

            var data = PlayerService.Instance.GetPlayerData(targetName);
            if (data == null || !data.HasBounty)
            {
                ctx.Reply($"<color=#ffff00>Player {targetName} has no active bounty.</color>");
                return;
            }

            data.HasBounty = false;
            PlayerService.Instance.SavePlayerData(data);
            ChannelManager.CancelChannel(targetName.ToLower());

            bool iconRemoved = false;
            var users = entityManager.CreateEntityQuery(ComponentType.ReadOnly<User>())
                                     .ToEntityArray(Unity.Collections.Allocator.Temp);

            foreach (var userEnt in users)
            {
                var user = entityManager.GetComponentData<User>(userEnt);
                if (user.CharacterName.ToString().ToLower() == targetName.ToLower())
                {
                    BountyService.Instance.RemoveBountyIcon(user.LocalCharacter._Entity);
                    iconRemoved = true;
                    break;
                }
            }
            users.Dispose();

            ctx.Reply($"Bounty removed for {targetName}. {(iconRemoved ? "(Icon removed)" : "(Player offline, icon already gone)")}");
        }

        [Command("bountylist", "bl", description: "Displays all active player bounties", adminOnly: false)]
        public static void HandleCommand(ChatCommandContext ctx)
        {
            if (!KillfeedSettings.EnableBounty.Value)
            {
                ctx.Reply("<color=#ff0000>The bounty system is currently disabled.</color>");
                return;
            }
            var activeBounties = PlayerService.Instance.GetAllPlayers()
                .Where(p => p.HasBounty)
                .OrderByDescending(p => p.CurrentStreak)
                .ToList();

            if (activeBounties.Count == 0)
            {
                ctx.Reply("<color=#aaaaaa>There are currently no active bounties on the map.</color>");
                return;
            }

            ctx.Reply("<color=#ff0000>--- ACTIVE BOUNTIES ---</color>");

            foreach (var player in activeBounties)
            {
                string line = $"<color=#ffffff>{player.Name}</color> " +
                               $"<color=#00ffff>[Lvl {player.MaxLevel}]</color> - " +
                               $"Current Streak: <color=#ffaa00>{player.CurrentStreak}</color> " +
                               $"<color=#ff5555>(Total Kills: {player.Kills})</color>";
                ctx.Reply(line);
            }

            ctx.Reply($"<color=#ffaa00>Total targets: {activeBounties.Count}</color>");
        }

        [Command("bounty-claim", "bc", description: "Claim your bounty — stay 30s at the Sacrificial Site", adminOnly: false)]
        public static void ClaimSelfBounty(ChatCommandContext ctx)
        {
            if (!KillfeedSettings.EnableBounty.Value)
            {
                ctx.Reply("<color=#ff0000>The bounty system is currently disabled.</color>");
                return;
            }
            var entityManager = VWorld.ServerEntityManager;
            var user = ctx.Event.User;
            var characterEntity = ctx.Event.SenderCharacterEntity;
            var playerName = user.CharacterName.ToString();
            var playerKey = playerName.ToLower();

            if (ChannelManager.HasActiveChannel(playerKey))
            {
                ctx.Reply("<color=#ffaa00>Your claim is already in progress, please wait...</color>");
                return;
            }

            if (!IsPlayerInZone(entityManager, characterEntity))
            {
                ctx.Reply("<color=#ff0000>You must be at the Sacrificial Site to claim your bounty!</color>");
                return;
            }

            var data = PlayerService.Instance.GetPlayerData(playerName);
            if (data == null || !data.HasBounty)
            {
                ctx.Reply("<color=#ff0000>You don't have an active bounty on your head.</color>");
                return;
            }

            var startAnnounce = $"<color=#ffaa00>[ BOUNTY ] {playerName} is attempting to claim their bounty at the Sacrificial Site! They must hold for <color=#ffffff>{ChannelDurationSeconds}s</color><color=#ffaa00>!</color>";
            Unity.Collections.FixedString512Bytes fStartAnnounce = startAnnounce;
            ServerChatUtils.SendSystemMessageToAllClients(entityManager, ref fStartAnnounce);

            ChannelManager.StartChannel(
                playerKey,
                durationSeconds: ChannelDurationSeconds,

                onTick: (remaining) =>
                {
                    if (IsPlayerDisconnected(entityManager, user))
                    {
                        _graceTicks.Remove(playerKey);
                        ChannelManager.CancelChannel(playerKey);
                        return;
                    }

                    if (IsPlayerDeadOrDown(entityManager, characterEntity))
                    {
                        _graceTicks.Remove(playerKey);
                        ChannelManager.CancelChannel(playerKey);
                        SendMessageToPlayer(entityManager, user,
                            "<color=#ff0000>[ BOUNTY ] You were downed or killed! Channel cancelled.</color>");
                        return;
                    }

                    if (!IsPlayerInZone(entityManager, characterEntity))
                    {
                        if (!_graceTicks.ContainsKey(playerKey))
                            _graceTicks[playerKey] = GracePeriodSeconds;

                        int graceLeft = _graceTicks[playerKey];

                        if (graceLeft <= 0)
                        {
                            _graceTicks.Remove(playerKey);
                            ChannelManager.CancelChannel(playerKey);
                            SendMessageToPlayer(entityManager, user,
                                "<color=#ff0000>[ BOUNTY ] You stayed outside the zone too long! Channel cancelled.</color>");
                            return;
                        }

                        SendMessageToPlayer(entityManager, user,
                            $"<color=#ff0000>[ BOUNTY ] Get back to the Sacrificial Site! <color=#ffffff>{graceLeft}s</color><color=#ff0000> before cancellation!</color>");
                        _graceTicks[playerKey] = graceLeft - 1;
                        return;
                    }

                    if (_graceTicks.ContainsKey(playerKey))
                    {
                        _graceTicks.Remove(playerKey);
                        SendMessageToPlayer(entityManager, user,
                            "<color=#00ff00>[ BOUNTY ] Back in the zone! Channel resumed.</color>");
                    }

                    foreach (var threshold in TickAnnounceAt)
                    {
                        if (remaining == threshold)
                        {
                            SendMessageToPlayer(entityManager, user,
                                $"<color=#ffaa00>[ BOUNTY ] <color=#ffffff>{remaining}s</color><color=#ffaa00> remaining...</color>");
                            break;
                        }
                    }
                },

                onComplete: () =>
                {
                    _graceTicks.Remove(playerKey);

                    if (!IsPlayerInZone(entityManager, characterEntity))
                    {
                        SendMessageToPlayer(entityManager, user,
                            "<color=#ff0000>[ BOUNTY ] You left the zone at the last second! Channel cancelled.</color>");
                        return;
                    }

                    var latestData = PlayerService.Instance.GetPlayerData(playerName);
                    if (latestData == null || !latestData.HasBounty)
                    {
                        SendMessageToPlayer(entityManager, user,
                            "<color=#ff0000>[ BOUNTY ] Your bounty has already been removed.</color>");
                        return;
                    }

                    latestData.HasBounty = false;
                    PlayerService.Instance.SavePlayerData(latestData);
                    BountyService.Instance.RemoveBountyIcon(characterEntity);
                    RewardService.Instance.GiveBountyReward(characterEntity, "", isSelfClaim: true);

                    SendMessageToPlayer(entityManager, user,
                        "<color=#00ff00>[ BOUNTY ] Congratulations! You held for 30 seconds and claimed your bounty!</color>");

                    var announce = $"<color=#ffff00>[ BOUNTY ] {playerName} survived their bounty and claimed the reward at the Sacrificial Site!</color>";
                    Unity.Collections.FixedString512Bytes fAnnounce = announce;
                    ServerChatUtils.SendSystemMessageToAllClients(entityManager, ref fAnnounce);
                }
            );
        }

        private static bool IsPlayerDeadOrDown(EntityManager em, Entity character)
        {
            if (!em.Exists(character)) return true;

            if (em.HasComponent<Health>(character))
            {
                var health = em.GetComponentData<Health>(character);
                if (health.IsDead) return true;
            }

            if (em.HasBuffer<BuffBuffer>(character))
            {
                var buffs = em.GetBuffer<BuffBuffer>(character);
                for (int i = 0; i < buffs.Length; i++)
                {
                    if (buffs[i].PrefabGuid.GuidHash == -1992158531)
                        return true;
                }
            }

            return false;
        }

        private static bool IsPlayerDisconnected(EntityManager em, User user)
        {
            var users = em.CreateEntityQuery(ComponentType.ReadOnly<User>())
                          .ToEntityArray(Unity.Collections.Allocator.Temp);
            bool connected = false;
            foreach (var u in users)
            {
                var userData = em.GetComponentData<User>(u);
                if (userData.CharacterName.ToString().ToLower() == user.CharacterName.ToString().ToLower())
                {
                    connected = userData.IsConnected;
                    break;
                }
            }
            users.Dispose();
            return !connected;
        }

        private static bool IsPlayerInZone(EntityManager em, Entity character)
        {
            if (!em.HasComponent<LocalToWorld>(character)) return false;
            var pos = em.GetComponentData<LocalToWorld>(character).Position;
            return IsPointInPolygon(new float2(pos.x, pos.z), SacrificialSitePolygon);
        }

        private static void SendMessageToPlayer(EntityManager em, User user, string message)
        {
            Unity.Collections.FixedString512Bytes msg = message;
            ServerChatUtils.SendSystemMessageToClient(em, user, ref msg);
        }

        private static bool IsPointInPolygon(float2 point, float2[] polygon)
        {
            int n = polygon.Length;
            bool inside = false;

            for (int i = 0, j = n - 1; i < n; j = i++)
            {
                float2 pi = polygon[i];
                float2 pj = polygon[j];

                bool intersects = ((pi.y > point.y) != (pj.y > point.y)) &&
                                  (point.x < (pj.x - pi.x) * (point.y - pi.y) / (pj.y - pi.y) + pi.x);

                if (intersects)
                    inside = !inside;
            }

            return inside;
        }

        [Command("bounty-force-clear-icon", "bfci", description: "Forcefully removes ONLY the bounty icon", adminOnly: true)]
        public static void ForceClearIcon(ChatCommandContext ctx, string targetName)
        {
            var entityManager = VWorld.ServerEntityManager;
            bool iconFound = false;

            var users = entityManager.CreateEntityQuery(ComponentType.ReadOnly<User>())
                                     .ToEntityArray(Unity.Collections.Allocator.Temp);

            Entity targetPlayerChar = Entity.Null;
            foreach (var u in users)
            {
                var userData = entityManager.GetComponentData<User>(u);
                if (userData.CharacterName.ToString().ToLower() == targetName.ToLower())
                {
                    targetPlayerChar = userData.LocalCharacter._Entity;
                    break;
                }
            }
            users.Dispose();

            if (targetPlayerChar == Entity.Null)
            {
                ctx.Reply($"<color=#ff0000>Error: Player '{targetName}' not found.</color>");
                return;
            }

            var targetNetworkId = entityManager.GetComponentData<NetworkId>(targetPlayerChar);

            var iconEntities = entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<MapIconTargetEntity>(),
                ComponentType.ReadOnly<PrefabGUID>())
                .ToEntityArray(Unity.Collections.Allocator.Temp);

            foreach (var iconEnt in iconEntities)
            {
                var data = entityManager.GetComponentData<MapIconTargetEntity>(iconEnt);
                var prefabGuid = entityManager.GetComponentData<PrefabGUID>(iconEnt);

                if (data.TargetNetworkId.Equals(targetNetworkId) && prefabGuid.Equals(BountyService.BountyIconPrefab))
                {
                    DestroyIconManual(entityManager, iconEnt);
                    iconFound = true;
                }
            }
            iconEntities.Dispose();

            if (iconFound)
                ctx.Reply($"<color=#00ff00>Bounty icon for {targetName} removed.</color>");
            else
                ctx.Reply($"<color=#ffff00>No bounty icon found for {targetName}.</color>");
        }

        private static void DestroyIconManual(EntityManager em, Entity iconEntity)
        {
            if (iconEntity == Entity.Null || !em.Exists(iconEntity)) return;

            if (em.HasComponent<MapIconTargetEntity>(iconEntity))
                em.RemoveComponent<MapIconTargetEntity>(iconEntity);

            if (em.HasComponent<AttachMapIconsToEntity>(iconEntity))
                em.RemoveComponent<AttachMapIconsToEntity>(iconEntity);

            StatChangeUtility.KillOrDestroyEntity(em, iconEntity, Entity.Null, Entity.Null, 0, StatChangeReason.Any, true);
        }
    }
}