/*
 * UO Community Script: The Ultimate ARPG-Style Loot Filter
 * Source: https://www.servuo.dev/archive/the-ultimate-arpg-style-loot-filter-diablo-poe-exactly-like-you-want-it.2606/
 *
 * XmlAttachment linking player mobile instances to their serialized LootFilterSettings profile.
 */

using System;
using Server;
using Server.Engines.XmlSpawner2;

namespace Server.Engines.LootFilter
{
    public class LootFilterAttachment : XmlAttachment
    {
        private LootFilterSettings m_Settings;

        [CommandProperty(AccessLevel.GameMaster)]
        public LootFilterSettings Settings 
        { 
            get => m_Settings; 
            set => m_Settings = value; 
        }

        public LootFilterAttachment(ASerial serial) : base(serial) { }

        [Attachable]
        public LootFilterAttachment()
        {
            m_Settings = new LootFilterSettings();
        }

        public static LootFilterAttachment GetAttachment(Mobile m)
        {
            if (m == null) return null;

            var att = XmlAttach.FindAttachment(m, typeof(LootFilterAttachment)) as LootFilterAttachment;
            if (att == null)
            {
                att = new LootFilterAttachment();
                XmlAttach.AttachTo(m, att);
            }
            return att;
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0); // version

            if (m_Settings == null)
                m_Settings = new LootFilterSettings();

            m_Settings.Serialize(writer);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();

            m_Settings = new LootFilterSettings();
            m_Settings.Deserialize(reader);
        }
    }
}
