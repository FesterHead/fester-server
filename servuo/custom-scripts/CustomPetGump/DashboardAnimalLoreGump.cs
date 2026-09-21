/************************************************************************************
* Community Script: Dashboard Animal Lore Gump (Modern Pet Gump)                    *
* Author: Feng / UO Wildlands Team (September 2026, version 1.6)                    *
* Source: https://www.servuo.dev/archive/dashboard-animal-lore-gump-modern-pet-gump.2598/ *
*                                                                                   *
* Consolidated Animal Lore dashboard layout:                                        *
* - Single-page consolidated view for pet attributes, ratings, resistances, damage  *
*   distribution, abilities, and skill levels                                       *
* - Integrated pet training progression monitoring and sub-gump controls            *
* - Accompanied by 11-custom-pet-gump.patch for core Animal Lore & Pet Training hooks *
************************************************************************************/

using System;
using Server;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;
using Server.Targeting;
using System.Collections.Generic;
using System.Linq;
using Server.Misc;

namespace Server.Mobiles
{
    // ==========================================================================================
    // ANIMAL LORE GUMP (Custom Dashboard Layout)
    // ==========================================================================================
    public class DashboardAnimalLoreGump : BaseGump
    {
        private BaseCreature m_Creature;

        // Color Constants
        private const int C_YELLOW = 2213;  // Label Titles
        private const int C_WHITE = 2036;   // Values
        private const int C_RED = 1258;     // Fire / Special Moves
        private const int C_BLUE = 1264;    // Cold
        private const int C_GREEN = 1270;   // Poison
        private const int C_PURPLE = 1276;  // Energy
        private const int C_GREY = 2401;    // Physical / Empty Slots

        public DashboardAnimalLoreGump(PlayerMobile pm, BaseCreature bc) : base(pm, 50, 50)
        {
            m_Creature = bc;
        }

        public override void AddGumpLayout()
        {
            if (m_Creature == null || m_Creature.Deleted) return;

            int width = 640;
            int height = 850;

            AddPage(0);

            // Main Dark Background
            AddBackground(0, 0, width, height, 9200);
            AddImageTiled(10, 10, width - 20, height - 20, 2624);
            AddAlphaRegion(10, 10, width - 20, height - 20);

            // ------------------------------------------------------------------------
            // HEADER SECTION
            // ------------------------------------------------------------------------
            // Icon Frame (Top Left)
            //   AddBackground(20, 20, 110, 110, 9270); 

            // Draw the actual pet icon
            //   int petItemID = ShrinkTable.Lookup(m_Creature);
            //   AddItem(35, 35, petItemID); 

            string name = m_Creature.Name;

            // Check if the creature is flagged as Legendary via the interface
            if (m_Creature is ILegendaryPet leg && leg.IsLegendary)
            {
                name += " <BASEFONT COLOR=#FFD700>[Legendary]</BASEFONT>";
            }

            if (m_Creature.IsBonded) name += " [Bonded]";

            AddHtml(140, 25, 360, 25, ColorAndCenter("#FFCC00", name), false, false);

            // Top Right Buttons
            AddButton(width - 75, 21, 4005, 4007, 5, GumpButtonType.Reply, 0); // Refresh
            AddButton(width - 40, 20, 0xFB1, 0xFB3, 0, GumpButtonType.Reply, 0); // Close

            // ------------------------------------------------------------------------
            // ATTRIBUTES PANEL
            // ------------------------------------------------------------------------
            int attrX = 30;   // Matches c1X
            int attrY = 55;
            int attrCol2 = 340; // Matches c2X

            AddLabel(attrX, attrY, C_YELLOW, "Attributes");

            // Helper strings to calculate alignment
            string sStr = m_Creature.Str.ToString();
            string sDex = m_Creature.Dex.ToString();
            string sInt = m_Creature.Int.ToString();

            string sHits = m_Creature.Hits + " / " + m_Creature.HitsMax;
            string sStam = m_Creature.Stam + " / " + m_Creature.StamMax;
            string sMana = m_Creature.Mana + " / " + m_Creature.ManaMax;

            // Left Column: Attributes
            AddLabel(attrX, attrY + 25, C_WHITE, "Strength");
            AddLabel(attrX + 240 - GetLabelWidth(sStr), attrY + 25, C_WHITE, sStr);

            AddLabel(attrX, attrY + 50, C_WHITE, "Dexterity");
            AddLabel(attrX + 240 - GetLabelWidth(sDex), attrY + 50, C_WHITE, sDex);

            AddLabel(attrX, attrY + 75, C_WHITE, "Intelligence");
            AddLabel(attrX + 240 - GetLabelWidth(sInt), attrY + 75, C_WHITE, sInt);

            // Right Column: Stats
            AddLabel(attrCol2, attrY + 25, C_WHITE, "Hit Points");
            AddLabel(attrCol2 + 240 - GetLabelWidth(sHits), attrY + 25, C_WHITE, sHits);

            AddLabel(attrCol2, attrY + 50, C_WHITE, "Stamina");
            AddLabel(attrCol2 + 240 - GetLabelWidth(sStam), attrY + 50, C_WHITE, sStam);

            AddLabel(attrCol2, attrY + 75, C_WHITE, "Mana");
            AddLabel(attrCol2 + 240 - GetLabelWidth(sMana), attrY + 75, C_WHITE, sMana);

            // ------------------------------------------------------------------------
            // RESISTANCES & DAMAGE TYPES
            // ------------------------------------------------------------------------
            int resY = 175;
            DrawDivider(20, resY - 7, width - 40);
            AddLabel(30, resY + 5, C_YELLOW, "Resistances");

            DrawResistBox(120, resY + 5, "Physical", m_Creature.PhysicalResistance, C_GREY);
            DrawResistBox(220, resY + 5, "Fire", m_Creature.FireResistance, C_RED);
            DrawResistBox(320, resY + 5, "Cold", m_Creature.ColdResistance, C_BLUE);
            DrawResistBox(420, resY + 5, "Poison", m_Creature.PoisonResistance, C_GREEN);
            DrawResistBox(520, resY + 5, "Energy", m_Creature.EnergyResistance, C_PURPLE);

            int damY = 210;
            DrawDivider(20, damY - 5, width - 40);
            AddLabel(30, damY + 5, C_YELLOW, "Damage Type");

            DrawDamageBox(120, damY + 5, "Physical", m_Creature.PhysicalDamage, C_GREY);
            DrawDamageBox(220, damY + 5, "Fire", m_Creature.FireDamage, C_RED);
            DrawDamageBox(320, damY + 5, "Cold", m_Creature.ColdDamage, C_BLUE);
            DrawDamageBox(420, damY + 5, "Poison", m_Creature.PoisonDamage, C_GREEN);
            DrawDamageBox(520, damY + 5, "Energy", m_Creature.EnergyDamage, C_PURPLE);

            // ------------------------------------------------------------------------
            // SKILLS COLUMNS
            // ------------------------------------------------------------------------
            int colY = 255;
            DrawDivider(20, colY - 10, width - 40);

            int c1X = 30;
            AddHtml(c1X, colY, 250, 18, "<CENTER><BASEFONT COLOR=#FFD700>Combat Ratings</BASEFONT></CENTER>", false, false);
            int curY = colY + 30;

            DrawSkill(c1X, curY, "Wrestling", SkillName.Wrestling); curY += 20;
            DrawSkill(c1X, curY, "Tactics", SkillName.Tactics); curY += 20;
            DrawSkill(c1X, curY, "Resisting Spells", SkillName.MagicResist); curY += 20;
            DrawSkill(c1X, curY, "Anatomy", SkillName.Anatomy); curY += 20;
            DrawSkill(c1X, curY, "Healing", SkillName.Healing); curY += 20;
            DrawSkill(c1X, curY, "Poisoning", SkillName.Poisoning); curY += 20;
            DrawSkill(c1X, curY, "Detecting Hidden", SkillName.DetectHidden); curY += 20;
            DrawSkill(c1X, curY, "Hiding", SkillName.Hiding); curY += 20;
            DrawSkill(c1X, curY, "Parrying", SkillName.Parry); curY += 20;

            int c2X = 340;
            AddHtml(c2X, colY, 250, 18, "<CENTER><BASEFONT COLOR=#FFD700>Lore & Knowledge</BASEFONT></CENTER>", false, false);
            curY = colY + 30;

            DrawSkill(c2X, curY, "Magery", SkillName.Magery); curY += 20;
            DrawSkill(c2X, curY, "Eval Intelligence", SkillName.EvalInt); curY += 20;
            DrawSkill(c2X, curY, "Meditation", SkillName.Meditation); curY += 20;
            DrawSkill(c2X, curY, "Necromancy", SkillName.Necromancy); curY += 20;
            DrawSkill(c2X, curY, "Spirit Speak", SkillName.SpiritSpeak); curY += 20;
            DrawSkill(c2X, curY, "Mysticism", SkillName.Mysticism); curY += 20;
            DrawSkill(c2X, curY, "Focus", SkillName.Focus); curY += 20;
            DrawSkill(c2X, curY, "Spellweaving", SkillName.Spellweaving); curY += 20;
            DrawSkill(c2X, curY, "Discordance", SkillName.Discordance); curY += 20;
            DrawSkill(c2X, curY, "Bushido", SkillName.Bushido); curY += 20;
            DrawSkill(c2X, curY, "Ninjitsu", SkillName.Ninjitsu); curY += 20;
            DrawSkill(c2X, curY, "Chivalry", SkillName.Chivalry); curY += 20;

            // ------------------------------------------------------------------------
            // REGENERATION & SPECIAL ABILITIES
            // ------------------------------------------------------------------------
            int regenY = colY + 235;
            //     DrawDivider(20, regenY - 5, (width / 2) - 40); 

            AddHtml(c1X, regenY, 250, 18, "<CENTER><BASEFONT COLOR=#FFD700>Regeneration</BASEFONT></CENTER>", false, false);
            var profile = PetTrainingHelper.GetAbilityProfile(m_Creature, true);

            AddLabel(c1X, regenY + 25, C_WHITE, "Hit Point Regeneration");
            AddLabel(c1X + 220, regenY + 25, C_WHITE, ((int)RegenRates.HitPointRegen(m_Creature)).ToString());

            AddLabel(c1X, regenY + 45, C_WHITE, "Stamina Regeneration");
            AddLabel(c1X + 220, regenY + 45, C_WHITE, ((int)RegenRates.StamRegen(m_Creature)).ToString());

            AddLabel(c1X, regenY + 65, C_WHITE, "Mana Regeneration");
            AddLabel(c1X + 220, regenY + 65, C_WHITE, ((int)RegenRates.ManaRegen(m_Creature)).ToString());

            // --- SPECIAL ABILITIES HEADING (RIGHT COLUMN - ALIGNED) ---
            // Changed + 36 to + 65 to match Mana Regeneration exactly
            AddHtml(c2X, regenY + 65, 250, 18, "<CENTER><BASEFONT COLOR=#FFD700>Special Abilities</BASEFONT></CENTER>", false, false);

            // --- ABILITIES LIST STARTING POINT ---
            int moveY = regenY + 70;

            // --- CENTERED SPECIAL ABILITIES (Combined Innate + Trained) ---
            List<string> displayedAbils = new List<string>();

            // 0. Magical Ability (school)
            if (profile.MagicalAbility != MagicalAbility.None)
            {
                foreach (var abil in PetTrainingHelper.MagicalAbilities)
                {
                    if ((profile.MagicalAbility & abil) != 0)
                    {
                        var loc = PetTrainingHelper.GetLocalization(abil);
                        string displayName = (loc != null && loc.Length > 0 && loc[0] != null && !string.IsNullOrEmpty(loc[0].String))
                            ? loc[0].String
                            : FormatAbilityName(abil.ToString());

                        AddHtml(c2X, moveY += 18, 250, 18, String.Format("<CENTER><BASEFONT COLOR=#FFFFFF>{0}</BASEFONT></CENTER>", displayName), false, false);
                    }
                }
            }

            if (profile.SpecialAbilities != null)
            {
                foreach (var abil in profile.SpecialAbilities)
                {
                    string typeKey = abil.GetType().Name;
                    if (displayedAbils.Contains(typeKey)) continue;
                    if (typeKey == "ColossalRage" || typeKey == "Heal") continue;
                    var loc = PetTrainingHelper.GetLocalization(abil);
                    string displayName = (loc != null && loc.Length > 0 && loc[0] != null && !string.IsNullOrEmpty(loc[0].String))
                        ? loc[0].String
                        : FormatAbilityName(typeKey);

                    // This now starts exactly 1 line (18px) below the "Special Abilities" header
                    AddHtml(c2X, moveY += 18, 250, 18, String.Format("<CENTER><BASEFONT COLOR=#FFFFFF>{0}</BASEFONT></CENTER>", displayName), false, false);
                    displayedAbils.Add(typeKey);
                }
            }

            // 2. Weapon Abilities stored in the profile (set via SetWeaponAbility)
            if (profile.WeaponAbilities != null)
            {
                foreach (var abil in profile.WeaponAbilities)
                {
                    string typeKey = abil.GetType().Name;
                    if (displayedAbils.Contains(typeKey)) continue;

                    // GetLocalization has no WeaponAbility overload in most builds,
                    // so always fall back to the formatted type name.
                    AddHtml(c2X, moveY += 18, 250, 18, String.Format("<CENTER><BASEFONT COLOR=#FFFFFF>{0}</BASEFONT></CENTER>", FormatAbilityName(typeKey)), false, false);
                    displayedAbils.Add(typeKey);
                }
            }

            // 3. Fallback: catch weapon ability returned only via GetWeaponAbility() override
            //    (i.e. a creature that overrides the method without calling SetWeaponAbility)
            WeaponAbility overrideWa = m_Creature.GetWeaponAbility();
            if (overrideWa != null)
            {
                string typeKey = overrideWa.GetType().Name;
                if (!displayedAbils.Contains(typeKey))
                {
                    AddHtml(c2X, moveY += 18, 250, 18, String.Format("<CENTER><BASEFONT COLOR=#FFFFFF>{0}</BASEFONT></CENTER>", FormatAbilityName(typeKey)), false, false);
                    displayedAbils.Add(typeKey);
                }
            }

            // 4. Area Effects
            if (profile.AreaEffects != null)
            {
                foreach (var abil in profile.AreaEffects)
                {
                    string typeKey = abil.GetType().Name;
                    if (displayedAbils.Contains(typeKey)) continue;

                    var loc = PetTrainingHelper.GetLocalization(abil);
                    string displayName = (loc != null && loc.Length > 0 && loc[0] != null && !string.IsNullOrEmpty(loc[0].String))
                        ? loc[0].String
                        : FormatAbilityName(typeKey);

                    AddHtml(c2X, moveY += 18, 250, 18, String.Format("<CENTER><BASEFONT COLOR=#FFFFFF>{0}</BASEFONT></CENTER>", displayName), false, false);
                    displayedAbils.Add(typeKey);
                }
            }

            // ------------------------------------------------------------------------
            // MISCELLANEOUS
            // ------------------------------------------------------------------------
            int miscY = regenY + 130;
            //    DrawDivider(20, miscY - 5, (width / 2) - 40); 
            AddHtml(c1X, miscY, 250, 18, "<CENTER><BASEFONT COLOR=#FFD700>Miscellaneous</BASEFONT></CENTER>", false, false);

            AddLabel(c1X, miscY + 25, C_WHITE, "Loyalty Rating");
            AddLabel(c1X + 140, miscY + 25, C_WHITE, GetHappinessString(m_Creature));

            AddLabel(c1X, miscY + 45, C_WHITE, "Preferred Foods");
            AddLabel(c1X + 140, miscY + 45, C_WHITE, GetFoodString(m_Creature.FavoriteFood));

            AddLabel(c1X, miscY + 65, C_WHITE, "Pack Instincts");
            AddLabel(c1X + 140, miscY + 65, C_WHITE, m_Creature.PackInstinct.ToString());

            AddLabel(c1X, miscY + 85, C_WHITE, "Base Damage");
            AddLabel(c1X + 140, miscY + 85, C_WHITE, m_Creature.DamageMin + " - " + m_Creature.DamageMax);

            AddLabel(c1X, miscY + 105, C_WHITE, "Pet Slots");
            AddLabel(c1X + 140, miscY + 105, C_WHITE, m_Creature.ControlSlots + " => " + m_Creature.ControlSlotsMax + " (Req. " + m_Creature.MinTameSkill.ToString("F1") + ")");

            // ------------------------------------------------------------------------
            // FOOTER & TRAINING
            // ------------------------------------------------------------------------
            int footerY = height - 60;
            DrawDivider(20, footerY - 10, width - 40);

            if (m_Creature.Controlled && m_Creature.ControlMaster == User)
            {
                var trainProfile = PetTrainingHelper.GetTrainingProfile(m_Creature, true);
                var def = PetTrainingHelper.GetTrainingDefinition(m_Creature);

                // Use robust logic from original gump to check if training is possible/begun
                if (trainProfile.HasBegunTraining && def != null && def.Class != Class.Untrainable)
                {
                    AddLabel(40, footerY + 10, C_YELLOW, "Training Progress:");
                    AddImage(170, footerY + 15, 0x805);

                    double progress = trainProfile.TrainingProgressPercentile * 100;
                    if (progress >= 1)
                        AddBackground(170, footerY + 15, (int)(109.0 * (progress / 100)), 11, 0x806);

                    AddLabel(290, footerY + 10, C_WHITE, progress.ToString("F1") + "%");

                    // Show options if 100% progress
                    if (trainProfile.CanApplyOptions)
                    {
                        AddButton(400, footerY + 10, 0x15E1, 0x15E5, 2, GumpButtonType.Reply, 0); // Options
                        AddLabel(430, footerY + 10, C_WHITE, "Options");
                    }


                    // Cancel button
                    AddButton(600, footerY + 12, 0xFB1, 0xFB3, 3, GumpButtonType.Reply, 0);
                }
                else if (m_Creature.ControlSlots < m_Creature.ControlSlotsMax)
                {
                    AddButton(40, footerY + 10, 4005, 4007, 4, GumpButtonType.Reply, 0);
                    AddLabel(75, footerY + 10, C_YELLOW, "Begin Animal Training");
                }
                else
                {
                    AddLabel(40, footerY + 10, 0x35, "This pet is fully trained.");
                }
            }
        }

        public override void OnResponse(RelayInfo info)
        {
            if (m_Creature == null || m_Creature.Deleted) return;

            switch (info.ButtonID)
            {


                case 2: // Options
                    {
                        if (User.HasGump(typeof(PetTrainingConfirmationGump)) ||
                            User.HasGump(typeof(PetTrainingOptionsGump)) ||
                            User.HasGump(typeof(PetTrainingPlanningGump)) ||
                            User.HasGump(typeof(PetTrainingConfirmGump)))
                        {
                            Refresh();
                            break;
                        }

                        var trainProfile = PetTrainingHelper.GetTrainingProfile(m_Creature, true);

                        if (trainProfile.HasBegunTraining)
                        {
                            if (!trainProfile.HasRecievedControlSlotWarning)
                            {
                                trainProfile.HasRecievedControlSlotWarning = true;

                                Timer.DelayCall(TimeSpan.FromSeconds(.5), () =>
                                {
                                    BaseGump.SendGump(new PetTrainingConfirmGump(User, 1157571, 1157572, () =>
                                    {
                                        Refresh();
                                        User.CloseGump(typeof(PetTrainingOptionsGump));
                                        BaseGump.SendGump(new PetTrainingOptionsGump(User, m_Creature));
                                    }));
                                });
                            }
                            else
                            {
                                Timer.DelayCall(TimeSpan.FromSeconds(.5), () =>
                                {
                                    Refresh();
                                    User.CloseGump(typeof(PetTrainingOptionsGump));
                                    BaseGump.SendGump(new PetTrainingOptionsGump(User, m_Creature));
                                });
                            }
                        }
                    }
                    break;

                case 3: // Cancel Training
                    BaseGump.SendGump(new PetTrainingConfirmGump(User, 1153093, 1158019, () => {
                        var tp = PetTrainingHelper.GetTrainingProfile(m_Creature, true);
                        if (tp != null) tp.EndTraining();
                    }));
                    break;

                case 4: // Begin Training
                    PetTrainingHelper.GetTrainingProfile(m_Creature, true).BeginTraining();
                    Refresh();
                    // Important: Trigger the quest check that original Gumps.cs has
                    Server.Engines.Quests.UsingAnimalLoreQuest.CheckComplete(User);
                    break;

                case 5: // Refresh
                    Refresh();
                    break;
            }
        }

        // ==========================================================================================
        // DYNAMIC SHRINK TABLE LOOKUP
        // ==========================================================================================
        public class ShrinkTable
        {
            public static int Lookup(BaseCreature bc)
            {
                if (bc == null) return 0x20D1;

                int body = bc.Body.BodyID;

                if (body == 178 || body == 179) return 0x2121; // Nightmare Statue
                if (body == 210 || body == 211) return 0x211F; // Bake Kitsune
                if (body == 0x317) return 0x2615; // Beetle

                // Fallback to a generic pet icon if a specific statue isn't mapped
                return 0x20D1;
            }
        }

        private void DrawResistBox(int x, int y, string name, int val, int color)
        {
            AddLabel(x, y, C_WHITE, name);
            AddLabel(x + 55, y, color, val + "%");
        }

        private void DrawDamageBox(int x, int y, string name, int val, int color)
        {
            AddLabel(x, y, C_WHITE, name);
            AddLabel(x + 55, y, val > 0 ? color : C_GREY, val > 0 ? val + "%" : "---");
        }

        private void DrawSkill(int x, int y, string name, SkillName sk)
        {
            Skill skill = m_Creature.Skills[sk];
            AddLabel(x, y, C_WHITE, name);

            string val = skill.Base > 0 ? skill.Value.ToString("F1") + " / " + skill.Cap.ToString("F1") : "---";

            // This forces the value to be right-aligned based on its exact pixel width
            AddLabel(x + 240 - GetLabelWidth(val), y, C_WHITE, val);
        }

        private int GetLabelWidth(string text)
        {
            int width = 0;
            foreach (char c in text)
            {
                switch (c)
                {
                    case '1': width += 4; break; // 3px + 1px spacing
                    case '.': width += 3; break; // 2px + 1px spacing
                    case ' ': width += 4; break; // 3px + 1px spacing
                    case '-': width += 6; break; // 5px + 1px spacing
                    case '/': width += 7; break; // 6px + 1px spacing
                    default: width += 8; break; // Most numbers and letters are 7px + 1px spacing
                }
            }
            return width;
        }

        private void DrawDivider(int x, int y, int width)
        {
            AddImageTiled(x, y, width, 2, 96);
        }

        // Converts a PascalCase type name (e.g. "ArmorIgnore", "LifeLeech") into a
        // human-readable label ("Armor Ignore", "Life Leech") for abilities whose
        // localization data is missing or empty.
        private string FormatAbilityName(string typeName)
        {
            if (string.IsNullOrEmpty(typeName)) return typeName;

            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            for (int i = 0; i < typeName.Length; i++)
            {
                if (i > 0 && char.IsUpper(typeName[i]))
                    sb.Append(' ');
                sb.Append(typeName[i]);
            }
            return sb.ToString();
        }

        private string GetHappinessString(BaseCreature bc)
        {
            if (bc.Loyalty >= 100) return "Wonderfully Happy";
            if (bc.Loyalty >= 80) return "Extremely Happy";
            if (bc.Loyalty >= 60) return "Very Happy";
            if (bc.Loyalty >= 40) return "Rather Happy";
            if (bc.Loyalty >= 20) return "Happy";
            return "Confused";
        }

        private string GetFoodString(FoodType food)
        {
            if (food == FoodType.None) return "None";
            List<string> foods = new List<string>();
            if ((food & FoodType.Meat) != 0) foods.Add("Meat");
            if ((food & FoodType.FruitsAndVegies) != 0) foods.Add("Fruits/Veg");
            if ((food & FoodType.GrainsAndHay) != 0) foods.Add("Grains");
            if ((food & FoodType.Fish) != 0) foods.Add("Fish");
            if ((food & FoodType.Eggs) != 0) foods.Add("Eggs");
            if ((food & FoodType.Gold) != 0) foods.Add("Gold");
            return foods.Count == 0 ? "Unknown" : String.Join(", ", foods);
        }
    }
}
