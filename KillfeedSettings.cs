using BepInEx.Configuration;
using System.IO;

namespace BestKillfeed
{
    public static class KillfeedSettings
    {
        private static ConfigFile config;

        // Color settings
        public static ConfigEntry<string> KillerNameColor;
        public static ConfigEntry<string> VictimNameColor;
        public static ConfigEntry<string> ClanTagColor;
        public static ConfigEntry<string> AllowedLevelColor;
        public static ConfigEntry<string> ForbiddenLevelColor;
        public static ConfigEntry<string> FfaTagColor;

        // Message format
        public static ConfigEntry<string> KillMessageFormat;

        // Level restriction settings
        public static ConfigEntry<int> MaxLevel;
        public static ConfigEntry<int> MaxLevelGapNormal;
        public static ConfigEntry<int> MaxLevelGapHigh;

        // Bounty
        public static ConfigEntry<bool> EnableBounty;
        public static ConfigEntry<int> BountyStreakThreshold;
        public static ConfigEntry<int> BountyClaimDuration;
        public static ConfigEntry<int> BountyClaimGracePeriod;

        // Reward
        public static ConfigEntry<bool> EnableKillRewards;
        public static ConfigEntry<int> RewardItemId;
        public static ConfigEntry<int> RewardItemAmount;
        public static ConfigEntry<int> BountyRewardItemAmount;
        public static ConfigEntry<float> BountyRewardStreakMultiplier;
        public static ConfigEntry<bool> RewardBloodBound;

        // Multiplier
        public static ConfigEntry<float> MultiplierSpree;
        public static ConfigEntry<float> MultiplierRelentless;
        public static ConfigEntry<float> MultiplierGodlike;

        // Threshold Streak
        public static ConfigEntry<int> ThresholdSpree;
        public static ConfigEntry<int> ThresholdRelentless;
        public static ConfigEntry<int> ThresholdGodlike;

        public static void Init(string configPath)
        {
            config = new ConfigFile(Path.Combine(configPath, "killfeed.cfg"), true);

            // Colors
            KillerNameColor = config.Bind("Colors", "KillerNameColor", "#ffffff", "Color of the killer's name.");
            VictimNameColor = config.Bind("Colors", "VictimNameColor", "#ffffff", "Color of the victim's name.");
            ClanTagColor = config.Bind("Colors", "ClanTagColor", "#888888", "Color of the clan tags.");
            AllowedLevelColor = config.Bind("Colors", "AllowedLevelColor", "#55ff55", "Color for allowed kills (fair level difference).");
            ForbiddenLevelColor = config.Bind("Colors", "ForbiddenLevelColor", "#ff5555", "Color for forbidden kills (too high level difference).");
            FfaTagColor = config.Bind("Colors", "FfaTagColor", "#ff8800", "Color of the [FFA] tag shown in killfeed when both players are in a FFA zone (requires BestFFAZones).");

            // Message format
            KillMessageFormat = config.Bind("Message", "KillMessageFormat",
                "{FfaTag}{Warning}<color={KillerNameColor}>{Killer}</color> [<color={LevelColor}>{KillerLevel}</color>] killed <color={VictimNameColor}>{Victim}</color> [<color={LevelColor}>{VictimLevel}</color>]<color={LevelColor}>{Streak}{Diff}</color>",
                "Killfeed message format. Available placeholders: {FfaTag}, {Warning}, {Killer}, {Victim}, {KillerClan}, {VictimClan}, {KillerLevel}, {VictimLevel}, {LevelColor}, {KillerNameColor}, {VictimNameColor}, {ClanTagColor}, {Streak}, {Diff}");

            // Level gap restrictions
            MaxLevel = config.Bind("Restrictions", "MaxLevel", 80, "Maximum level.");
            MaxLevelGapNormal = config.Bind("Restrictions", "MaxLevelGapNormal", 10, "Maximum level difference allowed for fair kills when killer is below max level.");
            MaxLevelGapHigh = config.Bind("Restrictions", "MaxLevelGapHigh", 6, "Maximum level difference allowed for fair kills when killer is max level.");

            // Bounty
            EnableBounty = config.Bind("Bounty", "EnableBounty", true, "Enable or disable the bounty system.");
            BountyStreakThreshold = config.Bind("Bounty", "BountyStreakThreshold", 5, "Number of kills required to trigger a bounty.");
            BountyClaimDuration = config.Bind("Bounty", "BountyClaimDuration", 30, "Duration in seconds the bounty target must stay at the Sacrificial Site to claim their bounty.");
            BountyClaimGracePeriod = config.Bind("Bounty", "BountyClaimGracePeriod", 5, "Grace period in seconds allowed outside the zone before the channel is cancelled.");

            // Rewards
            EnableKillRewards = config.Bind("Rewards", "EnableKillRewards", true, "Enable item rewards on kill.");
            RewardItemId = config.Bind("Rewards", "RewardItemId", -598100816, "PrefabGUID of the reward item.");
            RewardItemAmount = config.Bind("Rewards", "RewardItemAmount", 10, "Amount of items to reward per kill.");
            BountyRewardItemAmount = config.Bind("Rewards", "BountyRewardItemAmount", 50, "Base amount of items given for claiming or killing a bounty target.");
            BountyRewardStreakMultiplier = config.Bind("Rewards", "BountyRewardStreakMultiplier", 0.1f,
                "Multiplier applied per streak point to the bounty reward. " +
                "Formula: BountyRewardItemAmount * (1 + streak * multiplier). " +
                "Example: 50 base, streak 10, multiplier 0.1 → 50 * (1 + 10 * 0.1) = 100 items. Set to 0 to disable scaling.");
            RewardBloodBound = config.Bind("Rewards", "RewardBloodBound", true, "If true, reward items are blood-bound (lost on death, cannot be traded).");

            // Multiplier and Threshold
            ThresholdSpree = config.Bind("Rewards", "ThresholdSpree", 3, "Kills needed for the first multiplier.");
            MultiplierSpree = config.Bind("Rewards", "MultiplierSpree", 1.2f, "Multiplier for the first threshold.");
            ThresholdRelentless = config.Bind("Rewards", "ThresholdRelentless", 6, "Kills needed for the second multiplier.");
            MultiplierRelentless = config.Bind("Rewards", "MultiplierRelentless", 1.5f, "Multiplier for the second threshold.");
            ThresholdGodlike = config.Bind("Rewards", "ThresholdGodlike", 10, "Kills needed for the third multiplier.");
            MultiplierGodlike = config.Bind("Rewards", "MultiplierGodlike", 2.0f, "Multiplier for the third threshold.");
        }

        public static void Reload()
        {
            config?.Reload();
        }
    }
}