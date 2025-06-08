using BestKillfeed.Services;
using VampireCommandFramework;
using System.Linq;

namespace BestKillfeed.Commands
{
    internal class BountyListCommand
    {
        [Command("bounties", "b", description: "Displays the list of players with an active bounty")]
        public static void HandleCommand(ChatCommandContext ctx)
        {
            var allStats = StatsService.Instance.GetAllStats();

            var playersWithBounty = allStats
                .Where(kv => kv.Value.Bounty == 1)
                .OrderByDescending(kv => kv.Value.Kills)
                .ToList();

            if (playersWithBounty.Count == 0)
            {
                ctx.Reply("<color=#ffaa00>No players currently have an active bounty.</color>");
                return;
            }

            ctx.Reply($"<color=#ffaa00>--- List of players with an active bounty ({playersWithBounty.Count}) ---</color>");

            int rank = 1;
            foreach (var (playerName, stats) in playersWithBounty)
            {
                var line = $"<color=#aaaaaa>#{rank}</color> <color=#ff5555>{playerName}</color> - " +
                           $"Kills: <color=#55ff55>{stats.Kills}</color> / " +
                           $"Deaths: <color=#ff5555>{stats.Deaths}</color> / " +
                           $"Max Streak: <color=#55aaff>{stats.MaxStreak}</color>";
                ctx.Reply(line);
                rank++;
            }
        }
    }
}