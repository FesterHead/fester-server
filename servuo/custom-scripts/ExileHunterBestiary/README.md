# Hunter's Bestiary & Monster Treatises System

This directory (`servuo/custom-scripts/ExileHunterBestiary/`) contains the **Hunter's Bestiary & Monster Treatises System** for ServUO.

---

## Provenance & Attribution

- **Origin**: [ServUO Community Archive - Hunter's Bestiary & Monster Treatises System (Resource #2674)](https://www.servuo.dev/archive/release-hunters-bestiary-monster-treatises-system-193-creatures-1-line-install.2674/)
- **Original Author**: UO Exile Team / Imagine (ServUO Community Member)
- **Integration**: Placed in `servuo/custom-scripts/ExileHunterBestiary/` and compiled dynamically by ServUO into `Scripts.dll`. Hooked into core creature damage pipeline via [`27-hunter-bestiary.patch`](../../patches/27-hunter-bestiary.patch).

---

## System Overview

The Hunter's Bestiary introduces a horizontal progression and collectathon system for adventurers:
1. **193 Monster Treatises**: Slaying monsters gives a chance (2% standard, 5% boss) to discover a Monster Treatise scroll (`HunterTreatise`).
2. **Permanent +20% Damage Mastery**: Reading a treatise unlocks permanent +20% damage (melee, ranged, and spells) against that specific creature type for the character.
3. **Pet & Tamer Synergy**: Controlled pets benefit from their master's unlocked creature treatises.
4. **Dynamic Tooltips**: Hovering over any treatise scroll dynamically checks the viewer's profile, displaying `[Already Learned]` (green) or `[Not Learned Yet]` (cyan).
5. **The Grand Bestiary Tome (`HunterBestiary`)**: A blessed leather-bound grimoire (`ItemID 0x2252`) that opens the comprehensive bestiary book interface.
6. **Multi-Category Gump (`HunterBestiaryGump`)**: Displays mastery completion across 8 creature categories:
   - Undead
   - Daemons & Fiends
   - Reptiles & Dragons
   - Elementals
   - Humanoids & Fey
   - Arachnids & Insects
   - Beasts & Monsters
   - Bosses, Champions & Peerless
7. **Standalone XML Persistence**: Saves player mastery profiles to `Data/ExileHunterProfiles.xml` during world saves, requiring zero modifications to standard PlayerMobile serialization.

---

## Core Engine Patch Integration

To apply the +20% damage multiplier when attacking mastered creatures, the system hooks into `BaseCreature.OnBeforeDamage` via [`27-hunter-bestiary.patch`](../../patches/27-hunter-bestiary.patch):
- Adds `BaseCreature.OnCreatureDamageHook` delegate to `Scripts/Mobiles/Normal/BaseCreature.cs`.
- Registered automatically during startup by `HunterBestiaryEngine.Initialize()`.

---

## Player & GM Commands

| Command | Access Level | Description |
| :--- | :--- | :--- |
| `[Bestiary` (or `[Bestiario`) | Player | Opens the Hunter's Bestiary gump displaying categories and completion percentages. |
| `[add HunterBestiary` | Game Master | Spawns the physical blessed grimoire item. |
| `[add HunterTreatise <CreatureId>` | Game Master | Spawns a treatise scroll for the given creature (e.g. `Lich`, `Dragon`, `Balron`). |

---

## Directory Layout

```
servuo/custom-scripts/ExileHunterBestiary/
├── Core/
│   ├── BestiaryCategory.cs       # 8 creature category enum definitions
│   ├── HunterBestiaryEngine.cs   # Registry of 193 creatures, drop sink, damage hook, XML persistence
│   ├── HunterCreatureEntry.cs    # Metadata container per creature entry
│   └── HunterProfile.cs          # Per-player learned treatise tracking profile
├── Items/
│   ├── HunterBestiary.cs         # Blessed book item opening the bestiary gump
│   └── HunterTreatise.cs         # Consumable scroll item with dynamic viewer tooltips
├── Gumps/
│   └── HunterBestiaryGump.cs     # 8-tab category browser with completion progress bars
├── README.md                     # Markdown documentation and attribution
└── INSTALLATION_GUIDE.txt        # Original release guide
```
