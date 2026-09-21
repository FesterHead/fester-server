# Legendary Pets Configuration & Guide

This directory (`servuo/Config/LegendaryPets/`) contains standalone configuration files for the **Legendary Pets** system (patch [`13-legendary-pets.patch`](../../patches/13-legendary-pets.patch)).

---

## Provenance & Attribution

- **Origin**: [ServUO Community Archive - Custom Pet Bundle: Legendary (Resource #2610)](https://www.servuo.dev/archive/custom-pet-bundle.2610/)
- **Original Author**: Feng / UO Wildlands Team
- **License**: GNU General Public License v3.0 (GPL-3.0)
- **Integration Architecture**: Compiled directly into the core engine via [`13-legendary-pets.patch`](../../patches/13-legendary-pets.patch). This avoids C# duplicate type collisions (`CS0101` / `CS0111`) while ensuring wild world spawners across all facets naturally spawn Legendary variants.

---

## Spawning Mechanics

When wild creatures of supported species spawn in the world, their `OnBeforeSpawn` hook rolls against their configured spawn probability in this directory. If the roll succeeds, the creature mutates into its Legendary form:
- Displays the gold overhead title: `<BASEFONT COLOR=#FFD700>Legendary</BASEFONT>`.
- Adopts unique legendary hues, adjusted control slots, elevated stats, and enhanced taming requirements.
- Replaces standard combat abilities with specialized legendary abilities and advanced pet training definitions.

---

## Species Inventory

| Configuration File | Species | Base Class | Default Rate | Legendary Hue | Control Slots | Min Tame | Specialized Abilities & Mechanics |
| :--- | :--- | :--- | :---: | :--- | :---: | :---: | :--- |
| **[`FireSteed.cfg`](FireSteed.cfg)** | Fire Steed | `BaseMount` | `0.02` (2%) | `1174` (Golden Amber) | 1 | 120.0 | • Ability: Inferno (replaces Dragon Breath)<br>• Skills: Magery (80.0–100.0)<br>• Training: 1 to 5 slots, Magical / Dragon2 profile |
| **[`Kirin.cfg`](Kirin.cfg)** | Ki-rin | `BaseMount` | `0.02` (2%) | • Fire: `1161`<br>• Cold: `1153` | 2 | 120.0 | • Rolls 50/50 between Fire Ki-rin (AngryFire & DoubleStrike) and Cold Ki-rin (FlurryForce & Heal)<br>• Both variants use Melee AI and 2 to 5 training slots |
| **[`Nightmare.cfg`](Nightmare.cfg)** | Nightmare | `BaseMount` | `0.02` (2%) | `2040` (Deep Void) | 1 | 120.0 | • Ability: Heal (replaces Dragon Breath)<br>• Skills: Healing (72.2–98.9)<br>• Training: 1 to 5 slots, MagicalAndNecromantic profile |
| **[`OsseinRam.cfg`](OsseinRam.cfg)** | Ossein Ram | `BaseCreature` | `0.02` (2%) | `2076`, `2706`, `2955`, `2075` | 1 | 120.0 | • Abilities: LifeLeech & DoubleStrike<br>• Skills: Necromancy (50.6–75.0), Necromage magical ability<br>• Training: 1 to 5 slots, Class.None profile |
| **[`Phoenix.cfg`](Phoenix.cfg)** | Phoenix | `BaseCreature` | `0.02` (2%) | • Magery: `2753`<br>• Mystic: `1995`<br>• Weave: `2953` | 3 | 120.0 | • Rolls equally among 3 specialized caster variants:<br>  - Magery: MageryMastery, AuraDamage, EvalInt (200–250)<br>  - Mysticism: Mysticism (100), Focus (150–225), AuraDamage<br>  - Spellweaving: Spellweaving (225), AuraDamage<br>• Training: 3 to 5 slots |
| **[`PolarBear.cfg`](PolarBear.cfg)** | Polar Bear | `BaseMount` | `0.02` (2%) | `1153`, `2953` | 2 (max 5) | 120.0 | • **Rideable Mount**: Converted from `BaseCreature` to `BaseMount` (ItemID `0x3EC5`)<br>• Spawns exclusively in the Tokuno Winter Spur region<br>• Ability: ColossalRage, Melee AI<br>• Training: 2 to 5 slots, Clawed profile |
| **[`ShadowWyrm.cfg`](ShadowWyrm.cfg)** | Shadow Wyrm | `BaseCreature` | `0.02` (2%) | `1910` (Obsidian) | 2 | 120.0 | • Ability: LifeLeech (replaces Dragon Breath)<br>• Skills: Necromancy (130–150), SpiritSpeak (130–150)<br>• Damage: 20% Cold, 80% Poison<br>• Training: 2 to 5 slots, MagicalAndNecromantic profile |
| **[`TsukiWolf.cfg`](TsukiWolf.cfg)** | Tsuki Wolf | `BaseCreature` | `0.02` (2%) | `1929`, `1918`, `1910`, `1462`, `1158`, `2716`, `2747` | 2 | 120.0 | • Abilities: LifeLeech, Rage, ArmorIgnore weapon ability<br>• Training: 2 to 5 slots, MagicalClawedTailedNecromanticAndTokuno profile |

---

## Modifying Spawn Rates

1. Open the target pet's `.cfg` file in this directory.
2. Set `SpawnRate` to the desired probability between `0.00` (disabled) and `1.00` (100% chance):
   ```ini
   SpawnRate=0.02   # 2% spawn chance
   ```
3. Restart the container for changes to take effect:
   ```bash
   docker compose restart servuo
   ```

---

## In-Game Staff Commands

Spawn individual creatures directly to test Legendary roll behavior:

```text
[add FireSteed
[add Kirin
[add Nightmare
[add OsseinRam
[add Phoenix
[add PolarBear
[add ShadowWyrm
[add TsukiWolf
```

Inspect any spawned creature with `[props` to verify the `IsLegendary` property flag and inspect combat/training stats via the Animal Lore gump.
