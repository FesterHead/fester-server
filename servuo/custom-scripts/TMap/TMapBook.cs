/************************************************************************************
* Community Script: Treasure Map and SOS Storage Book                              *
* Author: 4737Carlin (January 12, 2025)                                             *
* Source: https://www.servuo.dev/archive/treasure-map-and-sos-storage-book.2546/   *
*                                                                                  *
* Based originally on BulkOrderBook architecture:                                  *
* - Holds up to 500 Treasure Maps and SOSs in a blessed, securable book            *
* - Filtering, viewing, dropping, price setting, and player vendor selling         *
************************************************************************************/

using System;
using System.Collections;
using System.Collections.Generic;
using Server.ContextMenus;
using Server.Gumps;
using Server.Mobiles;
using Server.Multis;
using Server.Prompts;

namespace Server.Items
{
    public class TMapBook : Item, ISecurable
    {
        private ArrayList m_Entries;
        private TMapFilter m_Filter;
        private string m_BookName;
        private SecureLevel m_Level;
        private int m_ItemCount;

        [Constructable]
        public TMapBook() : base(0x2259)
        {
            Weight = 1.0;
            Name = "a Treasure Map and SOS book";
            Hue = 45;
            LootType = LootType.Blessed;
            m_Entries = new ArrayList();
            m_Filter = new TMapFilter();
            m_Level = SecureLevel.CoOwners;
        }

        public TMapBook(Serial serial) : base(serial)
        {
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public string BookName { get { return m_BookName; } set { m_BookName = value; InvalidateProperties(); } }

        [CommandProperty(AccessLevel.GameMaster)]
        public SecureLevel Level { get { return m_Level; } set { m_Level = value; } }
        public ArrayList Entries { get { return m_Entries; } }
        public TMapFilter Filter { get { return m_Filter; } }
        public int ItemCount { get { return m_ItemCount; } set { m_ItemCount = value; } }

        public override void OnDoubleClick(Mobile from)
        {
            if (!from.InRange(GetWorldLocation(), 2))
            {
                from.LocalOverheadMessage(Network.MessageType.Regular, 0x3B2, 1019045); // I can't reach that.
            }
            else if (m_Entries.Count == 0)
            {
                from.SendLocalizedMessage(1062381); // The book is empty.
            }
            else if (from is PlayerMobile)
            {
                from.SendGump(new TMapGump((PlayerMobile)from, this));
            }
        }

        public override void OnDoubleClickSecureTrade(Mobile from)
        {
            if (!from.InRange(GetWorldLocation(), 2))
            {
                from.SendLocalizedMessage(500446); // That is too far away.
            }
            else if (m_Entries.Count == 0)
            {
                from.SendLocalizedMessage(1062381); // The book is empty.
            }
            else
            {
                from.SendGump(new TMapGump((PlayerMobile)from, this));

                SecureTradeContainer cont = GetSecureTradeCont();

                if (cont != null)
                {
                    SecureTrade trade = cont.Trade;

                    if (trade != null && trade.From.Mobile == from)
                    {
                        trade.To.Mobile.SendGump(new TMapGump((PlayerMobile)(trade.To.Mobile), this));
                    }
                    else if (trade != null && trade.To.Mobile == from)
                    {
                        trade.From.Mobile.SendGump(new TMapGump((PlayerMobile)(trade.From.Mobile), this));
                    }
                }
            }
        }

        public override bool OnDragDrop(Mobile from, Item dropped)
        {
            if (dropped is TreasureMap || dropped is SOS)
            {
                if (!IsChildOf(from.Backpack))
                {
                    from.SendMessage("You must have the book in your backpack to add maps to it"); // You must have the book in your backpack to add maps to it.

                    return false;
                }
                else if (!from.Backpack.CheckHold(from, dropped, true, true))
                {
                    return false;
                }
                else if (m_Entries.Count < 500)
                {
                    if (dropped is SOS)
                    {
                        m_Entries.Add(new SOSEntry((SOS)dropped));
                    }
                    else if (dropped is TreasureMap tmap) // Sanity
                    {
                        m_Entries.Add(new TMapEntry((TreasureMap)dropped));
                    }
                    else
                    {
                        return false;
                    }

                    InvalidateProperties();

                    if (m_Entries.Count / 5 > m_ItemCount)
                    {
                        m_ItemCount++;

                        InvalidateItems();
                    }

                    from.SendSound(0x42, GetWorldLocation());
                    from.SendMessage("Map added to book"); // Map added to book.

                    if (from is PlayerMobile)
                    {
                        from.SendGump(new TMapGump((PlayerMobile)from, this));
                    }

                    dropped.Delete();
                    return true;
                }
                else
                {
                    from.SendMessage("The book is full of maps"); // The book is full of maps.

                    return false;
                }
            }

            from.SendMessage("That is not a treasure map"); // That is not a treasure map

            return false;
        }

        public override int GetTotal(TotalType type)
        {
            int total = base.GetTotal(type);

            if (type == TotalType.Items)
            {
                total = m_ItemCount;
            }

            return total;
        }

        public void InvalidateItems()
        {
            if (RootParent is Mobile)
            {
                Mobile m = (Mobile)RootParent;

                m.UpdateTotals();

                InvalidateContainers(Parent);
            }
        }

        public void InvalidateContainers(object parent)
        {
            if (parent != null && parent is Container)
            {
                Container c = (Container)parent;

                c.InvalidateProperties();

                InvalidateContainers(c.Parent);
            }
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)2); // version
            writer.Write((int)m_ItemCount);
            writer.Write((int)m_Level);
            writer.Write(m_BookName);

            m_Filter.Serialize(writer);

            writer.WriteEncodedInt((int)m_Entries.Count);

            for (int i = 0; i < m_Entries.Count; ++i)
            {
                object obj = m_Entries[i];

                if (obj is TMapEntry)
                {
                    writer.WriteEncodedInt(0);

                    ((TMapEntry)obj).Serialize(writer);
                }
                else if (obj is SOSEntry)
                {
                    writer.WriteEncodedInt(1);

                    ((SOSEntry)obj).Serialize(writer);
                }
                else
                {
                    writer.WriteEncodedInt(-1);
                }
            }
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 2:
                    {
                        m_ItemCount = reader.ReadInt();

                        goto case 1;
                    }
                case 1:
                    {
                        m_Level = (SecureLevel)reader.ReadInt();

                        goto case 0;
                    }
                case 0:
                    {
                        m_BookName = reader.ReadString();
                        m_Filter = new TMapFilter(reader);

                        int count = reader.ReadEncodedInt();

                        m_Entries = new ArrayList(count);

                        for (int i = 0; i < count; ++i)
                        {
                            int v = reader.ReadEncodedInt();

                            switch (v)
                            {
                                case 0:
                                    m_Entries.Add(new TMapEntry(reader)); break;
                                case 1:
                                    m_Entries.Add(new SOSEntry(reader)); break;
                            }
                        }

                        break;
                    }
            }
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);

            list.Add($"Maps in book {m_Entries.Count.ToString()}"); // Maps in book: ~1_val~

            if (m_BookName != null && m_BookName.Length > 0)
            {
                list.Add(1062481, m_BookName); // Book Name: ~1_val~
            }
        }

        public override void GetContextMenuEntries(Mobile from, List<ContextMenuEntry> list)
        {
            base.GetContextMenuEntries(from, list);

            if (from.CheckAlive() && IsChildOf(from.Backpack))
            {
                list.Add(new NameBookEntry(from, this));
            }

            SetSecureLevelEntry.AddTo(from, this, list);
        }

        private class NameBookEntry : ContextMenuEntry
        {
            private readonly Mobile m_From;

            private readonly TMapBook m_Book;

            public NameBookEntry(Mobile from, TMapBook book) : base(6216)
            {
                m_From = from;
                m_Book = book;
            }

            public override void OnClick()
            {
                if (m_From.CheckAlive() && m_Book.IsChildOf(m_From.Backpack))
                {
                    m_From.Prompt = new NameBookPrompt(m_Book);

                    m_From.SendLocalizedMessage(1062479); // Type in the new name of the book:
                }
            }
        }

        private class NameBookPrompt : Prompt
        {
            public override int MessageCliloc { get { return 1062479; } }

            private readonly TMapBook m_Book;

            public NameBookPrompt(TMapBook book)
            {
                m_Book = book;
            }

            public override void OnResponse(Mobile from, string text)
            {
                if (text.Length > 40)
                {
                    text = text.Substring(0, 40);
                }

                if (from.CheckAlive() && m_Book.IsChildOf(from.Backpack))
                {
                    m_Book.BookName = Utility.FixHtml(text.Trim());
                    from.SendMessage("The treasure map book's name has been changed"); // The treasure map book's name has been changed.
                }
            }

            public override void OnCancel(Mobile from)
            {
            }
        }
    }

    public class TMapEntry
    {
        public int Level { get; set; }
        public Map Facet { get; set; }
        public TreasurePackage TreasurePackage { get; set; }
        public TreasureLevel TreasureLevel { get; set; }
        public bool Completed { get; set; }
        public Mobile CompletedBy { get; set; }
        public Mobile Decoder { get; set; }
        public Point2D ChestLocation { get; set; }
        public DateTime NextReset { get; set; }
        public int Price { get; set; }
        public int Decoded { get { return Decoder != null ? 1 : 0; } }

        public TMapEntry(TreasureMap tmap)
        {
            Level = tmap.Level;
            Facet = tmap.Facet;
            TreasurePackage = tmap.Package;
            TreasureLevel = tmap.TreasureLevel;
            Completed = tmap.Completed;
            CompletedBy = tmap.CompletedBy;
            Decoder = tmap.Decoder;
            ChestLocation = tmap.ChestLocation;
            NextReset = tmap.NextReset;
        }

        public TMapEntry(GenericReader reader)
        {
            int version = reader.ReadEncodedInt();

            switch (version)
            {
                case 0:
                    {
                        Level = reader.ReadEncodedInt();
                        Facet = reader.ReadMap();
                        TreasurePackage = (TreasurePackage)reader.ReadEncodedInt();
                        TreasureLevel = (TreasureLevel)reader.ReadEncodedInt();
                        Completed = reader.ReadBool();
                        CompletedBy = reader.ReadMobile();
                        Decoder = reader.ReadMobile();
                        ChestLocation = reader.ReadPoint2D();
                        NextReset = reader.ReadDateTime();
                        Price = reader.ReadEncodedInt();
                        break;
                    }
            }
        }

        public Item Reconstruct()
        {
            TreasureMap tmap = new TreasureMap();

            if (tmap != null)
            {
                tmap.Level = Level;
                tmap.Facet = Facet;

                if (TreasureMapInfo.NewSystem || TreasureMap.NewSystem)
                {
                    tmap.Package = TreasurePackage;
                    tmap.TreasureLevel = TreasureLevel;
                }

                tmap.ChestLocation = ChestLocation;
                tmap.AddWorldPin(ChestLocation.X, ChestLocation.Y);
                tmap.NextReset = NextReset;
                tmap.Completed = Completed;
                tmap.CompletedBy = CompletedBy;
                tmap.Decoder = Decoder;
            }

            return tmap;
        }

        public void Serialize(GenericWriter writer)
        {
            writer.WriteEncodedInt(0); // version
            writer.WriteEncodedInt((int)Level);
            writer.Write((Map)Facet);
            writer.WriteEncodedInt((int)TreasurePackage);
            writer.WriteEncodedInt((int)TreasureLevel);
            writer.Write((bool)Completed);
            writer.Write((Mobile)CompletedBy);
            writer.Write((Mobile)Decoder);
            writer.Write((Point2D)ChestLocation);
            writer.Write((DateTime)NextReset);
            writer.WriteEncodedInt((int)Price);
        }
    }

    public class SOSEntry
    {
        public int Level { get; set; }
        public Map TargetMap { get; set; }
        public Point3D TargetLocation { get; set; }
        public int MessageIndex { get; set; }
        public bool IsAncient { get; set; }
        public int Price { get; set; }
        public int Decoded { get { return 1; } }

        public SOSEntry(SOS sos)
        {
            Level = sos.Level;
            TargetMap = sos.TargetMap;
            TargetLocation = sos.TargetLocation;
            MessageIndex = sos.MessageIndex;
            IsAncient = sos.IsAncient;
        }

        public SOSEntry(GenericReader reader)
        {
            int version = reader.ReadEncodedInt();

            switch (version)
            {
                case 0:
                    {
                        Level = reader.ReadEncodedInt();
                        TargetMap = reader.ReadMap();
                        TargetLocation = reader.ReadPoint3D();
                        MessageIndex = reader.ReadEncodedInt();
                        IsAncient = reader.ReadBool();
                        Price = reader.ReadEncodedInt();
                        break;
                    }
            }
        }

        public Item Reconstruct()
        {
            SOS sos = new SOS();

            if (sos != null)
            {
                sos.Level = Level;
                sos.TargetMap = TargetMap;
                sos.TargetLocation = TargetLocation;
                sos.MessageIndex = MessageIndex;
                sos.UpdateHue();
            }

            return sos;
        }

        public void Serialize(GenericWriter writer)
        {
            writer.WriteEncodedInt(0); // version
            writer.WriteEncodedInt((int)Level);
            writer.Write((Map)TargetMap);
            writer.Write((Point3D)TargetLocation);
            writer.WriteEncodedInt((int)MessageIndex);
            writer.Write((bool)IsAncient);
            writer.WriteEncodedInt((int)Price);
        }
    }

    public class TMapFilter
    {
        public int Type { get; set; }
        public int Level { get; set; }
        public int Facet { get; set; }
        public int Decoded { get; set; }

        public TMapFilter()
        {
        }

        public TMapFilter(GenericReader reader)
        {
            int version = reader.ReadEncodedInt();

            switch (version)
            {
                case 0:
                    {
                        Type = reader.ReadEncodedInt();
                        Level = reader.ReadEncodedInt();
                        Facet = reader.ReadEncodedInt();
                        Decoded = reader.ReadEncodedInt();

                        break;
                    }
            }
        }

        public void Clear()
        {
            Type = 0;
            Level = 0;
            Facet = 0;
            Decoded = 0;
        }

        public bool IsDefault
        {
            get
            {
                return (Type == 0 && Level == 0 && Facet == 0 && Decoded == 0);
            }
        }

        public void Serialize(GenericWriter writer)
        {
            writer.WriteEncodedInt(0); // version
            writer.WriteEncodedInt(Type);
            writer.WriteEncodedInt(Level);
            writer.WriteEncodedInt(Facet);
            writer.WriteEncodedInt(Decoded);
        }
    }
}
