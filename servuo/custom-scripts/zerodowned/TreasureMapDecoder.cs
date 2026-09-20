/************************************************************************************
* Community Script: Treasure Map Decoder (Instant Transporter)                      *
* Author: zerodowned                                                                *
* Source: https://github.com/zerodowned/Custom-Scripts-for-ServUO/tree/master/Treasure%20Map%20Decoder *
*                                                                                   *
* Quality of Life (QoL) exploration utility:                                        *
* - Opens a timed moongate directly to the chest coordinates of a Treasure Map      *
* - Checks for combat, criminal status, overloading, and jail escape prevention     *
* - Decodes undeciphered maps on use and prevents completed map travel              *
* - Unlimited use / no charges                                                      *
************************************************************************************/

using System;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.Targeting;

namespace Server.Items
{
    public class TreasureMapDecoder : Item
    {
        [Constructable]
        public TreasureMapDecoder() : base(0x577E) // 22398 - scroll/astrolabe graphic
        {
            Movable = true;
            Hue = 1266;
            Weight = 0.0;
            Name = "Treasure Map Instant Transporter";
            LootType = LootType.Blessed;
        }

        public TreasureMapDecoder(Serial serial) : base(serial)
        {
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);
            list.Add("Creates a gateway directly to the<br>Chest location of a treasure map");
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (!from.Player)
                return;

            if (IsChildOf(from.Backpack))
            {
                UseBook(from);
            }
            else
            {
                from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
            }
        }

        public bool UseBook(Mobile m)
        {
            if (m.Criminal)
            {
                m.SendLocalizedMessage(1005561, "", 0x22); // Thou'rt a criminal and cannot escape so easily.
                return false;
            }
            else if (Server.Spells.SpellHelper.CheckCombat(m))
            {
                m.SendLocalizedMessage(1005564, "", 0x22); // Wouldst thou flee during the heat of battle??
                return false;
            }
            else if (Server.Misc.WeightOverloading.IsOverloaded(m))
            {
                m.SendLocalizedMessage(502359, "", 0x22); // Thou art too encumbered to move.
                return false;
            }
            else if (m.Region is Server.Regions.Jail)
            {
                m.SendLocalizedMessage(1041530, "", 0x35); // You'll need a better jailbreak plan then that!
                return false;
            }
            else if (m.Spell != null)
            {
                m.SendLocalizedMessage(1049616); // You are too busy to do that at the moment.
                return false;
            }
            else
            {
                m.Target = new TmapTarget(this);
                m.SendMessage("Target a Treasure Map");
                return true;
            }
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
        }
    }

    public class TmapTarget : Target
    {
        private readonly TreasureMapDecoder _mapDecoder;

        public TmapTarget(TreasureMapDecoder mapDecoder) : base(1, false, TargetFlags.None)
        {
            _mapDecoder = mapDecoder;
        }

        protected override void OnTarget(Mobile from, object target)
        {
            if (target is TreasureMap ts)
            {
                if (ts.Deleted)
                    return;

                if (ts.RootParent != from)
                {
                    from.SendMessage("The treasure map must be in your possession.");
                    return;
                }

                if (ts.Decoder != null && ts.Decoder != from)
                {
                    from.SendMessage("Someone else has already deciphered this map!");
                    return;
                }

                if (ts.Completed)
                {
                    from.SendMessage("That map has already been completed.");
                    return;
                }

                if (ts.Facet == null || ts.Facet == Map.Internal || from.Map == null || from.Map == Map.Internal)
                {
                    from.SendMessage("That map cannot be deciphered here.");
                    return;
                }

                TmapBookMoongate gate = new TmapBookMoongate();
                gate.TargetMap = ts.Facet;

                ts.Decoder = from;

                int z = gate.TargetMap.GetAverageZ(ts.ChestLocation.X, ts.ChestLocation.Y);
                Point3D p = new Point3D(ts.ChestLocation.X, ts.ChestLocation.Y, z);

                gate.Target = p;
                gate.MoveToWorld(new Point3D(from.Location), from.Map);
            }
            else
            {
                from.SendMessage("You can only use this on Treasure Maps!");
            }
        }
    }

    public class TmapBookMoongate : Moongate
    {
        public override bool ShowFeluccaWarning => false;

        [Constructable]
        public TmapBookMoongate() : base()
        {
            InternalTimer t = new InternalTimer(this);
            t.Start();
        }

        private class InternalTimer : Timer
        {
            private readonly Item m_Item;

            public InternalTimer(Item item)
                : base(TimeSpan.FromSeconds(30.0))
            {
                Priority = TimerPriority.OneSecond;
                m_Item = item;
            }

            protected override void OnTick()
            {
                m_Item.Delete();
            }
        }

        public override void OnGateUsed(Mobile m)
        {
            base.OnGateUsed(m);

            Delete();
        }

        public TmapBookMoongate(Serial serial) : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            Delete();
        }
    }
}
