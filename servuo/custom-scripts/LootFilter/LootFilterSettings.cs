/*
 * UO Community Script: The Ultimate ARPG-Style Loot Filter
 * Source: https://www.servuo.dev/archive/the-ultimate-arpg-style-loot-filter-diablo-poe-exactly-like-you-want-it.2606/
 *
 * Configurable loot filter settings model covering 8 categories (Primary, Combat,
 * Magic, Resists, Damage, HitsLeech, HitsMagic, Special), thresholds, and serialization.
 */

using System;
using System.Collections.Generic;
using Server;
using Server.Items;

namespace Server.Engines.LootFilter
{
    public enum LootFilterCategory
    {
        Primary,
        Combat,
        Magic,
        Resists,
        Damage,
        HitsLeech,
        HitsMagic,
        Special
    }

    [PropertyObject]
    public class LootFilterSettings
    {
        public bool Enabled { get; set; }

        // AOS Attributes
        public Dictionary<AosAttribute, int> Attributes { get; set; } = new Dictionary<AosAttribute, int>();
        
        // Weapon Attributes
        public Dictionary<AosWeaponAttribute, int> WeaponAttributes { get; set; } = new Dictionary<AosWeaponAttribute, int>();

        // Armor Attributes
        public Dictionary<AosArmorAttribute, int> ArmorAttributes { get; set; } = new Dictionary<AosArmorAttribute, int>();

        // Extended Weapon Attributes
        public Dictionary<ExtendedWeaponAttribute, int> ExtendedWeaponAttributes { get; set; } = new Dictionary<ExtendedWeaponAttribute, int>();

        // SA Absorption Attributes
        public Dictionary<SAAbsorptionAttribute, int> AbsorptionAttributes { get; set; } = new Dictionary<SAAbsorptionAttribute, int>();

        // Negative Attributes
        public Dictionary<NegativeAttribute, int> NegativeAttributes { get; set; } = new Dictionary<NegativeAttribute, int>();

        // Resistances
        public int MinResistPhysical { get; set; }
        public int MinResistFire { get; set; }
        public int MinResistCold { get; set; }
        public int MinResistPoison { get; set; }
        public int MinResistEnergy { get; set; }

        // Element Damage
        public int MinDamagePhysical { get; set; }
        public int MinDamageFire { get; set; }
        public int MinDamageCold { get; set; }
        public int MinDamagePoison { get; set; }
        public int MinDamageEnergy { get; set; }
        public int MinDamageChaos { get; set; }
        public int MinDamageDirect { get; set; }

        public HashSet<AosAttribute> EnabledAttributes { get; set; } = new HashSet<AosAttribute>();
        public HashSet<AosWeaponAttribute> EnabledWeaponAttributes { get; set; } = new HashSet<AosWeaponAttribute>();
        public HashSet<AosArmorAttribute> EnabledArmorAttributes { get; set; } = new HashSet<AosArmorAttribute>();
        public HashSet<ExtendedWeaponAttribute> EnabledExtendedWeaponAttributes { get; set; } = new HashSet<ExtendedWeaponAttribute>();
        public HashSet<SAAbsorptionAttribute> EnabledAbsorptionAttributes { get; set; } = new HashSet<SAAbsorptionAttribute>();
        public HashSet<NegativeAttribute> EnabledNegativeAttributes { get; set; } = new HashSet<NegativeAttribute>();

        public bool[] EnabledResistances { get; set; } = new bool[5];
        public bool[] EnabledDamages { get; set; } = new bool[7];

        public LootFilterSettings()
        {
            Enabled = true;
        }

        public void Serialize(GenericWriter writer)
        {
            writer.Write((int)2); // version

            writer.Write(Enabled);

            // Version 2 Additions
            writer.Write(EnabledAttributes.Count);
            foreach (var a in EnabledAttributes) writer.Write((int)a);

            writer.Write(EnabledWeaponAttributes.Count);
            foreach (var a in EnabledWeaponAttributes) writer.Write((int)a);

            writer.Write(EnabledArmorAttributes.Count);
            foreach (var a in EnabledArmorAttributes) writer.Write((int)a);

            writer.Write(EnabledExtendedWeaponAttributes.Count);
            foreach (var a in EnabledExtendedWeaponAttributes) writer.Write((int)a);

            writer.Write(EnabledAbsorptionAttributes.Count);
            foreach (var a in EnabledAbsorptionAttributes) writer.Write((int)a);

            writer.Write(EnabledNegativeAttributes.Count);
            foreach (var a in EnabledNegativeAttributes) writer.Write((int)a);

            for(int i = 0; i < 5; i++) writer.Write(EnabledResistances[i]);
            for(int i = 0; i < 7; i++) writer.Write(EnabledDamages[i]);

            // Dictionaries
            writer.Write(Attributes.Count);
            foreach (var kvp in Attributes)
            {
                writer.Write((int)kvp.Key);
                writer.Write(kvp.Value);
            }

            writer.Write(WeaponAttributes.Count);
            foreach (var kvp in WeaponAttributes)
            {
                writer.Write((int)kvp.Key);
                writer.Write(kvp.Value);
            }

            writer.Write(ArmorAttributes.Count);
            foreach (var kvp in ArmorAttributes)
            {
                writer.Write((int)kvp.Key);
                writer.Write(kvp.Value);
            }

            writer.Write(ExtendedWeaponAttributes.Count);
            foreach (var kvp in ExtendedWeaponAttributes)
            {
                writer.Write((int)kvp.Key);
                writer.Write(kvp.Value);
            }

            writer.Write(AbsorptionAttributes.Count);
            foreach (var kvp in AbsorptionAttributes)
            {
                writer.Write((int)kvp.Key);
                writer.Write(kvp.Value);
            }

            writer.Write(NegativeAttributes.Count);
            foreach (var kvp in NegativeAttributes)
            {
                writer.Write((int)kvp.Key);
                writer.Write(kvp.Value);
            }

            writer.Write(MinResistPhysical);
            writer.Write(MinResistFire);
            writer.Write(MinResistCold);
            writer.Write(MinResistPoison);
            writer.Write(MinResistEnergy);

            writer.Write(MinDamagePhysical);
            writer.Write(MinDamageFire);
            writer.Write(MinDamageCold);
            writer.Write(MinDamagePoison);
            writer.Write(MinDamageEnergy);
            writer.Write(MinDamageChaos);
            writer.Write(MinDamageDirect);
        }

        public void Deserialize(GenericReader reader)
        {
            int version = reader.ReadInt();

            Enabled = reader.ReadBool();

            if (version >= 2)
            {
                int eCount = reader.ReadInt();
                for (int i = 0; i < eCount; i++) EnabledAttributes.Add((AosAttribute)reader.ReadInt());

                eCount = reader.ReadInt();
                for (int i = 0; i < eCount; i++) EnabledWeaponAttributes.Add((AosWeaponAttribute)reader.ReadInt());

                eCount = reader.ReadInt();
                for (int i = 0; i < eCount; i++) EnabledArmorAttributes.Add((AosArmorAttribute)reader.ReadInt());

                eCount = reader.ReadInt();
                for (int i = 0; i < eCount; i++) EnabledExtendedWeaponAttributes.Add((ExtendedWeaponAttribute)reader.ReadInt());

                eCount = reader.ReadInt();
                for (int i = 0; i < eCount; i++) EnabledAbsorptionAttributes.Add((SAAbsorptionAttribute)reader.ReadInt());

                eCount = reader.ReadInt();
                for (int i = 0; i < eCount; i++) EnabledNegativeAttributes.Add((NegativeAttribute)reader.ReadInt());

                for (int i = 0; i < 5; i++) EnabledResistances[i] = reader.ReadBool();
                for (int i = 0; i < 7; i++) EnabledDamages[i] = reader.ReadBool();
            }

            int count = reader.ReadInt();
            for (int i = 0; i < count; i++)
                Attributes[(AosAttribute)reader.ReadInt()] = reader.ReadInt();

            count = reader.ReadInt();
            for (int i = 0; i < count; i++)
            {
                if (version == 0) reader.ReadLong(); // skip old long key
                else WeaponAttributes[(AosWeaponAttribute)reader.ReadInt()] = reader.ReadInt();
            }

            count = reader.ReadInt();
            for (int i = 0; i < count; i++)
                ArmorAttributes[(AosArmorAttribute)reader.ReadInt()] = reader.ReadInt();

            if (version >= 1)
            {
                count = reader.ReadInt();
                for (int i = 0; i < count; i++)
                    ExtendedWeaponAttributes[(ExtendedWeaponAttribute)reader.ReadInt()] = reader.ReadInt();

                count = reader.ReadInt();
                for (int i = 0; i < count; i++)
                    AbsorptionAttributes[(SAAbsorptionAttribute)reader.ReadInt()] = reader.ReadInt();

                count = reader.ReadInt();
                for (int i = 0; i < count; i++)
                    NegativeAttributes[(NegativeAttribute)reader.ReadInt()] = reader.ReadInt();
            }

            MinResistPhysical = reader.ReadInt();
            MinResistFire = reader.ReadInt();
            MinResistCold = reader.ReadInt();
            MinResistPoison = reader.ReadInt();
            MinResistEnergy = reader.ReadInt();

            MinDamagePhysical = reader.ReadInt();
            MinDamageFire = reader.ReadInt();
            MinDamageCold = reader.ReadInt();
            MinDamagePoison = reader.ReadInt();
            MinDamageEnergy = reader.ReadInt();

            if (version >= 1)
            {
                MinDamageChaos = reader.ReadInt();
                MinDamageDirect = reader.ReadInt();
            }
        }

        public override string ToString()
        {
            return "Loot Filter Settings";
        }
    }
}
