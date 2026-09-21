# Legendary Master of Skills

This directory (`servuo/custom-scripts/LegendaryMaster/`) contains the **Legendary Master of Skills** NPC questmaster system.

---

## Provenance & Attribution

- **Origin**: [ServUO Community Archive - Legendary Master of Skills (Resource #2244)](https://www.servuo.dev/archive/legendary-master-of-skills.2244/)
- **Original Author**: Keith (a.k.a. Jack / Keith on servuo.dev)
- **License**: GNU General Public License v3.0 (GPL-3.0)
- **Integration**: Placed in `servuo/custom-scripts/LegendaryMaster/` and compiled dynamically by ServUO into `Scripts.dll`.

---

## System Overview

The **Legendary Master of Skills** is an interactive questmaster NPC designed for small shards and solo adventurers as an alternative path to obtain PowerScrolls without running Champion Spawns:
1. Players who have attained Grandmaster standing (100.0) in a supported skill may approach the Master to request a combat challenge.
2. The Master tasks the adventurer with slaying a series of high-level dungeon bosses within an allotted countdown timer.
3. Successfully completing the trial rewards the player with the next +5 PowerScroll tier (105, 110, 115, or 120) for that skill, plus a small chance at a bonus Stat Cap Scroll.

---

## Player Speech Commands

Players interact with the NPC by speaking nearby (within 5 tiles):

| Spoken Command | Description | Example |
| :--- | :--- | :--- |
| `give task <skill>` | Requests a new combat task for the specified skill. | `give task swordsmanship`<br>`give task magery`<br>`give task taming` |
| `remove task`<br>`cancel task` | Abandons the currently active task so a new one can be started. | `remove task` |

---

## Quest Rules & Requirements

1. **Prerequisite Skill Level**:
   - The player must have reached at least **100.0 (Grandmaster)** in the requested skill.
   - The player must have capped their skill at their current maximum before seeking the next tier (e.g. 100.0/100 to receive a 105 scroll; 105.0/105 to receive a 110 scroll; 110.0/110 to receive a 115 scroll; 115.0/115 to receive a 120 scroll).
   - Once a skill cap reaches 120.0, no further tasks can be requested for that skill.
2. **One Active Task**:
   - Players may only have one active task at any given time across all skills.
3. **Valid PowerScroll Skills**:
   - Only skills that support standard UO PowerScrolls are eligible (e.g. Combat, Magic, Taming, Crafting, Bardic, Bushido, Ninjitsu, Spellweaving, Mysticism, Throwing, Imbuing).
4. **Target Pool**:
   - Tasks require hunting creatures randomly drawn from high-level dungeon encounters across Britannia, Ilshenar, Malas, and Tokuno (configurable in `LegendaryMaster.cfg`):
     - **Balron** (Hythloth, Abyss)
     - **Shadow Wyrm** (Destard)
     - **Ancient Lich** (Deceit)
     - **Ancient Wyrm** (Destard)
     - **Skeletal Dragon** (Ankh Dungeon)
     - **Greater Dragon** (Destard)
     - **Succubus** (Abyss, Hythloth)
     - **Rotting Corpse** (Deceit)
     - **Blood Elemental** (Blood Dungeon)
     - **Poison Elemental** (Destard, Shame)
     - **Serpentine Dragon** (Ilshenar)
     - **Bone Demon** (Doom)
     - **Rune Beetle** (Tokuno)
     - **Yamandon** (Tokuno)
     - **White Wyrm** (Ice Dungeon)
5. **Time Limit & Extensions**:
   - Tasks grant an initial countdown (default 60 minutes).
   - Each confirmed target kill adds bonus time (default +10 minutes) to the countdown.
   - Kills qualify when performed directly by the player, their pets, or their summons.

---

## Rewards

- **PowerScroll**: A +5 PowerScroll matching the requested skill tier (105, 110, 115, or 120) placed directly into the player's backpack.
- **Bonus Stat Cap Scroll**: A 1% chance (configurable) to receive an additional Stat Cap Scroll (+5 up to +25).

---

## Configuration (`servuo/Config/LegendaryMaster/LegendaryMaster.cfg`)

All task timers, kill counts, scaling factors, skill limits, stat scroll rewards, NPC behavior, and the target creature pool are externalized in [`servuo/Config/LegendaryMaster/LegendaryMaster.cfg`](../../Config/LegendaryMaster/LegendaryMaster.cfg):

```ini
# --- Quest Timers ---
TaskTimeMinutes=60
ExtraTimeMinutes=10

# --- Kill Counts & Difficulty Scaling ---
BaseMinKills=1
BaseMaxKills=3
KillsPerTier=1

# --- Skill Progression Limits ---
MinSkillRequired=100.0
MaxSkillCap=120.0
ScrollIncrement=5.0

# --- Bonus Stat Cap Scrolls ---
EnableStatScrolls=true
StatScrollChance=0.01
HighStatScrollChance=0.01
MaxStatScrollBonus=25

# --- NPC Behavior ---
InteractionRange=5
IdleBarkChance=0.15

# --- Target Creature Pool ---
Creatures=Balron, ShadowWyrm, AncientLich, AncientWyrm, SkeletalDragon, GreaterDragon, Succubus, RottingCorpse, BloodElemental, PoisonElemental, SerpentineDragon, BoneDemon, RuneBeetle, Yamandon, WhiteWyrm
```

Changes take effect upon restarting the container:
```bash
docker compose restart servuo
```

---

## In-Game Staff Commands

Spawn a Legendary Master NPC in a public hub or town plaza:

```text
[add LegendaryMaster
```

---

## Enhancements & Bug Fixes Applied

The following improvements were made over the original community script release:
- **Direct Player Kill Credit**: Fixed an issue where only pet/summon kills registered progress; direct weapon and spell kills now properly advance the task.
- **Fatal Index Out of Range Fix**: Replaced `taskInfos[pm.Serial]` indexing on speech with safe entity lookups to prevent server crashes.
- **Immediate Task Completion**: Corrected the task counter decrement order so rewards are awarded immediately upon defeating the final required creature.
- **Human-Readable Timers & Creature Names**: Formatted countdown messages in remaining minutes and cleaned creature class names (e.g. "Shadow Wyrm" instead of `Server.Mobiles.ShadowWyrm`).
- **Complete Creature Pool**: Fixed an off-by-one random selection bug that previously prevented White Wyrms from ever being selected.
- **Flexible & Case-Insensitive Speech**: Made speech parsing case-insensitive and accepted both display names and enum names.
- **World Persistence**: Added WorldSave/WorldLoad state persistence to `Saves/LegendaryMaster/Persistence.bin` so active player quests survive server restarts.
