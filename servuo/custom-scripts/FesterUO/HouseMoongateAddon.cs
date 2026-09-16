using System;
using Server;
using Server.Gumps;
using Server.Items;
using Server.Multis;
using Server.Spells;

namespace Server.Custom
{
    public class FestersMoongateComponent : AddonComponent
    {
        public override bool HandlesOnMovement => true;

        [Constructable]
        public FestersMoongateComponent() : base(0xF6C)
        {
            Name = "House Moongate";
            Light = LightType.Circle300;
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (from == null)
                return;

            if (from.InRange(GetWorldLocation(), 1))
            {
                UseGate(from);
            }
            else
            {
                from.SendLocalizedMessage(1019002); // You are too far away to use the gate.
            }
        }

        public override bool OnMoveOver(Mobile m)
        {
            if (m != null && m.Player && m.CanSee(this))
            {
                UseGate(m);
            }

            return base.OnMoveOver(m);
        }

        public override void OnMovement(Mobile m, Point3D oldLocation)
        {
            if (m != null && m.Player && !Utility.InRange(m.Location, Location, 1) && Utility.InRange(oldLocation, Location, 1))
            {
                m.CloseGump(typeof(MoongateGump));
            }
        }

        public void UseGate(Mobile m)
        {
            if (m == null || m.Map == null || m.Map == Map.Internal)
                return;

            if (m.Criminal)
            {
                m.SendLocalizedMessage(1005561, "", 0x22); // Thou'rt a criminal and cannot escape so easily.
                return;
            }

            if (SpellHelper.CheckCombat(m))
            {
                m.SendLocalizedMessage(1005564, "", 0x22); // Wouldst thou flee during the heat of battle??
                return;
            }

            if (m.Spell != null)
            {
                m.SendLocalizedMessage(1049616); // You are too busy to do that at the moment.
                return;
            }

            m.CloseGump(typeof(MoongateGump));
            m.SendGump(new MoongateGump(m, this));
        }

        public FestersMoongateComponent(Serial serial) : base(serial) { }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
        }
    }

    public class FestersMoongateAddon : BaseAddon
    {
        public override BaseAddonDeed Deed => new FestersMoongateAddonDeed();

        [Constructable]
        public FestersMoongateAddon()
        {
            AddComponent(new FestersMoongateComponent(), 0, 0, 0);
        }

        public FestersMoongateAddon(Serial serial) : base(serial) { }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
        }
    }

    public class FestersMoongateAddonDeed : BaseAddonDeed
    {
        public override BaseAddon Addon => new FestersMoongateAddon();

        [Constructable]
        public FestersMoongateAddonDeed()
        {
            Name = "House Moongate Deed";
            Hue = 0x482; // Antique gilded hue
            LootType = LootType.Blessed;
            Weight = 1.0;
        }

        public FestersMoongateAddonDeed(Serial serial) : base(serial) { }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
        }
    }
}
