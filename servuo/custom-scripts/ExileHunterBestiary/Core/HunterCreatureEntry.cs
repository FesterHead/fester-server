/*
 * UO Community Script: Hunter's Bestiary & Monster Treatises System
 * Original Author: UO Exile Team / Imagine (Resource #2674)
 * Source: https://www.servuo.dev/archive/release-hunters-bestiary-monster-treatises-system-193-creatures-1-line-install.2674/
 */

using System;

namespace Server.Custom.ExileHunterBestiary
{
    public class HunterCreatureEntry
    {
        public string Id { get; set; }
        public string DisplayName { get; set; }
        public Type CreatureType { get; set; }
        public BestiaryCategory Category { get; set; }
        public int BookHue { get; set; }
        public double DropChance { get; set; }
        public int IconItemID { get; set; }

        public HunterCreatureEntry(string id, string displayName, Type creatureType, BestiaryCategory category, int bookHue = 0, double dropChance = 0.02, int iconItemId = 0x1F5F)
        {
            Id = id;
            DisplayName = displayName;
            CreatureType = creatureType;
            Category = category;
            BookHue = bookHue;
            DropChance = dropChance;
            IconItemID = iconItemId;
        }
    }
}
