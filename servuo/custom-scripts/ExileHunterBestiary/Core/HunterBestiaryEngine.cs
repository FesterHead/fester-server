/*
 * UO Community Script: Hunter's Bestiary & Monster Treatises System
 * Original Author: UO Exile Team / Imagine (Resource #2674)
 * Source: https://www.servuo.dev/archive/release-hunters-bestiary-monster-treatises-system-193-creatures-1-line-install.2674/
 *
 * Provides a 193-creature horizontal progression bestiary with collectible treatises,
 * dynamic tooltips, +20% damage mastery bonuses, interactive multi-category book gump,
 * and XML persistence.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using Server.Commands;
using Server.Custom.ExileHunterBestiary.Gumps;
using Server.Custom.ExileHunterBestiary.Items;
using Server.Mobiles;

namespace Server.Custom.ExileHunterBestiary
{
    public static class HunterBestiaryEngine
    {
        public const double DefaultDropChance = 0.02; // 2%
        public const double BossDropChance = 0.05;    // 5%

        public static Dictionary<string, HunterCreatureEntry> RegistryById { get; private set; }
        public static Dictionary<Type, HunterCreatureEntry> RegistryByType { get; private set; }
        public static Dictionary<BestiaryCategory, List<HunterCreatureEntry>> EntriesByCategory { get; private set; }

        private static Dictionary<Serial, HunterProfile> m_Profiles = new Dictionary<Serial, HunterProfile>();
        private static string SavePath => Path.Combine(Core.BaseDirectory, "Data", "ExileHunterProfiles.xml");

        public static void Initialize()
        {
            RegistryById = new Dictionary<string, HunterCreatureEntry>(StringComparer.OrdinalIgnoreCase);
            RegistryByType = new Dictionary<Type, HunterCreatureEntry>();
            EntriesByCategory = new Dictionary<BestiaryCategory, List<HunterCreatureEntry>>();

            foreach (BestiaryCategory cat in Enum.GetValues(typeof(BestiaryCategory)))
            {
                EntriesByCategory[cat] = new List<HunterCreatureEntry>();
            }

            RegisterCreatures();

            BaseCreature.OnCreatureDamageHook = OnCreatureDamaged;

            EventSink.CreatureDeath += OnCreatureDeath;
            EventSink.WorldSave += OnWorldSave;
            EventSink.WorldLoad += OnWorldLoad;

            CommandSystem.Register("Bestiary", AccessLevel.Player, OnCommandBestiary);
            CommandSystem.Register("Bestiario", AccessLevel.Player, OnCommandBestiary);

            LoadProfiles();
            Console.WriteLine($"[ExileHunterBestiary] Initialized with {RegistryById.Count} creatures across {EntriesByCategory.Count} categories.");
        }

        private static void Register(string id, string displayName, Type type, BestiaryCategory category, int bookHue = 0, double dropChance = DefaultDropChance, int iconId = 0x1F14)
        {
            if (type == null)
                return;

            var entry = new HunterCreatureEntry(id, displayName, type, category, bookHue, dropChance, iconId);
            RegistryById[id] = entry;
            RegistryByType[type] = entry;
            EntriesByCategory[category].Add(entry);
        }

        private static void RegisterCreatures()
        {
            // --- 1. UNDEAD (Hue 1150-1175 necro/spectral) ---
            Register("Skeleton", "Skeleton", typeof(Skeleton), BestiaryCategory.Undead, 2101);
            Register("SkeletalMage", "Skeletal Mage", typeof(SkeletalMage), BestiaryCategory.Undead, 2101);
            Register("SkeletalKnight", "Skeletal Knight", typeof(SkeletalKnight), BestiaryCategory.Undead, 2101);
            Register("PatchworkSkeleton", "Patchwork Skeleton", typeof(PatchworkSkeleton), BestiaryCategory.Undead, 2101);
            Register("Zombie", "Zombie", typeof(Zombie), BestiaryCategory.Undead, 1175);
            Register("Ghoul", "Ghoul", typeof(Ghoul), BestiaryCategory.Undead, 1175);
            Register("Shade", "Shade", typeof(Shade), BestiaryCategory.Undead, 1150);
            Register("Spectre", "Spectre", typeof(Spectre), BestiaryCategory.Undead, 1150);
            Register("Wraith", "Wraith", typeof(Wraith), BestiaryCategory.Undead, 1150);
            Register("Mummy", "Mummy", typeof(Mummy), BestiaryCategory.Undead, 2305);
            Register("PestilentBandage", "Pestilent Bandage", typeof(PestilentBandage), BestiaryCategory.Undead, 1150);
            Register("BoneKnight", "Bone Knight", typeof(BoneKnight), BestiaryCategory.Undead, 2101);
            Register("BoneMagi", "Bone Magi", typeof(BoneMagi), BestiaryCategory.Undead, 2101);
            Register("RottingCorpse", "Rotting Corpse", typeof(RottingCorpse), BestiaryCategory.Undead, 1175);
            Register("Lich", "Lich", typeof(Lich), BestiaryCategory.Undead, 1150);
            Register("LichLord", "Lich Lord", typeof(LichLord), BestiaryCategory.Undead, 1153);
            Register("AncientLich", "Ancient Lich", typeof(AncientLich), BestiaryCategory.Undead, 1175);
            Register("SkeletalDragon", "Skeletal Dragon", typeof(SkeletalDragon), BestiaryCategory.Undead, 1175, 0.03);
            Register("BoneDemon", "Bone Demon", typeof(BoneDemon), BestiaryCategory.Undead, 2101, 0.04);
            Register("UndeadGargoyle", "Undead Gargoyle", typeof(UndeadGargoyle), BestiaryCategory.Undead, 1175);
            Register("PutridUndeadGargoyle", "Putrid Undead Gargoyle", typeof(PutridUndeadGargoyle), BestiaryCategory.Undead, 1175);
            Register("WailingBanshee", "Wailing Banshee", typeof(WailingBanshee), BestiaryCategory.Undead, 1153);
            Register("GoreFiend", "Gore Fiend", typeof(GoreFiend), BestiaryCategory.Undead, 1175);
            Register("FleshGolem", "Flesh Golem", typeof(FleshGolem), BestiaryCategory.Undead, 1175);
            Register("Gibberling", "Gibberling", typeof(Gibberling), BestiaryCategory.Undead, 1175);
            Register("Rotworm", "Rotworm", typeof(Rotworm), BestiaryCategory.Undead, 2215);

            // --- 2. DAEMONS & FIENDS (Hue 1255-1260 infernal/blood) ---
            Register("Daemon", "Daemon", typeof(Daemon), BestiaryCategory.Daemons, 1258);
            Register("FireDaemon", "Fire Daemon", typeof(FireDaemon), BestiaryCategory.Daemons, 1260);
            Register("IceFiend", "Ice Fiend", typeof(IceFiend), BestiaryCategory.Daemons, 1152);
            Register("Succubus", "Succubus", typeof(Succubus), BestiaryCategory.Daemons, 1166);
            Register("Balron", "Balron", typeof(Balron), BestiaryCategory.Daemons, 1255, 0.03);
            Register("ArcaneDaemon", "Arcane Daemon", typeof(ArcaneDaemon), BestiaryCategory.Daemons, 1165);
            Register("ChaosDaemon", "Chaos Daemon", typeof(ChaosDaemon), BestiaryCategory.Daemons, 1167);
            Register("Moloch", "Moloch", typeof(Moloch), BestiaryCategory.Daemons, 1257);
            Register("Ravager", "Ravager", typeof(Ravager), BestiaryCategory.Daemons, 1258);
            Register("Devourer", "Devourer of Souls", typeof(Devourer), BestiaryCategory.Daemons, 1153);
            Register("Imp", "Imp", typeof(Imp), BestiaryCategory.Daemons, 1255);
            Register("Gargoyle", "Gargoyle", typeof(Gargoyle), BestiaryCategory.Daemons, 2419);
            Register("StoneGargoyle", "Stone Gargoyle", typeof(StoneGargoyle), BestiaryCategory.Daemons, 2413);
            Register("FireGargoyle", "Fire Gargoyle", typeof(FireGargoyle), BestiaryCategory.Daemons, 1260);
            Register("GargoyleEnforcer", "Gargoyle Enforcer", typeof(GargoyleEnforcer), BestiaryCategory.Daemons, 2419);
            Register("GargoyleDestroyer", "Gargoyle Destroyer", typeof(GargoyleDestroyer), BestiaryCategory.Daemons, 2419);

            // --- 3. REPTILES & DRAGONS (Hue 1360-1420 scales/dragon) ---
            Register("Dragon", "Dragon", typeof(Dragon), BestiaryCategory.Reptiles, 1365, 0.03);
            Register("Drake", "Drake", typeof(Drake), BestiaryCategory.Reptiles, 1362);
            Register("WhiteWyrm", "White Wyrm", typeof(WhiteWyrm), BestiaryCategory.Reptiles, 1150, 0.03);
            Register("ShadowWyrm", "Shadow Wyrm", typeof(ShadowWyrm), BestiaryCategory.Reptiles, 1109, 0.03);
            Register("AncientWyrm", "Ancient Wyrm", typeof(AncientWyrm), BestiaryCategory.Reptiles, 1370, 0.04);
            Register("GreaterDragon", "Greater Dragon", typeof(GreaterDragon), BestiaryCategory.Reptiles, 1360, 0.04);
            Register("SwampDragon", "Swamp Dragon", typeof(SwampDragon), BestiaryCategory.Reptiles, 2212);
            Register("Hydra", "Hydra", typeof(Hydra), BestiaryCategory.Reptiles, 1368);
            Register("SerpentineDragon", "Serpentine Dragon", typeof(SerpentineDragon), BestiaryCategory.Reptiles, 1160);
            Register("Reptalon", "Reptalon", typeof(Reptalon), BestiaryCategory.Reptiles, 1372);
            Register("Lizardman", "Lizardman", typeof(Lizardman), BestiaryCategory.Reptiles, 2210);
            Register("OphidianWarrior", "Ophidian Warrior", typeof(OphidianWarrior), BestiaryCategory.Reptiles, 2001);
            Register("OphidianMage", "Ophidian Mage", typeof(OphidianMage), BestiaryCategory.Reptiles, 2002);
            Register("OphidianMatriarch", "Ophidian Matriarch", typeof(OphidianMatriarch), BestiaryCategory.Reptiles, 2003);
            Register("Slith", "Slith", typeof(Slith), BestiaryCategory.Reptiles, 2212);
            Register("ToxicSlith", "Toxic Slith", typeof(ToxicSlith), BestiaryCategory.Reptiles, 1267);
            Register("StoneSlith", "Stone Slith", typeof(StoneSlith), BestiaryCategory.Reptiles, 2413);
            Register("LavaLizard", "Lava Lizard", typeof(LavaLizard), BestiaryCategory.Reptiles, 1260);
            Register("LavaSerpent", "Lava Serpent", typeof(LavaSerpent), BestiaryCategory.Reptiles, 1260);
            Register("LavaSnake", "Lava Snake", typeof(LavaSnake), BestiaryCategory.Reptiles, 1260);
            Register("IceSerpent", "Ice Serpent", typeof(IceSerpent), BestiaryCategory.Reptiles, 1152);
            Register("IceSnake", "Ice Snake", typeof(IceSnake), BestiaryCategory.Reptiles, 1152);
            Register("SilverSerpent", "Silver Serpent", typeof(SilverSerpent), BestiaryCategory.Reptiles, 2301);
            Register("GiantSerpent", "Giant Serpent", typeof(GiantSerpent), BestiaryCategory.Reptiles, 2210);
            Register("Alligator", "Alligator", typeof(Alligator), BestiaryCategory.Reptiles, 2210);

            // --- 4. ELEMENTALS (Hue elemental tones) ---
            Register("EarthElemental", "Earth Elemental", typeof(EarthElemental), BestiaryCategory.Elementals, 2419);
            Register("AirElemental", "Air Elemental", typeof(AirElemental), BestiaryCategory.Elementals, 1153);
            Register("FireElemental", "Fire Elemental", typeof(FireElemental), BestiaryCategory.Elementals, 1260);
            Register("WaterElemental", "Water Elemental", typeof(WaterElemental), BestiaryCategory.Elementals, 1165);
            Register("PoisonElemental", "Poison Elemental", typeof(PoisonElemental), BestiaryCategory.Elementals, 1267);
            Register("BloodElemental", "Blood Elemental", typeof(BloodElemental), BestiaryCategory.Elementals, 1258);
            Register("SnowElemental", "Snow Elemental", typeof(SnowElemental), BestiaryCategory.Elementals, 1150);
            Register("IceElemental", "Ice Elemental", typeof(IceElemental), BestiaryCategory.Elementals, 1152);
            Register("Efreet", "Efreet", typeof(Efreet), BestiaryCategory.Elementals, 1260);
            Register("LavaElemental", "Lava Elemental", typeof(LavaElemental), BestiaryCategory.Elementals, 1260);
            Register("SandVortex", "Sand Vortex", typeof(SandVortex), BestiaryCategory.Elementals, 2419);

            // --- 5. HUMANOIDS & GIANTS / FEY (Hue rugged earth/forest) ---
            Register("Ogre", "Ogre", typeof(Ogre), BestiaryCategory.Humanoids, 2215);
            Register("OgreLord", "Ogre Lord", typeof(OgreLord), BestiaryCategory.Humanoids, 2216);
            Register("ArcticOgreLord", "Arctic Ogre Lord", typeof(ArcticOgreLord), BestiaryCategory.Humanoids, 1150);
            Register("Ettin", "Ettin", typeof(Ettin), BestiaryCategory.Humanoids, 2214);
            Register("Troll", "Troll", typeof(Troll), BestiaryCategory.Humanoids, 2213);
            Register("Titan", "Titan", typeof(Titan), BestiaryCategory.Humanoids, 1153);
            Register("Cyclops", "Cyclops", typeof(Cyclops), BestiaryCategory.Humanoids, 2217);
            Register("Orc", "Orc", typeof(Orc), BestiaryCategory.Humanoids, 2208);
            Register("OrcishLord", "Orcish Lord", typeof(OrcishLord), BestiaryCategory.Humanoids, 2209);
            Register("OrcCaptain", "Orc Captain", typeof(OrcCaptain), BestiaryCategory.Humanoids, 2210);
            Register("OrcishMage", "Orcish Mage", typeof(OrcishMage), BestiaryCategory.Humanoids, 2208);
            Register("OrcBrute", "Orc Brute", typeof(OrcBrute), BestiaryCategory.Humanoids, 2211);
            Register("Ratman", "Ratman", typeof(Ratman), BestiaryCategory.Humanoids, 2307);
            Register("RatmanArcher", "Ratman Archer", typeof(RatmanArcher), BestiaryCategory.Humanoids, 2307);
            Register("RatmanMage", "Ratman Mage", typeof(RatmanMage), BestiaryCategory.Humanoids, 2308);
            Register("Minotaur", "Minotaur", typeof(Minotaur), BestiaryCategory.Humanoids, 2418);
            Register("Centaur", "Centaur", typeof(Centaur), BestiaryCategory.Humanoids, 2215);
            Register("Satyr", "Satyr", typeof(Satyr), BestiaryCategory.Humanoids, 2215);
            Register("InsaneDryad", "Insane Dryad", typeof(InsaneDryad), BestiaryCategory.Humanoids, 2210);
            Register("Pixie", "Pixie", typeof(Pixie), BestiaryCategory.Humanoids, 1160);

            // --- 6. ARACHNIDS & INSECTS (Hue toxic chitin/web) ---
            Register("GiantSpider", "Giant Spider", typeof(GiantSpider), BestiaryCategory.Arachnids, 2110);
            Register("DreadSpider", "Dread Spider", typeof(DreadSpider), BestiaryCategory.Arachnids, 1175);
            Register("FrostSpider", "Frost Spider", typeof(FrostSpider), BestiaryCategory.Arachnids, 1152);
            Register("GiantBlackWidow", "Giant Black Widow", typeof(GiantBlackWidow), BestiaryCategory.Arachnids, 1109);
            Register("TerathanDrone", "Terathan Drone", typeof(TerathanDrone), BestiaryCategory.Arachnids, 2115);
            Register("TerathanWarrior", "Terathan Warrior", typeof(TerathanWarrior), BestiaryCategory.Arachnids, 2116);
            Register("TerathanMatriarch", "Terathan Matriarch", typeof(TerathanMatriarch), BestiaryCategory.Arachnids, 2117);
            Register("Scorpion", "Scorpion", typeof(Scorpion), BestiaryCategory.Arachnids, 2206);
            Register("RuneBeetle", "Rune Beetle", typeof(RuneBeetle), BestiaryCategory.Arachnids, 1165, 0.03);
            Register("FireBeetle", "Fire Beetle", typeof(FireBeetle), BestiaryCategory.Arachnids, 1260);
            Register("Beetle", "Giant Beetle", typeof(Beetle), BestiaryCategory.Arachnids, 2210);
            Register("IronBeetle", "Iron Beetle", typeof(IronBeetle), BestiaryCategory.Arachnids, 2413);
            Register("DeathwatchBeetle", "Deathwatch Beetle", typeof(DeathwatchBeetle), BestiaryCategory.Arachnids, 2215);
            Register("AntLion", "Ant Lion", typeof(AntLion), BestiaryCategory.Arachnids, 2419);
            Register("FireAnt", "Fire Ant", typeof(FireAnt), BestiaryCategory.Arachnids, 1260);
            Register("SkitteringHopper", "Skittering Hopper", typeof(SkitteringHopper), BestiaryCategory.Arachnids, 2210);

            // --- 7. BEASTS & MAGICAL CREATURES ---
            Register("Gazer", "Gazer", typeof(Gazer), BestiaryCategory.Beasts, 1166);
            Register("ElderGazer", "Elder Gazer", typeof(ElderGazer), BestiaryCategory.Beasts, 1167);
            Register("GazerLarva", "Gazer Larva", typeof(GazerLarva), BestiaryCategory.Beasts, 1166);
            Register("HellHound", "Hell Hound", typeof(HellHound), BestiaryCategory.Beasts, 1258);
            Register("HellCat", "Hell Cat", typeof(HellCat), BestiaryCategory.Beasts, 1258);
            Register("PredatorHellCat", "Predator Hell Cat", typeof(PredatorHellCat), BestiaryCategory.Beasts, 1258);
            Register("Nightmare", "Nightmare", typeof(Nightmare), BestiaryCategory.Beasts, 1109, 0.03);
            Register("CuSidhe", "Cu Sidhe", typeof(CuSidhe), BestiaryCategory.Beasts, 2212, 0.03);
            Register("Kirin", "Ki-Rin", typeof(Kirin), BestiaryCategory.Beasts, 1160);
            Register("Unicorn", "Unicorn", typeof(Unicorn), BestiaryCategory.Beasts, 1150);
            Register("Hiryu", "Hiryu", typeof(Hiryu), BestiaryCategory.Beasts, 1365, 0.03);
            Register("Phoenix", "Phoenix", typeof(Phoenix), BestiaryCategory.Beasts, 1260, 0.04);
            Register("TimberWolf", "Timber Wolf", typeof(TimberWolf), BestiaryCategory.Beasts, 2215);
            Register("GreyWolf", "Grey Wolf", typeof(GreyWolf), BestiaryCategory.Beasts, 2215);
            Register("WhiteWolf", "White Wolf", typeof(WhiteWolf), BestiaryCategory.Beasts, 1150);
            Register("DireWolf", "Dire Wolf", typeof(DireWolf), BestiaryCategory.Beasts, 2305);
            Register("Cougar", "Cougar", typeof(Cougar), BestiaryCategory.Beasts, 2215);
            Register("Panther", "Panther", typeof(Panther), BestiaryCategory.Beasts, 1109);
            Register("SnowLeopard", "Snow Leopard", typeof(SnowLeopard), BestiaryCategory.Beasts, 1150);
            Register("GrizzlyBear", "Grizzly Bear", typeof(GrizzlyBear), BestiaryCategory.Beasts, 2215);
            Register("PolarBear", "Polar Bear", typeof(PolarBear), BestiaryCategory.Beasts, 1150);
            Register("BlackBear", "Black Bear", typeof(BlackBear), BestiaryCategory.Beasts, 1109);
            Register("Gorilla", "Gorilla", typeof(Gorilla), BestiaryCategory.Beasts, 2210);
            Register("Walrus", "Walrus", typeof(Walrus), BestiaryCategory.Beasts, 2419);
            Register("GiantToad", "Giant Toad", typeof(GiantToad), BestiaryCategory.Beasts, 2210);
            Register("Bullfrog", "Bullfrog", typeof(BullFrog), BestiaryCategory.Beasts, 2210);
            Register("VampireBat", "Vampire Bat", typeof(VampireBat), BestiaryCategory.Beasts, 1109);
            Register("Mongbat", "Mongbat", typeof(Mongbat), BestiaryCategory.Beasts, 2215);
            Register("DarkWisp", "Dark Wisp", typeof(DarkWisp), BestiaryCategory.Beasts, 1109);
            Register("ShadowWisp", "Shadow Wisp", typeof(ShadowWisp), BestiaryCategory.Beasts, 1109);
            Register("GiantIceWorm", "Giant Ice Worm", typeof(GiantIceWorm), BestiaryCategory.Beasts, 1152);
            Register("Raptor", "Raptor", typeof(Raptor), BestiaryCategory.Beasts, 2210);
            Register("Kepetch", "Kepetch", typeof(Kepetch), BestiaryCategory.Beasts, 2215);
            Register("KepetchAmbusher", "Kepetch Ambusher", typeof(KepetchAmbusher), BestiaryCategory.Beasts, 2215);
            Register("BloodWorm", "Blood Worm", typeof(BloodWorm), BestiaryCategory.Beasts, 1258);
            Register("OsseinRam", "Ossein Ram", typeof(OsseinRam), BestiaryCategory.Beasts, 2101);
            Register("Harpy", "Harpy", typeof(Harpy), BestiaryCategory.Beasts, 2215);
            Register("StoneHarpy", "Stone Harpy", typeof(StoneHarpy), BestiaryCategory.Beasts, 2413);
            Register("Corpser", "Corpser", typeof(Corpser), BestiaryCategory.Beasts, 2215);
            Register("Reaper", "Reaper", typeof(Reaper), BestiaryCategory.Beasts, 2215);
            Register("BogThing", "Bog Thing", typeof(BogThing), BestiaryCategory.Beasts, 2212);
            Register("Bogling", "Bogling", typeof(Bogling), BestiaryCategory.Beasts, 2212);
            Register("PlagueBeast", "Plague Beast", typeof(PlagueBeast), BestiaryCategory.Beasts, 1267);
            Register("PlagueBeastLord", "Plague Beast Lord", typeof(PlagueBeastLord), BestiaryCategory.Beasts, 1267, 0.03);
            Register("Quagmire", "Quagmire", typeof(Quagmire), BestiaryCategory.Beasts, 2212);
            Register("Slime", "Slime", typeof(Slime), BestiaryCategory.Beasts, 2210);
            Register("FrostOoze", "Frost Ooze", typeof(FrostOoze), BestiaryCategory.Beasts, 1152);
            Register("SeaSerpent", "Sea Serpent", typeof(SeaSerpent), BestiaryCategory.Beasts, 1165);
            Register("DeepSeaSerpent", "Deep Sea Serpent", typeof(DeepSeaSerpent), BestiaryCategory.Beasts, 1165);
            Register("Kraken", "Kraken", typeof(Kraken), BestiaryCategory.Beasts, 1165, 0.03);
            Register("Leviathan", "Leviathan", typeof(Leviathan), BestiaryCategory.Beasts, 1165, 0.05);

            // --- 8. BOSSES, CHAMPIONS & PEERLESS (Hue mythic gold/purple) ---
            Register("DemonKnight", "Demon Knight (Dark Father)", typeof(DemonKnight), BestiaryCategory.Bosses, 1161, BossDropChance);
            Register("DarknightCreeper", "Darknight Creeper", typeof(DarknightCreeper), BestiaryCategory.Bosses, 1161, BossDropChance);
            Register("FleshRenderer", "Flesh Renderer", typeof(FleshRenderer), BestiaryCategory.Bosses, 1161, BossDropChance);
            Register("Impaler", "Impaler", typeof(Impaler), BestiaryCategory.Bosses, 1161, BossDropChance);
            Register("ShadowKnight", "Shadow Knight", typeof(ShadowKnight), BestiaryCategory.Bosses, 1161, BossDropChance);
            Register("AbysmalHorror", "Abysmal Horror", typeof(AbysmalHorror), BestiaryCategory.Bosses, 1161, BossDropChance);
            Register("Harrower", "The Harrower", typeof(Harrower), BestiaryCategory.Bosses, 1175, 0.10);
            Register("SlasherOfVeils", "Slasher of Veils", typeof(SlasherOfVeils), BestiaryCategory.Bosses, 1170, BossDropChance);
            Register("StygianDragon", "Stygian Dragon", typeof(StygianDragon), BestiaryCategory.Bosses, 1172, BossDropChance);
            Register("Medusa", "Medusa", typeof(Medusa), BestiaryCategory.Bosses, 1168, BossDropChance);
            Register("PrimevalLich", "Primeval Lich", typeof(PrimevalLich), BestiaryCategory.Bosses, 1153, BossDropChance);
            Register("AbyssalInfernal", "Abyssal Infernal", typeof(AbyssalInfernal), BestiaryCategory.Bosses, 1255, BossDropChance);
            Register("Rikktor", "Rikktor", typeof(Rikktor), BestiaryCategory.Bosses, 1365, BossDropChance);
            Register("Mephitis", "Mephitis", typeof(Mephitis), BestiaryCategory.Bosses, 1175, BossDropChance);
            Register("Semidar", "Semidar", typeof(Semidar), BestiaryCategory.Bosses, 1258, BossDropChance);
            Register("Barracoon", "Barracoon the Piper", typeof(Barracoon), BestiaryCategory.Bosses, 2307, BossDropChance);
            Register("Neira", "Neira the Necromancer", typeof(Neira), BestiaryCategory.Bosses, 1175, BossDropChance);
            Register("LordOaks", "Lord Oaks", typeof(LordOaks), BestiaryCategory.Bosses, 2215, BossDropChance);
            Register("Silvani", "Silvani", typeof(Silvani), BestiaryCategory.Bosses, 1160, BossDropChance);
            Register("Ilhenir", "Ilhenir the Stained", typeof(Ilhenir), BestiaryCategory.Bosses, 1267, BossDropChance);
            Register("Serado", "Serado the Scourge", typeof(Serado), BestiaryCategory.Bosses, 1267, BossDropChance);
            Register("ChiefParoxysmus", "Chief Paroxysmus", typeof(ChiefParoxysmus), BestiaryCategory.Bosses, 1267, BossDropChance);
            Register("DreadHorn", "Dread Horn", typeof(DreadHorn), BestiaryCategory.Bosses, 1160, BossDropChance);
            Register("Travesty", "Travesty", typeof(Travesty), BestiaryCategory.Bosses, 1153, BossDropChance);
            Register("ShimmeringEffusion", "Shimmering Effusion", typeof(ShimmeringEffusion), BestiaryCategory.Bosses, 1152, BossDropChance);
            Register("MonstrousInterredGrizzle", "Monstrous Interred Grizzle", typeof(MonstrousInterredGrizzle), BestiaryCategory.Bosses, 1175, BossDropChance);
        }

        public static HunterProfile GetProfile(Mobile m)
        {
            if (m == null)
                return null;

            if (!m_Profiles.TryGetValue(m.Serial, out var profile))
            {
                profile = new HunterProfile(m.Serial);
                m_Profiles[m.Serial] = profile;
            }

            return profile;
        }

        public static bool HasLearned(Mobile m, Type creatureType)
        {
            var profile = GetProfile(m);
            return profile != null && profile.HasLearned(creatureType);
        }

        public static bool HasLearned(Mobile m, string creatureId)
        {
            var profile = GetProfile(m);
            return profile != null && profile.HasLearned(creatureId);
        }

        public static void OnCreatureDamaged(BaseCreature victim, Mobile attacker, ref int totalDamage)
        {
            if (victim == null || attacker == null || totalDamage <= 0)
                return;

            Mobile master = attacker is BaseCreature pet && pet.Controlled ? pet.ControlMaster : attacker;
            if (master == null)
                return;

            var profile = GetProfile(master);
            if (profile != null && profile.HasLearned(victim.GetType()))
            {
                totalDamage = (int)(totalDamage * 1.20); // +20% damage
            }
        }

        private static void OnCreatureDeath(CreatureDeathEventArgs e)
        {
            if (e == null || e.Creature == null || e.Corpse == null || e.Corpse.Deleted)
                return;

            if (e.Creature is BaseCreature bc && RegistryByType.TryGetValue(bc.GetType(), out var entry))
            {
                if (Utility.RandomDouble() < entry.DropChance)
                {
                    e.Corpse.DropItem(new HunterTreatise(entry.Id));
                }
            }
        }

        private static void OnCommandBestiary(CommandEventArgs e)
        {
            Mobile from = e.Mobile;
            if (from != null && !from.Deleted)
            {
                from.CloseGump(typeof(HunterBestiaryGump));
                from.SendGump(new HunterBestiaryGump(from));
            }
        }

        // =========================================================================
        // PERSISTENCE (XML)
        // =========================================================================

        private static void OnWorldSave(WorldSaveEventArgs e)
        {
            SaveProfiles();
        }

        private static void OnWorldLoad()
        {
            LoadProfiles();
        }

        public static void SaveProfiles()
        {
            try
            {
                string dir = Path.GetDirectoryName(SavePath);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                XmlDocument doc = new XmlDocument();
                XmlElement root = doc.CreateElement("HunterProfiles");
                doc.AppendChild(root);

                foreach (var kvp in m_Profiles)
                {
                    var profile = kvp.Value;
                    if (profile.TotalMastered == 0)
                        continue;

                    XmlElement pElem = doc.CreateElement("Profile");
                    pElem.SetAttribute("serial", kvp.Key.Value.ToString());

                    foreach (string id in profile.LearnedCreatures)
                    {
                        XmlElement cElem = doc.CreateElement("Creature");
                        cElem.SetAttribute("id", id);
                        pElem.AppendChild(cElem);
                    }

                    root.AppendChild(pElem);
                }

                doc.Save(SavePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine("[ExileHunterBestiary] Error saving profiles: " + ex.Message);
            }
        }

        public static void LoadProfiles()
        {
            try
            {
                if (!File.Exists(SavePath))
                    return;

                XmlDocument doc = new XmlDocument();
                doc.Load(SavePath);

                XmlNodeList list = doc.SelectNodes("//HunterProfiles/Profile");
                if (list == null)
                    return;

                m_Profiles.Clear();

                foreach (XmlElement pElem in list)
                {
                    if (int.TryParse(pElem.GetAttribute("serial"), out int sVal))
                    {
                        Serial serial = (Serial)sVal;
                        var profile = new HunterProfile(serial);

                        XmlNodeList cList = pElem.SelectNodes("Creature");
                        if (cList != null)
                        {
                            foreach (XmlElement cElem in cList)
                            {
                                string id = cElem.GetAttribute("id");
                                if (!string.IsNullOrEmpty(id))
                                {
                                    profile.LearnedCreatures.Add(id);
                                }
                            }
                        }

                        m_Profiles[serial] = profile;
                    }
                }

                Console.WriteLine($"[ExileHunterBestiary] Loaded {m_Profiles.Count} player bestiary profiles.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("[ExileHunterBestiary] Error loading profiles: " + ex.Message);
            }
        }
    }
}
