# Apex Hunt System

This directory (`servuo/custom-scripts/ApexHunt/`) contains the **Apex Hunt** automated server-wide PvM hunting competition system.

---

## Provenance & Attribution

- **Origin**: [ServUO Community Archive - Apex Hunt - Automated Server-Wide PvM Hunting Competition (Resource #2678)](https://www.servuo.dev/archive/release-apex-hunt-automated-server-wide-pvm-hunting-competition.2678/)
- **Original Author**: Imagine (ServUO Community Member)
- **Integration**: Placed in `servuo/custom-scripts/ApexHunt/` and compiled dynamically by ServUO into `Scripts.dll`.

---

## System Overview

Apex Hunt is an automated hunting competition system for ServUO designed to bring excitement and activity to the wilderness:
1. **Dynamic Triggering**: When at least one player character is online and the 2-hour minimum cooldown between events has elapsed, the system performs a 10% chance roll to trigger an event, retrying with a randomized 45 to 75 minute variance between checks if the roll fails.
2. **Pre-Event Countdown**: Once triggered, global broadcast warnings notify online hunters at 10 minutes, 5 minutes, and 1 minute before the cull begins, providing preparation time.
3. **The Hunt**: The system randomly picks a target creature from a 4-tier roster (Swarm, Beast/Elemental, High Threat, Boss/Ancient), determines a randomized kill goal, and opens a floating HUD widget for all active players.
4. **Tie-Breaker Mechanic**: The first hunter to hit the target goal triggers a 60-second tie-breaker window, allowing other hunters a chance to challenge for 1st, 2nd, or 3rd place.
5. **Direct Rewards**: At the conclusion of the hunt, gold bounties (up to 25,000 gp) are deposited directly into winners' bank boxes, with automatic queueing for players who log off before the event concludes.

---

## Player & Staff Commands

| Command | Access Level | Description |
| :--- | :--- | :--- |
| `[ApexHunt` / `[ApexHuntStatus` | Player | Opens the full Leaderboard gump showing target details, kill counts, rank, and bounties. |
| `[ApexHuntToggle` | Player | Toggles the floating on-screen HUD tracker open or closed. |
| `[ApexHuntTop` | Player | Prints the top 3 current hunters in chat. |
| `[ApexHuntStart` | Administrator | Manually initiates a new Apex Hunt immediately. |
| `[ApexHuntStop` | Administrator | Cancels the active hunt or pending countdown immediately. |

---

## Configuration (`Core/ApexHuntConfig.cs`)

| Setting | Default | Description |
| :--- | :--- | :--- |
| `MinEventInterval` | `2 hours` | Minimum quiet period required between hunts before new checks can occur. |
| `MinCheckMinutes` | `45` | Minimum randomized delay (in minutes) between trigger checks. |
| `MaxCheckMinutes` | `75` | Maximum randomized delay (in minutes) between trigger checks. |
| `CheckFrequency` | `60 minutes` | Backward compatibility property representing the average check interval. |
| `TriggerChance` | `0.10` (10%) | Probability of triggering a hunt when checking. |
| `MinOnlinePlayers` | `1` | Minimum number of online characters required for a trigger roll to succeed. |
| `EventFrequency` | `2 hours` | Backward compatibility alias mapping directly to `MinEventInterval`. |
| `MinDurationMinutes` | `25` | Minimum duration of a hunt in minutes. |
| `MaxDurationMinutes` | `45` | Maximum duration of a hunt in minutes. |
| `TieBreakerDuration` | `60 seconds` | Countdown timer activated once a player completes the target goal. |
| `RewardGoldFirstPlace` | `25,000 gp` | 1st place gold bounty deposited into the hunter's bank. |
| `RewardGoldSecondPlace` | `15,000 gp` | 2nd place gold bounty. |
| `RewardGoldThirdPlace` | `10,000 gp` | 3rd place gold bounty. |
| `RewardGoldParticipation` | `5,000 gp` | Consolation bounty for hunters achieving at least 10% of the target goal. |

---

## Architecture & Directory Layout

```
servuo/custom-scripts/ApexHunt/
├── Core/
│   ├── ApexHuntConfig.cs       # Timing, reward values, maps, and creature roster
│   └── ApexHuntEvent.cs        # Event state machine, tick timer, sinks, commands, and persistence
├── Gumps/
│   ├── ApexHuntHudGump.cs      # Draggable / minimizable in-game HUD tracker
│   └── ApexHuntLeaderboardGump.cs # Full leaderboard, prize list, and event status gump
├── README.md                   # Markdown documentation and attribution
└── README.txt                  # Original release documentation with updated settings
```
