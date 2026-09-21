using System;
using Server;
using Server.Accounting;
using Server.Commands;
using Server.Gumps;
using Server.Items;
using Server.Multis;
using Server.Multis.Deeds;
using Server.Network;

namespace Server.Custom
{
    // A blessed voucher that allows choosing from the 6 classic small cottage styles
    public class StarterHouseVoucher : Item
    {
        [Constructable]
        public StarterHouseVoucher() : base(0x14F0) // Deed/scroll graphic
        {
            Name = "Starter Cottage Deed Voucher";
            LootType = LootType.Blessed;
            Weight = 0;
            Hue = 0x482; // Gilded parchment hue
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (!IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
                return;
            }

            from.CloseGump(typeof(StarterHouseSelectionGump));
            from.SendGump(new StarterHouseSelectionGump(from, this));
        }

        public StarterHouseVoucher(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) => base.Serialize(writer);
        public override void Deserialize(GenericReader reader) => base.Deserialize(reader);
    }

    public class StarterHouseSelectionGump : Gump
    {
        private readonly StarterHouseVoucher m_Voucher;

        private static readonly string[] HouseNames = new string[]
        {
            "Small Stone and Plaster House",
            "Small Field Stone House",
            "Small Brick House",
            "Small Wooden House",
            "Small Wood and Plaster House",
            "Thatched-Roof Cottage"
        };

        public StarterHouseSelectionGump(Mobile from, StarterHouseVoucher voucher) : base(100, 100)
        {
            m_Voucher = voucher;

            Closable = true;
            Disposable = true;
            Dragable = true;
            Resizable = false;

            AddPage(0);
            AddBackground(0, 0, 420, 355, 5054);
            AddImageTiled(10, 10, 400, 25, 2624);
            AddAlphaRegion(10, 10, 400, 25);
            AddHtml(15, 12, 390, 20, "<CENTER><BASEFONT COLOR=#F5D77F><B>Choose Your Starter Cottage</B></BASEFONT></CENTER>", false, false);

            AddImageTiled(10, 40, 400, 260, 2624);
            AddAlphaRegion(10, 40, 400, 260);

            string intro = "Select an architectural style for your estate (all 7x7 classic footprints). Your chosen deed will be placed in your backpack.";
            AddHtml(20, 48, 380, 35, intro, false, false);

            for (int i = 0; i < HouseNames.Length; i++)
            {
                int y = 90 + (i * 34);
                AddButton(25, y, 4005, 4007, i + 1, GumpButtonType.Reply, 0);
                AddLabel(65, y + 2, 0x480, HouseNames[i]);
            }

            AddButton(170, 315, 4017, 4019, 0, GumpButtonType.Reply, 0);
            AddLabel(205, 317, 0x384, "Decide Later");
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            Mobile from = sender.Mobile;
            if (from == null || from.Backpack == null)
                return;

            if (info.ButtonID < 1 || info.ButtonID > 6)
                return;

            HouseDeed newDeed = null;
            switch (info.ButtonID)
            {
                case 1: newDeed = new StonePlasterHouseDeed(); break;
                case 2: newDeed = new FieldStoneHouseDeed(); break;
                case 3: newDeed = new SmallBrickHouseDeed(); break;
                case 4: newDeed = new WoodHouseDeed(); break;
                case 5: newDeed = new WoodPlasterHouseDeed(); break;
                case 6: newDeed = new ThatchedRoofCottageDeed(); break;
            }

            if (newDeed == null)
                return;

            newDeed.LootType = LootType.Blessed;
            string selectedName = HouseNames[info.ButtonID - 1];

            if (m_Voucher != null && !m_Voucher.Deleted && m_Voucher.IsChildOf(from.Backpack))
            {
                m_Voucher.Delete();
                from.AddToBackpack(newDeed);
                from.SendMessage(68, $"You received the {selectedName} Deed! Double-click it in your pack to begin placing it.");
                from.PlaySound(0x5C9);
            }
        }
    }

    public class StarterKitDistribution
    {
        [CallPriority(100)]
        public static void Initialize()
        {
            EventSink.CharacterCreated += EventSink_CharacterCreated;
            EventSink.Login += EventSink_Login;
            CommandSystem.Register("ClaimStarterKit", AccessLevel.Player, ClaimStarterKit_OnCommand);
            Console.WriteLine("[StarterKit] StarterKitDistribution initialized with priority 100.");
        }

        [Usage("ClaimStarterKit")]
        [Description("Claims starter tools, satchel, spellbook, and runebook gear if not already received.")]
        private static void ClaimStarterKit_OnCommand(CommandEventArgs e)
        {
            if (e.Mobile != null)
            {
                if (CheckAndDistributeStarterKit(e.Mobile, true))
                {
                    e.Mobile.SendMessage(68, "Starter kit granted!");
                }
                else
                {
                    e.Mobile.SendMessage(53, "You have already received your character starter kit.");
                    if (e.Mobile.Backpack != null)
                    {
                        e.Mobile.Use(e.Mobile.Backpack);
                    }
                }
            }
        }

        private static void EventSink_CharacterCreated(CharacterCreatedEventArgs args)
        {
            if (args.Mobile != null)
            {
                CheckAndDistributeStarterKit(args.Mobile, false);
            }
        }

        private static void EventSink_Login(LoginEventArgs args)
        {
            Mobile m = args.Mobile;
            if (m != null)
            {
                // Delay slightly after login so client has finished loading world and packets
                Timer.DelayCall(TimeSpan.FromSeconds(1.5), () =>
                {
                    if (m.NetState != null)
                    {
                        CheckAndDistributeStarterKit(m, false);
                    }
                });
            }
        }

        public static bool CheckAndDistributeStarterKit(Mobile m, bool manualClaim)
        {
            if (m == null || m.Backpack == null)
                return false;

            Account acct = m.Account as Account;
            string charTagKey = $"StarterKit_{m.Serial}";

            bool hasSatchel = m.Backpack.FindItemByType<FestersResourceSatchel>() != null;
            bool alreadyClaimed = (acct != null && acct.GetTag(charTagKey) != null) || hasSatchel;

            if (alreadyClaimed)
            {
                if (acct != null && acct.GetTag(charTagKey) == null)
                    acct.SetTag(charTagKey, "true");

                return false;
            }

            Container pack = m.Backpack;

            // 1. Create and pack the personal Resource Satchel with 16 blank recall runes for every character
            FestersResourceSatchel satchel = new FestersResourceSatchel();
            int runeCount = Config.Get("StarterKit.BlankRunes", 16);
            for (int i = 0; i < runeCount; i++)
            {
                satchel.DropItem(new RecallRune());
            }
            pack.DropItem(satchel);

            // 2. Add the complete indestructible auto-tool set for every character
            pack.DropItem(new FestersPickaxe());
            pack.DropItem(new FestersHatchet());
            pack.DropItem(new FestersSkinningKnife());
            pack.DropItem(new FestersFishingPole());
            pack.DropItem(new FestersScythe());

            // 3. Add blessed full spellbook and blessed runebook (20 charges)
            Spellbook spellbook = new Spellbook
            {
                Content = ulong.MaxValue,
                LootType = LootType.Blessed
            };
            pack.DropItem(spellbook);

            int runebookCharges = Config.Get("StarterKit.RunebookCharges", 20);
            Runebook runebook = new Runebook(Math.Max(20, runebookCharges))
            {
                LootType = LootType.Blessed,
                CurCharges = runebookCharges
            };
            pack.DropItem(runebook);

            // 4. Account-level one-time distribution: Boat Deed and Starter Cottage Voucher
            // Prevents multiple characters on the same account from accumulating duplicate deeds
            bool isFirstCharacterOnAccount = false;

            if (acct != null)
            {
                if (acct.GetTag("StarterEstateClaimed") == null)
                {
                    acct.SetTag("StarterEstateClaimed", "true");
                    isFirstCharacterOnAccount = true;

                    SmallBoatDeed boatDeed = new SmallBoatDeed { LootType = LootType.Blessed };
                    StarterHouseVoucher voucher = new StarterHouseVoucher();
                    FesterUOGuideBook guide = new FesterUOGuideBook();
                    AnkhOfSacrificeDeed ankhDeed = new AnkhOfSacrificeDeed { LootType = LootType.Blessed };
                    FestersMoongateAddonDeed gateDeed = new FestersMoongateAddonDeed();

                    pack.DropItem(boatDeed);
                    pack.DropItem(voucher);
                    pack.DropItem(guide);
                    pack.DropItem(ankhDeed);
                    pack.DropItem(gateDeed);

                    // Automatically open the guide book after client login sequence completes
                    Timer.DelayCall(TimeSpan.FromSeconds(3.0), () =>
                    {
                        if (m.NetState != null && !guide.Deleted && guide.IsChildOf(m.Backpack))
                        {
                            guide.OnDoubleClick(m);
                        }
                    });
                }

                acct.SetTag(charTagKey, "true");
            }

            // Automatically open backpack for the player
            Timer.DelayCall(TimeSpan.FromSeconds(0.5), () =>
            {
                if (m.NetState != null && m.Backpack != null)
                {
                    m.Use(m.Backpack);
                }
            });

            if (isFirstCharacterOnAccount)
            {
                m.SendMessage(68, "Welcome to FesterUO! Please read your Adventurer's Guide for commands, features, and tools.");
                m.SendMessage(68, "Your pack contains your Cottage Voucher, Boat Deed, Ankh of Sacrifice Deed, and House Moongate Deed!");
            }
            else
            {
                m.SendMessage(68, "Welcome to FesterUO! Your starter tools and resource satchel have been placed in your pack.");
            }

            Console.WriteLine($"[StarterKit] Successfully delivered starter kit to {m.Name} (Serial={m.Serial}, Account={acct?.Username ?? "None"})");
            return true;
        }
    }
}
