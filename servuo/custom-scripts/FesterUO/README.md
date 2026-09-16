# FesterUO Custom Scripts

This directory (`servuo/custom-scripts/FesterUO/`) contains custom gameplay systems, utilities, and quality-of-life enhancements authored specifically for the **FesterUO** private shard.

---

## Script Inventory

| Script | Purpose & Mechanics | Commands / Trigger |
| :--- | :--- | :--- |
| [`CorpseFinder.cs`](CorpseFinder.cs) | Locates the player's most recent fallen corpse, displays coordinates/facet/distance, and directs an in-game quest arrow to the body. | `[Corpse` |
| [`CustomAutoTools.cs`](CustomAutoTools.cs) | Enchanted gathering and harvesting tools with automated processing: auto-smelt ore to ingots, auto-saw logs to boards, auto-fillet fish, corpse hide skinning, sheep shearing, and area crop scything. | Tool usage / harvest |
| [`FesterUOGuideBook.cs`](FesterUOGuideBook.cs) | Blessed reference tome detailing satchel mechanics, auto-tools, backpack fallback routing, open housing, runebook & blank runes, home fixtures (Ankh & Moongate), re-deeding instructions, stable stalls, and player commands. | Distributed to first character per account |
| [`GlobalChat.cs`](GlobalChat.cs) | Lightweight server-wide broadcast communication for private duo play, outputting in distinct cyan text across all facets. | `[c <message>`, `[chat <message>` |
| [`HouseMoongateAddon.cs`](HouseMoongateAddon.cs) | Custom house-placed moongate addon with full facet destination navigation (`MoongateGump`) and axe re-deeding support (`BaseAddonDeed`). | Double-click or walk through to travel; axe to re-deed |
| [`ResourceSatchel.cs`](ResourceSatchel.cs) | Blessed, weight-free storage satchel (100% weight reduction) that automatically intercepts and stores gathered resources upon harvest, and safely holds recall runes. | Double-click to open |
| [`StarterKitDistribution.cs`](StarterKitDistribution.cs) | Provisions every new character with a Blessed Full Spellbook (all 64 spells), Blessed Runebook (20 charges), 16 blank recall runes placed in the satchel, indestructible auto-tools, and personal resource satchel, plus an account-level one-time cottage voucher, small boat deed, Ankh of Sacrifice deed, and House Moongate deed. | Character creation, login fallback, or `[ClaimStarterKit` |
