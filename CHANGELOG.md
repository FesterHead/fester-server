# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/), and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [1.3.0] - 2026-09-16

### Added
- Added `servuo/Saves.backup*/` pattern to [.gitignore](.gitignore) and [servuo/.dockerignore](servuo/.dockerignore) to ignore timestamped save backup directories.
- Added `servuo` containerized service for Ultima Online emulation targeting `pub57` with .NET 10 and Mono multi-stage build, health checks, interactive admin console (`stdin_open`, `tty`), and persistent volume mounts.
- Added `PORT_SERVUO` host port mapping to [.env](.env) and [.env-template](.env-template).
- Added `servuo` service to [update.sh](update.sh).
- Added `servuo` entry to [README.md](README.md) services table.
- Added `servuo/Client/`, `servuo/Logs/`, `servuo/Saves/`, and `servuo/upstream/` to [.gitignore](.gitignore).
- Added explicit ServUO configuration files in `servuo/Config/` (`Accounts.cfg`, `AutoRestart.cfg`, `AutoSave.cfg`, `Champions.cfg`, `DataPath.cfg`, `Expansion.cfg`, `General.cfg`, `Harvest.cfg`, `Housing.cfg`, `Loot.cfg`, `PlayerCaps.cfg`, `Server.cfg`, `Stables.cfg`, `TreasureMaps.cfg`, `Vendors.cfg`, `VetRewards.cfg`) parameterizing all shard progression, harvesting, stable, and vendor mechanics via native `Config.Get` calls.
- Added `servuo/manage-patches.sh` CLI utility with idempotent patch application, already-applied detection, upstream synchronization, and Dockerfile commit alignment.
- Added custom gathering and utility scripts in `servuo/custom-scripts/FesterUO/` (`ResourceSatchel.cs`, `CustomAutoTools.cs`) providing auto-smelting, auto-sawing, auto-filleting, corpse hide conversion, sheep shearing, area crop scything, and automatic resource routing with 100% weight reduction.
- Added `servuo/custom-scripts/FesterUO/CorpseFinder.cs` providing a `[Corpse` command for players that calculates distance, displays coordinates/facet, and directs a client quest arrow straight to their fallen body.
- Added `servuo/custom-scripts/FesterUO/FesterUOGuideBook.cs` providing a blessed reference tome describing the resource satchel, enchanted auto-tools, backpack fallback behavior, open housing across Britannia, starter cottage styles, home fixtures (Ankh of Sacrifice and House Moongate), axe re-deeding instructions, runebook & blank runes, stable stalls, equipment preservation, and player commands (`[Stats`, `[Corpse`, and `[c <message>`), distributed exclusively to the first character per account and automatically opened upon creation.
- Added `servuo/custom-scripts/FesterUO/GlobalChat.cs` providing `[c <message>` and `[chat <message>` server-wide broadcast commands in cyan text for private duo communication.
- Added `servuo/custom-scripts/FesterUO/HouseMoongateAddon.cs` providing a re-deedable house-placed moongate addon (`FestersMoongateAddon`, `FestersMoongateAddonDeed`) with full facet moongate navigation (`MoongateGump`) and axe re-deeding mechanics.
- Added `servuo/custom-scripts/FesterUO/StarterKitDistribution.cs` provisioning every character with a Blessed Full Spellbook (all 64 spells), a Blessed Runebook (20 charges), 16 blank recall runes stored inside the resource satchel, indestructible auto-tools, personal resource satchel, and account-level one-time cottage selection voucher, small boat deed, Ankh of Sacrifice deed, and House Moongate deed.
- Added `servuo/patches/07-escort-destinations.patch` limiting NPC escort quest destinations exclusively to mainland Britannia towns: Britain, Minoc, Trinsic, Vesper, and Skara Brae.
- Added `servuo/patches/08-stable-slots.patch` establishing a baseline of 12 stable stalls for all characters at Animal Trainers configured via `Stables.cfg`.
- Added `servuo/patches/09-powder-vendor.patch` adding Powder of Fortifying (`PowderOfTemperament`) to Blacksmith and Tinker NPC vendor inventories with cost and charges parameterized in `Vendors.cfg`.
- Added `servuo/patches/10-bandage-delay.patch` accelerating self-healing bandage speed scaling with dexterity and lowering the minimum delay floor from 4.0s to 2.0s.
- Added `servuo/patches/11-custom-pet-gump.patch` cleanly hooking Animal Lore skill targeting (`AnimalLore.cs`), pet training option sub-gumps (`Gumps.cs`), and staff gate helpers (`PetTrainingGate.cs`) to dynamically open and refresh `DashboardAnimalLoreGump` while preserving fallback to standard gumps.
- Added `servuo/patches/12-config-subdirectories.patch` enabling `Server.Config` to register filename-based short scope aliases for configuration files residing in nested subdirectories.
- Added `servuo/patches/13-legendary-pets.patch` implementing the wild Legendary Pet mutation system across 8 mobile species (`FireSteed`, `Kirin`, `Nightmare`, `OsseinRam`, `Phoenix`, `PolarBear`, `ShadowWyrm`, `TsukiWolf`) with gold overhead titles (`<BASEFONT COLOR=#FFD700>Legendary</BASEFONT>`), rare hues, elevated stats, rideable Polar Bear mount conversion (`BaseMount`), specialized training trees, and versioned serialization.
- Added `servuo/patches/15-pet-bonding-delay.patch` parameterizing pet bonding delay via `servuo/Config/FesterUO/Stables.cfg` (`BondingDelayHours=24.0`, lowering retail 7-day delay to 24 hours).
- Added `servuo/patches/16-house-decay.patch` disabling unrefreshed house and boat decay when `Housing.DecayEnabled=False` in `servuo/Config/ServUO/Housing.cfg`.
- Added `servuo/patches/17-arcane-circle-focus.patch` enabling solo Arcane Circle casting (`ArcaneCircleMinWeavers=1`) and granting full Level 5 Focus (Level 6 in Sanctuary) when 2 players cast together (`ArcaneCircleDuoMaxFocus=True`) via `servuo/Config/ServUO/Expansion.cfg`.
- Added `servuo/patches/18-bod-delay.patch` parameterizing Bulk Order Deed request delay via `servuo/Config/ServUO/Vendors.cfg` (`BODDelayHours=1`, lowering retail 6-hour delay to 1 hour).
- Added `servuo/patches/19-peerless-key-lifespan.patch` scaling Peerless and dungeon boss key lifespans by multiplier in `servuo/Config/ServUO/General.cfg` (`PeerlessKeyLifespanScale=3.0`).
- Added `servuo/Config/LegendaryPets/` containing individual configuration files (`FireSteed.cfg`, `Kirin.cfg`, `Nightmare.cfg`, `OsseinRam.cfg`, `Phoenix.cfg`, `PolarBear.cfg`, `ShadowWyrm.cfg`, `TsukiWolf.cfg`) and documentation parameterizing wild spawn rates for each legendary species.
- Added `servuo/custom-scripts/LegendaryMaster/` (`LegendaryMaster.cs`, authored by Keith, https://www.servuo.dev/archive/legendary-master-of-skills.2244/) providing an NPC questmaster for solo adventurers to earn PowerScrolls (+5 up to 120 cap) by hunting high-level dungeon bosses, with bug fixes for direct player kills, safe entity lookups, human-readable timers, and world save persistence.
- Added `servuo/Config/LegendaryMaster/` (`LegendaryMaster.cfg` and `README.md`) parameterizing task time limits, kill extensions, kill scaling per tier, skill requirements and caps, scroll increment, bonus stat scrolls and caps, NPC interaction range, idle bark chance, and target creature pool.
- Reorganized `servuo/Config/` into dedicated functional subdirectories (`servuo/Config/ServUO/` for base engine configs, `servuo/Config/FesterUO/` for custom shard progression configs, and `servuo/Config/PetExchange/` for hitching post settings) with individual `README.md` documentation in each folder and an overarching directory index.
- Added custom scripts in `servuo/custom-scripts/TMap/` (`TMapBook.cs`, `TMapGumps.cs`, authored by 4737Carlin from ServUO community, https://www.servuo.dev/archive/treasure-map-and-sos-storage-book.2546/) providing a securable, blessed storage book for up to 500 Treasure Maps and SOS messages with filtering, multi-page browsing, and vendor support.
- Added custom script in `servuo/custom-scripts/MyStats/` (`MyStats.cs`, authored by Feng from ServUO community, https://www.servuo.dev/archive/mystats-modernized.2594/) providing a comprehensive player statistics and combat rating dashboard command (`[Stats`) with staff diagnostics fallback (`[Stats server`).
- Added custom script in `servuo/custom-scripts/CustomPetGump/` (`DashboardAnimalLoreGump.cs`, authored by Feng / UO Wildlands Team, https://www.servuo.dev/archive/dashboard-animal-lore-gump-modern-pet-gump.2598/) providing a consolidated, single-page pet information dashboard displaying attributes, resistances, damage profiles, abilities, and integrated pet training controls.
- Added custom script in `servuo/custom-scripts/PetExchange/` (`PetExchange.cs`, authored by 4737Carlin, https://www.servuo.dev/archive/pet-exchange-hitching-post-house-deed.2643/) providing a player house hitching post addon for private, cross-character and cross-account pet stabling with bonding progress tracking and access controls.
- Added `servuo/Config/PetExchange/PetExchange.cfg` parameterizing hitching post pet stabling capacity (`MaxStabled=12`).
- Added README documentation across all custom script subdirectories (`servuo/custom-scripts/FesterUO/README.md`, `servuo/custom-scripts/MyStats/README.md`, `servuo/custom-scripts/TMap/README.md`, `servuo/custom-scripts/CustomPetGump/README.md`, `servuo/custom-scripts/PetExchange/README.md`) detailing features, commands, and upstream resource links.
- Added [servuo/README.md](servuo/README.md) documenting container architecture, committed vs. uncommitted files, pinned commit rationale, volume mappings, client file installation from official UO classic client, custom client connection links (ClassicUO and TazUO), build and execution commands, private duo shard use case, custom QoL features, clean startup sequence, and reset procedures.
- Added `servuo/patches/20-championspawns-data-path.patch` relocating stock `ChampionSpawns.xml` to `Data/` during image build and adding fallback logic in `ChampionSystem.LoadSpawns` to check `Config/` (for host overrides) before falling back to `Data/`, avoiding file duplication on the host.
- Added `servuo/patches/20-championspawns-data-path.patch` entry to [servuo/patches/README.md](servuo/patches/README.md) patch inventory table.

### Changed
- Configured `seerr` with `user: "${PUID}:${PGID}"` and `crosswatch` with `APP_UID` and `APP_GID` in `docker-compose.yaml` to run containers with standard host file ownership.
- Migrated all in-game parameters (harvest yields, tool delays, account permissions, stable stalls, vendor prices) from `.env` and hardcoded code values into dedicated host-mounted `servuo/Config/*.cfg` files, removing all `SERVUO_HARVEST_*` variables from `docker-compose.yaml`, [.env](.env), and [.env-template](.env-template).
- Disabled `AutoCreateAccounts` in `servuo/Config/Accounts.cfg` to restrict shard access exclusively to pre-created accounts.
- Configured anti-macro enforcement and balanced 5-minute stat gain cooldowns (`PlayerStatTimeDelay` and `PetStatTimeDelay`) in `servuo/Config/PlayerCaps.cfg` to foster sustained, progressive character development.
- Refactored `servuo/Dockerfile` build overrides from inline `sed` string substitutions to modular unified patch files in `servuo/patches/` applied with `git apply`.
- Pinned `servuo/Dockerfile` build to explicit `SERVUO_COMMIT` hash (`d76bf44...`) for reproducible, deterministic builds.
- Updated `servuo/custom-scripts/FesterUO/ResourceSatchel.cs` to accept `RecallRune` items, enabling players to store both blank and marked recall runes inside the weightless satchel.

### Fixed
- Configured `zlib1g-dev`, library symlinks, and Mono DLL mapping in `servuo/Dockerfile` to resolve `System.DllNotFoundException: libz` when transmitting compressed gumps (e.g. `[admin`).
- Fixed `FestersResourceSatchel` weight calculation in `servuo/custom-scripts/FesterUO/ResourceSatchel.cs` so that the satchel itself has 0 stone weight (`DefaultWeight = 0.0`, `Weight = 0.0`), all items contained within have their weight neutralized to `0.0` while stored (restored to natural weight upon withdrawal), upward weight changes are not propagated to parent containers, and `CheckHold` bypasses parent backpack weight checks so large resource volumes can be dropped or auto-deposited without encumbering players or failing capacity checks.
- Patched `ServerList.cs` in `servuo/Dockerfile` to prevent redirecting private LAN clients to internal Docker container bridge IP addresses.
- Patched `AutoSave.cs` in `servuo/Dockerfile` to handle directory move errors gracefully when `Saves/` is a Docker bind mount point.
- Fixed starter kit distribution in `servuo/custom-scripts/FesterUO/StarterKitDistribution.cs` by assigning `[CallPriority(100)]` to ensure character creation delegates execute after core mobile/backpack initialization, adding a login safety net (`EventSink.Login`) to retroactively provision existing characters, and registering a `[ClaimStarterKit` player command.
- Added `servuo/patches/14-scripts-output-path.patch` setting `<OutputPath>..\</OutputPath>` unconditionally in `Scripts.csproj` and passing `-p:Platform=x64` to `dotnet build` in `ScriptCompiler.cs` so custom script changes overwrite the active root `/server/Scripts.dll`.
- Lifted 7-day character deletion restriction by setting `RestrictDeletion=False` and `DeleteDelay=00:00:00` in `servuo/Config/ServUO/Accounts.cfg` (removing `@` override to allow immediate character deletion).
- Fixed fish detection and auto-filleting in `servuo/custom-scripts/FesterUO/CustomAutoTools.cs` by handling standard `Server.Items.Fish` (as well as `BaseFish` and `BigFish`), auto-converting whole fish into `RawFishSteak` deposited directly into `FestersResourceSatchel`. Added backlog double-click processing across all enchanted harvesting tools.
- Expanded `FestersResourceSatchel` resource acceptance in `servuo/custom-scripts/FesterUO/ResourceSatchel.cs` to dynamically support all `ICommodity` items, all standard mining gems (`IGem`), currency (`Gold`, `BankCheck`), healing supplies (`Bandage`), all raw meats (`CookableFood`: `RawBird`, `RawRibs`, `RawLambLeg`, `RawChickenLeg`, `RawRotwormMeat`, `RawFishSteak`), all cooked meats (`CookedBird`, `Ribs`, `LambLeg`, `ChickenLeg`, `Bacon`, `Ham`, `Sausage`, `RoastPig`), colored wood logs (`BaseLog`), raw uncut hides (`BaseHides`), dragon scales (`BaseScales`), tailoring fibers and yarns (`Cotton`, `Flax`, `BaseClothMaterial`), monster harvestables (`Bone`, `Feather`), sand and quarry products, gardening/botany supplies (`FertileDirt`, `Seed`, `PlantClippings`), and foraged `Kindling`.
- Fixed weight reduction in `servuo/custom-scripts/FesterUO/ResourceSatchel.cs` by overriding `UpdateTotal(Item, TotalType, int)` to prevent live incremental weight deltas from propagating to parent containers, corrected `GetTotal(TotalType.Weight)` calculation, and set `DefaultWeight = 1.0` so contents are 100% weightless.
- Upgraded the Tanner's Skinning Knife (`FestersSkinningKnife`) in `servuo/custom-scripts/FesterUO/CustomAutoTools.cs` to automatically carve and deposit all meats, feathers, dragon scales, and wool directly into `FestersResourceSatchel` alongside cut leather.
- Fixed `System.IO.FileNotFoundException` when invoking `[CreateWorld` or `[GenChampSpawns` caused by host volume mount shadowing `/server/Config/ChampionSpawns.xml` by introducing `servuo/patches/20-championspawns-data-path.patch` to fall back to `/server/Data/ChampionSpawns.xml`.
- Fixed `cmd_reset()` in [servuo/manage-patches.sh](servuo/manage-patches.sh) to execute `git reset --hard HEAD` and `git clean -fd` to cleanly discard both staged and unstaged local modifications in upstream.

## [1.2.0] - 2026-09-13

### Added
- Added Git workflow and commit rules to [AGENTS.md](AGENTS.md) and [.github/copilot-instructions.md](.github/copilot-instructions.md) specifying user-managed commits and no automated git commands.
- Added environment variable configurations to [.env](.env) and [.env-template](.env-template) for DNS resolvers (`DNS_PRIMARY`, `DNS_SECONDARY`), Docker daemon socket (`DOCKER_SOCK`), host home directory (`HOST_HOME_DIR`), Emby hardware transcoding GIDs (`EMBY_GIDLIST`), Transmission limits, and host port mappings (`PORT_<SERVICE>`).

### Changed
- Replaced hardcoded host path in `dockhand` volume mapping with `${SERVICE_DIR}:${SERVICE_DIR}` in `docker-compose.yaml`.
- Parameterized DNS settings, Docker daemon socket mounts, Emby hardware transcoding GIDs, Homepage `/home` mount, Transmission limits, and host port mappings in `docker-compose.yaml` to use `.env` variables.
- Formatted `dockerproxy` ports, volumes, and environment variables in `docker-compose.yaml` to adhere to double-quoted string conventions.
- Updated [README.md](README.md) environment variables table and anonymized example paths to generic `/home/user/...` paths.
- Synchronized [AGENTS.md](AGENTS.md) and [.github/copilot-instructions.md](.github/copilot-instructions.md) with updated DNS, volume, and port configuration conventions.

## [1.1.0] - 2026-08-11

### Added
- Added `crosswatch` service to `docker-compose.yaml`.
- Added `crosswatch` to `update.sh`.
- Added `crosswatch` entry to [README.md](README.md) services table.
- Added [AGENTS.md](AGENTS.md) with repository guidelines for AI coding assistants.
- Added [CONTRIBUTING.md](CONTRIBUTING.md) with contribution instructions and standards.
- Added [.github/copilot-instructions.md](.github/copilot-instructions.md) synchronized with [AGENTS.md](AGENTS.md).
- Added GitHub issue templates (`bug_report.yml`, `config.yml`, `feature_request.yml`) and pull request template (`PULL_REQUEST_TEMPLATE.yml`).

## [1.0.0] - 2026-08-11

- Initial commit.
