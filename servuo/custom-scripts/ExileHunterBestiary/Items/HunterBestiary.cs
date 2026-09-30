/*
 * UO Community Script: Hunter's Bestiary & Monster Treatises System
 * Original Author: UO Exile Team / Imagine (Resource #2674)
 * Source: https://www.servuo.dev/archive/release-hunters-bestiary-monster-treatises-system-193-creatures-1-line-install.2674/
 */

using System;
using Server.Custom.ExileHunterBestiary.Gumps;
using Server.Network;

namespace Server.Custom.ExileHunterBestiary.Items
{
    public class HunterBestiary : Item
    {
        public const int DefaultBestiaryHue = 1153; // Ancient Mythic Tome Slate

        [Constructable]
        public HunterBestiary() : base(0x2252)
        {
            Name = "The Grand Hunter's Bestiary";
            Hue = DefaultBestiaryHue;
            Weight = 2.0;
            LootType = LootType.Blessed;
        }

        public HunterBestiary(Serial serial) : base(serial)
        {
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (!IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
                return;
            }

            from.CloseGump(typeof(HunterBestiaryGump));
            from.SendGump(new HunterBestiaryGump(from));
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);

            list.Add(1070722, "<BASEFONT COLOR=#D1C4E9>Grand Grimoire of Monster Lore<BASEFONT COLOR=#FFFFFF>");
            list.Add(1070722, "<BASEFONT COLOR=#ECEFF1>Tracks mastered treatises and combat bonuses<BASEFONT COLOR=#FFFFFF>");
            list.Add(1070722, "<BASEFONT COLOR=#FFE082>Double-click to open Bestiary<BASEFONT COLOR=#FFFFFF>");
        }

        public override void SendPropertiesTo(Mobile from)
        {
            var list = new ObjectPropertyList(this);
            GetProperties(list);

            var profile = HunterBestiaryEngine.GetProfile(from);
            int mastered = profile != null ? profile.TotalMastered : 0;
            int total = HunterBestiaryEngine.RegistryById.Count;

            list.Add(1070722, string.Format("<BASEFONT COLOR=#00FFCC>Mastery Progress: {0} / {1} Creatures<BASEFONT COLOR=#FFFFFF>", mastered, total));

            list.Terminate();
            from.Send(list);
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }
}
