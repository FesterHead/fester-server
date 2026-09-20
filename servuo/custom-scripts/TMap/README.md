# Treasure Map and SOS Storage Book

This directory (`servuo/custom-scripts/TMap/`) contains the Treasure Map and SOS Message storage book system for ServUO.

---

## Provenance & Attribution

- **Community Script**: Treasure Map and SOS Storage Book
- **Author**: 4737Carlin (January 12, 2025)
- **Source**: [https://www.servuo.dev/archive/treasure-map-and-sos-storage-book.2546/](https://www.servuo.dev/archive/treasure-map-and-sos-storage-book.2546/)

---

## Overview & Features

`TMapBook.cs` and `TMapGumps.cs` implement a specialized storage container modelled after the classic Bulk Order Book architecture:

- **High Capacity**: Stores up to 500 Treasure Maps and SOS messages in a single blessed, securable tome.
- **Filtering & Search**: Filter entries by type (Treasure Map vs. SOS), facet, decoding status, and difficulty level.
- **Modern ServUO Loot Support**: Seamlessly extracts modern treasure maps while retaining package type and treasure level information.
- **Player Vendor Compatible**: Supports setting prices per map and selling directly from player vendor backpacks.
- **Mapmaker Vendor Available**: Sold by NPC Mapmakers across Britannia for 1,000 gold (configured via `servuo/Config/ServUO/Vendors.cfg`).

---

## Included Files

- [`TMapBook.cs`](TMapBook.cs): Core storage item definition, serialization, filtering, entry management, and context menus.
- [`TMapGumps.cs`](TMapGumps.cs): Multi-page interactive UI for browsing, filtering, map withdrawal, and vendor pricing.
