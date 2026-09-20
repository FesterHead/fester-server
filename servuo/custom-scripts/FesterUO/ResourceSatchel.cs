using System;
using Server;
using Server.Items;
using Server.Engines.Plants;

namespace Server.Custom
{
    public class FestersResourceSatchel : Backpack
    {
        [CommandProperty(AccessLevel.GameMaster)]
        public int WeightReduction { get; set; } = 100;

        [Constructable]
        public FestersResourceSatchel()
        {
            Name = "Master Gatherer's Satchel";
            Hue = 1195; // Forest moss / deep slate hue
            LootType = LootType.Blessed;
            WeightReduction = 100;
            MaxItems = 1000;
            Weight = 0.0;
        }

        // Satchel itself has 0 stone weight
        public override double DefaultWeight => 0.0;

        // Recursive count of items contained inside the satchel
        public int ContainedItemsCount
        {
            get
            {
                int count = 0;
                var items = Items;
                if (items != null)
                {
                    for (int i = 0; i < items.Count; i++)
                    {
                        Item item = items[i];
                        if (item != null && !item.IsVirtualItem)
                        {
                            count += item.TotalItems + 1;
                        }
                    }
                }
                return count;
            }
        }

        // Contents inside satchel contribute 0 total weight and 0 items to parent containers
        // The parent backpack will only count 1 item (the satchel itself: item.TotalItems + 1 = 0 + 1 = 1)
        public override int GetTotal(TotalType type)
        {
            if (type == TotalType.Weight && WeightReduction >= 100)
                return 0;

            if (type == TotalType.Items)
                return 0;

            int total = base.GetTotal(type);

            if (type == TotalType.Weight && WeightReduction > 0)
                total -= total * WeightReduction / 100;

            return total;
        }

        // Prevent weight and item count changes from propagating upward to parent backpack or player
        public override void UpdateTotal(Item sender, TotalType type, int delta)
        {
            if (type == TotalType.Weight && WeightReduction >= 100)
            {
                InvalidateProperties();
                return;
            }

            if (type == TotalType.Weight && WeightReduction > 0)
                delta -= delta * WeightReduction / 100;

            if (type == TotalType.Items)
            {
                InvalidateProperties();
                return;
            }

            base.UpdateTotal(sender, type, delta);
        }

        // When an item enters the satchel, neutralize its weight to 0
        public override void OnItemAdded(Item item)
        {
            base.OnItemAdded(item);

            if (item != null)
            {
                item.Weight = 0.0;
            }
        }

        // When an item is withdrawn from the satchel, restore its natural default weight
        public override void OnItemRemoved(Item item)
        {
            base.OnItemRemoved(item);

            if (item != null)
            {
                Timer.DelayCall(TimeSpan.Zero, () =>
                {
                    if (item != null && !item.Deleted && !item.IsChildOf(this))
                    {
                        item.Weight = -1;
                    }
                });
            }
        }

        public override bool DisplaysContent => false;

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);
            list.Add(1073841, "{0}\t{1}\t{2}", ContainedItemsCount, MaxItems, 0); // Contents: ~1_COUNT~/~2_MAXCOUNT~ items, ~3_WEIGHT~ stones
            list.Add(1072210, "100"); // Weight reduction: ~1_PERCENTAGE~%
        }

        public override void OnSingleClick(Mobile from)
        {
            base.OnSingleClick(from);
            LabelTo(from, "({0} items, 0 stones)", ContainedItemsCount);
        }

        // Validate items placed into the satchel and satchel capacity without counting against parent backpack
        public override bool CheckHold(Mobile from, Item item, bool message, bool checkItems, int plusItems, int plusWeight)
        {
            if (item == null)
                return false;

            if (!IsGatheredResource(item))
            {
                if (message)
                    from?.SendMessage(38, "The satchel only accepts raw or converted harvesting resources and recall runes.");

                return false;
            }

            int maxItems = MaxItems;
            if (checkItems && maxItems != 0 && (ContainedItemsCount + plusItems + item.TotalItems + (item.IsVirtualItem ? 0 : 1)) > maxItems)
            {
                if (message)
                    SendFullItemsMessage(from, item);

                return false;
            }

            return true;
        }

        public static bool IsGatheredResource(Item item)
        {
            if (item == null)
                return false;

            return item is RecallRune ||
                   item is ICommodity ||
                   item is IGem ||
                   item is BaseIngot ||
                   item is BaseWoodBoard ||
                   item is BaseLog ||
                   item is BaseLeather ||
                   item is BaseHides ||
                   item is BaseScales ||
                   item is BaseGranite ||
                   item is BaseOre ||
                   item is Cloth ||
                   item is BoltOfCloth ||
                   item is BaseClothMaterial ||
                   item is Wool ||
                   item is Cotton ||
                   item is Flax ||
                   item is Fish ||
                   item is BaseFish ||
                   item is BigFish ||
                   item is FishSteak ||
                   item is RawFishSteak ||
                   item is BaseReagent ||
                   item is FertileDirt ||
                   item is Bone ||
                   item is Feather ||
                   item is Kindling ||
                   item is Seed ||
                   item is PlantClippings ||
                   item is Gold ||
                   item is BankCheck ||
                   item is Bandage ||
                   item is CookableFood ||
                   item is CookedBird ||
                   item is Ribs ||
                   item is LambLeg ||
                   item is ChickenLeg ||
                   item is Bacon ||
                   item is SlabOfBacon ||
                   item is Ham ||
                   item is Sausage ||
                   item is RoastPig;
        }

        // Global routing helper: deposits into satchel if found, else falls back to backpack
        public static void Deposit(Mobile from, Item resource)
        {
            if (from == null || resource == null)
                return;

            if (from.Backpack != null)
            {
                FestersResourceSatchel satchel = from.Backpack.FindItemByType<FestersResourceSatchel>();

                if (satchel != null && satchel.TryDropItem(from, resource, false))
                {
                    return;
                }
            }

            from.AddToBackpack(resource);
        }

        public FestersResourceSatchel(Serial serial) : base(serial) { }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)1); // version
            writer.Write(WeightReduction);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
            WeightReduction = reader.ReadInt();

            Weight = 0.0;

            // Ensure all contained items in existing world saves are 0 weight
            if (Items != null)
            {
                for (int i = 0; i < Items.Count; i++)
                {
                    if (Items[i] != null)
                    {
                        Items[i].Weight = 0.0;
                    }
                }
            }
        }
    }
}
