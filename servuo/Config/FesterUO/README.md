# FesterUO Custom Shard Configurations

This directory (`servuo/Config/FesterUO/`) contains custom gameplay and world tuning configuration files authored specifically for the **FesterUO** private shard.

---

## Configuration Inventory

| Configuration File | Scope | Consuming Systems & Purpose |
| :--- | :--- | :--- |
| [`Harvest.cfg`](Harvest.cfg) | `Harvest` | **Consuming Patches**: `03-harvest-amount`, `04-fishing-delay`, `05-mining-delay`, `06-lumberjacking-delay`<br>Configures resource harvest yields (`MinYield=3`, `MaxYield=6`) and tool delays (`FishingDelay=2.0s`, `MiningDelay=1.0s`, `LumberjackingDelay=1.0s`). |
| [`Stables.cfg`](Stables.cfg) | `Stables` | **Consuming Patches**: `08-stable-slots`<br>Configures baseline stable stalls (`BaseSlots=12`) granted to every character before taming skill bonuses. |
