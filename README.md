# 🩸 BestKillfeed

**BestKillfeed** is an advanced killfeed mod for **V Rising**, designed to enhance kill tracking, manage player levels, and introduce community features like leaderboards and detailed player info commands.

---

## 🔧 Main Features

### ✅ Enhanced Killfeed in Chat
- Displays **clan**, **player name**, and **max level** for each kill.
- Automatically detects player max levels on **login**, **kill**, or when executing `.lb` or `.pi` commands.

![Enhanced Killfeed 1](https://i.ibb.co/G3kkwzTp/Kill.png)
![Enhanced Killfeed 2](https://i.ibb.co/20wP0ckT/killOk.png)

### 🛡️ Kill-Steal Protection
- If **Player A** downs **Player B**, but **Player C** finishes them, the kill is credited to **Player A**.

### 🚫 Anti-Grief Level Difference System
- Configurable level-difference protection:
  - For level **91**: max difference = **10 levels**
  - For levels **below 91**: max difference = **15 levels**
- Player levels are shown in **red** if they exceed the allowed difference.

---

### 📊 Custom Commands

#### `.lb` – Leaderboard
- Displays an aesthetic **leaderboard** with:
  - Kills, deaths, max killstreaks
  - Pagination and ranking system

![Leaderboard](https://i.ibb.co/xqtRySgh/Lb.png)

#### `.pi` – Player Info
- Displays detailed **player info**:
  - Name, clan, level, clan members, and connection status
  - Name in **green** = connected  
  - Name in **red** = offline

![Player Info](https://i.ibb.co/FL6KwZBP/Pi.png)

---

### 🎯 Bounty System *(In Development)*
- Players with a **killstreak > 5** are marked with a **bounty icon** on the map.
- ⚠️ *This system is still experimental.*

---

### ⚙️ Easy Configuration
- Edit the `Killfeed.cfg` file to customize:
  - Text colors
  - Level difference thresholds
  - Bounty system toggles
  - Other mod mechanics

---

### 💾 Persistent Data Storage
- `MaxLevels.json` – Stores **maximum detected levels** per player.
- `PlayerStats.json` – Tracks **kills**, **deaths**, and **streaks**.

---

## 🚧 In Development
- 🧪 Improving **bounty system** stability
- 🩸 Adding **vBlood Boss kill tracking**
- 🔄 Planned **integration with other mods**

---

## 📝 License

This mod is distributed under the **MIT License**.  
You are free to **modify** and **redistribute** it, as long as proper **credit is given** to the original author.
