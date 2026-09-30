/*
 * UO Community Script: The Ultimate ARPG-Style Loot Filter
 * Source: https://www.servuo.dev/archive/the-ultimate-arpg-style-loot-filter-diablo-poe-exactly-like-you-want-it.2606/
 *
 * Core evaluation engine that checks whether an item on the ground or in a corpse
 * satisfies the player's active loot filter rules. Hooks into PlayerMobile.CanSee via PlayerMobile.LootFilterCheck.
 */

using System;
using Server;
using Server.Items;
using Server.Mobiles;

namespace Server.Engines.LootFilter
{
    public static class LootFilterController
    {
        public static void Initialize()
        {
            PlayerMobile.LootFilterCheck = PassesFilter;
        }

        public static bool PassesFilter(Mobile m, Item item)
        {
            if (m == null || item == null) return false;

            var attachment = LootFilterAttachment.GetAttachment(m);
            if (attachment == null || attachment.Settings == null || !attachment.Settings.Enabled)
                return true;

            LootFilterSettings s = attachment.Settings;

            // Only apply filters to equipment and never filter out Artifacts or special items
            bool isEquipment = false;

            if (item is BaseWeapon)
            {
                if (((BaseWeapon)item).ArtifactRarity > 0) return true;
                isEquipment = true;
            }
            else if (item is BaseArmor)
            {
                if (((BaseArmor)item).ArtifactRarity > 0) return true;
                isEquipment = true;
            }
            else if (item is BaseJewel)
            {
                if (((BaseJewel)item).ArtifactRarity > 0) return true;
                isEquipment = true;
            }
            else if (item is BaseClothing)
            {
                if (((BaseClothing)item).ArtifactRarity > 0) return true;
                isEquipment = true;
            }
            else if (item is BaseHat)
            {
                // BaseHat inherits from BaseClothing, but just in case
                if (((BaseHat)item).ArtifactRarity > 0) return true;
                isEquipment = true;
            }
            else if (item is BaseQuiver)
            {
                if (((BaseQuiver)item).ArtifactRarity > 0) return true;
                isEquipment = true;
            }

            // Also check for IsArtifact via reflection in case custom systems use it on non-standard items
            if (isEquipment)
            {
                var type = item.GetType();
                var isArtifactProp = type.GetProperty("IsArtifact");
                if (isArtifactProp != null)
                {
                    try 
                    { 
                        if ((bool)isArtifactProp.GetValue(item, null)) return true; 
                    } 
                    catch { }
                }
            }

            // If it's not equipment (like gold, potions, reagents, quest items), don't filter it
            if (!isEquipment) return true;

            // Check AOS Attributes
            foreach (var attr in s.EnabledAttributes)
            {
                int minVal = 0;
                s.Attributes.TryGetValue(attr, out minVal);
                if (GetAttributeValue(item, attr) < minVal) return false;
            }

            // Check Weapon Attributes
            if (item is BaseWeapon)
            {
                BaseWeapon bw = (BaseWeapon)item;
                foreach (var attr in s.EnabledWeaponAttributes)
                {
                    int minVal = 0;
                    s.WeaponAttributes.TryGetValue(attr, out minVal);
                    if (bw.WeaponAttributes[attr] < minVal) return false;
                }

                foreach (var attr in s.EnabledExtendedWeaponAttributes)
                {
                    int minVal = 0;
                    s.ExtendedWeaponAttributes.TryGetValue(attr, out minVal);
                    if (bw.ExtendedWeaponAttributes[attr] < minVal) return false;
                }
            }
            else
            {
                // If it's not a weapon, and any weapon attribute is enforced, it fails.
                if (s.EnabledWeaponAttributes.Count > 0 || s.EnabledExtendedWeaponAttributes.Count > 0) return false;
            }

            // Check Armor Attributes
            if (item is BaseArmor)
            {
                BaseArmor ba = (BaseArmor)item;
                foreach (var attr in s.EnabledArmorAttributes)
                {
                    int minVal = 0;
                    s.ArmorAttributes.TryGetValue(attr, out minVal);
                    if (ba.ArmorAttributes[attr] < minVal) return false;
                }
            }
            else
            {
                if (s.EnabledArmorAttributes.Count > 0) return false;
            }

            // Check SA Absorption Attributes
            foreach (var attr in s.EnabledAbsorptionAttributes)
            {
                int minVal = 0;
                s.AbsorptionAttributes.TryGetValue(attr, out minVal);
                if (GetAbsorptionAttributeValue(item, attr) < minVal) return false;
            }

            // Check Negative Attributes
            foreach (var attr in s.EnabledNegativeAttributes)
            {
                int minVal = 0;
                s.NegativeAttributes.TryGetValue(attr, out minVal);
                if (GetNegativeAttributeValue(item, attr) < minVal) return false;
            }

            // Check Resistances
            if (s.EnabledResistances[0] && GetResistValue(item, ResistanceType.Physical) < s.MinResistPhysical) return false;
            if (s.EnabledResistances[1] && GetResistValue(item, ResistanceType.Fire) < s.MinResistFire) return false;
            if (s.EnabledResistances[2] && GetResistValue(item, ResistanceType.Cold) < s.MinResistCold) return false;
            if (s.EnabledResistances[3] && GetResistValue(item, ResistanceType.Poison) < s.MinResistPoison) return false;
            if (s.EnabledResistances[4] && GetResistValue(item, ResistanceType.Energy) < s.MinResistEnergy) return false;

            // Check Element Damage
            if (item is BaseWeapon)
            {
                BaseWeapon bw = (BaseWeapon)item;
                if (s.EnabledDamages[0] && bw.AosElementDamages.Physical < s.MinDamagePhysical) return false;
                if (s.EnabledDamages[1] && bw.AosElementDamages.Fire < s.MinDamageFire) return false;
                if (s.EnabledDamages[2] && bw.AosElementDamages.Cold < s.MinDamageCold) return false;
                if (s.EnabledDamages[3] && bw.AosElementDamages.Poison < s.MinDamagePoison) return false;
                if (s.EnabledDamages[4] && bw.AosElementDamages.Energy < s.MinDamageEnergy) return false;
                if (s.EnabledDamages[5] && bw.AosElementDamages.Chaos < s.MinDamageChaos) return false;
                if (s.EnabledDamages[6] && bw.AosElementDamages.Direct < s.MinDamageDirect) return false;
            }
            else
            {
                bool anyDam = false;
                for(int i = 0; i < 7; i++) { if (s.EnabledDamages[i]) { anyDam = true; break; } }
                if (anyDam) return false;
            }

            return true;
        }

        private static int GetAttributeValue(Item item, AosAttribute attr)
        {
            if (item is BaseWeapon) return ((BaseWeapon)item).Attributes[attr];
            if (item is BaseArmor) return ((BaseArmor)item).Attributes[attr];
            if (item is BaseJewel) return ((BaseJewel)item).Attributes[attr];
            if (item is BaseClothing) return ((BaseClothing)item).Attributes[attr];
            if (item is BaseHat) return ((BaseHat)item).Attributes[attr];
            if (item is BaseQuiver) return ((BaseQuiver)item).Attributes[attr];
            return 0;
        }

        private static int GetAbsorptionAttributeValue(Item item, SAAbsorptionAttribute attr)
        {
            if (item is BaseWeapon) return ((BaseWeapon)item).AbsorptionAttributes[attr];
            if (item is BaseArmor) return ((BaseArmor)item).AbsorptionAttributes[attr];
            if (item is BaseJewel) return ((BaseJewel)item).AbsorptionAttributes[attr];
            return 0;
        }

        private static int GetNegativeAttributeValue(Item item, NegativeAttribute attr)
        {
            if (item is BaseWeapon) return ((BaseWeapon)item).NegativeAttributes[attr];
            if (item is BaseArmor) return ((BaseArmor)item).NegativeAttributes[attr];
            if (item is BaseJewel) return ((BaseJewel)item).NegativeAttributes[attr];
            return 0;
        }

        private static int GetResistValue(Item item, ResistanceType type)
        {
            if (item is BaseArmor)
            {
                BaseArmor ba = (BaseArmor)item;
                switch (type)
                {
                    case ResistanceType.Physical: return ba.PhysicalResistance;
                    case ResistanceType.Fire: return ba.FireResistance;
                    case ResistanceType.Cold: return ba.ColdResistance;
                    case ResistanceType.Poison: return ba.PoisonResistance;
                    case ResistanceType.Energy: return ba.EnergyResistance;
                }
            }
            if (item is BaseJewel)
            {
                BaseJewel bj = (BaseJewel)item;
                switch (type)
                {
                    case ResistanceType.Physical: return bj.Resistances.Physical;
                    case ResistanceType.Fire: return bj.Resistances.Fire;
                    case ResistanceType.Cold: return bj.Resistances.Cold;
                    case ResistanceType.Poison: return bj.Resistances.Poison;
                    case ResistanceType.Energy: return bj.Resistances.Energy;
                }
            }
            return 0;
        }
    }
}
