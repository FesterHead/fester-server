# ServUO Custom Scripts Directory

This directory (`servuo/custom-scripts/`) contains custom gameplay scripts, systems, items, and UI gumps mounted into the ServUO container at `/server/Scripts/Custom/`.

---

## Architecture & Loading Mechanism

- **Automatic Recursive Compilation**: ServUO compiles scripts using an SDK-style project (`Scripts.csproj`). All C# (`*.cs`) source files in `/server/Scripts/Custom/` and all nested subdirectories are discovered and compiled dynamically upon server startup.
- **Namespace Decoupling**: In C#, file system paths do not dictate class namespaces. Scripts can use any standard ServUO namespace (`Server.Items`, `Server.Mobiles`, `Server.Gumps`, `Server.Custom`) regardless of folder location.
- **Hot Reloading / Restarts**: Custom script changes made on the host take effect upon restarting the container:
  ```bash
  docker compose restart servuo
  ```

---

## Directory Inventory

Each subdirectory contains its own dedicated `README.md` detailing full feature descriptions, commands, and upstream community attribution:

| Subdirectory | Origin / Author | Focus Area | Key Features & Commands |
| :--- | :--- | :--- | :--- |
| **[`FesterUO/`](FesterUO/README.md)** | Custom Shard | Core Systems & QoL | • Starter kit provisioning (full spellbook, 20-charge runebook, blank recall runes, auto-tools, cottage voucher, boat deed, ankh deed, moongate deed)<br>• Blessed `FesterUOGuideBook`<br>• Corpse tracking quest arrow (`[Corpse`)<br>• Automated harvesting tools (auto-smelt, auto-saw, auto-fillet, sheep shear, area scythe)<br>• 100% weightless `ResourceSatchel` with auto-routing and recall rune storage<br>• Re-deedable `HouseMoongateAddon`<br>• Global player broadcast chat (`[c <message>`) |
| **[`CustomPetGump/`](CustomPetGump/README.md)** | Feng / UO Wildlands Team | Pet Management | • Single-page consolidated Animal Lore dashboard<br>• Real-time pet attributes, ratings, resistances, and damage distributions<br>• Integrated pet training progression controls and live refresh<br>• Paired with patch `11-custom-pet-gump.patch` |
| **[`LegendaryMaster/`](LegendaryMaster/README.md)** | Keith | PowerScroll Quests | • Interactive NPC questmaster for solo adventurers (`[add LegendaryMaster`)<br>• Grandmaster (100.0+) combat challenges granting +5 PowerScrolls up to 120 cap<br>• High-level dungeon boss hunting with countdown extensions<br>• 1% chance for bonus Stat Cap Scrolls (+5 to +25)<br>• World persistence and configurable timers via `servuo/Config/LegendaryMaster/` |
| **[`MyStats/`](MyStats/README.md)** | Feng | Player Dashboard | • Comprehensive combat, magic, defense, and skill profile dashboard (`[Stats`)<br>• Staff server diagnostic metrics fallback (`[Stats server`)<br>• Paired with patch `10-bandage-delay.patch` |
| **[`PetExchange/`](PetExchange/README.md)** | 4737Carlin | Housing & Stables | • Player house hitching post addon deed (`[add PetExchangeAddonDeed`)<br>• Private cross-character and cross-account pet stabling<br>• Per-pet access level permissions (Owner, Co-Owner, Friend)<br>• Live bonding timer countdowns on item tooltips<br>• Capacity parameterized via `servuo/Config/PetExchange/PetExchange.cfg` |
| **[`TMap/`](TMap/README.md)** | 4737Carlin | Treasure Hunting | • High-capacity (500-slot) blessed storage tome for Treasure Maps and SOS messages<br>• Multi-page filtering, map withdrawal, and price setting<br>• Player vendor backpack selling support |

---

## Developer Guidelines

1. **Avoid Duplicate Core Types**: Do not place copies of upstream core ServUO files into `custom-scripts/` unless you intend to introduce new distinct class names. Core modifications should be implemented as unified diffs in `servuo/patches/` to prevent `CS0101` / `CS0111` duplicate type compiler errors.
2. **Externalize Settings**: Hardcoded variables (capacities, cooldowns, plot coordinates) should be exposed via `servuo/Config/` and read via `Server.Config.Get()`.
3. **Directory Names**: Avoid creating subfolders named `bin` or `obj`, as the .NET SDK compiler treats those as reserved build artifact directories.
