# Customizable Runebook & Spellbook Dye Tubs

This directory (`servuo/custom-scripts/RuneBookandSpellBookDyeTubs/`) contains the **Customizable Runebook and Spellbook Dye Tubs** system for ServUO.

---

## Provenance & Attribution

- **Origin**: [ServUO Community Archive - Customizable Rune Book and Spell Book Dye Tubs (16 Preset Colors) (Resource #2642)](https://www.servuo.dev/archive/customizable-rune-book-and-spell-book-dye-tubs-16-preset-colors.2642/)
- **Compiled & Modified By**: Feng / UO Wildlands Team
- **License**: GNU General Public License v3.0 (GPL-3.0)
- **Integration**: Placed in `servuo/custom-scripts/RuneBookandSpellBookDyeTubs/` and dynamically compiled into `Scripts.dll`. Hooked into core dyeing mechanics via [`28-dyes-target-handler.patch`](../../patches/28-dyes-target-handler.patch).

---

## System Overview

The system provides dedicated, customizable dye tubs for runebooks and spellbooks featuring interactive palette gumps:

1. **Runebook Dye Tub (`RunebookCustomDyeTub`)**:
   - Blessed, non-decaying dye tub configured to dye runebooks (`AllowRunebooks = true`).
   - Defaults to 16 preset hues (Reds, Oranges, Yellows, Greens, Teal, Blues, Purples, Runic Atlas Brown `#1121`, Black `#1`, and White `#2050`).
2. **Spellbook Dye Tub (`SpellbookCustomDyeTub`)**:
   - Blessed, non-decaying dye tub configured to dye spellbooks (standard spellbooks, necromancy, chivalry, bushido, ninjitsu, spellweaving, and mysticism).
   - Defaults to 16 preset hues (including Spellweaving Green `#2210`, vibrant primaries, Black, and White).
3. **Interactive Visual Palette Gump**:
   - Using standard Dyes on the tub opens an interactive 16-slot palette gump (`RunebookCustomHueGump` / `SpellbookCustomHueGump`).
   - Each swatch is rendered dynamically with full multi-step gradient bars parsed from the server's hue data (`Ultima.Hues`).
4. **Palette Customization Mode**:
   - Clicking **"Customize Colors"** enters palette modification mode, allowing players to click any of the 16 slots to launch a standard `HuePicker` and reassign that slot to any preferred hue.
5. **Default Restore & Removal**:
   - **"Restore Default"**: Reverts all 16 slots back to the original preset hues.
   - **"Remove Dyed Hue"**: Resets the tub to hue `0`.
6. **GM Administration**:
   - Game Masters can access the configuration gump directly via the item's context menu (**Set Hue**).

---

## Core Engine Patch Integration

When a player double-clicks standard Dyes and targets a dye tub, the core `Dyes.cs` logic handles the interaction. To route dye actions into custom tubs without hardcoding specific custom classes into core items, the system utilizes [`28-dyes-target-handler.patch`](../../patches/28-dyes-target-handler.patch):

- **Interface Definition** (`Scripts/Items/Internal/ItemInterfaces.cs`):
  ```csharp
  public interface IDyesTargetHandler
  {
      void OnDyesUsed(Mobile from, DyeTub tub);
  }
  ```
- **Target Handler Hook** (`Scripts/Items/Tools/Dyes.cs`):
  ```csharp
  if (tub.Redyable)
  {
      if (tub is IDyesTargetHandler handler)
          handler.OnDyesUsed(from, tub);
      else if (tub.CustomHuePicker == null)
          from.SendHuePicker(new InternalPicker(tub));
      else
          from.SendGump(new CustomHuePickerGump(from, tub.CustomHuePicker, new CustomHuePickerCallback(SetTubHue), tub));
  }
  ```

---

## Item & Command Reference

| Item / Command | Type | Description |
| :--- | :--- | :--- |
| `[add RunebookCustomDyeTub` | Blessed Dye Tub | Spawns the 16-preset customizable runebook dye tub. |
| `[add SpellbookCustomDyeTub` | Blessed Dye Tub | Spawns the 16-preset customizable spellbook dye tub. |
| Double-click **Dyes** -> target tub | Action | Opens the 16-slot visual color palette gump. |
| Double-click tub -> target book | Action | Applies the active dyed hue to the targeted runebook or spellbook. |

---

## Directory Layout

```
servuo/custom-scripts/RuneBookandSpellBookDyeTubs/
├── README.md                 # System documentation, attribution, and usage guide
├── RunebookCustomDyeTub.cs   # Runebook custom dye tub item & interactive gump
└── SpellbookCustomDyeTub.cs  # Spellbook custom dye tub item, target, & interactive gump
```
