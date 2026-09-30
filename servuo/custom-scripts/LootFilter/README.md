# The Ultimate ARPG-Style Loot Filter

This directory (`servuo/custom-scripts/LootFilter/`) contains the **ARPG-Style Loot Filter** system for ServUO, inspired by Diablo and Path of Exile.

---

## Provenance & Attribution

- **Origin**: [ServUO Community Archive - The Ultimate ARPG-Style Loot Filter (Diablo / PoE - exactly like you want it) (Resource #2606)](https://www.servuo.dev/archive/the-ultimate-arpg-style-loot-filter-diablo-poe-exactly-like-you-want-it.2606/)
- **Integration**: Placed in `servuo/custom-scripts/LootFilter/` and dynamically compiled into `Scripts.dll`. Hooked into core player item visibility via [`29-loot-filter.patch`](../../patches/29-loot-filter.patch).

---

## System Overview

The system provides complete player-side control over ground and corpse loot visibility to streamline dungeon farming and grinding:

1. **8 Filter Categories & 87+ Properties**:
   - **Primary**: Physical, Fire, Cold, Poison, Energy resistances, and total resistance thresholds.
   - **Combat**: Hit Chance Increase, Defense Chance Increase, Damage Increase, Swing Speed Increase, Velocity, etc.
   - **Magic**: Lower Mana Cost, Faster Casting, Faster Cast Recovery, Spell Damage Increase, Lower Reagent Cost, Mage Armor, Mage Weapon, etc.
   - **Resists**: Granular armor and jewelry resistance filters.
   - **Damage**: Elemental damage percentage distributions (Physical, Fire, Cold, Poison, Energy, Chaos, Direct).
   - **HitsLeech**: Hit Life Leech, Hit Mana Leech, Hit Stamina Leech, Hit Fatigue, Hit Mana Drain.
   - **HitsMagic**: Hit Spell effects (Fireball, Lightning, Magic Arrow, Harm, Curse, Dispel).
   - **Special**: Balanced, Self Repair, Night Sight, Lower Stat Requirements, Soul Charge, Splintering Weapon, Absorption.
2. **Interactive Multi-Category Gump (`[LootFilter`)**:
   - Paginated navigation across categories with dynamic zebra-striping.
   - Individual enable/disable checkboxes and numeric threshold inputs for every property.
3. **Global Toggle**:
   - Master switch to turn the entire filter on or off without resetting individual property configurations.
4. **Safe Filtering Guarantees**:
   - Only filters equipment (Weapons, Armor, Clothing, Hats, Jewelry, Quivers).
   - Never hides artifacts (`ArtifactRarity > 0` or items with `IsArtifact = true`).
   - Never hides Gold, Gems, Reagents, Potions, Keys, Maps, or Quest items.
   - **Backpack & Bank Safety**: Items inside the player's backpack, subcontainers, or bank box are never hidden, regardless of filter strictness.
5. **Character Persistence**:
   - Attached via `XmlAttachment` (`LootFilterAttachment`), persisting each player's configuration across sessions and world saves.

---

## Core Engine Patch Integration

To hide filtered items without deleting them for other players, the system hooks into `PlayerMobile.CanSee(Item item)` via [`29-loot-filter.patch`](../../patches/29-loot-filter.patch):

- **Core Hook** (`Scripts/Mobiles/PlayerMobile.cs`):
  ```csharp
  public static Func<Mobile, Item, bool> LootFilterCheck { get; set; }

  public override bool CanSee(Item item)
  {
      ...
      // ARPG Loot Filter - applies to all levels if enabled
      bool isLoot = item.Parent == null || item.RootParent is Corpse;
      if (isLoot && LootFilterCheck != null && !LootFilterCheck(this, item))
      {
          return false;
      }

      return base.CanSee(item);
  }
  ```
- **Runtime Registration** (`LootFilterController.cs`):
  ```csharp
  public static void Initialize()
  {
      PlayerMobile.LootFilterCheck = PassesFilter;
  }
  ```

---

## Player Commands

| Command | Access Level | Description |
| :--- | :--- | :--- |
| `[LootFilter` | Player | Opens the ARPG Loot Filter configuration gump. |

---

## Directory Layout

```
servuo/custom-scripts/LootFilter/
├── README.md                 # System documentation, attribution, and architecture guide
├── Info.txt                  # Original release information notes
├── Install instructions.txt  # Upstream installation guide reference
├── LootFilterAttachment.cs   # XmlAttachment storing player filter settings
├── LootFilterController.cs   # Core evaluation engine & PlayerMobile hook registration
├── LootFilterGump.cs         # Main paginated configuration gump & [LootFilter command
├── LootFilterInfoGump.cs     # In-game information help gump
├── LootFilterSettings.cs     # Filter settings model, dictionaries, and serialization
└── LootGenerator.cs          # Utility drop generator helper
```
