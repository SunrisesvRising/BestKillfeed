using BestKillfeed.Services;
using VampireCommandFramework;
using System;
using System.Linq;
using BestKillfeed.Database;

namespace BestKillfeed.Commands
{
    internal class LeaderboardCommand
    {
        [Command("leaderboard", "lb", description: "Displays the player leaderboard with full stats", adminOnly: false)]
        public static void HandleCommand(ChatCommandContext ctx, string arg = null)
        {
            int pageSize = 10;
            int page = 1;

            if (!string.IsNullOrWhiteSpace(arg) && int.TryParse(arg, out var parsedPage))
            {
                page = parsedPage;
            }

            // 1. Fetch all players from LiteDB
            var allPlayers = PlayerService.Instance.GetAllPlayers();

            if (allPlayers == null || allPlayers.Count == 0)
            {
                ctx.Reply("The leaderboard is currently empty.");
                return;
            }

            // 2. Sort by Kills descending
            var sortedPlayers = allPlayers.OrderByDescending(p => p.Kills).ToList();

            var totalPages = (int)Math.Ceiling((double)sortedPlayers.Count / pageSize);
            page = Math.Clamp(page, 1, totalPages);

            // 3. Header
            ctx.Reply($"<color=#ffaa00>--- Leaderboard (Page {page}/{totalPages}) ---</color>");

            int startIndex = (page - 1) * pageSize;
            int count = Math.Min(pageSize, sortedPlayers.Count - startIndex);

            // 4. Player rows (Sent individually to bypass character limit)
            for (int i = 0; i < count; i++)
            {
                var player = sortedPlayers[startIndex + i];
                int rank = startIndex + i + 1;

                var line = $"<color=#aaaaaa>#{rank}</color> <color=#ffffff>{player.Name}</color> <color=#00ffff>[Lvl {player.MaxLevel}]</color> - " +
                           $"K: <color=#55ff55>{player.Kills}</color> | " +
                           $"D: <color=#ff5555>{player.Deaths}</color> | " +
                           $"MS: <color=#55aaff>{player.MaxStreak}</color>";

                ctx.Reply(line);
            }

            // 5. Colored Personal Footer
            var callerName = ctx.Event.User.CharacterName.ToString();
            var callerRank = sortedPlayers.FindIndex(p => p.Name == callerName) + 1;

            if (callerRank > 0)
            {
                var p = sortedPlayers[callerRank - 1];
                var footer = $"<color=#ffaa00>Your Rank: #{callerRank}</color> | " +
                             $"Lvl: <color=#00ffff>{p.MaxLevel}</color> | " +
                             $"K: <color=#55ff55>{p.Kills}</color> | " +
                             $"D: <color=#ff5555>{p.Deaths}</color> | " +
                             $"MS: <color=#55aaff>{p.MaxStreak}</color>";

                ctx.Reply(footer);
            }
        }
    }
}