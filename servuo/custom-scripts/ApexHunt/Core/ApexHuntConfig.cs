/*
 * UO Community Script: Apex Hunt - Automated Server-Wide PvM Hunting Competition
 * Original Author: Imagine (Resource #2678)
 * Source: https://www.servuo.dev/archive/release-apex-hunt-automated-server-wide-pvm-hunting-competition.2678/
 *
 * Configuration for the Apex Hunt system including dynamic interval timers,
 * player-presence trigger chance, reward amounts, and creature hunting tiers.
 */

using System;
using System.Collections.Generic;
using Server;
using Server.Mobiles;

namespace Server.Custom.ApexHunt
{
    public class ApexHuntCreatureEntry
    {
        public Type CreatureType { get; set; }
        public string DisplayName { get; set; }
        public int MinKills { get; set; }
        public int MaxKills { get; set; }
        public string Tier { get; set; }

        public ApexHuntCreatureEntry(Type type, string displayName, int minKills, int maxKills, string tier)
        {
            CreatureType = type;
            DisplayName = displayName;
            MinKills = minKills;
            MaxKills = maxKills;
            Tier = tier;
        }
    }

    public static class ApexHuntConfig
    {
        // --- Timing Configurations ---
        public static TimeSpan MinEventInterval { get; set; } = TimeSpan.FromHours(2);  // Minimum quiet period between events
        public static int MinCheckMinutes { get; set; } = 45;                           // Randomized check variance min (45 mins)
        public static int MaxCheckMinutes { get; set; } = 75;                           // Randomized check variance max (75 mins)
        public static double TriggerChance { get; set; } = 0.10;                        // 10% chance to trigger when checking
        public static int MinOnlinePlayers { get; set; } = 1;                           // Minimum online characters required to trigger

        public static TimeSpan GetRandomCheckInterval()
        {
            return TimeSpan.FromMinutes(Utility.RandomMinMax(MinCheckMinutes, MaxCheckMinutes));
        }

        // Backward compatibility alias for CheckFrequency (returns average check interval)
        public static TimeSpan CheckFrequency
        {
            get => TimeSpan.FromMinutes((MinCheckMinutes + MaxCheckMinutes) / 2.0);
            set
            {
                int total = (int)value.TotalMinutes;
                MinCheckMinutes = Math.Max(1, total - 15);
                MaxCheckMinutes = total + 15;
            }
        }

        // Backward compatibility alias for MinEventInterval
        public static TimeSpan EventFrequency
        {
            get => MinEventInterval;
            set => MinEventInterval = value;
        }

        public static int MinDurationMinutes { get; set; } = 25;
        public static int MaxDurationMinutes { get; set; } = 45;
        public static TimeSpan TieBreakerDuration { get; set; } = TimeSpan.FromSeconds(60);

        // Pre-event warnings
        public static readonly TimeSpan PreEventWarning1 = TimeSpan.FromMinutes(10);
        public static readonly TimeSpan PreEventWarning2 = TimeSpan.FromMinutes(5);
        public static readonly TimeSpan PreEventWarning3 = TimeSpan.FromMinutes(1);

        // Post-event countdown warnings
        public static readonly TimeSpan PostEventWarning1 = TimeSpan.FromMinutes(5);
        public static readonly TimeSpan PostEventWarning2 = TimeSpan.FromMinutes(2);
        public static readonly TimeSpan PostEventWarning3 = TimeSpan.FromSeconds(30);

        // --- Pure Gold Reward Configurations ---
        public static int RewardGoldFirstPlace { get; set; } = 25000;
        public static int RewardGoldSecondPlace { get; set; } = 15000;
        public static int RewardGoldThirdPlace { get; set; } = 10000;
        public static int RewardGoldParticipation { get; set; } = 5000;
        public static double ParticipationKillThreshold { get; set; } = 0.10; // Must kill at least 10% of target kills

        // --- Eligible Maps ---
        public static List<Map> EligibleMaps { get; set; } = new List<Map>
        {
            Map.Trammel,
            Map.Ilshenar,
            Map.Malas,
            Map.Tokuno
        };

        // --- Creature Roster by Tier ---
        public static List<ApexHuntCreatureEntry> EligibleCreatures { get; } = new List<ApexHuntCreatureEntry>
        {
            // === Tier 1: Swarm / Low Level (25 - 35 kills) ===
            new ApexHuntCreatureEntry(typeof(Mongbat), "Mongbat", 25, 35, "Swarm"),
            new ApexHuntCreatureEntry(typeof(HeadlessOne), "Headless One", 25, 35, "Swarm"),
            new ApexHuntCreatureEntry(typeof(Ratman), "Ratman", 25, 35, "Swarm"),
            new ApexHuntCreatureEntry(typeof(Orc), "Orc", 25, 35, "Swarm"),
            new ApexHuntCreatureEntry(typeof(Lizardman), "Lizardman", 25, 35, "Swarm"),
            new ApexHuntCreatureEntry(typeof(Skeleton), "Skeleton", 25, 35, "Swarm"),
            new ApexHuntCreatureEntry(typeof(Zombie), "Zombie", 25, 35, "Swarm"),
            new ApexHuntCreatureEntry(typeof(GiantSpider), "Giant Spider", 20, 30, "Swarm"),
            new ApexHuntCreatureEntry(typeof(Harpy), "Harpy", 20, 30, "Swarm"),
            new ApexHuntCreatureEntry(typeof(Brigand), "Brigand", 20, 30, "Swarm"),

            // === Tier 2: Medium / Beasts & Elementals (15 - 25 kills) ===
            new ApexHuntCreatureEntry(typeof(Ettin), "Ettin", 15, 25, "Beast"),
            new ApexHuntCreatureEntry(typeof(Ogre), "Ogre", 15, 25, "Beast"),
            new ApexHuntCreatureEntry(typeof(Troll), "Troll", 15, 25, "Beast"),
            new ApexHuntCreatureEntry(typeof(Gargoyle), "Gargoyle", 15, 25, "Beast"),
            new ApexHuntCreatureEntry(typeof(Gazer), "Gazer", 15, 25, "Beast"),
            new ApexHuntCreatureEntry(typeof(HellHound), "Hell Hound", 15, 25, "Beast"),
            new ApexHuntCreatureEntry(typeof(DireWolf), "Dire Wolf", 15, 25, "Beast"),
            new ApexHuntCreatureEntry(typeof(EarthElemental), "Earth Elemental", 12, 22, "Elemental"),
            new ApexHuntCreatureEntry(typeof(AirElemental), "Air Elemental", 12, 22, "Elemental"),
            new ApexHuntCreatureEntry(typeof(FireElemental), "Fire Elemental", 12, 22, "Elemental"),
            new ApexHuntCreatureEntry(typeof(WaterElemental), "Water Elemental", 12, 22, "Elemental"),
            new ApexHuntCreatureEntry(typeof(Scorpion), "Scorpion", 15, 25, "Beast"),
            new ApexHuntCreatureEntry(typeof(Wisp), "Wisp", 12, 20, "Magical"),

            // === Tier 3: High Threat / Apex Predators (8 - 18 kills) ===
            new ApexHuntCreatureEntry(typeof(Lich), "Lich", 10, 18, "Undead"),
            new ApexHuntCreatureEntry(typeof(Daemon), "Daemon", 8, 16, "Demonic"),
            new ApexHuntCreatureEntry(typeof(Drake), "Drake", 10, 18, "Draconic"),
            new ApexHuntCreatureEntry(typeof(Dragon), "Dragon", 8, 15, "Draconic"),
            new ApexHuntCreatureEntry(typeof(OgreLord), "Ogre Lord", 8, 16, "Beast"),
            new ApexHuntCreatureEntry(typeof(PoisonElemental), "Poison Elemental", 8, 16, "Elemental"),
            new ApexHuntCreatureEntry(typeof(SnowElemental), "Snow Elemental", 8, 16, "Elemental"),
            new ApexHuntCreatureEntry(typeof(Efreet), "Efreet", 8, 16, "Elemental"),
            new ApexHuntCreatureEntry(typeof(SilverSerpent), "Silver Serpent", 10, 18, "Beast"),
            new ApexHuntCreatureEntry(typeof(Wyvern), "Wyvern", 8, 16, "Draconic"),

            // === Tier 4: Boss & Ancient Apex (3 - 7 kills) ===
            new ApexHuntCreatureEntry(typeof(Balron), "Balron", 3, 6, "Boss"),
            new ApexHuntCreatureEntry(typeof(AncientWyrm), "Ancient Wyrm", 2, 5, "Boss"),
            new ApexHuntCreatureEntry(typeof(Titan), "Titan", 4, 8, "Boss"),
            new ApexHuntCreatureEntry(typeof(LichLord), "Lich Lord", 4, 8, "Boss"),
            new ApexHuntCreatureEntry(typeof(WhiteWyrm), "White Wyrm", 3, 6, "Boss"),
            new ApexHuntCreatureEntry(typeof(ShadowWyrm), "Shadow Wyrm", 2, 5, "Boss"),
            new ApexHuntCreatureEntry(typeof(Kraken), "Kraken", 3, 6, "Boss"),
            new ApexHuntCreatureEntry(typeof(RottingCorpse), "Rotting Corpse", 3, 7, "Boss")
        };

        public static ApexHuntCreatureEntry SelectRandomTarget()
        {
            if (EligibleCreatures.Count == 0)
                return null;

            return EligibleCreatures[Utility.Random(EligibleCreatures.Count)];
        }
    }
}
