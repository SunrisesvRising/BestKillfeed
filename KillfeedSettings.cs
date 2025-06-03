using BepInEx.Configuration;
using System.IO;

namespace BestKillfeed
{
    public static class KillfeedSettings
    {
        private static ConfigFile config;

        // Bounty Settings
        public static ConfigEntry<bool> EnableBountySystem;
        public static ConfigEntry<int> BountyStreakThreshold;

        // Color settings
        public static ConfigEntry<string> KillerNameColor;
        public static ConfigEntry<string> VictimNameColor;
        public static ConfigEntry<string> ClanTagColor;
        public static ConfigEntry<string> AllowedLevelColor;
        public static ConfigEntry<string> ForbiddenLevelColor;

        // Message format
        public static ConfigEntry<string> KillMessageFormat;

        // Level restriction settings
        public static ConfigEntry<int> MaxLevelGapNormal;
        public static ConfigEntry<int> MaxLevelGapHigh;

        public static void Init(string configPath)
        {
            config = new ConfigFile(Path.Combine(configPath, "killfeed.cfg"), true);

            // Bounty Settings
            EnableBountySystem = config.Bind("Bounty", "EnableBountySystem", true, "Enable or disable the bounty system.");
            BountyStreakThreshold = config.Bind("Bounty", "BountyStreakThreshold", 5, "Kill streak required to place a bounty.");

            // Colors
            KillerNameColor = config.Bind("Colors", "KillerNameColor", "#ffffff", "Color of the killer's name.");
            VictimNameColor = config.Bind("Colors", "VictimNameColor", "#ffffff", "Color of the victim's name.");
            ClanTagColor = config.Bind("Colors", "ClanTagColor", "#888888", "Color of the clan tags.");
            AllowedLevelColor = config.Bind("Colors", "AllowedLevelColor", "#55ff55", "Color for allowed kills (fair level difference).");
            ForbiddenLevelColor = config.Bind("Colors", "ForbiddenLevelColor", "#ff5555", "Color for forbidden kills (too high level difference).");

            // Message format
            KillMessageFormat = config.Bind("Message", "KillMessageFormat",
                "<color={ClanTagColor}>[{KillerClan}]</color><color={KillerNameColor}>{Killer}</color>[<color={LevelColor}>{KillerLevel}</color>] killed <color={ClanTagColor}>[{VictimClan}]</color><color={VictimNameColor}>{Victim}</color>[<color={LevelColor}>{VictimLevel}</color>]",
                "Killfeed message format. Available placeholders: {Killer}, {Victim}, {KillerClan}, {VictimClan}, {KillerLevel}, {VictimLevel}, {LevelColor}, {KillerNameColor}, {VictimNameColor}, {ClanTagColor}");

            // Level gap restrictions
            MaxLevelGapNormal = config.Bind("Restrictions", "MaxLevelGapNormal", 15, "Maximum level difference allowed for fair kills when killer is below level 91.");
            MaxLevelGapHigh = config.Bind("Restrictions", "MaxLevelGapHigh", 10, "Maximum level difference allowed for fair kills when killer is level 91 or higher.");

        }
    }
}
