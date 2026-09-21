# MyStats (Modernized)

This directory (`servuo/custom-scripts/MyStats/`) contains the player statistics and combat rating dashboard system for ServUO.

---

## Provenance & Attribution

- **Community Script**: MyStats (Modernized)
- **Author**: Feng (February 2026, version 2.0.0)
- **Source**: [https://www.servuo.dev/archive/mystats-modernized.2594/](https://www.servuo.dev/archive/mystats-modernized.2594/)

> [!NOTE]
> The original upstream release archive from ServUO also included a customized `Bandage.cs` that scales self-healing delay with dexterity down to a 2.0-second minimum delay floor. To avoid duplicate class conflicts with the core server engine, that enhancement was extracted and is cleanly applied during server builds via [`servuo/patches/10-bandage-delay.patch`](../../patches/10-bandage-delay.patch).

---

## Overview & Features

`MyStats.cs` provides players with a clean, centralized interface (gump) to inspect their character's full combat, magic, defensive, and skill profile:

- **Attributes & Pools**: Hit Points, Mana, Stamina, and regen rates.
- **Combat & Magic Ratings**: Hit Chance Increase (HCI), Defense Chance Increase (DCI), Damage Increase (DI), Spell Damage Increase (SDI), Faster Casting (FC), and Faster Cast Recovery (FCR).
- **Resistances**: Physical, Fire, Cold, Poison, and Energy resistances compared against current caps.
- **Skill Overview**: Displays active real and modified skill levels.

---

## Commands

| Command | Access Level | Description |
| :--- | :--- | :--- |
| `[Stats` | Player | Opens the character statistics and combat rating dashboard. |
| `[Stats server` | Counselor+ (Staff) | Diagnostic fallback printing active connections, mobile counts, and item counts to the console/chat. |

---

## Included Files

- [`MyStats.cs`](MyStats.cs): Command registration, gump layout, calculation logic, and staff diagnostic handler.
