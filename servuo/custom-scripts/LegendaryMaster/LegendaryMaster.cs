/*
 * UO Community Script: Legendary Master of Skills
 * Original Author: Keith (Resource #2244)
 * Source: https://www.servuo.dev/archive/legendary-master-of-skills.2244/
 * Licensed under the GNU General Public License v3.0 (GPL-3.0)
 *
 * Provides an NPC questmaster that offers combat tasks to Grandmaster (100.0+)
 * players seeking PowerScrolls without running Champion Spawns.
 *
 * Enhancements & Fixes:
 * - Fixed player kill credit: Direct player weapon/spell kills now properly count toward tasks (previously only pet/summon kills counted).
 * - Fixed fatal ArgumentOutOfRangeException: Removed invalid taskInfos[pm.Serial] list indexing on speech.
 * - Fixed reward timing: Tasks now properly reward the PowerScroll upon the final target kill (TaskAmount reaching 0).
 * - Fixed countdown display: Formatted remaining time in human-readable integer minutes instead of raw DateTime timestamps.
 * - Fixed creature name display: Displays clean creature names (e.g. "Shadow Wyrm", "Balron") instead of C# class Type names.
 * - Fixed creature selection off-by-one: Restored White Wyrm to the random selection pool.
 * - Case-insensitive & flexible speech: Recognizes "give task <skill>" case-insensitively and accepts both display and internal skill names.
 * - PowerScroll validation: Restricts tasks to valid skills defined in PowerScroll.Skills.
 * - State persistence: Added WorldSave / WorldLoad persistence so active player tasks survive server restarts.
 * - Externalized configuration: Time limits and bonus scroll chances are parameterized in servuo/Config/LegendaryMaster/LegendaryMaster.cfg.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Server;
using Server.Items;
using Server.Mobiles;

namespace Server.Custom.Misc
{
    public class LegendaryMaster : BaseCreature
    {
        private static readonly string PersistencePath = Path.Combine("Saves", "LegendaryMaster", "Persistence.bin");
        private static List<SkillTaskInfo> m_TaskInfos = new List<SkillTaskInfo>();

        private static List<Type> m_CreatureList;

        public static List<Type> CreatureList
        {
            get
            {
                if (m_CreatureList == null || m_CreatureList.Count == 0)
                {
                    LoadCreatures();
                }

                return m_CreatureList;
            }
        }

        public static void LoadCreatures()
        {
            string defaultList = "Balron, ShadowWyrm, AncientLich, AncientWyrm, SkeletalDragon, GreaterDragon, Succubus, RottingCorpse, BloodElemental, PoisonElemental, SerpentineDragon, BoneDemon, RuneBeetle, Yamandon, WhiteWyrm";
            string configStr = Config.Get("LegendaryMaster.Creatures", defaultList);

            var list = new List<Type>();
            string[] names = configStr.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string rawName in names)
            {
                string name = rawName.Trim();
                if (string.IsNullOrEmpty(name))
                    continue;

                Type t = ScriptCompiler.FindTypeByName(name);
                if (t != null && typeof(BaseCreature).IsAssignableFrom(t))
                {
                    if (!list.Contains(t))
                        list.Add(t);
                }
                else
                {
                    Console.WriteLine($"[LegendaryMaster]: Warning - creature type '{name}' could not be resolved as a valid BaseCreature.");
                }
            }

            if (list.Count == 0)
            {
                list.AddRange(new[]
                {
                    typeof(Balron),
                    typeof(ShadowWyrm),
                    typeof(AncientLich),
                    typeof(AncientWyrm),
                    typeof(SkeletalDragon),
                    typeof(WhiteWyrm)
                });
            }

            m_CreatureList = list;
        }

        public static void Configure()
        {
            EventSink.WorldSave += OnSave;
            EventSink.WorldLoad += OnLoad;
        }

        public static void Initialize()
        {
            EventSink.CreatureDeath += EventSink_CreatureDeath;
            LoadCreatures();
        }

        private static void OnSave(WorldSaveEventArgs e)
        {
            Persistence.Serialize(
                PersistencePath,
                writer =>
                {
                    writer.Write(0); // version

                    // Clean expired or completed tasks before saving
                    m_TaskInfos.RemoveAll(t => t.Completed || t.TimeLimit <= DateTime.UtcNow);

                    writer.Write(m_TaskInfos.Count);

                    foreach (var task in m_TaskInfos)
                    {
                        writer.Write((int)task.PlayerSerial);
                        writer.Write((int)task.PlayerSkill);
                        writer.Write(task.SkillCap);
                        writer.Write(task.TargetType != null ? task.TargetType.FullName : string.Empty);
                        writer.Write(task.TimeLimit);
                        writer.Write(task.TaskAmount);
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

                    m_TaskInfos = new List<SkillTaskInfo>(count);

                    for (int i = 0; i < count; i++)
                    {
                        Serial serial = reader.ReadInt();
                        SkillName skill = (SkillName)reader.ReadInt();
                        double cap = reader.ReadDouble();
                        string typeName = reader.ReadString();
                        DateTime timeLimit = reader.ReadDateTime();
                        int amount = reader.ReadInt();

                        Type targetType = ScriptCompiler.FindTypeByFullName(typeName);

                        if (targetType != null && timeLimit > DateTime.UtcNow)
                        {
                            m_TaskInfos.Add(new SkillTaskInfo
                            {
                                PlayerSerial = serial,
                                PlayerSkill = skill,
                                SkillCap = cap,
                                TargetType = targetType,
                                TimeLimit = timeLimit,
                                TaskAmount = amount,
                                Completed = false
                            });
                        }
                    }
                });
        }

        private static void EventSink_CreatureDeath(CreatureDeathEventArgs e)
        {
            if (e.Killer == null || e.Creature == null)
                return;

            if (!CreatureList.Contains(e.Creature.GetType()))
                return;

            if (m_TaskInfos == null || m_TaskInfos.Count == 0)
                return;

            PlayerMobile pm = null;

            if (e.Killer is PlayerMobile player)
            {
                pm = player;
            }
            else if (e.Killer is BaseCreature bc)
            {
                if (bc.Controlled && bc.ControlMaster is PlayerMobile cm)
                    pm = cm;
                else if (bc.Summoned && bc.SummonMaster is PlayerMobile sm)
                    pm = sm;
            }

            if (pm == null)
                return;

            SkillTaskInfo currentTask = m_TaskInfos.FirstOrDefault(t => t.PlayerSerial == pm.Serial && !t.Completed);

            if (currentTask == null)
                return;

            if (DateTime.UtcNow > currentTask.TimeLimit)
            {
                pm.SendMessage(53, "You ran out of time for your skill task!");
                m_TaskInfos.Remove(currentTask);
                return;
            }

            if (currentTask.TargetType == e.Creature.GetType())
            {
                currentTask.TaskAmount--;

                int extraMinutes = Config.Get("LegendaryMaster.ExtraTimeMinutes", 10);
                currentTask.TimeLimit += TimeSpan.FromMinutes(extraMinutes);

                if (currentTask.TaskAmount <= 0)
                {
                    currentTask.Completed = true;

                    double scrollInc = Config.Get("LegendaryMaster.ScrollIncrement", 5.0);
                    double rewardCap = currentTask.SkillCap + scrollInc;
                    pm.AddToBackpack(new PowerScroll(currentTask.PlayerSkill, rewardCap));
                    pm.SendMessage(53, $"Congratulations! You were rewarded a {rewardCap:F0} {currentTask.PlayerSkill} Powerscroll!");

                    TryGiveStatScroll(pm);
                    m_TaskInfos.Remove(currentTask);
                }
                else
                {
                    currentTask.TargetType = GetRandomCreature();
                    int remainingMinutes = Math.Max(1, (int)(currentTask.TimeLimit - DateTime.UtcNow).TotalMinutes);
                    pm.SendMessage(53, $"Target eliminated! {currentTask.TaskAmount} left to kill. Next target: {FormatCreatureName(currentTask.TargetType)} ({remainingMinutes} minutes remaining).");
                }
            }
        }

        private static void TryGiveStatScroll(PlayerMobile pm)
        {
            if (!Config.Get("LegendaryMaster.EnableStatScrolls", true))
                return;

            double statChance = Config.Get("LegendaryMaster.StatScrollChance", 0.01);

            if (Utility.RandomDouble() < statChance)
            {
                double highChance = Config.Get("LegendaryMaster.HighStatScrollChance", 0.01);
                int maxBonus = Config.Get("LegendaryMaster.MaxStatScrollBonus", 25);
                int bonus = (Utility.RandomDouble() < highChance) ? Math.Min(maxBonus, RandomStatScrollLevel()) : Math.Min(maxBonus, 5);

                pm.AddToBackpack(new StatCapScroll(pm.StatCap + bonus));
                pm.SendLocalizedMessage(1049524); // You have received a scroll of power!
                pm.SendMessage(53, $"Bonus reward: You have received a +{bonus} Stat Cap Scroll!");
            }
        }

        private static int RandomStatScrollLevel()
        {
            double random = Utility.RandomDouble();

            if (random <= 0.10)
                return 25;
            if (random <= 0.25)
                return 20;
            if (random <= 0.45)
                return 15;
            if (random <= 0.70)
                return 10;

            return 5;
        }

        private static Type GetRandomCreature()
        {
            return CreatureList[Utility.Random(CreatureList.Count)];
        }

        public static string FormatCreatureName(Type type)
        {
            if (type == null)
                return "Unknown";

            return Regex.Replace(type.Name, "(\\B[A-Z])", " $1");
        }

        public override bool IsInvulnerable => true;

        [Constructable]
        public LegendaryMaster() : base(AIType.AI_Vendor, FightMode.None, 10, 1, 0.2, 0.4)
        {
            Name = "Legendary Master of Skills";
            Title = "the Master of Skills";

            Body = 0x190;
            Hue = Race.RandomSkinHue();

            HairItemID = 0x203C;
            HairHue = 0x481;

            FacialHairItemID = 0x203E;
            FacialHairHue = 0x481;

            InitStats(150, 150, 150);

            SetWearable(new HoodedShroudOfShadows(), 0x481);
            SetWearable(new Sandals(), 0x481);

            Blessed = true;
            CantWalk = true;
            SpeechHue = 53;
        }

        public override bool HandlesOnSpeech(Mobile from)
        {
            return true;
        }

        public override void OnSpeech(SpeechEventArgs e)
        {
            int range = Config.Get("LegendaryMaster.InteractionRange", 5);

            if (e.Mobile is PlayerMobile pm && pm.InRange(Location, range))
            {
                string speech = e.Speech.Trim();

                if (speech.StartsWith("give task", StringComparison.OrdinalIgnoreCase))
                {
                    if (pm.IsStaff())
                    {
                        SayTo(pm, "I am on duty, waiting for mortal adventurers to assist!");
                        return;
                    }

                    var existing = m_TaskInfos.FirstOrDefault(t => t.PlayerSerial == pm.Serial && !t.Completed);
                    if (existing != null)
                    {
                        if (DateTime.UtcNow > existing.TimeLimit)
                        {
                            m_TaskInfos.Remove(existing);
                            SayTo(pm, "Your previous task expired. You may now undertake a new quest.");
                        }
                        else
                        {
                            int remaining = Math.Max(1, (int)(existing.TimeLimit - DateTime.UtcNow).TotalMinutes);
                            SayTo(pm, $"You already have an active task for {existing.PlayerSkill}! Kill {existing.TaskAmount} {FormatCreatureName(existing.TargetType)} ({remaining} minutes left).");
                            return;
                        }
                    }

                    string skillParam = speech.Substring("give task".Length).Trim();
                    if (string.IsNullOrEmpty(skillParam))
                    {
                        SayTo(pm, "Please specify which skill you want a task for. Example: 'give task swordsmanship' or 'give task magery'.");
                        return;
                    }

                    Skill targetSkill = null;
                    foreach (var s in pm.Skills)
                    {
                        if (s.Name.Equals(skillParam, StringComparison.OrdinalIgnoreCase) ||
                            s.SkillName.ToString().Equals(skillParam, StringComparison.OrdinalIgnoreCase) ||
                            s.Name.ToLower().StartsWith(skillParam.ToLower()))
                        {
                            targetSkill = s;
                            break;
                        }
                    }

                    if (targetSkill == null)
                    {
                        SayTo(pm, $"I could not find a skill matching '{skillParam}'. Please check your spelling.");
                        return;
                    }

                    if (!PowerScroll.Skills.Contains(targetSkill.SkillName))
                    {
                        SayTo(pm, $"Powerscrolls do not exist for {targetSkill.Name}.");
                        return;
                    }

                    double maxCap = Config.Get("LegendaryMaster.MaxSkillCap", 120.0);
                    double minSkill = Config.Get("LegendaryMaster.MinSkillRequired", 100.0);
                    double scrollInc = Config.Get("LegendaryMaster.ScrollIncrement", 5.0);

                    if (targetSkill.Cap >= maxCap)
                    {
                        if (targetSkill.Value < maxCap)
                        {
                            SayTo(pm, $"Your {targetSkill.Name} cap is already {targetSkill.Cap:F0}! Train your skill up before seeking mastery.");
                        }
                        else
                        {
                            SayTo(pm, $"Your {targetSkill.Name} is already at maximum ({targetSkill.Value:F0})! You're not fooling me!");
                        }
                        return;
                    }

                    if (targetSkill.Value < minSkill)
                    {
                        SayTo(pm, $"Your {targetSkill.Name} skill is too low ({targetSkill.Value:F1}). Return to me once you have reached {minSkill:F1} standing!");
                        return;
                    }

                    if (targetSkill.Value < targetSkill.Cap)
                    {
                        SayTo(pm, $"You must reach your current skill cap of {targetSkill.Cap:F0} in {targetSkill.Name} before you can unlock the next tier!");
                        return;
                    }

                    int tier = (int)Math.Max(0, (targetSkill.Cap - minSkill) / scrollInc);
                    int minKills = Config.Get("LegendaryMaster.BaseMinKills", 1);
                    int maxKills = Math.Max(minKills, Config.Get("LegendaryMaster.BaseMaxKills", 3) + (tier * Config.Get("LegendaryMaster.KillsPerTier", 1)));
                    int killCount = Utility.RandomMinMax(minKills, maxKills);

                    AssignTask(pm, targetSkill, GetRandomCreature(), killCount);
                }
                else if (speech.StartsWith("remove task", StringComparison.OrdinalIgnoreCase) ||
                         speech.StartsWith("cancel task", StringComparison.OrdinalIgnoreCase))
                {
                    var task = m_TaskInfos.FirstOrDefault(t => t.PlayerSerial == pm.Serial);
                    if (task != null)
                    {
                        m_TaskInfos.Remove(task);
                        SayTo(pm, "Your task has been cancelled. You are free to undertake a new quest at any time.");
                    }
                    else
                    {
                        SayTo(pm, "You do not currently have any active tasks.");
                    }
                }
                else if (Utility.RandomDouble() < Config.Get("LegendaryMaster.IdleBarkChance", 0.15))
                {
                    SayTo(pm, "Looking to expand your skills beyond mortal limits? Say 'give task <skill>' to receive a quest for a Powerscroll!");
                }
            }

            base.OnSpeech(e);
        }

        private void AssignTask(PlayerMobile pm, Skill skill, Type target, int amount)
        {
            int startMinutes = Config.Get("LegendaryMaster.TaskTimeMinutes", 60);
            double scrollInc = Config.Get("LegendaryMaster.ScrollIncrement", 5.0);

            var info = new SkillTaskInfo
            {
                PlayerSerial = pm.Serial,
                PlayerSkill = skill.SkillName,
                SkillCap = skill.Cap,
                TargetType = target,
                TimeLimit = DateTime.UtcNow + TimeSpan.FromMinutes(startMinutes),
                TaskAmount = amount,
                Completed = false
            };

            m_TaskInfos.Add(info);

            Effects.SendBoltEffect(pm, true);
            SayTo(pm, $"Task assigned! Slay {amount} {FormatCreatureName(target)} within {startMinutes} minutes to earn a {skill.Cap + scrollInc:F0} {skill.Name} Powerscroll!");
        }

        public LegendaryMaster(Serial serial) : base(serial)
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
            _ = reader.ReadInt();
        }

        private class SkillTaskInfo
        {
            public Serial PlayerSerial;
            public SkillName PlayerSkill;
            public double SkillCap;
            public Type TargetType;
            public DateTime TimeLimit;
            public bool Completed;
            public int TaskAmount;
        }
    }
}
