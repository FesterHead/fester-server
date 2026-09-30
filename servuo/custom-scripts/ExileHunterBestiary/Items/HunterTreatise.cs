/*
 * UO Community Script: Hunter's Bestiary & Monster Treatises System
 * Original Author: UO Exile Team / Imagine (Resource #2674)
 * Source: https://www.servuo.dev/archive/release-hunters-bestiary-monster-treatises-system-193-creatures-1-line-install.2674/
 */

using System;
using Server.Network;

namespace Server.Custom.ExileHunterBestiary.Items
{
    public class HunterTreatise : Item
    {
        private string m_CreatureId;

        [CommandProperty(AccessLevel.GameMaster)]
        public string CreatureId
        {
            get => m_CreatureId;
            set
            {
                m_CreatureId = value;
                UpdateAppearance();
                InvalidateProperties();
            }
        }

        public HunterCreatureEntry Entry
        {
            get
            {
                if (string.IsNullOrEmpty(m_CreatureId))
                    return null;

                if (HunterBestiaryEngine.RegistryById.TryGetValue(m_CreatureId, out var entry))
                    return entry;

                return null;
            }
        }

        [Constructable]
        public HunterTreatise() : this("Lich")
        {
        }

        [Constructable]
        public HunterTreatise(string creatureId) : base(0x1F5F)
        {
            m_CreatureId = creatureId;
            Weight = 1.0;
            LootType = LootType.Regular;

            UpdateAppearance();
        }

        public HunterTreatise(Type creatureType) : base(0x1F5F)
        {
            if (creatureType != null && HunterBestiaryEngine.RegistryByType.TryGetValue(creatureType, out var entry))
            {
                m_CreatureId = entry.Id;
            }
            else
            {
                m_CreatureId = "Lich";
            }

            Weight = 1.0;
            LootType = LootType.Regular;

            UpdateAppearance();
        }

        public HunterTreatise(Serial serial) : base(serial)
        {
        }

        public void UpdateAppearance()
        {
            ItemID = 0x1F5F; // Magery spell scroll graphic

            var entry = Entry;
            if (entry != null)
            {
                Name = $"Hunter's Treatise: {entry.DisplayName}";
                Hue = entry.BookHue != 0 ? entry.BookHue : 2101;
            }
            else
            {
                Name = $"Hunter's Treatise: {m_CreatureId}";
                Hue = 2101;
            }
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (!IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
                return;
            }

            var entry = Entry;
            string displayName = entry != null ? entry.DisplayName : m_CreatureId;

            var profile = HunterBestiaryEngine.GetProfile(from);
            if (profile == null)
                return;

            if (profile.HasLearned(m_CreatureId))
            {
                from.SendMessage(0x22, $"You have already mastered the study of {displayName}. Keep this treatise to trade with other hunters!");
                from.PlaySound(0x5C);
                return;
            }

            if (profile.Learn(m_CreatureId))
            {
                from.SendMessage(0x3F, $"You thoroughly study the treatise and permanently unlock +20% damage against {displayName}!");
                from.FixedParticles(0x373A, 10, 15, 5012, EffectLayer.Waist);
                from.PlaySound(0x249);

                Consume();
            }
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);

            var entry = Entry;
            string creatureName = entry != null ? entry.DisplayName : m_CreatureId;
            string category = entry != null ? entry.Category.ToString() : "Unknown";

            list.Add(1070722, string.Format("<BASEFONT COLOR=#D1C4E9>Hunter's Study: +20% Damage vs {0}<BASEFONT COLOR=#FFFFFF>", creatureName));
            list.Add(1070722, string.Format("<BASEFONT COLOR=#ECEFF1>Category: {0}<BASEFONT COLOR=#FFFFFF>", category));
            list.Add(1070722, "<BASEFONT COLOR=#FFE082>Double-click to permanently master<BASEFONT COLOR=#FFFFFF>");
        }

        public override void SendPropertiesTo(Mobile from)
        {
            var list = new ObjectPropertyList(this);
            GetProperties(list);

            var profile = HunterBestiaryEngine.GetProfile(from);
            bool learned = profile != null && profile.HasLearned(m_CreatureId);

            if (learned)
            {
                list.Add(1070722, "<BASEFONT COLOR=#00E676>[Already Learned]<BASEFONT COLOR=#FFFFFF>");
            }
            else
            {
                list.Add(1070722, "<BASEFONT COLOR=#00E5FF>[Not Learned Yet]<BASEFONT COLOR=#FFFFFF>");
            }

            list.Terminate();
            from.Send(list);
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version
            writer.Write(m_CreatureId);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
            m_CreatureId = reader.ReadString();

            if (ItemID == 0x1F14)
                ItemID = 0x1F5F;
        }
    }
}
