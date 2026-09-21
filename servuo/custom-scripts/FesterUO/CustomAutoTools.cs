using System;
using System.Collections.Generic;
using Server;
using Server.Engines.Harvest;
using Server.Items;
using Server.Mobiles;
using Server.Targeting;

namespace Server.Custom
{
    // =========================================================================
    // GLOBAL HARVEST EVENT HOOK
    // Automatically intercepts harvest success when using custom tools
    // =========================================================================
    public static class CustomAutoToolsHandler
    {
        public static void Initialize()
        {
            EventSink.ResourceHarvestSuccess += OnResourceHarvestSuccess;
        }

        private static void OnResourceHarvestSuccess(ResourceHarvestSuccessEventArgs e)
        {
            if (e.Harvester == null || e.Tool == null)
                return;

            if (e.Tool is FestersPickaxe)
            {
                FestersPickaxe.ProcessOre(e.Harvester);
            }
            else if (e.Tool is FestersHatchet)
            {
                FestersHatchet.ProcessLogs(e.Harvester);
            }
            else if (e.Tool is FestersFishingPole)
            {
                FestersFishingPole.ProcessFish(e.Harvester);
            }
        }
    }

    // =========================================================================
    // 1. FESTER'S PICKAXE (Auto-Smelt Ore -> Ingots to Satchel)
    // =========================================================================
    public class FestersPickaxe : Pickaxe
    {
        public override HarvestSystem HarvestSystem => Mining.System;

        [Constructable]
        public FestersPickaxe() : base()
        {
            Name = "Smelter's Pickaxe";
            Hue = 1161; // Burnished gold
            LootType = LootType.Blessed;
            UsesRemaining = 99999;
            ShowUsesRemaining = false;
        }

        public override void OnSingleClick(Mobile from)
        {
            base.OnSingleClick(from);
            LabelTo(from, "[Indestructible & Auto-Smelt]", 68);
        }

        public override void OnDoubleClick(Mobile from)
        {
            ProcessOre(from);
            base.OnDoubleClick(from);
        }

        public static void ProcessOre(Mobile from)
        {
            if (from?.Backpack == null)
                return;

            // Find any newly mined ore in the backpack and convert directly to ingots
            List<Item> ores = new List<Item>(from.Backpack.FindItemsByType(typeof(BaseOre)));

            foreach (Item oreItem in ores)
            {
                if (oreItem is BaseOre ore)
                {
                    int ingotAmount = ore.Amount * 2; // Standard 1 ore -> 2 ingots ratio
                    CraftResource resourceType = ore.Resource;
                    ore.Delete();

                    Item ingots;

                    switch (resourceType)
                    {
                        case CraftResource.DullCopper: ingots = new DullCopperIngot(ingotAmount); break;
                        case CraftResource.ShadowIron:  ingots = new ShadowIronIngot(ingotAmount); break;
                        case CraftResource.Copper:      ingots = new CopperIngot(ingotAmount); break;
                        case CraftResource.Bronze:      ingots = new BronzeIngot(ingotAmount); break;
                        case CraftResource.Gold:        ingots = new GoldIngot(ingotAmount); break;
                        case CraftResource.Agapite:     ingots = new AgapiteIngot(ingotAmount); break;
                        case CraftResource.Verite:      ingots = new VeriteIngot(ingotAmount); break;
                        case CraftResource.Valorite:    ingots = new ValoriteIngot(ingotAmount); break;
                        default:                        ingots = new IronIngot(ingotAmount); break;
                    }

                    FestersResourceSatchel.Deposit(from, ingots);
                    from.SendMessage(68, $"You instantly smelt the vein into {ingotAmount} ingots and stow them.");
                }
            }

            // Also tuck any mined granite straight into the satchel
            List<Item> granites = new List<Item>(from.Backpack.FindItemsByType(typeof(BaseGranite)));
            foreach (Item granite in granites)
            {
                FestersResourceSatchel.Deposit(from, granite);
            }
        }

        public FestersPickaxe(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) => base.Serialize(writer);
        public override void Deserialize(GenericReader reader) => base.Deserialize(reader);
    }

    // =========================================================================
    // 2. FESTER'S HATCHET (Auto-Saw Logs -> Boards to Satchel)
    // =========================================================================
    public class FestersHatchet : Hatchet
    {
        public override HarvestSystem HarvestSystem => Lumberjacking.System;

        [Constructable]
        public FestersHatchet() : base()
        {
            Name = "Forester's Hatchet";
            Hue = 1436; // Deep evergreen
            LootType = LootType.Blessed;
            UsesRemaining = 99999;
            ShowUsesRemaining = false;
        }

        public override void OnSingleClick(Mobile from)
        {
            base.OnSingleClick(from);
            LabelTo(from, "[Indestructible & Auto-Saw]", 68);
        }

        public override void OnDoubleClick(Mobile from)
        {
            ProcessLogs(from);
            base.OnDoubleClick(from);
        }

        public static void ProcessLogs(Mobile from)
        {
            if (from?.Backpack == null)
                return;

            List<Item> logs = new List<Item>(from.Backpack.FindItemsByType(typeof(BaseLog)));

            foreach (Item logItem in logs)
            {
                if (logItem is BaseLog log)
                {
                    int boardAmount = log.Amount;
                    CraftResource resourceType = log.Resource;
                    log.Delete();

                    Item boards;

                    switch (resourceType)
                    {
                        case CraftResource.OakWood:      boards = new OakBoard(boardAmount); break;
                        case CraftResource.AshWood:      boards = new AshBoard(boardAmount); break;
                        case CraftResource.YewWood:      boards = new YewBoard(boardAmount); break;
                        case CraftResource.Heartwood:    boards = new HeartwoodBoard(boardAmount); break;
                        case CraftResource.Bloodwood:    boards = new BloodwoodBoard(boardAmount); break;
                        case CraftResource.Frostwood:    boards = new FrostwoodBoard(boardAmount); break;
                        default:                         boards = new Board(boardAmount); break;
                    }

                    FestersResourceSatchel.Deposit(from, boards);
                    from.SendMessage(68, $"You mill the timber into {boardAmount} boards and stow them.");
                }
            }
        }

        public FestersHatchet(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) => base.Serialize(writer);
        public override void Deserialize(GenericReader reader) => base.Deserialize(reader);
    }

    // =========================================================================
    // 3. FESTER'S FISHING POLE (Auto-Fillet Steaks & Trash Purge to Satchel)
    // =========================================================================
    public class FestersFishingPole : FishingPole
    {
        [Constructable]
        public FestersFishingPole() : base()
        {
            Name = "Master Angler's Rod";
            Hue = 1153; // Ocean cerulean
            LootType = LootType.Blessed;
            UsesRemaining = 99999;
            ShowUsesRemaining = false;
        }

        public override void OnSingleClick(Mobile from)
        {
            base.OnSingleClick(from);
            LabelTo(from, "[Indestructible & Auto-Fillet]", 68);
        }

        public override void OnDoubleClick(Mobile from)
        {
            ProcessFish(from);
            base.OnDoubleClick(from);
        }

        public static void ProcessFish(Mobile from)
        {
            if (from?.Backpack == null)
                return;

            // Turn raw whole fish into clean fish steaks
            List<Item> fishList = new List<Item>();
            foreach (Item item in from.Backpack.Items)
            {
                if (item is Fish || item is BaseFish || item is BigFish ||
                    (item is BaseHighseasFish && !(item is RareFish) && !(item is BaseCrabAndLobster)))
                {
                    fishList.Add(item);
                }
            }

            foreach (Item found in fishList)
            {
                int steaksCount = found.Amount * 4;
                found.Delete();

                RawFishSteak steaks = new RawFishSteak(steaksCount);
                FestersResourceSatchel.Deposit(from, steaks);
                from.SendMessage(68, $"You reel in a catch and stow {steaksCount} clean raw fish steaks.");
            }

            // Clean up soggy shoes or junk boots caught on the line
            List<Item> shoes = new List<Item>();
            foreach (Item item in from.Backpack.Items)
            {
                if (item is BaseShoes shoe && shoe.Hue == 0 && shoe.LootType != LootType.Blessed)
                {
                    shoes.Add(shoe);
                }
            }
            foreach (Item shoe in shoes)
            {
                shoe.Delete();
                from.SendMessage(38, "You toss waterlogged junk back into the depths.");
            }
        }

        public FestersFishingPole(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) => base.Serialize(writer);
        public override void Deserialize(GenericReader reader) => base.Deserialize(reader);
    }

    // =========================================================================
    // 4. FESTER'S SKINNING KNIFE (Auto Cut-Leather & Auto-Shear to Satchel)
    // =========================================================================
    public class FestersSkinningKnife : SkinningKnife
    {
        [Constructable]
        public FestersSkinningKnife() : base()
        {
            Name = "Tanner's Skinning Knife";
            Hue = 1170; // Saddle leather brown
            LootType = LootType.Blessed;
            UsesRemaining = 99999;
            ShowUsesRemaining = false;
        }

        public override void OnSingleClick(Mobile from)
        {
            base.OnSingleClick(from);
            LabelTo(from, "[Indestructible & Auto-Dressing]", 68);
        }

        public override void OnDoubleClick(Mobile from)
        {
            from.SendMessage(68, "Target an animal corpse to skin or a living sheep to shear:");
            from.Target = new InternalTannerTarget(this);
        }

        public static void ProcessCorpseHarvest(Mobile from, Corpse corpse)
        {
            if (from == null || corpse == null)
                return;

            List<Item> harvestables = new List<Item>();
            foreach (Item subItem in corpse.Items)
            {
                if (subItem is BaseHides || subItem is BaseScales || subItem is Feather || subItem is Wool || subItem is CookableFood)
                {
                    harvestables.Add(subItem);
                }
            }

            int meatsHarvested = 0;
            int feathersHarvested = 0;
            int scalesHarvested = 0;

            foreach (Item item in harvestables)
            {
                if (item is BaseHides hide)
                {
                    int amount = hide.Amount;
                    CraftResource res = hide.Resource;
                    hide.Delete();

                    Item leather;
                    switch (res)
                    {
                        case CraftResource.SpinedLeather: leather = new SpinedLeather(amount); break;
                        case CraftResource.HornedLeather: leather = new HornedLeather(amount); break;
                        case CraftResource.BarbedLeather: leather = new BarbedLeather(amount); break;
                        default:                          leather = new Leather(amount); break;
                    }

                    FestersResourceSatchel.Deposit(from, leather);
                    from.SendMessage(68, $"You skin the beast and stow {amount} cut leather into your satchel.");
                }
                else if (item is BaseScales)
                {
                    scalesHarvested += item.Amount;
                    FestersResourceSatchel.Deposit(from, item);
                }
                else if (item is Feather)
                {
                    feathersHarvested += item.Amount;
                    FestersResourceSatchel.Deposit(from, item);
                }
                else if (item is Wool)
                {
                    FestersResourceSatchel.Deposit(from, item);
                    from.SendMessage(68, "You harvest fleece and stow it into your satchel.");
                }
                else if (item is CookableFood)
                {
                    meatsHarvested += item.Amount;
                    FestersResourceSatchel.Deposit(from, item);
                }
            }

            if (feathersHarvested > 0)
                from.SendMessage(68, $"You pluck {feathersHarvested} feathers and stow them into your satchel.");

            if (scalesHarvested > 0)
                from.SendMessage(68, $"You harvest {scalesHarvested} dragon scales and stow them into your satchel.");

            if (meatsHarvested > 0)
                from.SendMessage(68, $"You butcher {meatsHarvested} fresh meat and stow it into your satchel.");
        }

        private class InternalTannerTarget : Target
        {
            private readonly FestersSkinningKnife m_Knife;

            public InternalTannerTarget(FestersSkinningKnife knife) : base(3, false, TargetFlags.None)
            {
                m_Knife = knife;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (targeted is Corpse corpse)
                {
                    if (!corpse.Carved)
                    {
                        corpse.Carve(from, m_Knife);
                        ProcessCorpseHarvest(from, corpse);
                    }
                    else
                    {
                        from.SendMessage(38, "That corpse has already been carved.");
                    }
                }
                else if (targeted is Sheep sheep)
                {
                    if (sheep.Alive && !sheep.Controlled)
                    {
                        sheep.PlaySound(0xD6);
                        sheep.BodyValue = 0xDF; // Sheared graphic

                        BoltOfCloth cloth = new BoltOfCloth(2);
                        FestersResourceSatchel.Deposit(from, cloth);
                        from.SendMessage(68, "You shear the fleece and pack 2 bolts of cloth directly into your satchel.");
                    }
                }
                else
                {
                    from.SendMessage(38, "You can only use this on animal corpses or sheep.");
                }
            }
        }

        public FestersSkinningKnife(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) => base.Serialize(writer);
        public override void Deserialize(GenericReader reader) => base.Deserialize(reader);
    }

    // =========================================================================
    // 5. FESTER'S SCYTHE (Radius Crop Sweeper & Auto-Loom to Satchel)
    // =========================================================================
    public class FestersScythe : Scythe
    {
        [Constructable]
        public FestersScythe() : base()
        {
            Name = "Harvester's Scythe";
            Hue = 1365; // Amber grain
            LootType = LootType.Blessed;
            UsesRemaining = 99999;
            ShowUsesRemaining = false;
        }

        public override void OnSingleClick(Mobile from)
        {
            base.OnSingleClick(from);
            LabelTo(from, "[Indestructible & Field Sweeper]", 68);
        }

        public override void OnDoubleClick(Mobile from)
        {
            Map map = from.Map;
            if (map == null) return;

            // 3-tile area sweep around player
            IPooledEnumerable eable = map.GetItemsInRange(from.Location, 3);
            int clothHarvested = 0;
            int threadHarvested = 0;

            foreach (Item item in eable)
            {
                // Cotton plants -> Bolts of Cloth
                if (item.ItemID >= 0x0C51 && item.ItemID <= 0x0C54)
                {
                    item.Delete();
                    BoltOfCloth bolt = new BoltOfCloth(1);
                    FestersResourceSatchel.Deposit(from, bolt);
                    clothHarvested++;
                }
                // Flax plants -> Spools of Thread
                else if (item.ItemID >= 0x1A99 && item.ItemID <= 0x1A9B)
                {
                    item.Delete();
                    SpoolOfThread thread = new SpoolOfThread(3);
                    FestersResourceSatchel.Deposit(from, thread);
                    threadHarvested++;
                }
            }
            eable.Free();

            if (clothHarvested > 0 || threadHarvested > 0)
            {
                from.PlaySound(0x248);
                from.SendMessage(68, $"You scythe the field: {clothHarvested} cloth bolts and {threadHarvested} thread bundles stowed.");
            }
            else
            {
                from.SendMessage(38, "No mature cotton or flax plants found within 3 tiles.");
            }
        }

        public FestersScythe(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) => base.Serialize(writer);
        public override void Deserialize(GenericReader reader) => base.Deserialize(reader);
    }
}
