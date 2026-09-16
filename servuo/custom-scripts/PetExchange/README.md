# Pet Exchange Hitching Post House Deed

This directory (`servuo/custom-scripts/PetExchange/`) contains the Pet Exchange Hitching Post house addon system for ServUO.

---

## Provenance & Attribution

- **Community Script**: Pet Exchange Hitching Post House Deed
- **Author**: 4737Carlin (June 8, 2026)
- **UI Gump Architecture**: Massapequa (`StableMaster.cs`)
- **Source**: [https://www.servuo.dev/archive/pet-exchange-hitching-post-house-deed.2643/](https://www.servuo.dev/archive/pet-exchange-hitching-post-house-deed.2643/)

---

## Overview & Features

`PetExchange.cs` provides a player house addon that enables private pet stabling and cross-character pet transfers:

- **Independent Stabling**: Pets stabled at the hitching post are saved directly to the addon entity and do not consume global NPC stable slots.
- **Cross-Character & Account Transfer**: Characters on the same account (or authorized house co-owners and friends) can retrieve pets placed at the post.
- **Access Level Controls**: Players choose the minimum required house access level (`Owner Only`, `Co-Owners`, or `Friends`) when stabling each pet.
- **Bonding Progress Tracking**: Automatically tracks pet bonding timers and displays real-time countdowns (`Bonding: DD:HH:MM:SS`) on object properties and the claim interface.
- **Safe Addon Removal**: If the house or hitching post is demolished while holding stabled pets, all stored pets are automatically placed safely into the world at the post's coordinates.

---

## Configuration

Capacity is dynamically parameterized via [`servuo/Config/PetExchange/PetExchange.cfg`](../../Config/PetExchange/PetExchange.cfg):

```cfg
# Maximum number of pets that can be stabled in a single Pet Exchange Hitching Post addon.
# Default: 10
MaxStabled=12
```

---

## Included Files

- [`PetExchange.cs`](PetExchange.cs): Complete house deed, addon item, context menus, serialization, and retrieval gumps.
