/*
 * UO Community Script: The Ultimate ARPG-Style Loot Filter
 * Source: https://www.servuo.dev/archive/the-ultimate-arpg-style-loot-filter-diablo-poe-exactly-like-you-want-it.2606/
 *
 * Optional/utility helper demonstrating ARPG-style ground scattering and drop filtering.
 */

using System;
using System.Collections.Generic;
using Server;
using Server.Items;
using Server.Mobiles;

namespace Server.Engines.LootFilter
{
    public static class LootGenerator
    {
        public static void GenerateDrops(Mobile killed, Mobile killer)
        {
            if (killed == null || killed.Map == null || killed.Map == Map.Internal)
                return;

            List<Item> loot = new List<Item>();

            // 1. Collect loot from the corpse or generate new loot
            // For now, let's assume we extract what the creature would normally drop
            // and then toss it on the ground.
            
            // Note: ServUO normally generates loot inside the corpse. 
            // We can either let it generate and then move it, or generate it ourselves.
            
            // To be truly "Diablo-style", we should probably handle the generation here.
            // But as a first step, let's just make items drop on the ground.
            
            // Example generation (placeholder for real rarity system):
            int count = Utility.RandomMinMax(2, 5);
            for (int i = 0; i < count; i++)
            {
                Item item = CreateRandomItem();
                if (item != null)
                    loot.Add(item);
            }

            // 2. Apply Loot Filter
            PlayerMobile pm = killer as PlayerMobile;
            if (pm != null)
            {
                List<Item> filteredLoot = new List<Item>();
                foreach (Item item in loot)
                {
                    if (LootFilterController.PassesFilter(pm, item))
                    {
                        filteredLoot.Add(item);
                    }
                    else
                    {
                        item.Delete(); // If it doesn't pass, it doesn't exist for this player
                    }
                }
                loot = filteredLoot;
            }

            // 3. Drop on ground scattered
            ScatterLoot(killed.Location, killed.Map, loot);
        }

        private static Item CreateRandomItem()
        {
            // Placeholder: randomize between some basic items
            switch (Utility.Random(4))
            {
                case 0: return new Katana();
                case 1: return new PlateChest();
                case 2: return new Gold(Utility.RandomMinMax(50, 200));
                case 3: return new Amber();
            }
            return null;
        }

        private static void ScatterLoot(Point3D loc, Map map, List<Item> items)
        {
            if (items.Count == 0) return;

            foreach (Item item in items)
            {
                Point3D dropLoc = GetRandomDropLocation(loc, map);
                item.MoveToWorld(dropLoc, map);
                
                // Add a small effect?
                Effects.PlaySound(dropLoc, map, 0x1F2);
            }
        }

        private static Point3D GetRandomDropLocation(Point3D loc, Map map)
        {
            for (int i = 0; i < 10; i++) // try 10 times
            {
                int x = loc.X + Utility.RandomMinMax(-2, 2);
                int y = loc.Y + Utility.RandomMinMax(-2, 2);
                int z = loc.Z;

                // Basic check for line of sight and walkability could go here
                Point3D p = new Point3D(x, y, z);
                if (map.CanFit(p, 1, true, false))
                    return p;
            }
            return loc;
        }
    }
}
