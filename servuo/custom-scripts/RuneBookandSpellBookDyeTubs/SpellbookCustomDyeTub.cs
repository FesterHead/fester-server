/*
 * UO Community Script: Customizable Rune Book and Spell Book Dye Tubs (16 Preset Colors)
 * Source: https://www.servuo.dev/archive/customizable-rune-book-and-spell-book-dye-tubs-16-preset-colors.2642/
 * Compiled & Modified by: [Feng / UO Wildlands Team]
 * Licensed under the GNU General Public License v3.0 (GPL-3.0)
 *
 * Provides a customizable dye tub for spellbooks featuring 16 preset color palettes
 * with in-game swatch previews, individual palette slot customization, and default resets.
 */

using Server;
using System;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;
using Server.Network;
using Server.HuePickers;
using Server.Targeting;
using System.Collections.Generic;
using Server.ContextMenus;

namespace Server.Items
{    
    public class SpellbookCustomDyeTub : DyeTub, IDyesTargetHandler, Engines.VeteranRewards.IRewardItem
    {
        
        public class Default {
            // Use IN-GAME hue numbers, One higher than UoFiddler (By Default) or HueBrowser shows.
             public static readonly int[] Colors = new int[]
             {
                 37, 38,   // Reds
                 43, 48,   // Oranges
                 53, 55,   // Yellows
                 58, 67,   // Greens
                 2210,     // Spellweaving green
                 83,       // Teal
                 3,  93,   // Blues
                 13, 18,   // Purples
                 1,        // Black
                 2050      // White
             };
             public static int Length => Colors.Length;
        }

        private bool m_IsRewardItem;
        public int[] m_CustomColors;
        private bool m_ChangeMode = false;
        public override bool AllowDyables => false;

        [Constructable]
        public SpellbookCustomDyeTub()
        {
            LootType = LootType.Blessed;
            Name = "Custom Color Spellbook Dye Tub";

            m_CustomColors = new int[Default.Length];

            for (int i = 0; i < Default.Length; i++)
                m_CustomColors[i] = Default.Colors[i];

            int hue = m_CustomColors[Utility.Random(Default.Length)];
            Hue = hue;
            DyedHue = hue;
        }

        public SpellbookCustomDyeTub(Serial serial) : base(serial) { }


        [CommandProperty(AccessLevel.GameMaster)]
        public bool IsRewardItem
        {
            get { return m_IsRewardItem; }
            set { m_IsRewardItem = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int[] CustomColors
        {
            get { return m_CustomColors; }
            set { m_CustomColors = value; }
        }
        
        public bool ChangeMode
        {
            get { return m_ChangeMode; }
            set { m_ChangeMode = value; }
        }

        public void SetTubHue(int hue)
        {
            Hue = hue;
            DyedHue = hue;
            InvalidateProperties();
        }
        
        public override void GetContextMenuEntries(Mobile from, List<ContextMenuEntry> list)
        {
            base.GetContextMenuEntries(from, list);
            if (from.AccessLevel >= AccessLevel.GameMaster)
            {
                list.Add(new SpellbookCustomDyeTubEntry(from, this));
            }
        }
        
        public class SpellbookCustomDyeTubEntry : ContextMenuEntry
        {
            Mobile m_From;
            SpellbookCustomDyeTub m_Tub;
        
            public SpellbookCustomDyeTubEntry(Mobile from, SpellbookCustomDyeTub tub)
                : base(1151720, 10) // Set Hue
            {
                m_From = from;
                m_Tub = tub;
            }
        
            public override void OnClick()
            {
                if (m_From == null || m_Tub == null)
                {
                    return;
                }
                m_Tub.ChangeMode = false;
                m_From.CloseGump(typeof(SpellbookCustomHueGump));
                m_From.SendGump(new SpellbookCustomHueGump(m_From, m_Tub));
            }
        }
        
        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);
            list.Add($"Set to hue {DyedHue}");
        }
        
        public void SetCustomColor(int index, int hue)
        {
            if (index < 0 || index >= Default.Length)
                return;

            m_CustomColors[index] = hue;
        }
        
        public void OnDyesUsed(Mobile from, DyeTub tub)
        {
            from.SendGump(new SpellbookCustomHueGump(from, this));
        }
        


        public override void OnDoubleClick(Mobile from)
        {
            if (from.InRange(this.GetWorldLocation(), 3))
            {
                from.SendMessage("Select the item to dye");
                from.Target = new SpellbookCustomDyeTubTarget(this);
            }
            else
            {
                from.SendLocalizedMessage(500446); // That is too far away.
            }
        }

        public class SpellbookCustomDyeTubTarget : Target
        {
            private SpellbookCustomDyeTub m_Tub;

            public SpellbookCustomDyeTubTarget(SpellbookCustomDyeTub tub) : base(12, false, TargetFlags.None)
            {
                m_Tub = tub;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (targeted is Item)
                {
                    Item item = (Item)targeted;

                    if (item is Spellbook)
                    {
                        item.Hue = m_Tub.DyedHue;
                        from.PlaySound(0x23E);
                    }
                    else
                    {
                        from.SendMessage("That item cannot be dyed.");
                    }
                }
                else
                {
                    from.SendMessage("You cannot dye that.");
                }
            }
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0);
            writer.Write(m_IsRewardItem);
            writer.Write(Default.Length);

            for (int i = 0; i < Default.Length; i++)
                writer.Write(m_CustomColors[i]);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 0:
                {
                    m_IsRewardItem = reader.ReadBool();

                    int length = reader.ReadInt();
                    m_CustomColors = new int[length];

                    for (int i = 0; i < length; i++)
                        m_CustomColors[i] = reader.ReadInt();

                    break;
                }
            }
        }
    }

    public class SpellbookCustomHueGump : Gump
    {
        private Mobile m_From;
        private SpellbookCustomDyeTub m_Tub;
        
        public SpellbookCustomHueGump(Mobile from, SpellbookCustomDyeTub tub) : base(0, 0)
        {
            m_From = from;
            m_Tub = tub;
            int width = 625;
            int noHueButtonOffset = 380;
            int resetButtonOffset = 160;
            string CustomizeColorBtnText = tub.ChangeMode ? "Exit customization mode" : "Customize Colors";
            string GumpTitle = tub.ChangeMode ? "Select Palette Slot(s) To Change" : "Customizable Spellbook Dye Palette";

            Closable = true;
            Disposable = true;
            Dragable = true;

            AddPage(0);
            
            
            AddBackground(0, 0, width, 250, 9270);
            AddImageTiled(10, 10, 605, 230, 2624); // Black inset background
            AddHtml(0, 20, width, 40, String.Format("<BASEFONT COLOR=#FFFFFF><CENTER>{0}", GumpTitle), false, false);

            int index = 0;

            for (int y = 0; y < 2; y++)
            {
                for (int x = 0; x < 8; x++)
                {
                    int hue = m_Tub.CustomColors[index];

                    int px = 25 + (x * 70);
                    int py = 55 + (y * 75);
                    int color = 0;
                    string hueHex = "";
                    int btnOffset = 5;
                    int stripeWidth = 6;
                    int pixelOffset = 32;

                    AddButton(px, py, 2328, 2329, 1000 + index, GumpButtonType.Reply, 0);
                    // AddTooltip($"Hue: {hue}");
                    
                    for (color = 0; color < 32; color++) {
                        hueHex = GetHueHtml(hue, color);
                        AddHtml(px+btnOffset+color, py+btnOffset, 2, 50, $"<BODYBGCOLOR=#{hueHex}>", false, false);
                    }
                    color = 31;
                    hueHex = GetHueHtml(hue, color);
                    AddHtml(px+btnOffset+pixelOffset, py+btnOffset, stripeWidth, 50, $"<BODYBGCOLOR=#{hueHex}>", false, false);
                    for (color = 0; color < 32 ; color++) {
                        hueHex = GetHueHtml(hue, 31-color);
                        AddHtml(px+btnOffset+pixelOffset+stripeWidth+color, py+btnOffset, 2, 50, $"<BODYBGCOLOR=#{hueHex}>", false, false);
                    }

                    index++;
                }
            }
            
            AddButton(25, 200, 4005, 4007, 1, GumpButtonType.Reply, 0);
            AddHtml(60, 200, 300, 20, String.Format("<BASEFONT COLOR=#FFFFFF>{0}", CustomizeColorBtnText), false, false);
            AddButton(width-noHueButtonOffset, 200, 4005, 4007, 2, GumpButtonType.Reply, 0);
            AddHtml(width-noHueButtonOffset+35, 200, 200, 20, String.Format("<BASEFONT COLOR=#FFFFFF>{0}", "Remove Dyed Hue"), false, false);
            AddButton(width-resetButtonOffset, 200, 4005, 4007, 3, GumpButtonType.Reply, 0);
            AddHtml(width-resetButtonOffset+35, 200, 200, 20, String.Format("<BASEFONT COLOR=#FFFFFF>{0}", "Restore Default"), false, false);
        }
        
        private string GetHueHtml(int hue, int colorIndex = -1)
        {
            if (hue < 1)
                return "080808";
            hue -= 1; //Decrement hue to fix offset difference between Hues and in-game indices
            if (hue < 0 || hue >= Ultima.Hues.List.Length)
            {
                Console.WriteLine("GetHueHtml: hue=" + hue + " does not exist in Hues.List");
                return "080808";
            }
            Ultima.Hue hd = Ultima.Hues.List[hue];
            if (hd == null || hd.Colors == null || hd.Colors.Length == 0)
            {
                Console.WriteLine("GetHueHtml: hue=" + hue + " hd=null/empty");
                return "080808";
            }
            int mid = 0;
            if (colorIndex < 0) { mid = hd.Colors.Length / 2;}
            else            { mid = colorIndex;}
            if (mid < 0 || mid >= hd.Colors.Length)
            {
                Console.WriteLine("GetHueHtml: hue=" + hue + " mid out of range (" + mid + ")");
                return "080808";
            }
            var c = hd.GetColor(mid);
            if (c.IsEmpty || c.A <= 0)
            {
                Console.WriteLine("GetHueHtml: hue=" + hue + " mid color empty");
                return "080808";
            }
            int rgb = c.ToArgb() & 0x00FFFFFF;
            return rgb.ToString("X6");
        }
        
        public override void OnResponse(NetState sender, RelayInfo info)
        {
            if (m_Tub == null || m_Tub.Deleted)
                return;
        
            if (info.ButtonID >= 1000 && info.ButtonID < 2000)
            {
                int index = info.ButtonID - 1000;
        
                if (m_Tub.ChangeMode)
                {
                    m_From.SendHuePicker(new InternalPicker(m_From, m_Tub, index));
                    return;
                }
        
                m_Tub.SetTubHue(m_Tub.CustomColors[index]);
                m_From.CloseGump(typeof(SpellbookCustomHueGump));
                return;
            }
        
            switch (info.ButtonID)
            {
                case 1:
                {
                    m_Tub.ChangeMode = !m_Tub.ChangeMode;
                    m_From.SendGump(new SpellbookCustomHueGump(m_From, m_Tub));
                    break;
                }
                case 2:
                {
                    m_Tub.SetTubHue(0);
                    m_Tub.ChangeMode = false;
                    m_From.CloseGump(typeof(SpellbookCustomHueGump));
                    break;
                }
                case 3:
                {
                    for (int i = 0; i < SpellbookCustomDyeTub.Default.Length; i++)
                        m_Tub.SetCustomColor(i, SpellbookCustomDyeTub.Default.Colors[i]);
                    m_From.SendGump(new SpellbookCustomHueGump(m_From, m_Tub));
                    break;
                }
            }
        }

        private class InternalPicker : HuePicker
        {
            private readonly SpellbookCustomDyeTub m_Tub;
            private readonly int m_Index;
            private readonly Mobile m_From;

            public InternalPicker(Mobile from, SpellbookCustomDyeTub tub, int index)
                : base(tub.ItemID)
            {
                m_Tub = tub;
                m_Index = index;
                m_From = from;
            }

            public override void OnResponse(int hue)
            {
                if (m_Tub == null || m_Tub.Deleted)
                    return;
            
                m_Tub.SetCustomColor(m_Index, hue);
                m_From.SendGump(new SpellbookCustomHueGump(m_From, m_Tub));
            }
        }
    }
}