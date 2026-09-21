# Dashboard Animal Lore Gump (Modern Pet Gump)

This directory (`servuo/custom-scripts/CustomPetGump/`) contains the consolidated, modern Animal Lore dashboard system for ServUO.

---

## Provenance & Attribution

- **Community Script**: Dashboard Animal Lore Gump (Modern Pet Gump)
- **Author**: Feng / UO Wildlands Team (September 2026, version 1.6)
- **Source**: [https://www.servuo.dev/archive/dashboard-animal-lore-gump-modern-pet-gump.2598/](https://www.servuo.dev/archive/dashboard-animal-lore-gump-modern-pet-gump.2598/)

> [!NOTE]
> The original release archive from ServUO also included full modified copies of core engine files (`AnimalLore.cs` and `Gumps.cs` [named `NewAnimalLoreGump.cs`]). To prevent duplicate class conflicts (`CS0101`) and avoid introducing non-standard external dependencies (e.g. `ShrinkSystem`), those integration hooks are cleanly applied to core ServUO via [`servuo/patches/11-custom-pet-gump.patch`](../../patches/11-custom-pet-gump.patch).
>
> An optional `PetTrainingGate.cs` file from the original archive was intentionally omitted because standard pet training progression is used and no test gate items are required on this shard.

---

## Overview & Features

Replaces the traditional multi-page Animal Lore gump with an all-in-one consolidated dashboard:

- **Attributes & Ratings**: Hit Points, Stamina, Mana, Strength, Dexterity, Intelligence, Bashing, and dynamic regeneration rates.
- **Resistances & Damage Breakdown**: Visual breakdown of Physical, Fire, Cold, Poison, and Energy resists and attack damage distributions.
- **Combat & Magic Skills**: Real and cap values for Wrestling, Tactics, Resist, Anatomy, Healing/Poisoning, Magery, EvalInt, Meditation, etc.
- **Innate & Weapon Abilities**: Direct listing of active and innate creature abilities with tooltips.
- **Integrated Pet Training Controls**: Live pet training progress tracking and buttons to open pet training option sub-gumps directly from the dashboard.

---

## Included Files

- [`DashboardAnimalLoreGump.cs`](DashboardAnimalLoreGump.cs): Consolidated pet statistics layout, page controls, refresh logic, and training action handlers.
