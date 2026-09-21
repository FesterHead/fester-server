# zerodowned Custom Scripts

This directory (`servuo/custom-scripts/zerodowned/`) contains custom scripts and utilities authored by **zerodowned** from the ServUO community.

---

## Provenance & Attribution

- **Author**: zerodowned
- **Source Repository**: [https://github.com/zerodowned/Custom-Scripts-for-ServUO](https://github.com/zerodowned/Custom-Scripts-for-ServUO)
- **Script Origins**:
  - [`SOS Decoder/SOSDecoder.cs`](https://github.com/zerodowned/Custom-Scripts-for-ServUO/blob/master/SOS%20Decoder/SOSDecoder.cs)
  - [`Treasure Map Decoder/TreasureMapDecoder.cs`](https://github.com/zerodowned/Custom-Scripts-for-ServUO/tree/master/Treasure%20Map%20Decoder)

---

## Overview & Features

### SOS Instant Transporter (`SOSDecoder.cs`)

`SOSDecoder` is a seafaring quality-of-life item designed to streamline SOS salvage voyages:

- **Instant Boat Transport**: While standing on a stationary boat, double-click the transporter and target an SOS in your backpack. The boat automatically calculates open-water clearance near the sunken ship coordinates and teleports directly to the target location with nautical splash animations.
- **Pre-flight Validations**: Checks that the player is not criminal, overloaded, in combat, in jail, casting a spell, or on a moving vessel.
- **Facet Safety**: Verifies that the SOS destination matches the boat's current facet (e.g. Trammel vs. Felucca) before calculating displacement.
- **Unlimited Usage**: No charges required; players can freely decode and navigate between SOS salvage coordinates.
- **Procurement & Vendor Stock**: Sold by NPC Fishermen for 10,000 gold (configured via `SOSDecoderCost` in `servuo/Config/ServUO/Vendors.cfg`). Staff can also spawn it via `[add SOSDecoder`.

### Treasure Map Instant Transporter (`TreasureMapDecoder.cs`)

`TreasureMapDecoder` is an exploration quality-of-life item designed for treasure hunters:

- **Moongate to Dig Site**: Double-click from your backpack and target a Treasure Map. Opens a 30-second timed moongate directly onto the chest coordinates (automatically calculating ground Z-elevation).
- **Auto-Decodes Undeciphered Maps**: Targeting an un-decoded map marks it decoded by the player so you don't need to manually decipher it first.
- **Completion Check**: Prevents opening gates to already completed/looted treasure maps.
- **Pre-flight Validations**: Checks that the player is not criminal, overloaded, in combat, in jail, or casting a spell.
- **Unlimited Usage**: No charges required; players can freely decode and gate to any of their treasure maps.
- **Procurement & Vendor Stock**: Sold by NPC Mapmakers for 10,000 gold (configured via `TreasureMapDecoderCost` in `servuo/Config/ServUO/Vendors.cfg`). Staff can also spawn it via `[add TreasureMapDecoder`.
