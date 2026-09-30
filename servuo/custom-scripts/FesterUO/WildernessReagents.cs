using System;
using System.Collections.Generic;
using System.IO;
using Server;
using Server.Commands;
using Server.Engines.CannedEvil;
using Server.Items;
using Server.Multis;
using Server.Regions;

namespace FesterUO
{
    public static class WildernessReagents
    {
        private static readonly string PersistencePath = Path.Combine("Saves", "FesterUO", "WildernessReagents.bin");

        public static bool Enabled { get; set; }
        public static int MaxGroundReagents { get; set; }
        public static int RespawnIntervalMinutes { get; set; }
        public static int MinSpawnAmount { get; set; }
        public static int MaxSpawnAmount { get; set; }
        public static bool SpawnTrammel { get; set; }
        public static bool SpawnFelucca { get; set; }

        // Core active wilderness zones across Britannia mainland & major inhabited islands
        private static readonly Rectangle2D[] SpawnZones = new Rectangle2D[]
        {
            // Mainland Britannia: Yew, Britain, Minoc, Vesper, Cove, Trinsic, mountains, forests & swamps
            new Rectangle2D(400, 400, 3100, 2600),
            // Moonglow Island
            new Rectangle2D(4300, 800, 500, 800),
            // Jhelom Islands
            new Rectangle2D(1200, 3600, 500, 500),
            // Magincia Island
            new Rectangle2D(3500, 2000, 400, 400),
            // Skara Brae Island
            new Rectangle2D(550, 2100, 250, 300),
        };

        private static readonly List<Item> m_SpawnedReagents = new List<Item>();
        private static Timer m_Timer;

        public static void Initialize()
        {
            LoadConfig();

            EventSink.WorldLoad += OnLoad;
            EventSink.WorldSave += OnSave;
            EventSink.ItemDeleted += OnItemDeleted;

            CommandSystem.Register("WildReagents", AccessLevel.GameMaster, OnCommand);

            if (Enabled)
            {
                m_Timer = Timer.DelayCall(TimeSpan.FromSeconds(15.0), TimeSpan.FromMinutes(Math.Max(1, RespawnIntervalMinutes)), Replenish);
            }
        }

        public static void LoadConfig()
        {
            Enabled = Config.Get("Reagents.Enabled", true);
            MaxGroundReagents = Config.Get("Reagents.MaxGroundReagents", 1500);
            RespawnIntervalMinutes = Config.Get("Reagents.RespawnIntervalMinutes", 15);
            MinSpawnAmount = Config.Get("Reagents.MinSpawnAmount", 1);
            MaxSpawnAmount = Config.Get("Reagents.MaxSpawnAmount", 3);
            SpawnTrammel = Config.Get("Reagents.SpawnTrammel", true);
            SpawnFelucca = Config.Get("Reagents.SpawnFelucca", true);
        }

        private static void OnSave(WorldSaveEventArgs e)
        {
            PruneList();

            Persistence.Serialize(
                PersistencePath,
                writer =>
                {
                    writer.Write(0); // version

                    writer.Write(m_SpawnedReagents.Count);
                    for (int i = 0; i < m_SpawnedReagents.Count; i++)
                    {
                        writer.Write((int)m_SpawnedReagents[i].Serial);
                    }
                });
        }

        private static void OnLoad()
        {
            Persistence.Deserialize(
                PersistencePath,
                reader =>
                {
                    int version = reader.ReadInt();
                    int count = reader.ReadInt();

                    m_SpawnedReagents.Clear();

                    for (int i = 0; i < count; i++)
                    {
                        Serial serial = reader.ReadInt();
                        Item item = World.FindItem(serial);

                        // Only retain if it's still a valid item on the ground (no parent container)
                        if (item != null && !item.Deleted && item.Parent == null)
                        {
                            item.HonestyItem = true; // Keep decay suppressed while in the wild
                            m_SpawnedReagents.Add(item);
                        }
                    }
                });

            PruneList();
        }

        private static void OnItemDeleted(ItemDeletedEventArgs e)
        {
            if (e.Item != null && m_SpawnedReagents.Contains(e.Item))
            {
                m_SpawnedReagents.Remove(e.Item);
            }
        }

        public static void PruneList()
        {
            for (int i = m_SpawnedReagents.Count - 1; i >= 0; i--)
            {
                Item item = m_SpawnedReagents[i];
                if (item == null || item.Deleted)
                {
                    m_SpawnedReagents.RemoveAt(i);
                }
                else if (item.Parent != null)
                {
                    item.HonestyItem = false; // Reset decay flag once safely in player possession
                    m_SpawnedReagents.RemoveAt(i);
                }
            }
        }

        public static void Replenish()
        {
            if (!Enabled)
                return;

            PruneList();

            int needed = MaxGroundReagents - m_SpawnedReagents.Count;
            if (needed <= 0)
                return;

            int attempts = needed * 6;
            int spawnedThisCycle = 0;

            for (int i = 0; i < attempts && spawnedThisCycle < needed; i++)
            {
                Map map = GetRandomMap();
                if (map == null)
                    continue;

                Rectangle2D zone = SpawnZones[Utility.Random(SpawnZones.Length)];
                int x = Utility.RandomMinMax(zone.X, zone.X + zone.Width);
                int y = Utility.RandomMinMax(zone.Y, zone.Y + zone.Height);

                if (TryFindSpawnLocation(map, x, y, out Point3D loc, out Type reagentType))
                {
                    Item item = (Item)Activator.CreateInstance(reagentType);
                    item.Amount = Utility.RandomMinMax(Math.Max(1, MinSpawnAmount), Math.Max(1, MaxSpawnAmount));
                    item.Movable = true;
                    item.HonestyItem = true; // Prevents natural world item decay while lying on the ground
                    item.MoveToWorld(loc, map);

                    m_SpawnedReagents.Add(item);
                    spawnedThisCycle++;
                }
            }
        }

        private static Map GetRandomMap()
        {
            if (SpawnTrammel && SpawnFelucca)
                return Utility.RandomBool() ? Map.Trammel : Map.Felucca;

            if (SpawnTrammel)
                return Map.Trammel;

            if (SpawnFelucca)
                return Map.Felucca;

            return null;
        }

        private static bool TryFindSpawnLocation(Map map, int x, int y, out Point3D loc, out Type reagentType)
        {
            loc = Point3D.Zero;
            reagentType = null;

            if (map == null || map == Map.Internal)
                return false;

            if (x < 0 || y < 0 || x >= map.Width || y >= map.Height)
                return false;

            LandTile lt = map.Tiles.GetLandTile(x, y);
            LandData ld = TileData.LandTable[lt.ID & TileData.MaxLandValue];

            // Filter out impassable, roof, or wet/water tiles
            if (lt.Ignored || (ld.Flags & TileFlag.Impassable) != 0 || (ld.Flags & TileFlag.Roof) != 0 || (ld.Flags & TileFlag.Wet) != 0)
                return false;

            int z = map.GetAverageZ(x, y);
            Point3D p = new Point3D(x, y, z);

            // Region checks - no spawning inside towns, dungeons, house plots, or champion spawns
            Region reg = Region.Find(p, map);
            if (reg != null)
            {
                if (reg.IsPartOf<TownRegion>() || reg.IsPartOf<DungeonRegion>() ||
                    reg.IsPartOf<ChampionSpawnRegion>() || reg.IsPartOf<HouseRegion>())
                {
                    return false;
                }
            }

            // House radius check
            for (int hx = x - 5; hx <= x + 5; hx++)
            {
                for (int hy = y - 5; hy <= y + 5; hy++)
                {
                    if (BaseHouse.FindHouseAt(new Point3D(hx, hy, z), map, Region.MaxZ - z) != null)
                        return false;
                }
            }

            // CanFit check to ensure an item can sit at this ground location
            if (!map.CanFit(x, y, z, 1, false, false, false))
                return false;

            loc = p;
            reagentType = SelectReagentByTerrain(ld.Name);
            return reagentType != null;
        }

        private static Type SelectReagentByTerrain(string landName)
        {
            string name = (landName ?? string.Empty).ToLowerInvariant();

            // Swamps: Mandrake Root & Bloodmoss
            if (name.Contains("swamp"))
            {
                return Utility.RandomBool() ? typeof(MandrakeRoot) : typeof(Bloodmoss);
            }

            // Beaches, Coasts & Sand: Black Pearl
            if (name.Contains("sand"))
            {
                return typeof(BlackPearl);
            }

            // Rocks, Mountains, Lava: Sulfurous Ash
            if (name.Contains("rock") || name.Contains("mountain") || name.Contains("lava"))
            {
                return typeof(SulfurousAsh);
            }

            // Forests, Jungle, Grasslands: Nightshade, Garlic, Ginseng, Spiders' Silk
            if (name.Contains("grass") || name.Contains("forest") || name.Contains("jungle") || name.Contains("leaves"))
            {
                switch (Utility.Random(4))
                {
                    case 0: return typeof(Nightshade);
                    case 1: return typeof(Garlic);
                    case 2: return typeof(Ginseng);
                    default: return typeof(SpidersSilk);
                }
            }

            // Dirt, Furrows, Wilderness Trails: All 8 classic reagents
            switch (Utility.Random(8))
            {
                case 0: return typeof(BlackPearl);
                case 1: return typeof(Bloodmoss);
                case 2: return typeof(Garlic);
                case 3: return typeof(Ginseng);
                case 4: return typeof(MandrakeRoot);
                case 5: return typeof(Nightshade);
                case 6: return typeof(SulfurousAsh);
                default: return typeof(SpidersSilk);
            }
        }

        private static void OnCommand(CommandEventArgs e)
        {
            Mobile m = e.Mobile;
            PruneList();

            if (e.Length == 0)
            {
                int tramCount = 0;
                int felCount = 0;
                for (int i = 0; i < m_SpawnedReagents.Count; i++)
                {
                    if (m_SpawnedReagents[i] != null && !m_SpawnedReagents[i].Deleted)
                    {
                        if (m_SpawnedReagents[i].Map == Map.Trammel) tramCount++;
                        else if (m_SpawnedReagents[i].Map == Map.Felucca) felCount++;
                    }
                }

                m.SendMessage(0x35, "=== Wilderness Reagents Status ===");
                m.SendMessage(0x44, "Enabled: {0} | Active on Ground: {1} / {2} (Trammel: {3}, Felucca: {4})",
                    Enabled, m_SpawnedReagents.Count, MaxGroundReagents, tramCount, felCount);
                m.SendMessage(0x44, "Respawn Interval: {0}m | Yield per Spawn: {1}-{2}",
                    RespawnIntervalMinutes, MinSpawnAmount, MaxSpawnAmount);
                m.SendMessage(0x44, "Facets: Trammel ({0}), Felucca ({1})",
                    SpawnTrammel ? "Yes" : "No", SpawnFelucca ? "Yes" : "No");
                m.SendMessage(0x35, "Commands: [WildReagents [respawn | clear | reload | find | goto | near]");
                return;
            }

            string sub = e.GetString(0).ToLowerInvariant();
            switch (sub)
            {
                case "find":
                    {
                        Item nearest = null;
                        double bestDist = double.MaxValue;
                        for (int i = 0; i < m_SpawnedReagents.Count; i++)
                        {
                            Item item = m_SpawnedReagents[i];
                            if (item != null && !item.Deleted && item.Map == m.Map)
                            {
                                double dist = m.GetDistanceToSqrt(item);
                                if (dist < bestDist)
                                {
                                    bestDist = dist;
                                    nearest = item;
                                }
                            }
                        }

                        if (nearest != null)
                        {
                            m.SendMessage(0x44, "Nearest {0}: {1} at {2} ({3} tiles away).", nearest.GetType().Name, nearest.Amount, nearest.Location, (int)bestDist);
                        }
                        else
                        {
                            m.SendMessage(0x22, "No spawned reagents found on your facet.");
                        }
                        break;
                    }
                case "goto":
                    {
                        Item target = null;
                        if (e.Length > 1)
                        {
                            string filter = e.GetString(1).ToLowerInvariant();
                            for (int i = 0; i < m_SpawnedReagents.Count; i++)
                            {
                                Item item = m_SpawnedReagents[i];
                                if (item != null && !item.Deleted && item.Map == m.Map && item.GetType().Name.ToLowerInvariant().Contains(filter))
                                {
                                    target = item;
                                    break;
                                }
                            }
                        }
                        else
                        {
                            // Pick random on same facet
                            List<Item> sameFacet = new List<Item>();
                            for (int i = 0; i < m_SpawnedReagents.Count; i++)
                            {
                                Item item = m_SpawnedReagents[i];
                                if (item != null && !item.Deleted && item.Map == m.Map)
                                {
                                    sameFacet.Add(item);
                                }
                            }

                            if (sameFacet.Count > 0)
                            {
                                target = sameFacet[Utility.Random(sameFacet.Count)];
                            }
                        }

                        if (target != null)
                        {
                            m.MoveToWorld(target.Location, target.Map);
                            m.SendMessage(0x44, "Teleported to {0} ({1}) at {2}.", target.GetType().Name, target.Amount, target.Location);
                        }
                        else
                        {
                            m.SendMessage(0x22, "No matching reagent found on your facet.");
                        }
                        break;
                    }
                case "near":
                    {
                        int radius = 50;
                        if (e.Length > 1)
                            int.TryParse(e.GetString(1), out radius);

                        int found = 0;
                        for (int i = 0; i < m_SpawnedReagents.Count; i++)
                        {
                            Item item = m_SpawnedReagents[i];
                            if (item != null && !item.Deleted && item.Map == m.Map && m.InRange(item.Location, radius))
                            {
                                m.SendMessage(0x44, "- {0} ({1}) at {2} (Dist: {3})", item.GetType().Name, item.Amount, item.Location, (int)m.GetDistanceToSqrt(item));
                                found++;
                            }
                        }
                        m.SendMessage(0x35, "Total reagents within {0} tiles: {1}", radius, found);
                        break;
                    }
                case "respawn":
                    {
                        int before = m_SpawnedReagents.Count;
                        Replenish();
                        int after = m_SpawnedReagents.Count;
                        m.SendMessage(0x44, "Replenished wilderness reagents: {0} new spawned (Total: {1} / {2}).", after - before, after, MaxGroundReagents);
                        break;
                    }
                case "clear":
                    {
                        int count = m_SpawnedReagents.Count;
                        for (int i = 0; i < m_SpawnedReagents.Count; i++)
                        {
                            if (m_SpawnedReagents[i] != null && !m_SpawnedReagents[i].Deleted)
                            {
                                m_SpawnedReagents[i].Delete();
                            }
                        }
                        m_SpawnedReagents.Clear();
                        m.SendMessage(0x44, "Cleared {0} wilderness ground reagents.", count);
                        break;
                    }
                case "reload":
                    {
                        LoadConfig();
                        m.SendMessage(0x44, "Reloaded Reagents.cfg configuration. (MaxGroundReagents: {0})", MaxGroundReagents);
                        break;
                    }
                default:
                    {
                        m.SendMessage("Usage: [WildReagents [respawn | clear | reload | find | goto | near]");
                        break;
                    }
            }
        }
    }
}
