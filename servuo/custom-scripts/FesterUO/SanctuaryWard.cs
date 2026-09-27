using System;
using Server;
using Server.Commands;
using Server.Items;
using Server.Mobiles;
using Server.Regions;
using Server.Engines.CannedEvil;

namespace Server.Custom
{
    // =========================================================================
    // SANCTUARY WARD CONTROLLER
    // Evaluates creature hostility suppression outside dungeons
    // =========================================================================
    public static class SanctuaryWard
    {
        public static void Initialize()
        {
            BaseCreature.CheckSanctuary = IsProtected;
            CommandSystem.Register("GetSanctuary", AccessLevel.Player, GetSanctuary_OnCommand);
            CommandSystem.Register("Sanctuary", AccessLevel.Player, GetSanctuary_OnCommand);
        }

        /// <summary>
        /// Called by BaseCreature.IsEnemy to check if a potential target is protected by a sanctuary ward.
        /// </summary>
        public static bool IsProtected(Mobile m)
        {
            if (m == null || m.Deleted || !m.Alive)
                return false;

            PlayerMobile pm = null;

            if (m is PlayerMobile player)
            {
                pm = player;
            }
            else if (m is BaseCreature pet && pet.Controlled && pet.ControlMaster is PlayerMobile master)
            {
                pm = master;
            }

            if (pm == null || pm.Deleted || !pm.Alive)
                return false;

            // Inactive inside dungeons and champion arenas
            if (IsInDungeon(m))
                return false;

            return HasActiveSanctuary(pm);
        }

        /// <summary>
        /// Checks whether the mobile is within a dungeon, underground cave, or champion arena.
        /// </summary>
        public static bool IsInDungeon(Mobile m)
        {
            if (m?.Region == null)
                return false;

            Region reg = m.Region;

            return reg.IsPartOf<DungeonRegion>() ||
                   reg.IsPartOf<MondainRegion>() ||
                   reg.IsPartOf<ChampionSpawnRegion>();
        }

        /// <summary>
        /// Checks if the player is carrying or equipping an active Talisman of Sanctuary.
        /// </summary>
        public static bool HasActiveSanctuary(PlayerMobile pm)
        {
            if (pm == null || pm.Deleted)
                return false;

            // 1. Check equipped talisman slot
            if (pm.FindItemOnLayer(Layer.Talisman) is SanctuaryTalisman equipped && equipped.Active)
                return true;

            // 2. Check main backpack and satchel
            if (pm.Backpack != null)
            {
                var talismans = pm.Backpack.FindItemsByType<SanctuaryTalisman>();
                foreach (var talisman in talismans)
                {
                    if (talisman != null && !talisman.Deleted && talisman.Active)
                        return true;
                }
            }

            return false;
        }

        [Usage("GetSanctuary")]
        [Aliases("Sanctuary")]
        [Description("Provides a Talisman of Sanctuary if the player does not already carry one.")]
        private static void GetSanctuary_OnCommand(CommandEventArgs e)
        {
            Mobile from = e.Mobile;
            if (from == null || from.Backpack == null)
                return;

            if (from is PlayerMobile pm)
            {
                if (pm.FindItemOnLayer(Layer.Talisman) is SanctuaryTalisman ||
                    pm.Backpack.FindItemByType<SanctuaryTalisman>() != null)
                {
                    from.SendMessage(38, "You already possess a Talisman of Sanctuary.");
                    return;
                }

                SanctuaryTalisman talisman = new SanctuaryTalisman();
                from.Backpack.DropItem(talisman);
                from.PlaySound(0x1F2);
                from.SendMessage(68, "A blessed Talisman of Sanctuary has been placed in your backpack.");
            }
        }
    }

    // =========================================================================
    // TALISMAN OF SANCTUARY
    // Blessed ward preventing wild creature aggro outside dungeons
    // =========================================================================
    public class SanctuaryTalisman : Item
    {
        private bool m_Active = true;

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Active
        {
            get => m_Active;
            set
            {
                m_Active = value;
                InvalidateProperties();
            }
        }

        [Constructable]
        public SanctuaryTalisman() : base(0x2F58) // Ornate medallion/talisman graphic
        {
            Name = "Talisman of Sanctuary";
            Hue = 1153; // Celestial ocean cerulean / radiant sheen
            Weight = 1.0;
            Layer = Layer.Talisman;
            LootType = LootType.Blessed;
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (from == null)
                return;

            if (!IsChildOf(from.Backpack) && Parent != from)
            {
                from.SendMessage(38, "The talisman must be in your backpack or equipped to use.");
                return;
            }

            Active = !Active;

            if (Active)
            {
                from.PlaySound(0x1F2);
                from.SendMessage(68, "You activate the Talisman of Sanctuary. Hostile creatures in the wild will ignore you.");
            }
            else
            {
                from.PlaySound(0x1E3);
                from.SendMessage(38, "You deactivate the Talisman of Sanctuary. The wild is no longer pacified.");
            }
        }

        public override void OnSingleClick(Mobile from)
        {
            base.OnSingleClick(from);

            if (!Active)
            {
                LabelTo(from, "[Protection: Inactive]", 38);
            }
            else if (SanctuaryWard.IsInDungeon(from))
            {
                LabelTo(from, "[Protection: Suppressed in Dungeon]", 38);
            }
            else
            {
                LabelTo(from, "[Protection: Active (Wilds Only)]", 68);
            }
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);

            if (!Active)
            {
                list.Add(1049644, "[Protection: Inactive (Double-Click to Enable)]");
            }
            else if (RootParent is Mobile wearer && SanctuaryWard.IsInDungeon(wearer))
            {
                list.Add(1049644, "[Protection: Suppressed in Dungeon]");
            }
            else
            {
                list.Add(1049644, "[Protection: Active (Wilds Only)]");
            }

            list.Add(1070722, "Wild beasts will not attack you or your companions. Ineffective in dungeons.");
        }

        public SanctuaryTalisman(Serial serial) : base(serial) { }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0); // version

            writer.Write(m_Active);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();

            m_Active = reader.ReadBool();
        }
    }
}
