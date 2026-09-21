/************************************************************************************
* Community Script: MyStats (Modernized)                                            *
* Author: Feng (February 2026)                                                      *
* Source: https://www.servuo.dev/archive/mystats-modernized.2594/                    *
*                                                                                   *
* Quality of Life (QoL) player statistics and combat rating dashboard:              *
* - Clean, centralized gump displaying character attributes, combat ratings,        *
*   resistances, damage increases, and real skill values                            *
* - Invoked via [Stats command (or [Stats server for staff diagnostics)             *
************************************************************************************/

using System;
using System.Collections.Generic;
using Server;
using Server.Commands;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;
using Server.Network;
using Server.Accounting;
using Server.Engines.UOStore;
using Server.Engines.Points;

namespace Server.Gumps
{
    public class MyStatsCommand
    {
        [CallPriority(100)]
        public static void Initialize()
        {
            CommandSystem.Register("Stats", AccessLevel.Player, new CommandEventHandler(Stats_OnCommand));
        }

        [Usage("Stats [server]")]
        [Description("Opens the Player Statistics dashboard.")]
        public static void Stats_OnCommand(CommandEventArgs e)
        {
            Mobile from = e.Mobile;
            if (from == null || from.Deleted)
            {
                return;
            }

            if (from.AccessLevel >= AccessLevel.Counselor && e.Length > 0 && Insensitive.Equals(e.GetString(0), "server"))
            {
                SendServerStats(from);
                return;
            }

            from.CloseGump(typeof(PlayerStatsGump));
            from.SendGump(new PlayerStatsGump(from));
        }

        [Usage("serverstats")]
        [Description("View server connections, mobile count, and item count metrics.")]
        public static void ServerStats_OnCommand(CommandEventArgs e)
        {
            if (e.Mobile != null && !e.Mobile.Deleted)
            {
                SendServerStats(e.Mobile);
            }
        }

        public static void SendServerStats(Mobile m)
        {
            m.SendMessage("Open Connections: {0}", NetState.Instances.Count);
            m.SendMessage("Mobiles: {0}", World.Mobiles.Count);
            m.SendMessage("Items: {0}", World.Items.Count);
        }
    }

    public class PlayerStatsGump : Gump
    {
        private Mobile m_From;

        // Color Constants
        private const int C_YELLOW = 2213;
        private const int C_WHITE = 2036;
        private const int C_RED = 1258;
        private const int C_BLUE = 1264;
        private const int C_GREEN = 1270;
        private const int C_PURPLE = 1276;
        private const int C_GREY = 2401;

        public PlayerStatsGump(Mobile from) : base(50, 50)
        {
            m_From = from;
            AddGumpLayout();
        }

        public void AddGumpLayout()
        {
            int width = 640;
            int height = 920; // Increased to 920 to fit the new rows comfortably

            AddPage(0);
            AddBackground(0, 0, width, height, 9200);
            AddImageTiled(10, 10, width - 20, height - 20, 2624);
            AddAlphaRegion(10, 10, width - 20, height - 20);

            // --- HEADER ---
            // Name (Centered Top)
            AddHtml(0, 25, width, 25, ColorAndCenter("#FFCC00", m_From.Name), false, false);

            // Top Right Buttons
            AddButton(width - 75, 21, 4005, 4007, 1, GumpButtonType.Reply, 0); // Sync Button
            AddButton(width - 40, 20, 0xFB1, 0xFB3, 0, GumpButtonType.Reply, 0); // Close Button

            // --- ATTRIBUTES ---
            // Aligned Left (30) and Right (340)
            int attrY = 55;

            AddLabel(30, attrY, C_YELLOW, "Attributes");

            DrawStat(30, attrY + 25, "Strength", m_From.Str.ToString());
            DrawStat(340, attrY + 25, "Hit Points", m_From.Hits + " / " + m_From.HitsMax);

            DrawStat(30, attrY + 50, "Dexterity", m_From.Dex.ToString());
            DrawStat(340, attrY + 50, "Stamina", m_From.Stam + " / " + m_From.StamMax);

            DrawStat(30, attrY + 75, "Intelligence", m_From.Int.ToString());
            DrawStat(340, attrY + 75, "Mana", m_From.Mana + " / " + m_From.ManaMax);

            // --- RESISTANCES ---
            int resY = 175;
            DrawDivider(20, resY - 5, width - 40);
            AddLabel(30, resY + 5, C_YELLOW, "Resistances");

            DrawResistBox(130, resY + 5, "Phys", m_From.PhysicalResistance, C_GREY);
            DrawResistBox(230, resY + 5, "Fire", m_From.FireResistance, C_RED);
            DrawResistBox(330, resY + 5, "Cold", m_From.ColdResistance, C_BLUE);
            DrawResistBox(430, resY + 5, "Poison", m_From.PoisonResistance, C_GREEN);
            DrawResistBox(530, resY + 5, "Energy", m_From.EnergyResistance, C_PURPLE);

            // --- RATINGS (Combat & Magic) ---
            int colY = 220;
            DrawDivider(20, colY - 5, width - 40);
            AddLabel(30, colY + 5, C_YELLOW, "Combat Ratings");
            AddLabel(340, colY + 5, C_YELLOW, "Magic Ratings");

            // AOS Fetches
            int hci = AosAttributes.GetValue(m_From, AosAttribute.AttackChance);
            int dci = AosAttributes.GetValue(m_From, AosAttribute.DefendChance);
            int di = AosAttributes.GetValue(m_From, AosAttribute.WeaponDamage);
            int ssi = AosAttributes.GetValue(m_From, AosAttribute.WeaponSpeed);
            int sdi = AosAttributes.GetValue(m_From, AosAttribute.SpellDamage);
            int lmc = AosAttributes.GetValue(m_From, AosAttribute.LowerManaCost);
            int lrc = AosAttributes.GetValue(m_From, AosAttribute.LowerRegCost);
            int fc = AosAttributes.GetValue(m_From, AosAttribute.CastSpeed);
            int fcr = AosAttributes.GetValue(m_From, AosAttribute.CastRecovery);

            // Custom Fetches
            double swingSpeed = 0.0;
            if (m_From.Weapon is Server.Items.BaseWeapon)
                swingSpeed = ((Server.Items.BaseWeapon)m_From.Weapon).GetDelay(m_From).TotalSeconds;

            double bandageSeconds = BandageContext.GetDelay(m_From, m_From, false).TotalSeconds;

            // New Attribute Fetches
            int hpInc = AosAttributes.GetValue(m_From, AosAttribute.BonusHits);
            int stamInc = AosAttributes.GetValue(m_From, AosAttribute.BonusStam);
            int manaInc = AosAttributes.GetValue(m_From, AosAttribute.BonusMana);

            int strBon = AosAttributes.GetValue(m_From, AosAttribute.BonusStr);
            int dexBon = AosAttributes.GetValue(m_From, AosAttribute.BonusDex);
            int intBon = AosAttributes.GetValue(m_From, AosAttribute.BonusInt);

            int enhPot = AosAttributes.GetValue(m_From, AosAttribute.EnhancePotions);
            int lAmmo = AosAttributes.GetValue(m_From, AosAttribute.LowerAmmoCost);

            // Calculate Total Damage Eater from Equipped Gear
            int dmgEater = 0;
            foreach (Item item in m_From.Items)
            {
                if (item is BaseArmor) dmgEater += ((BaseArmor)item).AbsorptionAttributes[SAAbsorptionAttribute.EaterDamage];
                else if (item is BaseJewel) dmgEater += ((BaseJewel)item).AbsorptionAttributes[SAAbsorptionAttribute.EaterDamage];
                else if (item is BaseWeapon) dmgEater += ((BaseWeapon)item).AbsorptionAttributes[SAAbsorptionAttribute.EaterDamage];
                else if (item is BaseClothing) dmgEater += ((BaseClothing)item).SAAbsorptionAttributes[SAAbsorptionAttribute.EaterDamage];
            }
            int tithing = (m_From as PlayerMobile)?.TithingPoints ?? 0;

            // Account Gold & Followers
            double totalGold = 0;
            if (m_From.Account is Account acct)
            {
                int goldStub;
                acct.GetGoldBalance(out goldStub, out totalGold);
            }
            string followers = m_From.Followers + " / " + m_From.FollowersMax;

            long sovereigns = UltimaStore.GetCurrency(m_From);
            double cubPoints = PointsSystem.CleanUpBritannia.GetPoints(m_From);

            // Left Side: Combat
            DrawStat(30, colY + 30, "HCI", hci + " / 45");
            DrawStat(30, colY + 50, "DCI", dci + " / 45");
            DrawStat(30, colY + 70, "DI", di + " / 100");
            DrawStat(30, colY + 90, "SSI", ssi + " / 60");

            DrawStat(30, colY + 130, "Swing Speed", swingSpeed > 0 ? swingSpeed.ToString("F2") + "s" : "---");
            DrawStat(30, colY + 150, "Bandage Speed", bandageSeconds.ToString("F1") + "s");

            DrawStat(30, colY + 185, "HP Regen", AosAttributes.GetValue(m_From, AosAttribute.RegenHits).ToString());
            DrawStat(30, colY + 205, "Stam Regen", AosAttributes.GetValue(m_From, AosAttribute.RegenStam).ToString());
            DrawStat(30, colY + 225, "Mana Regen", AosAttributes.GetValue(m_From, AosAttribute.RegenMana).ToString());

            // Group 1: Increases
            DrawStat(30, colY + 265, "HP Increase", hpInc + " / 25");
            DrawStat(30, colY + 285, "Stam Increase", stamInc + " / 25");
            DrawStat(30, colY + 305, "Mana Increase", manaInc + " / 25");

            // Group 2: Stat Bonus
            DrawStat(30, colY + 345, "Str Bonus", strBon.ToString());
            DrawStat(30, colY + 365, "Dex Bonus", dexBon.ToString());
            DrawStat(30, colY + 385, "Int Bonus", intBon.ToString());

            // Group 3: Utility
            DrawStat(30, colY + 425, "Damage Eater", dmgEater + " / 30");
            DrawStat(30, colY + 445, "Enhance Pots", enhPot + " / 50");
            DrawStat(30, colY + 465, "Lower Ammo", lAmmo + "");

            DrawStat(30, colY + 505, "Followers", followers);
            DrawStat(30, colY + 525, "Weight", m_From.TotalWeight + " / " + m_From.MaxWeight);

            // Right Side: Magic
            DrawStat(340, colY + 30, "SDI", sdi + "%");
            DrawStat(340, colY + 50, "LRC", lrc + "%");
            DrawStat(340, colY + 70, "LMC", lmc + "%");
            DrawStat(340, colY + 90, "FC", fc + " / 4");
            DrawStat(340, colY + 110, "FCR", fcr + " / 6");

            // Miscellaneous Section
            AddLabel(340, colY + 140, C_YELLOW, "Miscellaneous");
            DrawStat(340, colY + 165, "Luck", AosAttributes.GetValue(m_From, AosAttribute.Luck).ToString());
            DrawStat(340, colY + 185, "Fame", m_From.Fame.ToString());
            DrawStat(340, colY + 205, "Karma", m_From.Karma.ToString());
            DrawStat(340, colY + 225, "Tithing", tithing.ToString());

            DrawStat(340, colY + 265, "Acct Gold", totalGold.ToString("N0"));
            DrawStat(340, colY + 285, "Sovereigns", sovereigns.ToString("N0"));
            DrawStat(340, colY + 305, "CUB Points", cubPoints.ToString("N0"));

            // --- SKILL LIST ---
            int skillY = colY + 365;
            AddLabel(340, skillY - 20, C_YELLOW, "Real Skills");

            int count = 0;
            int startX = 340;
            int startY = skillY + 10;
            int rowSpacing = 20;

            for (int i = 0; i < m_From.Skills.Length; i++)
            {
                Skill sk = m_From.Skills[i];

                // Filters out Human Jack of All Trades (20.0) unless base is invested or gear pushes it higher
                double humanMinimum = (m_From.Race == Race.Human) ? 20.0 : 0.0;

                if (sk.Base > 0 || (sk.Value > sk.Base && sk.Value > humanMinimum))
                {
                    int yPos = startY + (count * rowSpacing);
                    string val = sk.Value.ToString("F1") + " / " + sk.Cap.ToString("F1");

                    AddLabel(startX, yPos, C_WHITE, sk.Name);
                    AddLabel(startX + 130, yPos, C_WHITE, val);

                    count++;
                }
            }
        }

        // --- HELPERS ---

        private string ColorAndCenter(string color, string text)
        {
            return String.Format("<BASEFONT COLOR={0}><CENTER>{1}</CENTER></BASEFONT>", color, text);
        }

        private void DrawResistBox(int x, int y, string name, int val, int color)
        {
            AddLabel(x, y, C_WHITE, name);
            AddLabel(x + 55, y, color, val + "%");
        }

        private void DrawStat(int x, int y, string name, string val)
        {
            AddLabel(x, y, C_WHITE, name);
            AddLabel(x + 130, y, C_WHITE, val);
        }

        private void DrawDivider(int x, int y, int width)
        {
            AddImageTiled(x, y, width, 2, 9264);
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            if (info.ButtonID == 1) // Refresh
            {
                m_From.SendGump(new PlayerStatsGump(m_From));
            }
        }
    }
}
