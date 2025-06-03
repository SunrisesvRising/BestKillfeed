using ProjectM;
using ProjectM.Network;
using Unity.Entities;
using VampireCommandFramework;
using Unity.Collections;
using System.Text;
using System.Collections.Generic;
using BestKillfeed.Services;

#nullable enable
namespace BestKillfeed.Commands
{
    internal class PlayerInfo
    {
        public static class PlayerInfoCommand
        {
            [Command("playerinfo", "pi", description: "Displays information about a player.", adminOnly: false)]
            public static void HandleCommand(ChatCommandContext ctx, string arg)
            {
                var levelService = LevelService.Instance;
                var serverWorld = GetServerWorld();
                if (serverWorld == null)
                {
                    ctx.Reply("Server world is not available.");
                    return;
                }

                var entityManager = serverWorld.EntityManager;
                var allUsers = entityManager.CreateEntityQuery(ComponentType.ReadOnly<User>()).ToEntityArray(Allocator.Temp);
                var onlinePlayers = entityManager.CreateEntityQuery(ComponentType.ReadOnly<PlayerCharacter>()).ToEntityArray(Allocator.Temp);

                Entity targetUserEntity = Entity.Null;
                User targetUserData = default;
                bool isOnline = false;

                foreach (var entity in allUsers)
                {
                    var userData = entityManager.GetComponentData<User>(entity);
                    var nameMatches = userData.CharacterName.ToString().ToLower().Contains(arg.ToLower());
                    var steamIdMatches = userData.PlatformId.ToString() == arg;

                    if (nameMatches || steamIdMatches)
                    {
                        targetUserEntity = entity;
                        targetUserData = userData;

                        foreach (var p in onlinePlayers)
                        {
                            var pc = entityManager.GetComponentData<PlayerCharacter>(p);
                            if (pc.UserEntity == entity)
                            {
                                isOnline = true;
                                break;
                            }
                        }
                        levelService.UpdatePlayerLevel(entity);
                        break;
                    }
                }

                if (targetUserEntity == Entity.Null)
                {
                    ctx.Reply("No player found.");
                    allUsers.Dispose();
                    onlinePlayers.Dispose();
                    return;
                }

                string clanName = "No clan";
                Entity clanEntity = targetUserData.ClanEntity._Entity;
                List<string> memberList = new();

                if (clanEntity != Entity.Null && entityManager.HasComponent<ClanTeam>(clanEntity))
                {
                    var clan = entityManager.GetComponentData<ClanTeam>(clanEntity);
                    clanName = clan.Name.ToString();

                    if (entityManager.HasComponent<SyncToUserBuffer>(clanEntity))
                    {
                        var userBuffer = entityManager.GetBuffer<SyncToUserBuffer>(clanEntity);
                        foreach (var userEntry in userBuffer)
                        {
                            if (!entityManager.Exists(userEntry.UserEntity)) continue;
                            if (!entityManager.HasComponent<User>(userEntry.UserEntity)) continue;

                            var member = entityManager.GetComponentData<User>(userEntry.UserEntity);
                            levelService.UpdatePlayerLevel(userEntry.UserEntity);
                            var memberColor = member.IsConnected ? "#00FF00" : "#FF0000";
                            var memberLevel = levelService.GetMaxLevel(member.CharacterName.ToString());
                            memberList.Add($"<color={memberColor}>{member.CharacterName} [{memberLevel}]</color>");
                        }
                    }
                }

                var targetLevel = levelService.GetMaxLevel(targetUserData.CharacterName.ToString());
                var playerColor = isOnline ? "#00FF00" : "#FF0000";
                var coloredName = $"<color={playerColor}>{targetUserData.CharacterName} [{targetLevel}]</color>";

                var sb = new StringBuilder();
                sb.AppendLine();
                sb.AppendLine("--- <color=#FFD700>Player Info</color> ---");
                sb.AppendLine($"<color=#FFFF00>Name:</color> {coloredName}");
                sb.AppendLine($"<color=#FFFF00>Clan:</color> <color=#FFFFFF>{clanName}</color>");

                if (memberList.Count > 0)
                {
                    var memberLine = string.Join(" - ", memberList);
                    sb.AppendLine($"<color=#FFFF00>Clan Members:</color> {memberLine}");
                }

                ctx.Reply(sb.ToString());

                allUsers.Dispose();
                onlinePlayers.Dispose();
            }

            private static World? GetServerWorld()
            {
                foreach (var world in World.All)
                    if (world.Name == "Server")
                        return world;
                return null;
            }
        }
    }
}
