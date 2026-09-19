# ServUO Core Engine Patches

This directory (`servuo/patches/`) contains modular, unified patch files (`*.patch`) applied during the Docker image build stage (`servuo/Dockerfile`) to customize the upstream ServUO codebase in a clean, reproducible manner.

---

## Build Architecture

During the container build stage:
1. Git fetches the exact upstream commit pinned by `ARG SERVUO_COMMIT` in `servuo/Dockerfile`.
2. All unified patches in this directory are applied sequentially:
   ```dockerfile
   git apply --verbose /tmp/patches/*.patch
   ```
3. The engine is compiled into the release binaries (`Ultima.dll`, `ServUO.exe`, `Scripts.dll`).

---

## Patch Management CLI (`manage-patches.sh`)

A helper script is provided in `servuo/manage-patches.sh` to maintain and test patches against the local `servuo/upstream/` mirror:

```bash
# Verify that all patches apply cleanly against upstream
./servuo/manage-patches.sh check

# Check upstream commit vs Dockerfile pinned commit
./servuo/manage-patches.sh status

# Export current git diff in upstream to a new patch file
./servuo/manage-patches.sh export <patch-name>

# Apply all patches locally to upstream for development/IDE inspection
./servuo/manage-patches.sh apply

# Revert upstream back to clean stock commit
./servuo/manage-patches.sh reset
```

---

## Patch Inventory

| Patch | Target Upstream File(s) | Focus Area | Description & Consuming Configuration |
| :--- | :--- | :--- | :--- |
| [`01-serverlist-address.patch`](01-serverlist-address.patch) | `Scripts/Misc/ServerList.cs` | Networking | Prevents LAN clients from being redirected to internal Docker container bridge IP addresses (`172.18.x.x`). |
| [`02-autosave-trycatch.patch`](02-autosave-trycatch.patch) | `Scripts/Misc/AutoSave.cs` | Storage / I/O | Catches directory move exceptions during world saves when `Saves/` is an active Docker host bind mount. |
| [`03-harvest-amount.patch`](03-harvest-amount.patch) | `Scripts/Services/Harvest/Mining.cs`<br>`Scripts/Services/Harvest/Lumberjacking.cs` | Gathering | Parameterizes mining and lumberjacking harvest resource yields via `servuo/Config/FesterUO/Harvest.cfg` (`MinYield=3`, `MaxYield=6`). |
| [`04-fishing-delay.patch`](04-fishing-delay.patch) | `Scripts/Services/Harvest/Fishing.cs` | Gathering | Parameterizes fishing action delay floor via `servuo/Config/FesterUO/Harvest.cfg` (`FishingDelay=2.0s`). |
| [`05-mining-delay.patch`](05-mining-delay.patch) | `Scripts/Services/Harvest/Mining.cs` | Gathering | Parameterizes mining tool action delay floor via `servuo/Config/FesterUO/Harvest.cfg` (`MiningDelay=1.0s`). |
| [`06-lumberjacking-delay.patch`](06-lumberjacking-delay.patch) | `Scripts/Services/Harvest/Lumberjacking.cs` | Gathering | Parameterizes lumberjacking tool action delay floor via `servuo/Config/FesterUO/Harvest.cfg` (`LumberjackingDelay=1.0s`). |
| [`07-escort-destinations.patch`](07-escort-destinations.patch) | `Scripts/Mobiles/NPCs/BaseEscortable.cs` | Quests | Limits NPC escort quest destinations to mainland Britannia towns: Britain, Minoc, Vesper, and Skara Brae. |
| [`08-stable-slots.patch`](08-stable-slots.patch) | `Scripts/Mobiles/NPCs/AnimalTrainer.cs` | Stables | Establishes a baseline of 12 stable stalls for all characters at town Animal Trainers configured via `servuo/Config/FesterUO/Stables.cfg`. |
| [`09-powder-vendor.patch`](09-powder-vendor.patch) | `Scripts/VendorInfo/SBBlacksmith.cs`<br>`Scripts/VendorInfo/SBTinker.cs` | Economy / Equipment | Adds Powder of Fortifying (`PowderOfTemperament`) to NPC Blacksmith and Tinker inventories with cost and charges parameterized in `servuo/Config/ServUO/Vendors.cfg`. |
| [`10-bandage-delay.patch`](10-bandage-delay.patch) | `Scripts/Items/Resource/Bandage.cs` | Combat / Healing | Scales self-healing bandage speed with dexterity (`dex / 10`) and lowers the minimum delay floor from 4.0s to 2.0s. |
| [`11-custom-pet-gump.patch`](11-custom-pet-gump.patch) | `Scripts/Skills/AnimalLore.cs`<br>`Scripts/Services/Pet Training/Gumps.cs`<br>`Scripts/Services/Pet Training/PetTrainingGate.cs`<br>`Scripts/Services/Pet Training/PetTrainingHelper.cs` | Pet Management | Adds dynamic resolution hooks to route Animal Lore and pet training sub-gumps to the modern `DashboardAnimalLoreGump` with safe fallback to standard gumps. |
| [`12-config-subdirectories.patch`](12-config-subdirectories.patch) | `Server/Config.cs` | Configuration Engine | Enhances `Server.Config.LoadFile` to register short scope aliases matching filenames, allowing `.cfg` files to be organized into arbitrary subdirectories without breaking code lookups. |
| [`13-legendary-pets.patch`](13-legendary-pets.patch) | `Scripts/Mobiles/Normal/FireSteed.cs`<br>`Scripts/Mobiles/Normal/Kirin.cs`<br>`Scripts/Mobiles/Normal/Nightmare.cs`<br>`Scripts/Mobiles/Normal/OsseinRam.cs`<br>`Scripts/Mobiles/Normal/Phoenix.cs`<br>`Scripts/Mobiles/Normal/PolarBear.cs`<br>`Scripts/Mobiles/Normal/ShadowWyrm.cs`<br>`Scripts/Mobiles/Normal/TsukiWolf.cs` | Pet Progression | Implements the wild Legendary Pet mutation system across 8 species with unique hues, abilities, specialized training trees, rideable Polar Bear mount conversion, and spawn rates parameterized in `servuo/Config/LegendaryPets/`. |
| [`14-scripts-output-path.patch`](14-scripts-output-path.patch) | `Scripts/Scripts.csproj`<br>`Server/ScriptCompiler.cs` | Build System / Dynamic Compilation | Ensures `Scripts.csproj` always outputs directly to `/server/Scripts.dll` regardless of build architecture, and passes `-p:Platform=x64` during dynamic script compilation so custom script changes overwrite the active root assembly. |
| [`15-pet-bonding-delay.patch`](15-pet-bonding-delay.patch) | `Scripts/Mobiles/Normal/BaseCreature.cs` | Pet Progression | Parameterizes pet bonding delay via `servuo/Config/FesterUO/Stables.cfg` (`BondingDelayHours=24.0`, lowering retail 7-day delay to 24 hours). |
| [`16-house-decay.patch`](16-house-decay.patch) | `Scripts/Multis/BaseHouse.cs`<br>`Scripts/Multis/Boats/BaseBoat.cs` | Housing & Boats | Disables unrefreshed decay for player houses and boats when `Housing.DecayEnabled=False` in `servuo/Config/ServUO/Housing.cfg`. |
| [`17-arcane-circle-focus.patch`](17-arcane-circle-focus.patch) | `Scripts/Spells/Spellweaving/ArcaneCircle.cs` | Spellweaving | Enables solo casting (`ArcaneCircleMinWeavers=1`) and grants full Level 5 Focus (Level 6 in Sanctuary) when 2 players cast together (`ArcaneCircleDuoMaxFocus=True`) via `servuo/Config/ServUO/Expansion.cfg`. |
| [`18-bod-delay.patch`](18-bod-delay.patch) | `Scripts/Services/BulkOrders/BulkOrderSystem.cs` | Crafting / Bulk Orders | Parameterizes Bulk Order Deed refresh delay via `servuo/Config/ServUO/Vendors.cfg` (`BODDelayHours=1`, lowering retail 6-hour delay to 1 hour). |
| [`19-peerless-key-lifespan.patch`](19-peerless-key-lifespan.patch) | `Scripts/Services/Peerless/PeerlessKey.cs` | Dungeons / Bosses | Scales Peerless and dungeon boss key lifespans by multiplier in `servuo/Config/ServUO/General.cfg` (`PeerlessKeyLifespanScale=3.0`). |
| [`20-championspawns-data-path.patch`](20-championspawns-data-path.patch) | `Config/ChampionSpawns.xml`<br>`Scripts/Services/ChampionSystem/ChampionSystem.cs` | Spawns / Champions | Relocates upstream `ChampionSpawns.xml` to `Data/` during image build and updates `ChampionSystem.LoadSpawns` to check `Config/` (for custom overrides) with fallback to `Data/`, eliminating the need to duplicate the stock XML file on the host. |
| [`21-young-player-duration.patch`](21-young-player-duration.patch) | `Scripts/Accounting/Account.cs` | Account & Player Progression | Parameterizes Young player status duration via `servuo/Config/ServUO/Accounts.cfg` (`YoungPlayerDuration=80`, doubling default 40 hours to 80 hours). |
| [`22-tithing-mechanics.patch`](22-tithing-mechanics.patch) | `Scripts/Gumps/TithingGump.cs`<br>`Scripts/Items/Functional/Ankhs.cs`<br>`Scripts/Mobiles/NPCs/ShrineHealer.cs` | Shrines & Tithing | Unifies titheable gold detection across bank ledger and player backpack/subcontainers, fixes client text desync/zero submissions for ClassicUO/TazUO, and implements safe multi-source gold deduction. |
| [`23-vendor-reagent-stock.patch`](23-vendor-reagent-stock.patch) | `Scripts/Mobiles/NPCs/BaseVendor.cs`<br>`Scripts/VendorInfo/GenericBuy.cs` | Economy & Vendors | Classifies all reagent types (`BaseReagent`, `Bone`, `FertileDirt`) as stackable trade commodities and parameterizes vendor reagent inventory stock via `servuo/Config/ServUO/Vendors.cfg` (`ReagentStockAmount=1000`). |

---

## Workflow: Creating a New Patch

1. Ensure the upstream directory is clean:
   ```bash
   ./servuo/manage-patches.sh reset
   ```
2. Modify the desired file(s) in `servuo/upstream/`.
3. Export the unified diff to `servuo/patches/`:
   ```bash
   ./servuo/manage-patches.sh export <number>-<feature-name>
   ```
4. Reset upstream back to stock:
   ```bash
   ./servuo/manage-patches.sh reset
   ```
5. Test that all patches apply cleanly:
   ```bash
   ./servuo/manage-patches.sh check
   ```
6. Rebuild the Docker image:
   ```bash
   docker compose build servuo
   ```
