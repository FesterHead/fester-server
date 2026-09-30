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
| **[`ApexHunt/`](ApexHunt/README.md)** | Imagine | PvM Competitions | • Automated server-wide hunting competition (`[ApexHunt`, `[ApexHuntToggle`, `[ApexHuntTop`)<br>• Dynamic triggering: 10% chance with 45–75 minute variance checks when players are online with 2-hour minimum cooldown<br>• 10m / 5m / 1m pre-hunt broadcast warnings<br>• 4-tier creature roster with random target selection and kill targets<br>• Real-time floating HUD widget and full leaderboard gump<br>• 60-second tie-breaker window upon first goal completion<br>• Automated gold bounties deposited directly to winner bank vaults with offline delivery queueing |
| **[`CustomPetGump/`](CustomPetGump/README.md)** | Feng / UO Wildlands Team | Pet Management | • Single-page consolidated Animal Lore dashboard<br>• Real-time pet attributes, ratings, resistances, and damage distributions<br>• Integrated pet training progression controls and live refresh<br>• Paired with patch `11-custom-pet-gump.patch` |
| **[`ExileHunterBestiary/`](ExileHunterBestiary/README.md)** | UO Exile Team / Imagine | Monster Progression | • 193-creature horizontal progression bestiary (`[Bestiary`)<br>• Collectible Monster Treatises granting permanent +20% damage mastery against specific creature types<br>• Full pet master damage inheritance<br>• Dynamic viewer-dependent tooltips (`[Already Learned]` vs `[Not Learned Yet]`)<br>• Blessed `HunterBestiary` grimoire and 8-category book gump with progress tracking<br>• 2% standard and 5% boss drop chances on creature death<br>• Core damage hook via `27-hunter-bestiary.patch`<br>• Standalone XML persistence (`Data/ExileHunterProfiles.xml`) |
| **[`FesterUO/`](FesterUO/README.md)** | Custom Shard | Core Systems & QoL | • Starter kit provisioning (full spellbook, 20-charge runebook, blank recall runes, auto-tools, cottage voucher, boat deed, ankh deed, moongate deed)<br>• Blessed `FesterUOGuideBook`<br>• Corpse tracking quest arrow (`[Corpse`)<br>• Automated harvesting tools (auto-smelt, auto-saw, auto-fillet, sheep shear, area scythe)<br>• 100% weightless `ResourceSatchel` with auto-routing and recall rune storage<br>• Re-deedable `HouseMoongateAddon`<br>• Global player broadcast chat (`[c <message>`) |
| **[`LegendaryMaster/`](LegendaryMaster/README.md)** | Keith | PowerScroll Quests | • Interactive NPC questmaster for solo adventurers (`[add LegendaryMaster`)<br>• Grandmaster (100.0+) combat challenges granting +5 PowerScrolls up to 120 cap<br>• High-level dungeon boss hunting with countdown extensions<br>• 1% chance for bonus Stat Cap Scrolls (+5 to +25)<br>• World persistence and configurable timers via `servuo/Config/LegendaryMaster/` |
| **[`LootFilter/`](LootFilter/README.md)** | Community | ARPG Loot Filtering | • Diablo / Path of Exile style loot visibility filter for equipment on corpses and ground (`[LootFilter`)<br>• 8 filter categories covering 87+ item properties (AOS attributes, resists, elemental damage, hit effects, absorption, negative attributes)<br>• Per-property enable toggles and minimum threshold inputs<br>• Global master toggle with safe filtering guarantees (never hides artifacts, gold, reagents, potions, or keys)<br>• Backpack and bank container safety guarantee<br>• Character persistence via `XmlAttachment`<br>• Paired with core engine patch `29-loot-filter.patch` |
| **[`MyStats/`](MyStats/README.md)** | Feng | Player Dashboard | • Comprehensive combat, magic, defense, and skill profile dashboard (`[Stats`)<br>• Staff server diagnostic metrics fallback (`[Stats server`)<br>• Paired with patch `10-bandage-delay.patch` |
| **[`PetExchange/`](PetExchange/README.md)** | 4737Carlin | Housing & Stables | • Player house hitching post addon deed (`[add PetExchangeAddonDeed`)<br>• Private cross-character and cross-account pet stabling<br>• Per-pet access level permissions (Owner, Co-Owner, Friend)<br>• Live bonding timer countdowns on item tooltips<br>• Capacity parameterized via `servuo/Config/PetExchange/PetExchange.cfg` |
| **[`RuneBookandSpellBookDyeTubs/`](RuneBookandSpellBookDyeTubs/README.md)** | Feng / UO Wildlands Team | Custom Dye Tubs | • 16-preset customizable dye tubs for runebooks and spellbooks (`[add RunebookCustomDyeTub`, `[add SpellbookCustomDyeTub`)<br>• Interactive visual palette gumps with dynamic multi-step gradient bars rendered from server hue tables (`Ultima.Hues`)<br>• Palette customization mode allowing re-coloring of individual palette slots with standard `HuePicker`<br>• One-click reset to default colors or removal of dyed hue<br>• GM context menu configuration<br>• Paired with core engine patch `28-dyes-target-handler.patch` |
| **[`TMap/`](TMap/README.md)** | 4737Carlin | Treasure Hunting | • High-capacity (500-slot) blessed storage tome for Treasure Maps and SOS messages<br>• Multi-page filtering, map withdrawal, and price setting<br>• Player vendor backpack selling support |
| **[`zerodowned/`](zerodowned/README.md)** | zerodowned | Seafaring & Exploration | • SOS Instant Transporter (`[add SOSDecoder`): teleports player vessel directly to targeted SOS coordinates<br>• Treasure Map Instant Transporter (`[add TreasureMapDecoder`): opens timed moongate directly to map chest coordinates<br>• Unlimited uses without charges |

---

## Developer Guidelines

1. **Avoid Duplicate Core Types**: Do not place copies of upstream core ServUO files into `custom-scripts/` unless you intend to introduce new distinct class names. Core modifications should be implemented as unified diffs in `servuo/patches/` to prevent `CS0101` / `CS0111` duplicate type compiler errors.
2. **Externalize Settings**: Hardcoded variables (capacities, cooldowns, plot coordinates) should be exposed via `servuo/Config/` and read via `Server.Config.Get()`.
3. **Directory Names**: Avoid creating subfolders named `bin` or `obj`, as the .NET SDK compiler treats those as reserved build artifact directories.
