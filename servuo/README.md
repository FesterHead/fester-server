# ServUO Server Container

This directory (`servuo/`) contains the complete Dockerized deployment configuration, core engine patches, runtime settings, custom gameplay scripts, and operational tooling for running a private **[ServUO](https://github.com/ServUO/ServUO)** (Publish 57) Ultima Online server within this infrastructure stack.

---

## 🤖 AI-Assisted Development

This project is developed and managed using Google AI models. The architecture, implementation, and repository maintenance are guided by specialized AI agents to ensure engineering standards.

---

## 📋 Folder Purpose & Architecture

ServUO is an open-source, community-driven C# Ultima Online server emulator targeting modern expansions and client features. Running ServUO inside a Docker container allows the server to run reliably in a containerized Linux environment without polluting the host operating system with .NET SDK versions, Mono libraries, or system-level dependencies.

### Directory Layout

```
servuo/
├── Dockerfile             # Multi-stage container build (.NET 10 SDK & Mono runtime)
├── manage-patches.sh      # CLI tool for checking, exporting, and updating patches
├── patches/               # Modular unified diff patches applied during Docker build
├── Config/                # Runtime configuration files (.cfg) mounted into /server/Config
├── custom-scripts/        # Custom C# gameplay scripts mounted into /server/Scripts/Custom
├── Client/                # [Ignored] UO Classic client files required for map/tile data
├── Saves/                 # [Ignored] World state persistence (accounts, items, mobiles)
├── Logs/                  # [Ignored] Console and runtime log files
├── upstream/              # [Ignored] Local shallow git clone used for patch generation
├── .dockerignore          # Excludes local data from Docker build context
└── README.md              # This documentation file
```

---

## 👥 Shard Use Case & Quality of Life

### Private Duo Shard Model
This server configuration is purposefully tailored for a **private, two-person cooperative server**.

Because this is a dedicated private environment, public self-registration is disabled (`AutoCreateAccounts=False` in [`Config/ServUO/Accounts.cfg`](Config/ServUO/Accounts.cfg)). Restricting auto-account creation prevents unauthorized network access, avoids cluttering the saves database with accidental login attempts, and ensures account provisioning remains explicitly managed by the server administrator.

### Quality of Life via `custom-scripts/FesterUO`
To remove tedious classic MMO friction for a small two-player team without diminishing combat challenge or world progression, the custom systems in [`custom-scripts/FesterUO/`](custom-scripts/FesterUO/README.md) introduce essential quality-of-life enhancements:
* **Comprehensive Starter Kit**: Equips every new character with a Blessed Full Spellbook (all 64 spells), a Blessed Runebook (20 charges), 16 blank recall runes, durable auto-tools, and a personal resource satchel. Each account also receives a one-time starter package including a cottage voucher, small boat deed, Ankh of Sacrifice deed, and House Moongate deed to quickly establish a shared base.
* **100% Weight-Free Resource Satchel**: A blessed container that intercepts gathered materials (ore, ingots, logs, boards, leather, scales, fish) and eliminates weight penalties, enabling extended cooperative mining and crafting sessions without constant inventory micromanagement.
* **Automated Gathering Tools**: Specialized tools automatically smelt raw ore to ingots, saw logs to boards, fillet catches, shear sheep, and clear area crops upon harvesting.
* **Corpse Locator (`[Corpse`)**: Directs an in-game quest navigation arrow pointing directly to the player's fallen corpse with exact coordinates, distance, and facet information to minimize recovery frustration.
* **Global Duo Broadcast Chat (`[c <message>`)**: Provides simple, instantaneous cross-facet server-wide chat formatted in cyan text for seamless communication between the two players.
* **Home Travel Fixtures**: Re-deedable House Moongate and Ankh addons provide instant facet teleportation and home resurrection convenience.
* **In-Game Guidebook**: Distributes the blessed `FesterUOGuideBook` explaining shard systems, commands, and mechanics in-game.

### Subdirectory Provenance & Documentation
Every major subcomponent in this folder maintains its own dedicated `README.md` documenting its upstream origins, community author attribution, and configuration schema:
* **Custom Scripts**: [`custom-scripts/README.md`](custom-scripts/README.md) details the compilation mechanics and contains links to subfolders ([`FesterUO`](custom-scripts/FesterUO/README.md), [`CustomPetGump`](custom-scripts/CustomPetGump/README.md), [`LegendaryMaster`](custom-scripts/LegendaryMaster/README.md), [`MyStats`](custom-scripts/MyStats/README.md), [`PetExchange`](custom-scripts/PetExchange/README.md), [`TMap`](custom-scripts/TMap/README.md)) with their original community authors (Feng, Keith, 4737Carlin, etc.).
* **Engine Patches**: [`patches/README.md`](patches/README.md) documents every patch's target files, engine modifications, and upstream behavior adjustments.
* **Runtime Configurations**: [`Config/README.md`](Config/README.md) and its subfolders ([`ServUO`](Config/ServUO/README.md), [`FesterUO`](Config/FesterUO/README.md), [`LegendaryPets`](Config/LegendaryPets/README.md), [`LegendaryMaster`](Config/LegendaryMaster/README.md), [`PetExchange`](Config/PetExchange/README.md)) detail every exposed `.cfg` setting.

---

## 🔒 Repository Tracking: Committed vs. Ignored Files

To maintain clean version control, protect proprietary game assets, and ensure container portability, files in this directory are divided into committed assets and ignored runtime data:

### Committed to Git
* **[`Dockerfile`](Dockerfile)**: The multi-stage build recipe that fetches the pinned ServUO commit, applies engine patches, builds the server binaries (`ServUO.exe`, `Ultima.dll`, `Scripts.dll`), and sets up the Linux runtime environment.
* **[`manage-patches.sh`](manage-patches.sh)**: A helper script that validates patches against upstream, exports new diffs, and facilitates testing against newer upstream commits.
* **[`patches/`](patches/README.md)**: Unified `.patch` files applied sequentially during the build stage. These introduce core engine enhancements (e.g., LAN IP redirect fixes, safe save handling, harvesting delays, custom gump hooks).
* **[`Config/`](Config/README.md)**: Runtime server configuration files parsed dynamically by ServUO on boot. Allows tuning server settings, loot, housing, and custom systems without rebuilding the container.
* **[`custom-scripts/`](custom-scripts/README.md)**: Custom C# systems, starter kits, quest givers, and player dashboards that are mounted directly into the server and compiled dynamically on startup.
* **`.dockerignore`**: Ensures ephemeral logs, saves, client data, and local git clones are not transferred into the Docker build daemon.

### Ignored by Git (`.gitignore`)
* **`Client/`**: Proprietary Ultima Online Classic Client game data files (`.mul`, `.uop`, clilocs, definitions, and textures) owned by Electronic Arts / Broadsword. These files cannot legally be redistributed in public git repositories.
* **`Saves/`**: Live world state database (player accounts, character positions, items, guilds, and automated backups). These change continuously at runtime and contain sensitive account credentials.
* **`Logs/`**: Server console outputs, auto-save notifications, speech logs, and crash call stacks generated during execution.
* **`upstream/`**: A local shallow git clone of the upstream `ServUO/ServUO` repository used by developers with `manage-patches.sh` to test and generate new patches.

---

## 📌 Pinned Commit Rationale

The container build defined in [`Dockerfile`](Dockerfile) explicitly pins upstream ServUO to a specific git commit hash:

```dockerfile
ARG SERVUO_COMMIT=d76bf4443cf76d081ddaf8f57c87ff33749256af
```

### Why Pin to a Specific Commit?
1. **Build Determinism & Reproducibility**: ServUO's `pub57` branch undergoes frequent updates. Relying on `HEAD` or a rolling branch name means two builds performed days apart could produce different binaries or suffer from upstream regressions.
2. **Patch Integrity**: The modular unified diffs located in [`patches/`](patches/README.md) target exact source code lines and context within the upstream engine. If upstream modifies surrounding code, `git apply` will fail during `docker compose build`, breaking image generation.
3. **Controlled Upgrades**: Using [`manage-patches.sh update`](manage-patches.sh), administrators can intentionally fetch new upstream commits, verify that all existing patches apply cleanly (or adjust them if upstream changed), and update the pinned SHA in a tested, deliberate manner.

---

## 💾 Container Volumes & Data Architecture

The `servuo` service defined in `docker-compose.yaml` utilizes five host bind mounts:

| Host Directory | Container Path | Mode | Purpose |
| :--- | :--- | :--- | :--- |
| `${SERVICE_DIR}/servuo/Client` | `/server/Client` | `ro` | Provides static UO client files required by ServUO for map geometry, statics, multi structures, and tile data. |
| `${SERVICE_DIR}/servuo/Saves` | `/server/Saves` | `rw` | Stores persistent world data, account credentials, mobiles, items, and scheduled backups. |
| `${SERVICE_DIR}/servuo/Logs` | `/server/Logs` | `rw` | Captures engine logs, command activity, and crash diagnostics on the host. |
| `${SERVICE_DIR}/servuo/custom-scripts` | `/server/Scripts/Custom` | `rw` | Injects custom C# script files dynamically compiled by the .NET SDK on server launch. |
| `${SERVICE_DIR}/servuo/Config` | `/server/Config` | `rw` | Injects custom configuration files (`.cfg`) read by the engine during startup. |

### Populating the `Client/` Directory

ServUO requires an authentic installation of Ultima Online Classic client data files to parse facets, maps, statics, and tile properties:

1. **Download the Official Client**:
   Download the **Ultima Online Classic Client** installer directly from the [Official Ultima Online Client Download Page](https://uo.com/client-download/).
2. **Install the Client**:
   Install and run the game on a Windows computer or virtual machine. Allow the official patcher to complete all updates so all current `.uop`, `.mul`, and `Cliloc` files are fully populated.
3. **Copy Files to Host**:
   Copy the contents of the installed client folder (typically `C:\Program Files (x86)\Electronic Arts\Ultima Online Classic`) into `${SERVICE_DIR}/servuo/Client/` on your Docker host.
4. **DataPath Configuration**:
   The Dockerfile automatically generates `/server/Config/DataPath.cfg` containing `CustomPath=/server/Client`, instructing ServUO to read all client assets from the mounted directory.

---

## 🎮 Supported Client Software

To connect to your ServUO server, modern open-source enhanced clients are recommended for smooth performance, high frame rates, and cross-platform support:

* **[ClassicUO](https://www.classicuo.eu/)** ([GitHub Repository](https://github.com/ClassicUO/ClassicUO)):
  An open-source, cross-platform client reimplementation in C#/FNA supporting Windows, Linux, and macOS with uncapped frame rates, zoom, smooth animations, and sound improvements.
* **[TazUO](https://tazuo.org)** ([GitHub Repository](https://github.com/PlayTazUO/TazUO)):
  A modern, feature-rich client forked from ClassicUO featuring built-in scripting (Legion Scripting Engine), modernized grid-based containers, customizable user interfaces, and built-in assistant utilities.

---

## 🚀 Building & Running the Container

### Build the Image
Build the container using Docker Compose:
```bash
docker compose build servuo
```

### Start the Service
Start the server in the background:
```bash
docker compose up -d servuo
```

### View Server Logs
Monitor the console output in real time:
```bash
docker compose logs -f servuo
```

### Attach to the Server Console
To interact directly with the ServUO console:
```bash
docker attach servuo
```

> [!WARNING]
> **Detaching Safely**: When attached to the container, **DO NOT** press `Ctrl + C`, as this will send a SIGINT signal and terminate the server process. To detach safely while leaving the server running, press **`Ctrl + P`** followed immediately by **`Ctrl + Q`**.

---

## 🔄 Clean Startup Sequence

When initializing a brand new world or rebuilding after a wipe, run the following sequence in-game from an account with Administrator permissions:

> [!TIP]
> Running `[CreateWorld` with **Select All** automatically handles the vast majority of world generation—including moongates, doors, signs, teleporters, decorations, revamped dungeons (Covetous, Shame, Despise, Wrong, Blackthorn), champion spawns (with the Lich puzzle), and creature/vendor spawners (`[xmlload Spawns]`). Only 5 supplemental encounters need to be generated separately.

### Step 1: Pre-Generation Preparation
Disable automatic saving to ensure incomplete world generation states are not committed to disk:
```text
[SetSaves false
```

### Step 2: Automated Global Generation (`[CreateWorld`)
Open the world generation interface:
```text
[CreateWorld
```
1. In the gump that appears, click **Select All** (or check all desired categories).
2. Click **Okay**.
3. Allow the automated generation to complete. ServUO will report progress in your journal and console.

### Step 3: Supplemental Encounters & Systems
Run the 5 standalone systems not bundled into the `[CreateWorld]` menu:
```text
[GenMiniChamp
[GenExodusNexus
[GenExploringTheDeep
[GenForgottenPyramid
[ArenaSetup
```

* `[GenMiniChamp`: Generates the Stygian Abyss Mini-Champion spawners in Ter Mur.
* `[GenExodusNexus`: Generates the Exodus Encounter Nexus and summoning altars.
* `[GenExploringTheDeep`: Spawns the ocean and Sorcerer's Dungeon quest encounters.
* `[GenForgottenPyramid`: Builds the Sphynx quest and Forgotten Pyramid encounters in Malas.
* `[ArenaSetup`: Initializes the PvP Arena tournament controller and dueling pits.

### Step 4: Commit and Re-enable Saves
Commit all generated fixtures, dungeons, and spawns to disk, then restore automated saves:
```text
[Save
[SetSaves true
```

---

## ♻️ Reset Procedure

Follow these steps to completely reset the server world state and accounts back to a clean baseline:

### Step 1: Stop the ServUO Container
```bash
docker compose stop servuo
```

### Step 2: Clear the Saves and Logs Directories
Back up the existing world save state, purge runtime data, and preserve gitkeep anchors:
```bash
cp -r servuo/Saves servuo/Saves.backup.$(date +%Y%m%d)
rm -rf servuo/Saves/* servuo/Saves/.[!.]*
touch servuo/Saves/.gitkeep
rm -rf servuo/Logs/*
touch servuo/Logs/.gitkeep
```

### Step 3: Start ServUO
```bash
docker compose up -d servuo
```

### Step 4: Create Your Initial Admin Account
Attach to the container console to respond to the initial administrator setup prompt:
```bash
docker attach servuo
```

When prompted:
```text
No accounts detected!
Please enter an admin username: <type username>
Please enter an admin password: <type password>
```

> [!IMPORTANT]
> **Detaching from Console**: Press **`Ctrl + P`** followed by **`Ctrl + Q`** to safely detach from the container without stopping the server. Do not use `Ctrl + C`.

### Step 5: Rebuild the World Spawns & Decorations
1. Launch your ClassicUO or TazUO client and log in with your new Administrator account.
2. Create your owner character.
3. Execute the [Clean Startup Sequence](#-clean-startup-sequence) detailed above.

### Step 6: Create Additional Player Accounts

> [!NOTE]
> If [`Config/ServUO/Accounts.cfg`](Config/ServUO/Accounts.cfg) has `AutoCreateAccounts=True` enabled (allowing clients to automatically register accounts upon their first login attempt), then Step 6 is not required. However, because new account creation is disabled by default (`AutoCreateAccounts=False`) on this private two-person server, all additional player accounts must be created manually using the steps below.

1. In-game, open the administrative interface:
   ```text
   [admin
   ```
2. In the admin menu on the left side, click **ACCOUNT LIST**.
3. At the top of the Account List window, enter the credentials:
   - **Name**: (type the desired username)
   - **Pass**: (type the desired password)
4. Click the **Add** button.
5. The account is created immediately as a standard player account (`AccessLevel.Player`).
