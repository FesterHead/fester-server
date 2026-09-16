# Base ServUO Platform Configurations

This directory (`servuo/Config/ServUO/`) contains the base ServUO platform configuration files that originate directly from the upstream [ServUO Core](https://github.com/ServUO/ServUO) distribution, tuned specifically for the **FesterUO** private shard.

---

## Configuration Inventory

| Configuration File | Scope | Key Customizations / Purpose |
| :--- | :--- | :--- |
| [`Accounts.cfg`](Accounts.cfg) | `Accounts` | Sets `AccountsPerIp=20` and disables `AutoCreateAccounts=False`. |
| [`AutoRestart.cfg`](AutoRestart.cfg) | `AutoRestart` | Configures scheduled server restart frequency and hour. |
| [`AutoSave.cfg`](AutoSave.cfg) | `AutoSave` | World save interval (15 minutes) and quiet save warnings. |
| [`Champions.cfg`](Champions.cfg) | `Champions` | Duo-scaled kill counts (64/32/16/8 per tier) and 2 Power/Stat scrolls per boss kill. |
| [`DataPath.cfg`](DataPath.cfg) | `DataPath` | Sets `CustomPath=/server/Client` to mount client UOP/MUL asset files. |
| [`Expansion.cfg`](Expansion.cfg) | `Expansion` | Active expansion set to `CurrentExpansion=TOL` (Time of Legends). |
| [`General.cfg`](General.cfg) | `General` | Felucca red restriction toggles and ground item decay intervals (`DefaultItemDecayTime=60`). |
| [`Housing.cfg`](Housing.cfg) | `Housing` | Sets `AccountHouseLimit=1`. |
| [`Loot.cfg`](Loot.cfg) | `Loot` | Enables `CanPOFJewelry=True` and configures Felucca luck bonus (+1000). |
| [`PlayerCaps.cfg`](PlayerCaps.cfg) | `PlayerCaps` | Total stat cap (450), total skill cap (1200.0%), anti-macro checks, and 5-minute stat gain timer. |
| [`Server.cfg`](Server.cfg) | `Server` | Shard name `FesterUO`, port `2593`, and Docker host LAN routing (`PrivateAddress=192.168.86.48`). |
| [`TreasureMaps.cfg`](TreasureMaps.cfg) | `TreasureMaps` | Enables modern treasure map chest system and lowers chest reset timer to 7 days. |
| [`Vendors.cfg`](Vendors.cfg) | `Vendors` | Sets NPC restock delay to 15 minutes and configures Powder of Fortifying vendor stock. |
| [`VetRewards.cfg`](VetRewards.cfg) | `VetRewards` | Sets reward intervals to 30 days per tier and locks veteran skill cap bonuses to preserve caps. |
