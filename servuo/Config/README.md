# ServUO Configuration Guide

This directory (`servuo/Config/`) contains the runtime configuration files mounted into the ServUO container at `/server/Config/`.

ServUO dynamically parses every `*.cfg` file in this directory and its subdirectories upon startup. Each file defines a configuration **scope** matching its filename or directory hierarchy. Values are accessed in C# scripts via:

```csharp
Server.Config.Get("Scope.SettingKey", defaultValue);
```

---

## Directory Organization

The configuration files are organized into functional subdirectories, each with its own detailed `README.md`:

1. **[`ServUO/`](ServUO/README.md)**:
   Contains the 14 base ServUO platform configuration files (`Accounts.cfg`, `AutoRestart.cfg`, `AutoSave.cfg`, `Champions.cfg`, `DataPath.cfg`, `Expansion.cfg`, `General.cfg`, `Housing.cfg`, `Loot.cfg`, `PlayerCaps.cfg`, `Server.cfg`, `TreasureMaps.cfg`, `Vendors.cfg`, `VetRewards.cfg`) tuned for this shard.

2. **[`FesterUO/`](FesterUO/README.md)**:
   Contains custom shard progression, harvesting, and stable stall configurations (`Harvest.cfg`, `Stables.cfg`) consumed by custom systems and patches.

3. **[`PetExchange/`](PetExchange/README.md)**:
   Contains configuration files for the Pet Exchange Hitching Post house addon (`PetExchange.cfg`).

4. **[`LegendaryPets/`](LegendaryPets/README.md)**:
   Contains individual configuration files for wild Legendary pet species spawn rates (`FireSteed.cfg`, `Kirin.cfg`, `Nightmare.cfg`, `OsseinRam.cfg`, `Phoenix.cfg`, `PolarBear.cfg`, `ShadowWyrm.cfg`, `TsukiWolf.cfg`).

5. **[`LegendaryMaster/`](LegendaryMaster/README.md)**:
   Contains configuration settings for the Legendary Master of Skills NPC questmaster system (`LegendaryMaster.cfg`).

---

## Modifying Configurations

1. Edit any `.cfg` file directly on the host in `servuo/Config/`.
2. Changes to `.cfg` files take effect whenever the container restarts:
   ```bash
   docker compose restart servuo
   ```
3. No image rebuilding or patch recompilation is required when altering values in these `.cfg` files.
