# BestKillfeed

**BestKillfeed** is a server-side BepInEx mod for **V Rising** that enhances PvP with a rich killfeed, persistent player statistics, an item reward system, a bounty system, and matchmaking tools. All data is persisted via LiteDB across server restarts.

---

## Features

- **Killfeed** — Broadcasts kill announcements to all players with killer/victim names, levels, clan tags, kill streaks, and a level-gap warning indicator. The message format is fully customizable.
- **FFA Zone Integration** — Optional integration with **BestFFAZones**. When both players are in a FFA zone, the killfeed displays a `[FFA]` tag and level gap restrictions are ignored — rewards are granted regardless of level difference.
- **Persistent Stats** — Every player's kills, deaths, current streak, max streak, and max level are stored in a LiteDB database (`BepInEx/config/BestKillfeed/KillfeedData.db`).
- **Kill Rewards** — Grants configurable items on kill. Streak multipliers (Killing Spree / Relentless / Godlike) increase the reward amount. Rewards can optionally be flagged Blood-Bound (lost on death, untradeable).
- **Bounty System** — Automatically places a bounty on players who reach a configurable kill streak. Bounty targets appear on the map with a custom icon and receive a visible buff. The bounty can be claimed by killing the target or by the target themselves at the Sacrificial Site. The channel duration and grace period are fully configurable.
- **Bounty Reward Scaling** — Bounty rewards scale with the target's streak. The higher the streak, the greater the reward — both for the killer and for the target if they self-claim. The multiplier is configurable.
- **Leaderboard** — Paginated leaderboard sorted by total kills, showing level, kills, deaths, and max streak. Displays the caller's own rank at the bottom.
- **Player Info** — Detailed card for any player showing online status, level, K/D ratio, current streak, clan name, clan members (with online status), and aggregated clan stats.
- **Target Finder** — Two matchmaking commands that list online or offline players (and clans) within the configured level gap, useful for finding fair fights.
- **Live Admin Controls** — All key settings can be toggled or adjusted in real time without restarting the server.

---

## Dependencies

- [VampireCommandFramework](https://thunderstore.io/c/v-rising/p/deca/VampireCommandFramework/) `>= 0.10.3`

### Optional
- [BestFFAZones](https://github.com/SunrisesvRising/BestFFAZones) — Enables FFA zone integration (soft-dependency, BestKillfeed works without it).

---

## Installation

1. Install **BepInEx** on your V Rising server.
2. Install **VampireCommandFramework**.
3. Drop `BestKillfeed.dll` and `LiteDB.dll` into `BepInEx/plugins/`.
4. *(Optional)* Install **BestFFAZones** to enable FFA zone integration.
5. Start the server. The config file `killfeed.cfg` and the database will be created automatically under `BepInEx/config/BestKillfeed/`.

> **Note:** If you are upgrading from a previous version, delete the old `killfeed.cfg` first to ensure all new settings are generated correctly.

---

## Configuration

The file `BepInEx/config/BestKillfeed/killfeed.cfg` is generated on first launch and can be reloaded at runtime with `.kf-reload`.

| Section | Key | Default | Description |
|---|---|---|---|
| Colors | KillerNameColor | `#ffffff` | Killer name color |
| Colors | VictimNameColor | `#ffffff` | Victim name color |
| Colors | ClanTagColor | `#888888` | Clan tag color |
| Colors | AllowedLevelColor | `#55ff55` | Color for fair kills |
| Colors | ForbiddenLevelColor | `#ff5555` | Color for level-gap violations |
| Colors | FfaTagColor | `#ff8800` | Color of the `[FFA]` tag (requires BestFFAZones) |
| Message | KillMessageFormat | *(see below)* | Killfeed message template |
| Restrictions | MaxLevel | `80` | Level considered "max level" |
| Restrictions | MaxLevelGapNormal | `10` | Max allowed level gap below max level |
| Restrictions | MaxLevelGapHigh | `6` | Max allowed level gap at max level |
| Bounty | EnableBounty | `true` | Enable/disable the bounty system |
| Bounty | BountyStreakThreshold | `5` | Kill streak that triggers a bounty |
| Bounty | BountyClaimDuration | `30` | Seconds the target must hold at the Sacrificial Site to self-claim |
| Bounty | BountyClaimGracePeriod | `5` | Grace period in seconds allowed outside the zone before cancellation |
| Rewards | EnableKillRewards | `true` | Enable/disable item rewards on kill |
| Rewards | RewardItemId | `-598100816` | PrefabGUID of the reward item |
| Rewards | RewardItemAmount | `10` | Base reward amount per kill |
| Rewards | BountyRewardItemAmount | `50` | Base reward for claiming a bounty |
| Rewards | BountyRewardStreakMultiplier | `0.1` | Multiplier per streak point applied to bounty rewards. Formula: `base * (1 + streak * multiplier)`. Set to `0` to disable scaling |
| Rewards | RewardBloodBound | `true` | Make reward items Blood-Bound |
| Rewards | ThresholdSpree | `3` | Streak for first multiplier |
| Rewards | MultiplierSpree | `1.2` | First streak multiplier |
| Rewards | ThresholdRelentless | `6` | Streak for second multiplier |
| Rewards | MultiplierRelentless | `1.5` | Second streak multiplier |
| Rewards | ThresholdGodlike | `10` | Streak for third multiplier |
| Rewards | MultiplierGodlike | `2.0` | Third streak multiplier |

### Kill Message Placeholders

`{FfaTag}` `{Warning}` `{Killer}` `{Victim}` `{KillerClan}` `{VictimClan}` `{KillerLevel}` `{VictimLevel}` `{Streak}` `{Diff}` `{LevelColor}` `{KillerNameColor}` `{VictimNameColor}` `{ClanTagColor}`

---

## Commands

### Player Commands

| Command | Alias | Description |
|---|---|---|
| `.leaderboard [page]` | `.lb` | Paginated leaderboard (10 per page) sorted by kills |
| `.playerinfo <name>` | `.pi` | Full stats card for a player including clan info |
| `.targetsonline [page]` | `.ton` | Online players/clans within your level gap |
| `.targetsoffline [page]` | `.toff` | Offline players/clans within your level gap |
| `.bountylist` | `.bl` | List all players with an active bounty |
| `.bounty-claim` | `.bc` | Claim your own bounty — stay at the Sacrificial Site for the configured duration |

### Admin Commands

| Command | Alias | Description |
|---|---|---|
| `.bounty-add <name>` | `.ba` | Manually place a bounty on a player |
| `.bounty-remove <name>` | `.br` | Manually remove a bounty from a player |
| `.bounty-force-clear-icon <name>` | `.bfci` | Force-remove only the map icon (no DB change) |
| `.kf-reload` | `.kfr` | Reload `killfeed.cfg` from disk |
| `.kf-bounty on\|off` | `.kfb` | Enable or disable the bounty system live |
| `.kf-rewards on\|off` | `.kfrew` | Enable or disable kill rewards live |
| `.kf-bloodbound on\|off` | `.kfbb` | Toggle Blood-Bound flag on the reward item |
| `.kf-setreward <amount>` | `.kfsr` | Set the kill reward item amount |
| `.kf-setbountyreward <amount>` | `.kfsbr` | Set the base bounty reward amount |
| `.kf-setclaimduration <seconds>` | `.kfscd` | Set the bounty claim channel duration |
| `.kf-setclaimgrace <seconds>` | `.kfscg` | Set the bounty claim grace period |
| `.kf-setbountymultiplier <value>` | `.kfsbm` | Set the bounty reward streak multiplier |
| `.kf-status` | `.kfs` | Display the current live configuration |

---

## Bounty System — How It Works

1. A player reaches the configured streak threshold (default: 5 kills) → a server-wide announcement is made and a map icon appears over them.
2. Any player can kill the target to claim the bounty reward. The reward scales with the target's current streak.
3. Alternatively, the bounty target can go to the **Sacrificial Site** and type `.bc` to start a channel. If they stay in the zone for the configured duration and survive, they claim the reward themselves and their streak is reset to zero. Leaving the zone starts a grace countdown before cancellation.
4. Dying or disconnecting during the channel cancels it immediately.

### Bounty Reward Scaling Examples

With default values (base `50`, multiplier `0.1`):

| Streak | Reward |
|---|---|
| 5 | **75 items** |
| 10 | **100 items** |
| 15 | **125 items** |
| 20 | **150 items** |

---

## FFA Zone Integration

If **BestFFAZones** is installed alongside BestKillfeed, the following applies when both the killer and the victim are inside a FFA zone:

- The killfeed message is prefixed with a `[FFA]` tag.
- Level gap restrictions are ignored — rewards are always granted regardless of level difference.

BestFFAZones is a **soft-dependency** — BestKillfeed works normally without it.

---

## Level Gap Rules

Kills where the killer's level exceeds the victim's level by more than the configured gap are flagged with `[!]` in the killfeed and do **not** grant item rewards. The gap is tighter at max level:

- Below max level: gap ≤ `MaxLevelGapNormal` (default 10)
- At max level: gap ≤ `MaxLevelGapHigh` (default 6)

> Level gap rules are automatically bypassed in FFA zones (requires BestFFAZones).

---

## Data Storage

Player data is stored in a LiteDB embedded database at:
```
BepInEx/config/BestKillfeed/KillfeedData.db
```
The database is opened in **Direct** mode and is closed cleanly when the plugin unloads. Backing it up is as simple as copying the `.db` file.

---

## License

This project is provided as-is for private server use. No warranty is implied.