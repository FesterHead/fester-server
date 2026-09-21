/************************************************************************************
* Community Script: Pet Exchange Hitching Post House Deed                           *
* Author: 4737Carlin (June 8, 2026)                                                 *
* Gump Architecture: Massapequa (StableMaster.cs)                                    *
* Source: https://www.servuo.dev/archive/pet-exchange-hitching-post-house-deed.2643/ *
*                                                                                   *
* House addon providing cross-character and cross-account pet stabling:             *
* - Allows stabling pets inside player housing independently of global NPC stables  *
* - Configurable access permissions (Owner, Co-Owner, Friend) per stabled pet       *
* - Tracks bonding timers and displays real-time countdowns on hitching post props  *
* - Capacity parameterized via Config/PetExchange/PetExchange.cfg                   *
************************************************************************************/

using System;
using System.Collections.Generic;
using Server;
using Server.ContextMenus;
using Server.Gumps;
using Server.Mobiles;
using Server.Multis;
using Server.Network;
using Server.Targeting;

namespace Server.Items
{
    public enum AccessList
    {
        Owner,
        CoOwner,
        Friend
    }

    public class PetExchangeAddonDeed : BaseAddonDeed
    {
        public override BaseAddon Addon => new PetExchangeAddon();

        [Constructable]
        public PetExchangeAddonDeed()
        {
            Hue = 1161;
            Name = "Pet Exchange Hitching Post Deed";
        }

        public PetExchangeAddonDeed(Serial serial) : base(serial)
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
        }
    }

    public class PetExchangeAddon : BaseAddon
    {
        public override BaseAddonDeed Deed => new PetExchangeAddonDeed();

        [Constructable]
        public PetExchangeAddon()
        {
            Hue = 1161;
            AddComponent(new PetExchange(this), 0, 0, 0);
        }

        public PetExchangeAddon(Serial serial) : base(serial)
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
        }
    }

    public class PetExchange : AddonComponent
    {
        public static int MaxStabled => Config.Get("PetExchange.PetExchange.MaxStabled", Config.Get("PetExchange.MaxStabled", 10));

        public List<ExchangeEntry> ExchangeList = new List<ExchangeEntry>();

        private PetExchangeAddon m_Addon;

        public AccessList Access { get; set; }

        public PetExchange() : base(0x14E7)
        {
            Name = "Pet Exchange Hitching Post";
            Weight = 10;
            Hue = 1161;
        }

        [Constructable]
        public PetExchange(PetExchangeAddon addon) : base(0x14E7)
        {
            Name = "Pet Exchange Hitching Post";
            Weight = 10;
            Hue = 1161;
            m_Addon = addon;
        }

        public override void GetContextMenuEntries(Mobile from, List<ContextMenuEntry> list)
        {
            base.GetContextMenuEntries(from, list);

            if (AccessAllowed(from))
            {
                list.Add(new StableEntry(this, from));
                list.Add(new ClaimEntry(this, from));
            }
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);

            list.Add($"Stabled: {ExchangeList.Count} / {MaxStabled}");

            for (int i = 0; i < ExchangeList.Count; ++i)
            {
                if (ExchangeList[i].Pet is BaseCreature pet && pet.BondingBegin > DateTime.MinValue)
                {
                    list.Add($"{pet.Name}: {FormatTimeSpan(pet)}");
                }
            }
        }

        public override void OnDoubleClick(Mobile m)
        {
            BeginStable(m);
        }

        public override void OnDelete()
        {
            List<BaseCreature> list = new List<BaseCreature>();

            for (int i = 0; i < ExchangeList.Count; ++i)
            {
                if (ExchangeList[i].Pet is BaseCreature pet && !pet.Deleted)
                {
                    list.Add(pet);
                }
            }

            for (int i = 0; i < list.Count; ++i)
            {
                BaseCreature bc = list[i];
                bc.IsStabled = false;
                bc.StabledBy = null;
                bc.MoveToWorld(Location, Map);
            }

            base.OnDelete();
        }

        public bool AccessAllowed(Mobile from, int access = 2)
        {
            BaseHouse house = BaseHouse.FindHouseAt(from);

            if (house != null)
            {
                if (house.IsOwner(from))
                {
                    return true;
                }

                if (access == 2 && house.IsFriend(from))
                {
                    return true;
                }

                if (house.IsCoOwner(from))
                {
                    return true;
                }
            }

            from.SendLocalizedMessage(1005213); // You can't do that
            return false;
        }

        #region Stable
        public void BeginStable(Mobile from)
        {
            if (ExchangeList.Count < MaxStabled)
            {
                if (AccessAllowed(from))
                {
                    from.SendMessage("Target the pet you wish to stable.");
                    from.Target = new StableTarget(this);
                }

                return;
            }

            from.SendMessage("This hitching post is full.");
        }

        public void EndStable(Mobile from, BaseCreature pet)
        {
            if (Deleted || !from.CheckAlive())
            {
                return;
            }

            if (pet.Body.IsHuman)
            {
                from.SendLocalizedMessage(1005213); // You can't do that
            }
            else if (!pet.Controlled || pet.Allured)
            {
                from.SendLocalizedMessage(1048053); // You can't stable that!
            }
            else if (pet.ControlMaster != from)
            {
                from.SendLocalizedMessage(1042562); // You do not own that pet!
            }
            else if (pet.IsDeadPet)
            {
                from.SendLocalizedMessage(1049668); // Living pets only, please.
            }
            else if (pet.Summoned)
            {
                from.SendMessage("You cannot stable summoned creatures.");
            }
            else if ((pet is PackLlama || pet is PackHorse || pet is Beetle) && pet.Backpack != null && pet.Backpack.Items.Count > 0)
            {
                from.SendLocalizedMessage(1042563); // You need to unload your pet.
            }
            else if (pet.Combatant != null && pet.InRange(pet.Combatant, 12) && pet.Map == pet.Combatant.Map)
            {
                from.SendMessage("Your pet seems to be busy.");
            }
            else if (pet is IMount imount && imount.Rider != null)
            {
                from.SendMessage("You must dismount first.");
            }
            else if (ExchangeList.Count >= MaxStabled)
            {
                from.SendMessage("This hitching post is full.");
            }
            else
            {
                from.SendMessage("Select the minimum house access level required to claim this pet.");

                if (from.HasGump(typeof(SetAccessGump)))
                {
                    from.CloseGump(typeof(SetAccessGump));
                }

                from.SendGump(new SetAccessGump(this, from, pet));
            }
        }

        public void DoStable(Mobile from, BaseCreature pet, int level)
        {
            pet.ControlTarget = null;
            pet.ControlOrder = OrderType.Stay;
            pet.Internalize();

            pet.SetControlMaster(null);
            pet.SummonMaster = null;

            pet.IsStabled = true;
            pet.StabledBy = from;

            pet.Loyalty = BaseCreature.MaxLoyalty; // Wonderfully happy

            ExchangeList.Add(new ExchangeEntry(pet, from, DateTime.Now, level));

            from.SendLocalizedMessage(1049677); // Your pet has been stabled.

            InvalidateProperties();

            if (from.HasGump(typeof(ClaimListGump)))
            {
                BeginClaim(from);
            }
        }
        #endregion

        #region Claim
        public void BeginClaim(Mobile from)
        {
            if (Deleted || !from.CheckAlive())
            {
                return;
            }

            List<ExchangeEntry> list = new List<ExchangeEntry>();

            for (int i = 0; i < ExchangeList.Count; ++i)
            {
                BaseCreature pet = ExchangeList[i].Pet as BaseCreature;
                int access = ExchangeList[i].Access;

                if (pet == null || pet.Deleted)
                {
                    if (pet != null)
                    {
                        pet.IsStabled = false;
                        pet.StabledBy = null;
                    }

                    ExchangeList.RemoveAt(i--);
                    continue;
                }

                if (AccessAllowed(from, access))
                {
                    list.Add(ExchangeList[i]);
                }
            }

            if (list.Count > 0)
            {
                from.CloseGump(typeof(ClaimListGump));
                from.SendGump(new ClaimListGump(this, from, list));
            }
            else
            {
                from.SendMessage("You have no animals stabled at this hitching post.");
            }
        }

        public void EndClaim(Mobile from, ExchangeEntry entry)
        {
            if (entry.Pet == null || entry.Pet.Deleted || from.Map != Map || !from.CheckAlive() || !from.InRange(this, 14))
            {
                from.SendLocalizedMessage(1005213); // You can't do that
                return;
            }

            if (entry.Pet is BaseCreature pet && CanClaim(from, pet))
            {
                DoClaim(from, pet);
                DefragClaim(entry);
                InvalidateProperties();
            }
            else
            {
                from.SendLocalizedMessage(1049612, entry.Pet.Name); // ~1_NAME~ remained in the stables because you have too many followers.
            }
        }

        public bool CanClaim(Mobile from, BaseCreature pet)
        {
            return from.IsStaff() || (from.Followers + pet.ControlSlots <= from.FollowersMax);
        }

        private void DoClaim(Mobile from, BaseCreature pet)
        {
            pet.SetControlMaster(from);

            if (pet.Summoned)
            {
                pet.SummonMaster = from;
            }

            pet.ControlTarget = from;
            pet.ControlOrder = OrderType.Follow;

            pet.MoveToWorld(from.Location, from.Map);

            pet.IsStabled = false;
            pet.StabledBy = null;

            pet.Loyalty = BaseCreature.MaxLoyalty; // Wonderfully happy
        }

        public void DefragClaim(ExchangeEntry entry)
        {
            ExchangeList.Remove(entry);

            foreach (ExchangeEntry check in new List<ExchangeEntry>(ExchangeList))
            {
                if (check.Pet == null || check.Pet.Deleted)
                {
                    ExchangeList.Remove(check);
                }
            }
        }
        #endregion

        #region Functions
        public static string FormatTimeSpan(BaseCreature pet)
        {
            TimeSpan ts = (pet.BondingBegin + pet.BondingDelay) - DateTime.UtcNow;

            return string.Format("Bonding: {0:D2}:{1:D2}:{2:D2}:{3:D2}", ts.Days, ts.Hours % 24, ts.Minutes % 60, ts.Seconds % 60);
        }
        #endregion

        #region Serialization
        public PetExchange(Serial serial) : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version

            writer.Write(ExchangeList.Count);

            for (int i = 0; i < ExchangeList.Count; i++)
            {
                writer.Write(ExchangeList[i].Pet);
                writer.Write(ExchangeList[i].From);
                writer.Write(ExchangeList[i].Stamp);
                writer.Write(ExchangeList[i].Access);
            }
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    {
                        int subcount = reader.ReadInt();

                        for (int i = 0; i < subcount; i++)
                        {
                            ExchangeList.Add(new ExchangeEntry(
                                reader.ReadMobile(),
                                reader.ReadMobile(),
                                reader.ReadDateTime(),
                                reader.ReadInt()));
                        }

                        break;
                    }
            }
        }
        #endregion

        #region Helper Classes
        public class ExchangeEntry
        {
            public Mobile Pet { get; private set; }
            public Mobile From { get; private set; }
            public DateTime Stamp { get; private set; }
            public int Access { get; private set; }

            public ExchangeEntry(Mobile pet, Mobile from, DateTime stamp, int access)
            {
                Pet = pet;
                From = from;
                Stamp = stamp;
                Access = access;
            }
        }

        private class StableEntry : ContextMenuEntry
        {
            private readonly Mobile _From;
            private readonly PetExchange _Exchange;

            public StableEntry(PetExchange exchange, Mobile from) : base(6126, 12)
            {
                _Exchange = exchange;
                _From = from;
            }

            public override void OnClick()
            {
                _Exchange.BeginStable(_From);
            }
        }

        private class ClaimEntry : ContextMenuEntry
        {
            private readonly Mobile _From;
            private readonly PetExchange _Exchange;

            public ClaimEntry(PetExchange exchange, Mobile from) : base(3006127, 12)
            {
                _Exchange = exchange;
                _From = from;
            }

            public override void OnClick()
            {
                _Exchange.BeginClaim(_From);
            }
        }

        private class StableTarget : Target
        {
            private readonly PetExchange _Exchange;

            public StableTarget(PetExchange exchange) : base(12, false, TargetFlags.None)
            {
                _Exchange = exchange;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (targeted is BaseCreature bc)
                {
                    _Exchange.EndStable(from, bc);
                    return;
                }

                from.SendLocalizedMessage(1048053); // You can't stable that!
            }
        }
        #endregion

        #region Gumps
        private class ClaimListGump : Gump
        {
            private readonly Mobile _From;
            private readonly List<ExchangeEntry> _List;
            private readonly PetExchange _Exchange;
            private int _Page;

            public ClaimListGump(PetExchange exchange, Mobile from, List<ExchangeEntry> list, int page = 0) : base(50, 50)
            {
                _Exchange = exchange;
                _From = from;
                _List = list;
                _Page = page;

                int width = 250;
                int height = 200;
                int length = Math.Min(list.Count, 4);

                if (length == 1)
                {
                    width += 50;
                }

                AddPage(0);
                AddBackground(0, 0, (width * length), height, 0x6DB);
                AddHtml(15, 15, 300, 20, FormatString("Select a pet to retrieve from the hitching post:", "#FFFF00"), false, false);

                if (list.Count > (4 + page))
                {
                    AddButton((width * length) - 50, (height / 2), 0x15E1, 0x15E5, 100, GumpButtonType.Reply, 0);
                }

                if (page > 0)
                {
                    AddButton(10, (height / 2), 0x15E3, 0x15E7, 101, GumpButtonType.Reply, 0);
                }

                for (var i = page; i < (length + page); ++i)
                {
                    BaseCreature pet = list[i].Pet as BaseCreature;
                    Mobile stabledby = list[i].From;
                    int access = list[i].Access;

                    int n = i - page;

                    AddItem(100 + (n * width), (height - 150), ShrinkTable.Lookup(pet.Body), pet.Hue);

                    AddButton(50 + (n * width), (height - 90), 4006, 4007, i + 1, GumpButtonType.Reply, 0);
                    AddHtml(100 + (n * width), (height - 90), 275, 18, FormatString(pet.Name, "#FFFFFF"), false, false);

                    AddHtml(100 + (n * width), (height - 70), 150, 18, FormatString($"{stabledby.Name}", "#FFFFFF"), false, false);

                    AddHtml(100 + (n * width), (height - 50), 100, 18, FormatString($"Access: {(AccessList)access}", "#FFFFFF"), false, false);

                    if (pet.BondingBegin > DateTime.MinValue)
                    {
                        AddHtml(100 + (n * width), (height - 30), 200, 18, FormatString(PetExchange.FormatTimeSpan(pet), "#FFFFFF"), false, false);
                    }
                    else
                    {
                        AddHtml(100 + (n * width), (height - 30), 100, 18, FormatString(pet.IsBonded ? "Bonded" : "Tamed", "#FFFFFF"), false, false);
                    }
                }
            }

            public string FormatString(string str, string color, bool left = true)
            {
                if (left)
                {
                    return string.Format("<BASEFONT COLOR={1}>{0}", str, color);
                }

                return string.Format("<BASEFONT COLOR={1}><CENTER>{0}</CENTER>", str, color);
            }

            public override void OnResponse(NetState sender, RelayInfo info)
            {
                switch (info.ButtonID)
                {
                    case 0:
                        return; // Close
                    case 100:
                        _Page++;
                        break; // Page forward
                    case 101:
                        _Page--;
                        break; // Page backward
                    default:
                        {
                            var index = info.ButtonID - 1;

                            if (index >= 0 && index < _List.Count)
                            {
                                _Exchange.EndClaim(_From, _List[index]);
                                return;
                            }

                            break;
                        }
                }

                _From.SendGump(new ClaimListGump(_Exchange, _From, _List, _Page));
            }
        }

        private class SetAccessGump : Gump
        {
            private readonly Mobile _From;
            private readonly BaseCreature _Pet;
            private readonly PetExchange _Exchange;

            public SetAccessGump(PetExchange exchange, Mobile from, BaseCreature pet) : base(50, 50)
            {
                _Exchange = exchange;
                _From = from;
                _Pet = pet;

                AddPage(0);
                AddBackground(0, 0, 220, 160, 0x6DB);
                AddHtmlLocalized(10, 10, 200, 20, 1061276, 32767, false, false); // <CENTER>SET ACCESS</CENTER>

                AddButton(10, 70, 4006, 4007, 1, GumpButtonType.Reply, 0);
                AddHtmlLocalized(45, 70, 150, 20, 1061277, 0x7FFF, false, false); // Owner Only

                AddButton(10, 90, 4006, 4007, 2, GumpButtonType.Reply, 0);
                AddHtmlLocalized(45, 90, 150, 20, 1061278, 0x7FFF, false, false); // Co-Owners

                AddButton(10, 110, 4006, 4007, 3, GumpButtonType.Reply, 0);
                AddHtmlLocalized(45, 110, 150, 20, 1061279, 0x7FFF, false, false); // Friends
            }

            public override void OnResponse(NetState state, RelayInfo info)
            {
                if (info.ButtonID == 0)
                {
                    return;
                }

                _From.SendLocalizedMessage(1061280); // New access level set.
                _Exchange.DoStable(_From, _Pet, info.ButtonID - 1);
            }
        }
        #endregion
    }
}
