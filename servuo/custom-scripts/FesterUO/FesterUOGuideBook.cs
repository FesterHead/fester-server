using System;
using Server;
using Server.Items;

namespace Server.Custom
{
    public class FesterUOGuideBook : BrownBook
    {
        public static readonly BookContent Content = new BookContent(
            "Adventurer's Guide", "Britannia Council",
            new BookPageInfo(
                "  -- FESTER UO --",
                " Adventurer's Guide",
                "===================",
                "Welcome to Britannia!",
                "This tome details your",
                "resource satchel, auto",
                "harvest tools, housing,",
                "magic, and commands."),
            new BookPageInfo(
                "RESOURCE SATCHEL",
                "-------------------",
                "Your blessed satchel",
                "automatically gathers",
                "all resources harvested",
                "by your special tools:",
                "ingots, boards, leather,",
                "cloth, wool, and fish."),
            new BookPageInfo(
                "SATCHEL DETAILS",
                "-------------------",
                "Holds 1,000 items with",
                "100% weight reduction.",
                "",
                "Accepts raw resources",
                "and recall runes.",
                "",
                "Keep it in your pack!"),
            new BookPageInfo(
                "WITHOUT SATCHEL",
                "-------------------",
                "If your satchel is not",
                "in your backpack, all",
                "harvested resources go",
                "straight to your pack,",
                "where standard weight",
                "& item limits apply!"),
            new BookPageInfo(
                "ENCHANTED TOOLS",
                "-------------------",
                "All starter tools are",
                "blessed & unbreakable.",
                "",
                "* Pickaxe: Auto-smelts",
                "  mined ore into ingots.",
                "",
                "* Hatchet: Auto-saws",
                "  cut logs into boards."),
            new BookPageInfo(
                "* Skinning Knife:",
                "  Carves corpses into",
                "  cut leather, shears",
                "  sheep for wool, and",
                "  fillets raw fish.",
                "",
                "* Fishing Pole: Cleans",
                "  catches into steaks."),
            new BookPageInfo(
                "* Scythe: Reaps all",
                "  crops in a 3-tile",
                "  radius (wheat, cotton,",
                "  flax, and vegetables)."),
            new BookPageInfo(
                "HOUSING",
                "-------------------",
                "Player housing may be",
                "constructed anywhere",
                "land is open and valid",
                "across Britannia.",
                "",
                "Housing is allowed",
                "on Trammel, Felucca,",
                "Malas, Tokuno, & TerMur."),
            new BookPageInfo(
                "STARTER COTTAGE",
                "-------------------",
                "Your pack contains a",
                "Cottage Deed Voucher.",
                "",
                "Double-click it to pick",
                "from 6 classic 7x7",
                "styles whenever you are",
                "ready to build a home!"),
            new BookPageInfo(
                "HOME FIXTURES",
                "-------------------",
                "Your first character",
                "also receives two home",
                "fixture addon deeds:",
                "",
                "* Ankh of Sacrifice Deed",
                "* House Moongate Deed",
                "",
                "Place them in your home!"),
            new BookPageInfo(
                "ANKH OF SACRIFICE",
                "-------------------",
                "Double-click deed in",
                "your house to place.",
                "Choose East or South.",
                "",
                "* Automatically revives",
                "  ghosts within 1 tile.",
                "* Tithes & karma prayer.",
                "* Chop with axe to",
                "  re-deed anytime!"),
            new BookPageInfo(
                "HOUSE MOONGATE",
                "-------------------",
                "Double-click deed in",
                "your house to place.",
                "",
                "* Step on or double-",
                "  click to open the",
                "  Public Moongate menu.",
                "* Travel instantly to",
                "  any city or facet",
                "  right from home!"),
            new BookPageInfo(
                "RE-DEEDING GATES",
                "-------------------",
                "Unlike standard UO,",
                "your House Moongate",
                "can be easily re-deeded!",
                "",
                "Double-click an axe",
                "(e.g. your hatchet)",
                "and target the gate.",
                "",
                "It safely returns to a",
                "deed in your pack!"),
            new BookPageInfo(
                "MAGIC & RUNES",
                "-------------------",
                "Your starter kit has:",
                "* Blessed Spellbook with",
                "  all 64 magery spells.",
                "* Blessed Runebook with",
                "  20 recall charges.",
                "* 16 blank recall runes",
                "  inside your satchel!"),
            new BookPageInfo(
                "PLAYER COMMANDS",
                "-------------------",
                "[c <message>",
                "  Global chat broadcast",
                "  to all online players.",
                "",
                "[Stats",
                "  View full combat,",
                "  magic, and ratings.",
                "",
                "[Corpse",
                "  Points a quest arrow",
                "  to your fallen body."),
            new BookPageInfo(
                "STABLES & REPAIRS",
                "-------------------",
                "* Stables: All players",
                "  enjoy 12 base stalls",
                "  at any Animal Trainer.",
                "",
                "* Powder of Fortifying:",
                "  Sold by smiths and",
                "  tinkers to protect",
                "  gear max durability."),
            new BookPageInfo(
                "SEA & ESTATES",
                "-------------------",
                "Housing allowance is",
                "granted one per account",
                "(shared by characters).",
                "",
                "A Blessed Small Boat",
                "Deed is also included",
                "for your sea voyages.")
        );

        [Constructable]
        public FesterUOGuideBook() : base(false)
        {
            Name = "Adventurer's Guide";
            Hue = 0x482; // Antique gilded hue
            LootType = LootType.Blessed;
            Weight = 1.0;
        }

        public FesterUOGuideBook(Serial serial) : base(serial)
        {
        }

        public override BookContent DefaultContent => Content;

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.WriteEncodedInt(0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadEncodedInt();
        }
    }
}
