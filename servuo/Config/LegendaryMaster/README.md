# Legendary Master Configuration Directory

This directory (`servuo/Config/LegendaryMaster/`) contains configuration settings for the **Legendary Master of Skills** NPC questmaster system.

---

## Configuration Settings (`LegendaryMaster.cfg`)

| Parameter | Default | Description |
| :--- | :---: | :--- |
| **Quest Timers** | | |
| `TaskTimeMinutes` | `60` | The initial time limit (in minutes) given to a player upon accepting a new task. |
| `ExtraTimeMinutes` | `10` | The bonus time (in minutes) added to the player's countdown for each target kill. |
| **Kill Counts & Difficulty Scaling** | | |
| `BaseMinKills` | `1` | The minimum number of kills rolled for a baseline task. |
| `BaseMaxKills` | `3` | The maximum number of kills rolled for a baseline task. |
| `KillsPerTier` | `1` | Extra kills added to the maximum pool per tier (+5) above GM (105=+1, 110=+2, etc.). |
| **Skill Progression Limits** | | |
| `MinSkillRequired` | `100.0` | Minimum skill level required to request a quest task (default: 100.0 GM). |
| `MaxSkillCap` | `120.0` | Maximum skill cap supported by this NPC (default: 120.0 Legendary). |
| `ScrollIncrement` | `5.0` | Skill cap increment granted per completed task (default: 5.0). |
| **Bonus Stat Cap Scrolls** | | |
| `EnableStatScrolls` | `true` | Master toggle enabling or disabling bonus Stat Cap Scrolls upon task completion. |
| `StatScrollChance` | `0.01` (1%) | The base probability to receive a bonus Stat Cap Scroll upon completing a task. |
| `HighStatScrollChance` | `0.01` (1%) | The probability that an awarded Stat Cap Scroll is high-tier (+10 to +25) instead of standard (+5). |
| `MaxStatScrollBonus` | `25` | Maximum stat cap bonus allowed from high-tier rolls (5, 10, 15, 20, or 25). |
| **NPC Behavior** | | |
| `InteractionRange` | `5` | Radius (in tiles) for player speech recognition. |
| `IdleBarkChance` | `0.15` (15%) | Probability to speak a helpful hint when non-command speech is heard nearby. |
| **Target Creature Pool** | | |
| `Creatures` | *(15 apex species)* | Comma-separated list of creature class names that can be assigned as hunt targets (e.g. `Balron, GreaterDragon, SkeletalDragon, Yamandon`). |

---

## Modifying Configurations

1. Open [`LegendaryMaster.cfg`](LegendaryMaster.cfg).
2. Adjust time limits, kill counts, skill progression, stat scroll rewards, or creature targets as desired.
3. Restart the ServUO container for changes to take effect:
   ```bash
   docker compose restart servuo
   ```
