using BestKillfeed.Services;
using VampireCommandFramework;

namespace BestKillfeed.Commands
{
    internal class ConfigCommand
    {
        // ─────────────────────────────────────────────
        //  RELOAD
        // ─────────────────────────────────────────────

        [Command("kf-reload", "kfr", description: "Reloads killfeed.cfg from disk without restarting the server", adminOnly: true)]
        public static void ReloadConfig(ChatCommandContext ctx)
        {
            KillfeedSettings.Reload();

            // Réappliquer le flag BloodBound après rechargement au cas où RewardItemId ou RewardBloodBound a changé
            BloodBoundService.SetRewardItemBloodBound(KillfeedSettings.RewardBloodBound.Value);

            ctx.Reply("<color=#00ff88>[BestKillfeed]</color> Configuration reloaded from killfeed.cfg.");
        }

        // ─────────────────────────────────────────────
        //  BOUNTY TOGGLE
        // ─────────────────────────────────────────────

        [Command("kf-bounty", "kfb", description: "Enable or disable the bounty system. Usage: .kf-bounty on|off", adminOnly: true)]
        public static void ToggleBounty(ChatCommandContext ctx, string state)
        {
            switch (state.ToLower())
            {
                case "on":
                    KillfeedSettings.EnableBounty.Value = true;
                    BountyService.Instance.RestoreAllBountyIcons();
                    ctx.Reply("<color=#00ff88>[BestKillfeed]</color> Bounty system <color=#00ff00>enabled</color>. Icons and buffs restored for online players.");
                    break;

                case "off":
                    KillfeedSettings.EnableBounty.Value = false;
                    BountyService.Instance.ClearAllBountyIcons();
                    ctx.Reply("<color=#00ff88>[BestKillfeed]</color> Bounty system <color=#ff5555>disabled</color>. All icons and buffs cleared.");
                    break;

                default:
                    ctx.Reply("<color=#ff5555>[BestKillfeed]</color> Usage: .kf-bounty on|off");
                    break;
            }
        }

        // ─────────────────────────────────────────────
        //  KILL REWARDS TOGGLE
        // ─────────────────────────────────────────────

        [Command("kf-rewards", "kfrew", description: "Enable or disable kill rewards. Usage: .kf-rewards on|off", adminOnly: true)]
        public static void ToggleRewards(ChatCommandContext ctx, string state)
        {
            switch (state.ToLower())
            {
                case "on":
                    KillfeedSettings.EnableKillRewards.Value = true;
                    ctx.Reply("<color=#00ff88>[BestKillfeed]</color> Kill rewards <color=#00ff00>enabled</color>.");
                    break;

                case "off":
                    KillfeedSettings.EnableKillRewards.Value = false;
                    ctx.Reply("<color=#00ff88>[BestKillfeed]</color> Kill rewards <color=#ff5555>disabled</color>.");
                    break;

                default:
                    ctx.Reply("<color=#ff5555>[BestKillfeed]</color> Usage: .kf-rewards on|off");
                    break;
            }
        }

        // ─────────────────────────────────────────────
        //  BLOOD-BOUND TOGGLE
        // ─────────────────────────────────────────────

        [Command("kf-bloodbound", "kfbb", description: "Enable or disable blood-bound flag on the reward item. Usage: .kf-bloodbound on|off", adminOnly: true)]
        public static void ToggleBloodBound(ChatCommandContext ctx, string state)
        {
            switch (state.ToLower())
            {
                case "on":
                    KillfeedSettings.RewardBloodBound.Value = true;
                    BloodBoundService.SetRewardItemBloodBound(true);
                    ctx.Reply("<color=#00ff88>[BestKillfeed]</color> Reward item is now <color=#ff4444>Blood-Bound</color> (lost on death, cannot be traded).");
                    break;

                case "off":
                    KillfeedSettings.RewardBloodBound.Value = false;
                    BloodBoundService.SetRewardItemBloodBound(false);
                    ctx.Reply("<color=#00ff88>[BestKillfeed]</color> Reward item is <color=#aaaaaa>no longer blood-bound</color>.");
                    break;

                default:
                    ctx.Reply("<color=#ff5555>[BestKillfeed]</color> Usage: .kf-bloodbound on|off");
                    break;
            }
        }

        // ─────────────────────────────────────────────
        //  REWARD AMOUNTS
        // ─────────────────────────────────────────────

        [Command("kf-setreward", "kfsr", description: "Set the kill reward item amount. Usage: .kf-setreward <amount>", adminOnly: true)]
        public static void SetKillReward(ChatCommandContext ctx, int amount)
        {
            if (amount <= 0)
            {
                ctx.Reply("<color=#ff5555>[BestKillfeed]</color> Amount must be greater than 0.");
                return;
            }
            KillfeedSettings.RewardItemAmount.Value = amount;
            ctx.Reply($"<color=#00ff88>[BestKillfeed]</color> Kill reward set to <color=#ffffff>x{amount}</color>.");
        }

        [Command("kf-setbountyreward", "kfsbr", description: "Set the bounty claim reward item amount. Usage: .kf-setbountyreward <amount>", adminOnly: true)]
        public static void SetBountyReward(ChatCommandContext ctx, int amount)
        {
            if (amount <= 0)
            {
                ctx.Reply("<color=#ff5555>[BestKillfeed]</color> Amount must be greater than 0.");
                return;
            }
            KillfeedSettings.BountyRewardItemAmount.Value = amount;
            ctx.Reply($"<color=#00ff88>[BestKillfeed]</color> Bounty reward set to <color=#ffffff>x{amount}</color>.");
        }

        // ─────────────────────────────────────────────
        //  STATUS
        // ─────────────────────────────────────────────

        [Command("kf-status", "kfs", description: "Shows the current BestKillfeed live configuration", adminOnly: true)]
        public static void ShowStatus(ChatCommandContext ctx)
        {
            string bountyState = KillfeedSettings.EnableBounty.Value ? "<color=#00ff00>ON</color>" : "<color=#ff5555>OFF</color>";
            string rewardsState = KillfeedSettings.EnableKillRewards.Value ? "<color=#00ff00>ON</color>" : "<color=#ff5555>OFF</color>";
            string bbState = KillfeedSettings.RewardBloodBound.Value ? "<color=#ff4444>YES</color>" : "<color=#aaaaaa>no</color>";

            ctx.Reply(
                "<color=#ffaa00>--- BestKillfeed Status ---</color>\n" +
                $"  Bounty          : {bountyState}  (streak threshold: <color=#ffffff>{KillfeedSettings.BountyStreakThreshold.Value} kills</color>)\n" +
                $"  Kill rewards    : {rewardsState}  (x<color=#ffffff>{KillfeedSettings.RewardItemAmount.Value}</color>)\n" +
                $"  Bounty reward   : x<color=#ffffff>{KillfeedSettings.BountyRewardItemAmount.Value}</color>\n" +
                $"  Reward item     : <color=#ffffff>{KillfeedSettings.RewardItemId.Value}</color>\n" +
                $"  Blood-Bound     : {bbState}\n" +
                $"  Max level       : <color=#ffffff>{KillfeedSettings.MaxLevel.Value}</color>\n" +
                $"  Level gap       : normal <color=#ffffff>{KillfeedSettings.MaxLevelGapNormal.Value}</color> / max level <color=#ffffff>{KillfeedSettings.MaxLevelGapHigh.Value}</color>"
            );
        }
    }
}