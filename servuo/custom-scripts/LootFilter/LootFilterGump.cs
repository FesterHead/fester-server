/*
 * UO Community Script: The Ultimate ARPG-Style Loot Filter
 * Source: https://www.servuo.dev/archive/the-ultimate-arpg-style-loot-filter-diablo-poe-exactly-like-you-want-it.2606/
 *
 * Interactive paginated configuration gump providing toggles and threshold entry
 * for 87+ weapon, armor, jewelry, and clothing item properties. Registers [LootFilter command.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using Server;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;
using Server.Network;
using Server.Commands;

namespace Server.Engines.LootFilter
{
    public class LootFilterGump : Gump
    {
        private PlayerMobile m_Player;
        private LootFilterAttachment m_Attachment;
        private LootFilterCategory m_Category;

        public static void Initialize()
        {
            CommandSystem.Register("LootFilter", AccessLevel.Player, new CommandEventHandler(LootFilter_OnCommand));
        }

        [Usage("LootFilter")]
        [Description("Opens the loot filter interface.")]
        private static void LootFilter_OnCommand(CommandEventArgs e)
        {
            PlayerMobile pm = e.Mobile as PlayerMobile;
            if (pm != null)
            {
                pm.CloseGump(typeof(LootFilterGump));
                pm.SendGump(new LootFilterGump(pm));
            }
        }

        private int m_RowIndex;

        public LootFilterGump(PlayerMobile pm, LootFilterCategory category = LootFilterCategory.Primary) : base(50, 50)
        {
            m_Player = pm;
            m_Attachment = LootFilterAttachment.GetAttachment(pm);
            m_Category = category;

            AddPage(0);
            
            // Main Window
            AddBackground(0, 0, 600, 580, 9270);
            AddAlphaRegion(10, 10, 580, 560);

            // Header Frame
            AddImageTiled(15, 15, 570, 40, 2624);
            AddAlphaRegion(15, 15, 570, 40);

            // Title
            AddHtml(15, 25, 420, 20, "<BASEFONT COLOR=#FCCA03><CENTER>ULTIMATE ARPG LOOT FILTER</CENTER></BASEFONT>", false, false);
            
            // Info Button
            AddButton(370, 25, 4011, 4013, 2, GumpButtonType.Reply, 0);
            AddHtml(405, 25, 45, 20, "<BASEFONT COLOR=#00BFFF>INFO</BASEFONT>", false, false);

            // Toggle Filter
            bool globalEnabled = m_Attachment.Settings.Enabled;
            AddHtml(460, 25, 70, 20, globalEnabled ? "<BASEFONT COLOR=#00FF00>ENABLED</BASEFONT>" : "<BASEFONT COLOR=#FF0000>DISABLED</BASEFONT>", false, false);
            AddButton(535, 25, globalEnabled ? 2154 : 2151, globalEnabled ? 2154 : 2151, 1, GumpButtonType.Reply, 0);

            // Left Category Frame
            AddImageTiled(15, 65, 140, 500, 2624);
            AddAlphaRegion(15, 65, 140, 500);

            int y = 75;
            AddCategoryButton(25, ref y, "Primary", LootFilterCategory.Primary);
            AddCategoryButton(25, ref y, "Combat", LootFilterCategory.Combat);
            AddCategoryButton(25, ref y, "Magic", LootFilterCategory.Magic);
            AddCategoryButton(25, ref y, "Resistances", LootFilterCategory.Resists);
            AddCategoryButton(25, ref y, "Damage", LootFilterCategory.Damage);
            AddCategoryButton(25, ref y, "Hit Leech", LootFilterCategory.HitsLeech);
            AddCategoryButton(25, ref y, "Hit Magic", LootFilterCategory.HitsMagic);
            AddCategoryButton(25, ref y, "Special", LootFilterCategory.Special);

            // Right Content Frame
            AddImageTiled(165, 65, 420, 500, 2624);
            AddAlphaRegion(165, 65, 420, 500);

            // Content Headers
            AddHtml(180, 75, 50, 20, "<BASEFONT COLOR=#FFFFFF>On/Off</BASEFONT>", false, false);
            AddHtml(235, 75, 200, 20, "<BASEFONT COLOR=#FFFFFF>Property Name</BASEFONT>", false, false);
            AddHtml(430, 75, 60, 20, "<BASEFONT COLOR=#FFFFFF><CENTER>Minimum</CENTER></BASEFONT>", false, false);
            AddHtml(510, 75, 60, 20, "<BASEFONT COLOR=#FFFFFF><CENTER>Adjust</CENTER></BASEFONT>", false, false);
            
            // Header Divider
            AddImageTiled(175, 95, 400, 2, 2624);
            AddAlphaRegion(175, 95, 400, 2);

            RenderCategory(m_Category);
        }

        private void AddCategoryButton(int x, ref int y, string label, LootFilterCategory cat)
        {
            bool active = m_Category == cat;
            
            if (active)
            {
                AddImageTiled(x - 5, y - 5, 120, 42, 2624);
                AddAlphaRegion(x - 5, y - 5, 120, 42);
            }

            AddButton(x, y + 6, active ? 4006 : 4005, active ? 4007 : 4006, 10 + (int)cat, GumpButtonType.Reply, 0);
            
            AddHtml(x + 35, y + 6, 90, 40, active ? $"<BASEFONT COLOR=#FCCA03>{label}</BASEFONT>" : $"<BASEFONT COLOR=#999999>{label}</BASEFONT>", false, false);

            y += 45;

            // Category Divider
            if (cat != LootFilterCategory.Special)
            {
                AddImageTiled(x - 5, y, 120, 2, 2624);
                AddAlphaRegion(x - 5, y, 120, 2);
            }
            y += 10;
        }

        private void RenderCategory(LootFilterCategory cat)
        {
            int y = 105;
            int x = 180;
            m_RowIndex = 0;
            switch (cat)
            {
                case LootFilterCategory.Primary:
                    AddAttributeEntry(x, ref y, AosAttribute.BonusStr, "Strength");
                    AddAttributeEntry(x, ref y, AosAttribute.BonusDex, "Dexterity");
                    AddAttributeEntry(x, ref y, AosAttribute.BonusInt, "Intelligence");
                    AddAttributeEntry(x, ref y, AosAttribute.BonusHits, "Hit Points");
                    AddAttributeEntry(x, ref y, AosAttribute.BonusStam, "Stamina");
                    AddAttributeEntry(x, ref y, AosAttribute.BonusMana, "Mana");
                    AddAttributeEntry(x, ref y, AosAttribute.RegenHits, "Hit Point Regeneration");
                    AddAttributeEntry(x, ref y, AosAttribute.RegenStam, "Stamina Regeneration");
                    AddAttributeEntry(x, ref y, AosAttribute.RegenMana, "Mana Regeneration");
                    AddAttributeEntry(x, ref y, AosAttribute.Luck, "Luck");
                    AddAttributeEntry(x, ref y, AosAttribute.NightSight, "Night Sight");
                    break;
                case LootFilterCategory.Combat:
                    AddAttributeEntry(x, ref y, AosAttribute.AttackChance, "Hit Chance Increase");
                    AddAttributeEntry(x, ref y, AosAttribute.DefendChance, "Defense Chance Increase");
                    AddAttributeEntry(x, ref y, AosAttribute.WeaponDamage, "Damage Increase");
                    AddAttributeEntry(x, ref y, AosAttribute.WeaponSpeed, "Swing Speed Increase");
                    AddAttributeEntry(x, ref y, AosAttribute.ReflectPhysical, "Reflect Physical Damage");
                    AddWeaponAttributeEntry(x, ref y, AosWeaponAttribute.BattleLust, "Battle Lust");
                    AddAbsorptionAttributeEntry(x, ref y, SAAbsorptionAttribute.ResonanceFire, "Resonance: Fire");
                    AddAbsorptionAttributeEntry(x, ref y, SAAbsorptionAttribute.ResonanceCold, "Resonance: Cold");
                    AddAbsorptionAttributeEntry(x, ref y, SAAbsorptionAttribute.ResonancePoison, "Resonance: Poison");
                    AddAbsorptionAttributeEntry(x, ref y, SAAbsorptionAttribute.ResonanceEnergy, "Resonance: Energy");
                    AddAbsorptionAttributeEntry(x, ref y, SAAbsorptionAttribute.ResonanceKinetic, "Resonance: Kinetic");
                    break;
                case LootFilterCategory.Magic:
                    AddAttributeEntry(x, ref y, AosAttribute.SpellDamage, "Spell Damage Increase");
                    AddAttributeEntry(x, ref y, AosAttribute.CastSpeed, "Faster Casting");
                    AddAttributeEntry(x, ref y, AosAttribute.CastRecovery, "Faster Cast Recovery");
                    AddAttributeEntry(x, ref y, AosAttribute.LowerManaCost, "Lower Mana Cost");
                    AddAttributeEntry(x, ref y, AosAttribute.LowerRegCost, "Lower Reagent Cost");
                    AddAttributeEntry(x, ref y, AosAttribute.SpellChanneling, "Spell Channeling");
                    AddAbsorptionAttributeEntry(x, ref y, SAAbsorptionAttribute.CastingFocus, "Casting Focus");
                    AddArmorAttributeEntry(x, ref y, AosArmorAttribute.SoulCharge, "Soul Charge");
                    AddAttributeEntry(x, ref y, AosAttribute.EnhancePotions, "Enhance Potions");
                    break;
                case LootFilterCategory.Resists:
                    AddResistEntry(x, ref y, "Physical Resistance", 100);
                    AddResistEntry(x, ref y, "Fire Resistance", 101);
                    AddResistEntry(x, ref y, "Cold Resistance", 102);
                    AddResistEntry(x, ref y, "Poison Resistance", 103);
                    AddResistEntry(x, ref y, "Energy Resistance", 104);
                    break;
                case LootFilterCategory.Damage:
                    AddDamageEntry(x, ref y, "Physical Damage %", 200);
                    AddDamageEntry(x, ref y, "Fire Damage %", 201);
                    AddDamageEntry(x, ref y, "Cold Damage %", 202);
                    AddDamageEntry(x, ref y, "Poison Damage %", 203);
                    AddDamageEntry(x, ref y, "Energy Damage %", 204);
                    AddDamageEntry(x, ref y, "Chaos Damage %", 205);
                    AddDamageEntry(x, ref y, "Direct Damage %", 206);
                    break;
                case LootFilterCategory.HitsLeech:
                    AddWeaponAttributeEntry(x, ref y, AosWeaponAttribute.HitLeechHits, "Hit Life Leech");
                    AddWeaponAttributeEntry(x, ref y, AosWeaponAttribute.HitLeechStam, "Hit Stamina Leech");
                    AddWeaponAttributeEntry(x, ref y, AosWeaponAttribute.HitLeechMana, "Hit Mana Leech");
                    AddWeaponAttributeEntry(x, ref y, AosWeaponAttribute.HitLowerAttack, "Hit Lower Attack");
                    AddWeaponAttributeEntry(x, ref y, AosWeaponAttribute.HitLowerDefend, "Hit Lower Defense");
                    AddWeaponAttributeEntry(x, ref y, AosWeaponAttribute.HitManaDrain, "Hit Mana Drain");
                    AddWeaponAttributeEntry(x, ref y, AosWeaponAttribute.HitFatigue, "Hit Fatigue");
                    break;
                case LootFilterCategory.HitsMagic:
                    AddWeaponAttributeEntry(x, ref y, AosWeaponAttribute.HitMagicArrow, "Hit Magic Arrow");
                    AddWeaponAttributeEntry(x, ref y, AosWeaponAttribute.HitHarm, "Hit Harm");
                    AddWeaponAttributeEntry(x, ref y, AosWeaponAttribute.HitFireball, "Hit Fireball");
                    AddWeaponAttributeEntry(x, ref y, AosWeaponAttribute.HitLightning, "Hit Lightning");
                    AddWeaponAttributeEntry(x, ref y, AosWeaponAttribute.HitCurse, "Hit Curse");
                    AddWeaponAttributeEntry(x, ref y, AosWeaponAttribute.HitPhysicalArea, "Hit Physical Area");
                    AddWeaponAttributeEntry(x, ref y, AosWeaponAttribute.HitFireArea, "Hit Fire Area");
                    AddWeaponAttributeEntry(x, ref y, AosWeaponAttribute.HitColdArea, "Hit Cold Area");
                    AddWeaponAttributeEntry(x, ref y, AosWeaponAttribute.HitPoisonArea, "Hit Poison Area");
                    AddWeaponAttributeEntry(x, ref y, AosWeaponAttribute.HitEnergyArea, "Hit Energy Area");
                    break;
                case LootFilterCategory.Special:
                    AddWeaponAttributeEntry(x, ref y, AosWeaponAttribute.SelfRepair, "Self Repair");
                    AddWeaponAttributeEntry(x, ref y, AosWeaponAttribute.SplinteringWeapon, "Splintering Weapon");
                    AddWeaponAttributeEntry(x, ref y, AosWeaponAttribute.BloodDrinker, "Blood Drinker");
                    AddExtendedWeaponAttributeEntry(x, ref y, ExtendedWeaponAttribute.Bane, "Bane");
                    AddAbsorptionAttributeEntry(x, ref y, SAAbsorptionAttribute.EaterDamage, "Damage Eater");
                    AddAbsorptionAttributeEntry(x, ref y, SAAbsorptionAttribute.EaterFire, "Fire Eater");
                    AddAbsorptionAttributeEntry(x, ref y, SAAbsorptionAttribute.EaterCold, "Cold Eater");
                    AddAbsorptionAttributeEntry(x, ref y, SAAbsorptionAttribute.EaterPoison, "Poison Eater");
                    AddAbsorptionAttributeEntry(x, ref y, SAAbsorptionAttribute.EaterEnergy, "Energy Eater");
                    AddAbsorptionAttributeEntry(x, ref y, SAAbsorptionAttribute.EaterKinetic, "Kinetic Eater");
                    break;
            }
        }

        private void AddAttributeEntry(int x, ref int y, AosAttribute attr, string name)
        {
            int val = 0;
            m_Attachment.Settings.Attributes.TryGetValue(attr, out val);
            bool isEnabled = m_Attachment.Settings.EnabledAttributes.Contains(attr);
            AddEntry(x, ref y, name, val, isEnabled, 15000 + (int)attr, 1000 + (int)attr, 2000 + (int)attr);
        }

        private void AddWeaponAttributeEntry(int x, ref int y, AosWeaponAttribute attr, string name)
        {
            int val = 0;
            m_Attachment.Settings.WeaponAttributes.TryGetValue(attr, out val);
            bool isEnabled = m_Attachment.Settings.EnabledWeaponAttributes.Contains(attr);
            AddEntry(x, ref y, name, val, isEnabled, 16000 + (int)attr, 3000 + (int)attr, 4000 + (int)attr);
        }

        private void AddArmorAttributeEntry(int x, ref int y, AosArmorAttribute attr, string name)
        {
            int val = 0;
            m_Attachment.Settings.ArmorAttributes.TryGetValue(attr, out val);
            bool isEnabled = m_Attachment.Settings.EnabledArmorAttributes.Contains(attr);
            AddEntry(x, ref y, name, val, isEnabled, 21000 + (int)attr, 11000 + (int)attr, 12000 + (int)attr);
        }

        private void AddExtendedWeaponAttributeEntry(int x, ref int y, ExtendedWeaponAttribute attr, string name)
        {
            int val = 0;
            m_Attachment.Settings.ExtendedWeaponAttributes.TryGetValue(attr, out val);
            bool isEnabled = m_Attachment.Settings.EnabledExtendedWeaponAttributes.Contains(attr);
            AddEntry(x, ref y, name, val, isEnabled, 19000 + (int)attr, 9000 + (int)attr, 10000 + (int)attr);
        }

        private void AddAbsorptionAttributeEntry(int x, ref int y, SAAbsorptionAttribute attr, string name)
        {
            int val = 0;
            m_Attachment.Settings.AbsorptionAttributes.TryGetValue(attr, out val);
            bool isEnabled = m_Attachment.Settings.EnabledAbsorptionAttributes.Contains(attr);
            AddEntry(x, ref y, name, val, isEnabled, 23000 + (int)attr, 13000 + (int)attr, 14000 + (int)attr);
        }

        private void AddEntry(int x, ref int y, string name, int val, bool isEnabled, int btnToggle, int btnIncr, int btnDecr)
        {
            m_RowIndex++;
            if (m_RowIndex % 2 != 0)
            {
                AddImageTiled(x - 10, y - 5, 410, 35, 2624);
                AddAlphaRegion(x - 10, y - 5, 410, 35);
            }

            AddButton(x, y + 2, isEnabled ? 2154 : 2151, isEnabled ? 2154 : 2151, btnToggle, GumpButtonType.Reply, 0);
            
            AddHtml(x + 55, y + 2, 200, 20, isEnabled ? $"<BASEFONT COLOR=#FFFFFF>{name}</BASEFONT>" : $"<BASEFONT COLOR=#777777>{name}</BASEFONT>", false, false);
            
            AddImageTiled(x + 250, y, 60, 24, 2624);
            AddAlphaRegion(x + 250, y, 60, 24);
            
            AddHtml(x + 250, y + 2, 60, 20, $"<BASEFONT COLOR=#FCCA03><CENTER>{val}</CENTER></BASEFONT>", false, false);
            
            AddButton(x + 335, y + 3, 2435, 2436, btnIncr, GumpButtonType.Reply, 0);
            AddButton(x + 365, y + 3, 2437, 2438, btnDecr, GumpButtonType.Reply, 0);
            
            y += 35;
        }

        private void AddResistEntry(int x, ref int y, string name, int id)
        {
            int val = 0;
            if (id == 100) val = m_Attachment.Settings.MinResistPhysical;
            else if (id == 101) val = m_Attachment.Settings.MinResistFire;
            else if (id == 102) val = m_Attachment.Settings.MinResistCold;
            else if (id == 103) val = m_Attachment.Settings.MinResistPoison;
            else if (id == 104) val = m_Attachment.Settings.MinResistEnergy;
            
            bool isEnabled = m_Attachment.Settings.EnabledResistances[id - 100];
            AddEntry(x, ref y, name, val, isEnabled, 17000 + id, 5000 + id, 6000 + id);
        }

        private void AddDamageEntry(int x, ref int y, string name, int id)
        {
            int val = 0;
            if (id == 200) val = m_Attachment.Settings.MinDamagePhysical;
            else if (id == 201) val = m_Attachment.Settings.MinDamageFire;
            else if (id == 202) val = m_Attachment.Settings.MinDamageCold;
            else if (id == 203) val = m_Attachment.Settings.MinDamagePoison;
            else if (id == 204) val = m_Attachment.Settings.MinDamageEnergy;
            else if (id == 205) val = m_Attachment.Settings.MinDamageChaos;
            else if (id == 206) val = m_Attachment.Settings.MinDamageDirect;

            bool isEnabled = m_Attachment.Settings.EnabledDamages[id - 200];
            AddEntry(x, ref y, name, val, isEnabled, 18000 + id, 7000 + id, 8000 + id);
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            int id = info.ButtonID;

            if (id == 1) // Enable/Disable Global
            {
                m_Attachment.Settings.Enabled = !m_Attachment.Settings.Enabled;
                m_Player.SendGump(new LootFilterGump(m_Player, m_Category));
                return;
            }

            if (id == 2) // Info button
            {
                m_Player.CloseGump(typeof(LootFilterInfoGump));
                m_Player.SendGump(new LootFilterInfoGump(m_Player));
                return;
            }

            if (id >= 10 && id < 20) // Change Category
            {
                m_Category = (LootFilterCategory)(id - 10);
                m_Player.SendGump(new LootFilterGump(m_Player, m_Category));
                return;
            }

            // Increments and Decrements
            if (id >= 1000 && id < 2000) { HandleAttr(id - 1000, 1); return; }
            if (id >= 2000 && id < 3000) { HandleAttr(id - 2000, -1); return; }
            if (id >= 3000 && id < 4000) { HandleWepAttr(id - 3000, 1); return; }
            if (id >= 4000 && id < 5000) { HandleWepAttr(id - 4000, -1); return; }
            if (id >= 5100 && id <= 5104) { HandleResist(id - 5000, 1); return; }
            if (id >= 6100 && id <= 6104) { HandleResist(id - 6000, -1); return; }
            if (id >= 7200 && id <= 7206) { HandleDam(id - 7000, 1); return; }
            if (id >= 8200 && id <= 8206) { HandleDam(id - 8000, -1); return; }
            if (id >= 9000 && id < 10000) { HandleExtWepAttr(id - 9000, 1); return; }
            if (id >= 10000 && id < 11000) { HandleExtWepAttr(id - 10000, -1); return; }
            if (id >= 11000 && id < 12000) { HandleArmAttr(id - 11000, 1); return; }
            if (id >= 12000 && id < 13000) { HandleArmAttr(id - 12000, -1); return; }
            if (id >= 13000 && id < 14000) { HandleAbsorpAttr(id - 13000, 1); return; }
            if (id >= 14000 && id < 15000) { HandleAbsorpAttr(id - 14000, -1); return; }

            // Toggles
            if (id >= 15000 && id < 16000) { ToggleAttr(id - 15000); return; }
            if (id >= 16000 && id < 17000) { ToggleWepAttr(id - 16000); return; }
            if (id >= 17100 && id <= 17104) { ToggleResist(id - 17000); return; }
            if (id >= 18200 && id <= 18206) { ToggleDam(id - 18000); return; }
            if (id >= 19000 && id < 20000) { ToggleExtWepAttr(id - 19000); return; }
            if (id >= 21000 && id < 22000) { ToggleArmAttr(id - 21000); return; }
            if (id >= 23000 && id < 24000) { ToggleAbsorpAttr(id - 23000); return; }
        }

        private void ToggleAttr(int attrId)
        {
            var attr = (AosAttribute)attrId;
            if (m_Attachment.Settings.EnabledAttributes.Contains(attr)) m_Attachment.Settings.EnabledAttributes.Remove(attr);
            else m_Attachment.Settings.EnabledAttributes.Add(attr);
            m_Player.SendGump(new LootFilterGump(m_Player, m_Category));
        }

        private void ToggleWepAttr(int attrId)
        {
            var attr = (AosWeaponAttribute)attrId;
            if (m_Attachment.Settings.EnabledWeaponAttributes.Contains(attr)) m_Attachment.Settings.EnabledWeaponAttributes.Remove(attr);
            else m_Attachment.Settings.EnabledWeaponAttributes.Add(attr);
            m_Player.SendGump(new LootFilterGump(m_Player, m_Category));
        }

        private void ToggleArmAttr(int attrId)
        {
            var attr = (AosArmorAttribute)attrId;
            if (m_Attachment.Settings.EnabledArmorAttributes.Contains(attr)) m_Attachment.Settings.EnabledArmorAttributes.Remove(attr);
            else m_Attachment.Settings.EnabledArmorAttributes.Add(attr);
            m_Player.SendGump(new LootFilterGump(m_Player, m_Category));
        }

        private void ToggleExtWepAttr(int attrId)
        {
            var attr = (ExtendedWeaponAttribute)attrId;
            if (m_Attachment.Settings.EnabledExtendedWeaponAttributes.Contains(attr)) m_Attachment.Settings.EnabledExtendedWeaponAttributes.Remove(attr);
            else m_Attachment.Settings.EnabledExtendedWeaponAttributes.Add(attr);
            m_Player.SendGump(new LootFilterGump(m_Player, m_Category));
        }

        private void ToggleAbsorpAttr(int attrId)
        {
            var attr = (SAAbsorptionAttribute)attrId;
            if (m_Attachment.Settings.EnabledAbsorptionAttributes.Contains(attr)) m_Attachment.Settings.EnabledAbsorptionAttributes.Remove(attr);
            else m_Attachment.Settings.EnabledAbsorptionAttributes.Add(attr);
            m_Player.SendGump(new LootFilterGump(m_Player, m_Category));
        }

        private void ToggleResist(int id)
        {
            m_Attachment.Settings.EnabledResistances[id - 100] = !m_Attachment.Settings.EnabledResistances[id - 100];
            m_Player.SendGump(new LootFilterGump(m_Player, m_Category));
        }

        private void ToggleDam(int id)
        {
            m_Attachment.Settings.EnabledDamages[id - 200] = !m_Attachment.Settings.EnabledDamages[id - 200];
            m_Player.SendGump(new LootFilterGump(m_Player, m_Category));
        }

        private void HandleAttr(int attrId, int delta)
        {
            AosAttribute attr = (AosAttribute)attrId;
            int val = 0;
            m_Attachment.Settings.Attributes.TryGetValue(attr, out val);
            m_Attachment.Settings.Attributes[attr] = Math.Max(0, val + delta);
            m_Player.SendGump(new LootFilterGump(m_Player, m_Category));
        }

        private void HandleWepAttr(int attrId, int delta)
        {
            AosWeaponAttribute attr = (AosWeaponAttribute)attrId;
            int val = 0;
            m_Attachment.Settings.WeaponAttributes.TryGetValue(attr, out val);
            m_Attachment.Settings.WeaponAttributes[attr] = Math.Max(0, val + delta);
            m_Player.SendGump(new LootFilterGump(m_Player, m_Category));
        }

        private void HandleArmAttr(int attrId, int delta)
        {
            AosArmorAttribute attr = (AosArmorAttribute)attrId;
            int val = 0;
            m_Attachment.Settings.ArmorAttributes.TryGetValue(attr, out val);
            m_Attachment.Settings.ArmorAttributes[attr] = Math.Max(0, val + delta);
            m_Player.SendGump(new LootFilterGump(m_Player, m_Category));
        }

        private void HandleExtWepAttr(int attrId, int delta)
        {
            ExtendedWeaponAttribute attr = (ExtendedWeaponAttribute)attrId;
            int val = 0;
            m_Attachment.Settings.ExtendedWeaponAttributes.TryGetValue(attr, out val);
            m_Attachment.Settings.ExtendedWeaponAttributes[attr] = Math.Max(0, val + delta);
            m_Player.SendGump(new LootFilterGump(m_Player, m_Category));
        }

        private void HandleAbsorpAttr(int attrId, int delta)
        {
            SAAbsorptionAttribute attr = (SAAbsorptionAttribute)attrId;
            int val = 0;
            m_Attachment.Settings.AbsorptionAttributes.TryGetValue(attr, out val);
            m_Attachment.Settings.AbsorptionAttributes[attr] = Math.Max(0, val + delta);
            m_Player.SendGump(new LootFilterGump(m_Player, m_Category));
        }

        private void HandleResist(int id, int delta)
        {
            if (id == 100) m_Attachment.Settings.MinResistPhysical = Math.Max(0, m_Attachment.Settings.MinResistPhysical + delta);
            else if (id == 101) m_Attachment.Settings.MinResistFire = Math.Max(0, m_Attachment.Settings.MinResistFire + delta);
            else if (id == 102) m_Attachment.Settings.MinResistCold = Math.Max(0, m_Attachment.Settings.MinResistCold + delta);
            else if (id == 103) m_Attachment.Settings.MinResistPoison = Math.Max(0, m_Attachment.Settings.MinResistPoison + delta);
            else if (id == 104) m_Attachment.Settings.MinResistEnergy = Math.Max(0, m_Attachment.Settings.MinResistEnergy + delta);
            m_Player.SendGump(new LootFilterGump(m_Player, m_Category));
        }

        private void HandleDam(int id, int delta)
        {
            if (id == 200) m_Attachment.Settings.MinDamagePhysical = Math.Max(0, m_Attachment.Settings.MinDamagePhysical + delta);
            else if (id == 201) m_Attachment.Settings.MinDamageFire = Math.Max(0, m_Attachment.Settings.MinDamageFire + delta);
            else if (id == 202) m_Attachment.Settings.MinDamageCold = Math.Max(0, m_Attachment.Settings.MinDamageCold + delta);
            else if (id == 203) m_Attachment.Settings.MinDamagePoison = Math.Max(0, m_Attachment.Settings.MinDamagePoison + delta);
            else if (id == 204) m_Attachment.Settings.MinDamageEnergy = Math.Max(0, m_Attachment.Settings.MinDamageEnergy + delta);
            else if (id == 205) m_Attachment.Settings.MinDamageChaos = Math.Max(0, m_Attachment.Settings.MinDamageChaos + delta);
            else if (id == 206) m_Attachment.Settings.MinDamageDirect = Math.Max(0, m_Attachment.Settings.MinDamageDirect + delta);
            m_Player.SendGump(new LootFilterGump(m_Player, m_Category));
        }
    }
}
