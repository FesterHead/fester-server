using System;
using System.Collections.Generic;
using System.Linq;
using Server;
using Server.Commands;
using Server.Items;

namespace Server.Custom
{
    public class CorpseArrow : QuestArrow
    {
        public Corpse Corpse { get; }

        public CorpseArrow(Mobile m, Corpse c)
            : base(m, c.Location, c.X, c.Y)
        {
            Corpse = c;
        }

        public override void OnClick(bool rightClick)
        {
            Stop();
            Mobile.SendMessage(68, "Corpse tracking arrow dismissed.");
        }
    }

    public class CorpseFinder
    {
        private static readonly Dictionary<Mobile, CorpseArrow> m_ActiveArrows = new Dictionary<Mobile, CorpseArrow>();

        public static void Initialize()
        {
            CommandSystem.Register("Corpse", AccessLevel.Player, new CommandEventHandler(Corpse_OnCommand));
        }

        [Usage("Corpse")]
        [Description("Locates your most recent active corpse and points a directional arrow toward it.")]
        private static void Corpse_OnCommand(CommandEventArgs e)
        {
            Mobile from = e.Mobile;
            if (from == null)
                return;

            if (m_ActiveArrows.TryGetValue(from, out CorpseArrow oldArrow))
            {
                oldArrow.Stop();
                m_ActiveArrows.Remove(from);
            }

            Corpse latestCorpse = World.Items.Values
                .OfType<Corpse>()
                .Where(c => c.Owner == from && !c.Deleted && c.Map != null && c.Map != Map.Internal)
                .OrderByDescending(c => c.TimeOfDeath)
                .FirstOrDefault();

            if (latestCorpse == null)
            {
                from.SendMessage(68, "You do not have an active corpse in the world.");
                return;
            }

            if (from.Map != latestCorpse.Map)
            {
                string facetName = latestCorpse.Map != null ? latestCorpse.Map.Name : "Unknown";
                from.SendMessage(38, $"Your corpse was located on the facet of {facetName} at ({latestCorpse.X}, {latestCorpse.Y}, {latestCorpse.Z}). Travel to {facetName} to track it.");
                return;
            }

            int dist = (int)from.GetDistanceToSqrt(latestCorpse.Location);
            CorpseArrow arrow = new CorpseArrow(from, latestCorpse);
            m_ActiveArrows[from] = arrow;

            from.SendMessage(68, $"Your corpse was located {dist} paces away at ({latestCorpse.X}, {latestCorpse.Y}, {latestCorpse.Z}). A directional arrow is pointing the way (click or right-click arrow to dismiss).");
        }
    }
}
