/*
 * UO Community Script: The Ultimate ARPG-Style Loot Filter
 * Source: https://www.servuo.dev/archive/the-ultimate-arpg-style-loot-filter-diablo-poe-exactly-like-you-want-it.2606/
 *
 * In-game help dialog detailing loot filter operations, global toggles, and safe filtering guarantees.
 */

using System;
using Server;
using Server.Gumps;
using Server.Mobiles;
using Server.Network;

namespace Server.Engines.LootFilter
{
    public class LootFilterInfoGump : Gump
    {
        private PlayerMobile m_Player;

        public LootFilterInfoGump(PlayerMobile pm) : base(150, 100)
        {
            m_Player = pm;

            AddPage(0);
            
            // Background
            AddBackground(0, 0, 450, 450, 9270);
            AddAlphaRegion(10, 10, 430, 430);

            // Header
            AddImageTiled(15, 15, 420, 30, 2624);
            AddAlphaRegion(15, 15, 420, 30);
            AddHtml(15, 20, 420, 20, "<BASEFONT COLOR=#FCCA03><CENTER>LOOT FILTER INFORMATION</CENTER></BASEFONT>", false, false);
            
            // Content text area
            AddHtml(20, 55, 410, 340, 
                "<BASEFONT COLOR=#FFFFFF>" +
                "The ARPG Loot Filter allows you to completely customize the items you see dropping in the world.<br><br>" +
                "<BASEFONT COLOR=#FCCA03>How to Use:</BASEFONT><br>" +
                "Use the checkboxes on the left side of any property to <BASEFONT COLOR=#00FF00>Enable</BASEFONT> or <BASEFONT COLOR=#FF0000>Disable</BASEFONT> tracking that property.<br><br>" +
                "If a property is <BASEFONT COLOR=#00FF00>Enabled</BASEFONT>, the filter will check every dropped weapon, armor, or jewelry. If the item does not have AT LEAST the value you specified, it will be instantly hidden from your screen.<br><br>" +
                "If a property is <BASEFONT COLOR=#FF0000>Disabled</BASEFONT>, the filter will completely ignore that property when deciding if an item should be shown.<br><br>" +
                "<BASEFONT COLOR=#FCCA03>Global Toggle:</BASEFONT><br>" +
                "The 'ENABLED' button at the top right of the main menu turns the ENTIRE filter on or off. If the filter is off, you will see all items normally regardless of your individual property settings.<br><br>" +
                "<BASEFONT COLOR=#FCCA03>Important Notes:</BASEFONT><br>" +
                "- The filter ONLY affects equipment (Weapons, Armor, Clothing, Jewelry). It will never hide Gold, Reagents, Potions, or other resources.<br>" +
                "- Items inside your Backpack or Bank Box will always remain visible, even if they don't pass your strict filter rules." +
                "</BASEFONT>", false, true);

            // Back Button
            AddButton(175, 405, 4005, 4007, 1, GumpButtonType.Reply, 0);
            AddHtml(210, 405, 100, 20, "<BASEFONT COLOR=#FFFFFF>Back to Filter</BASEFONT>", false, false);
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            if (info.ButtonID == 1) // Go Back
            {
                m_Player.SendGump(new LootFilterGump(m_Player));
            }
        }
    }
}
