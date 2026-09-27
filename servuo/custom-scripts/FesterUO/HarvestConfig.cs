using System;
using Server;
using Server.Commands;
using Server.Engines.Harvest;

namespace Server.Custom
{
    public static class HarvestConfig
    {
        public static void Initialize()
        {
            ApplyConfig();
            CommandSystem.Register("HarvestInfo", AccessLevel.GameMaster, HarvestInfo_OnCommand);
        }

        public static void ApplyConfig()
        {
            // Respawn times (in minutes)
            double oreMinRespawn = Config.Get("Harvest.OreMinRespawnMinutes", 10.0);
            double oreMaxRespawn = Config.Get("Harvest.OreMaxRespawnMinutes", 20.0);
            double treeMinRespawn = Config.Get("Harvest.TreeMinRespawnMinutes", 20.0);
            double treeMaxRespawn = Config.Get("Harvest.TreeMaxRespawnMinutes", 30.0);

            // Bank capacity (vein/log pool per bank)
            int oreMinCapacity = Config.Get("Harvest.OreMinCapacity", 10);
            int oreMaxCapacity = Config.Get("Harvest.OreMaxCapacity", 34);
            int treeMinCapacity = Config.Get("Harvest.TreeMinCapacity", 20);
            int treeMaxCapacity = Config.Get("Harvest.TreeMaxCapacity", 45);

            var mining = Mining.System;
            if (mining?.OreAndStone != null)
            {
                mining.OreAndStone.MinRespawn = TimeSpan.FromMinutes(Math.Max(0.01, oreMinRespawn));
                mining.OreAndStone.MaxRespawn = TimeSpan.FromMinutes(Math.Max(0.01, oreMaxRespawn));
                mining.OreAndStone.MinTotal = Math.Max(1, oreMinCapacity);
                mining.OreAndStone.MaxTotal = Math.Max(mining.OreAndStone.MinTotal, oreMaxCapacity);
            }

            if (mining?.Sand != null)
            {
                mining.Sand.MinRespawn = TimeSpan.FromMinutes(Math.Max(0.01, oreMinRespawn));
                mining.Sand.MaxRespawn = TimeSpan.FromMinutes(Math.Max(0.01, oreMaxRespawn));
            }

            var lumber = Lumberjacking.System;
            if (lumber?.Definition != null)
            {
                lumber.Definition.MinRespawn = TimeSpan.FromMinutes(Math.Max(0.01, treeMinRespawn));
                lumber.Definition.MaxRespawn = TimeSpan.FromMinutes(Math.Max(0.01, treeMaxRespawn));
                lumber.Definition.MinTotal = Math.Max(1, treeMinCapacity);
                lumber.Definition.MaxTotal = Math.Max(lumber.Definition.MinTotal, treeMaxCapacity);
            }

            Utility.PushColor(ConsoleColor.Green);
            Console.WriteLine($"HarvestConfig: Ore respawn={oreMinRespawn:F1}-{oreMaxRespawn:F1}m (cap={oreMinCapacity}-{oreMaxCapacity}), Trees respawn={treeMinRespawn:F1}-{treeMaxRespawn:F1}m (cap={treeMinCapacity}-{treeMaxCapacity})");
            Utility.PopColor();
        }

        [Usage("HarvestInfo")]
        [Description("Displays current harvest respawn intervals and bank capacities.")]
        private static void HarvestInfo_OnCommand(CommandEventArgs e)
        {
            var mining = Mining.System?.OreAndStone;
            var lumber = Lumberjacking.System?.Definition;

            if (mining != null)
            {
                e.Mobile.SendMessage(68, $"[Mining] Respawn: {mining.MinRespawn.TotalMinutes:F1} - {mining.MaxRespawn.TotalMinutes:F1} min | Bank Capacity: {mining.MinTotal} - {mining.MaxTotal} ore");
            }

            if (lumber != null)
            {
                e.Mobile.SendMessage(68, $"[Lumberjacking] Respawn: {lumber.MinRespawn.TotalMinutes:F1} - {lumber.MaxRespawn.TotalMinutes:F1} min | Bank Capacity: {lumber.MinTotal} - {lumber.MaxTotal} logs");
            }
        }
    }
}
