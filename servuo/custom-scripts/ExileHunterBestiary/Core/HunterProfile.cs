/*
 * UO Community Script: Hunter's Bestiary & Monster Treatises System
 * Original Author: UO Exile Team / Imagine (Resource #2674)
 * Source: https://www.servuo.dev/archive/release-hunters-bestiary-monster-treatises-system-193-creatures-1-line-install.2674/
 */

using System;
using System.Collections.Generic;
using Server.Mobiles;

namespace Server.Custom.ExileHunterBestiary
{
    public class HunterProfile
    {
        public Serial MobileSerial { get; set; }
        public HashSet<string> LearnedCreatures { get; private set; }

        public HunterProfile(Serial serial)
        {
            MobileSerial = serial;
            LearnedCreatures = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        public bool HasLearned(string creatureId)
        {
            if (string.IsNullOrEmpty(creatureId))
                return false;

            return LearnedCreatures.Contains(creatureId);
        }

        public bool HasLearned(Type creatureType)
        {
            if (creatureType == null)
                return false;

            if (HunterBestiaryEngine.RegistryByType.TryGetValue(creatureType, out var entry))
            {
                return HasLearned(entry.Id);
            }

            return false;
        }

        public bool Learn(string creatureId)
        {
            if (string.IsNullOrEmpty(creatureId) || LearnedCreatures.Contains(creatureId))
                return false;

            LearnedCreatures.Add(creatureId);
            return true;
        }

        public int GetCategoryMasteredCount(BestiaryCategory category)
        {
            int count = 0;
            if (HunterBestiaryEngine.EntriesByCategory.TryGetValue(category, out var list))
            {
                for (int i = 0; i < list.Count; i++)
                {
                    if (LearnedCreatures.Contains(list[i].Id))
                        count++;
                }
            }
            return count;
        }

        public int TotalMastered => LearnedCreatures.Count;
    }
}
