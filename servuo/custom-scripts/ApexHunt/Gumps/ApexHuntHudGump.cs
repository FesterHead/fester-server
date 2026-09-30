/*
 * UO Community Script: Apex Hunt - Automated Server-Wide PvM Hunting Competition
 * Original Author: Imagine (Resource #2678)
 * Source: https://www.servuo.dev/archive/release-apex-hunt-automated-server-wide-pvm-hunting-competition.2678/
 *
 * Floating HUD widget displaying live hunting targets, personal kill progress,
 * event leader, and remaining timer.
 */

using System;
using System.Collections.Generic;
using Server;
using Server.Gumps;
using Server.Mobiles;
using Server.Network;

namespace Server.Custom.ApexHunt
{
    public class ApexHuntHudGump : Gump
    {
        public const int GumpTypeID = 0x41504558; // "APEX" constant TypeID
        public override int GetTypeID() => GumpTypeID;

        public static readonly HashSet<Serial> MinimizedPlayers = new HashSet<Serial>();
        public static readonly HashSet<Serial> ClosedPlayers = new HashSet<Serial>();
        public static readonly Dictionary<Serial, Point2D> SavedPositions = new Dictionary<Serial, Point2D>();

        private readonly PlayerMobile m_Player;
        private readonly bool m_Minimized;

        public bool Minimized => m_Minimized;

        public static void UpdateHud(PlayerMobile pm)
        {
            if (pm == null || pm.NetState == null || pm.Deleted)
                return;

            if (ClosedPlayers.Contains(pm.Serial))
                return;

            ApexHuntHudGump existing = pm.FindGump<ApexHuntHudGump>();
            if (existing == null)
                return;

            int curX = existing.X;
            int curY = existing.Y;
            bool curMin = existing.Minimized;

            SavedPositions[pm.Serial] = new Point2D(curX, curY);

            pm.CloseGump(typeof(ApexHuntHudGump));
            pm.SendGump(new ApexHuntHudGump(pm, curX, curY, curMin));
        }

        public ApexHuntHudGump(PlayerMobile pm, int x = 530, int y = 20, bool? minimized = null) : base(x, y)
        {
            m_Player = pm;

            if (minimized.HasValue)
            {
                m_Minimized = minimized.Value;
                if (m_Minimized)
                    MinimizedPlayers.Add(pm.Serial);
                else
                    MinimizedPlayers.Remove(pm.Serial);
            }
            else
            {
                m_Minimized = MinimizedPlayers.Contains(pm.Serial);
            }

            if (x == 530 && y == 20 && SavedPositions.TryGetValue(pm.Serial, out Point2D pos))
            {
                X = pos.X;
                Y = pos.Y;
            }
            else if (x != 530 || y != 20)
            {
                SavedPositions[pm.Serial] = new Point2D(x, y);
            }

            // Always visible: cannot be accidentally closed
            Closable = false;
            Disposable = false;
            Dragable = true;
            Resizable = false;

            AddPage(0);

            if (!ApexHuntEvent.IsEventActive)
            {
                // Inactive Badge
                AddBackground(0, 0, 180, 36, 9270);
                AddAlphaRegion(5, 5, 170, 26);
                AddHtml(12, 8, 160, 20, "<BASEFONT COLOR=#9E9E9E>Apex Hunt (Idle)</BASEFONT>", false, false);
                return;
            }

            string targetName = ApexHuntEvent.FormatCreatureName(ApexHuntEvent.CurrentTargetCreature?.Name ?? "Monster");
            int targetGoal = ApexHuntEvent.CurrentTargetKillCount;
            int playerKills = ApexHuntEvent.GetPlayerKills(pm.Serial);
            TimeSpan remaining = ApexHuntEvent.RemainingTime;
            string timeStr = remaining.TotalSeconds > 0 
                ? $"{remaining.Minutes:D2}:{remaining.Seconds:D2}" 
                : (ApexHuntEvent.IsEventEnding ? "Tie Breaker" : "Ending...");

            string timeColor = ApexHuntEvent.IsEventEnding ? "#FF4500" : "#FFFFFF";

            if (m_Minimized)
            {
                // Compact Minimized Pill (240 x 32)
                AddBackground(0, 0, 240, 32, 9270);
                AddAlphaRegion(4, 4, 232, 24);

                AddHtml(8, 6, 198, 20, $"<BASEFONT COLOR=#FFD700>Apex:</BASEFONT> <BASEFONT COLOR=#FFFFFF>{targetName}</BASEFONT> <BASEFONT COLOR=#00FFCC>[{playerKills}/{targetGoal}]</BASEFONT> <BASEFONT COLOR={timeColor}>({timeStr})</BASEFONT>", false, false);
                AddButton(212, 6, 2118, 2117, 1, GumpButtonType.Reply, 0); // Maximize [+]
                return;
            }

            // Full HUD Widget (250 x 182)
            AddBackground(0, 0, 250, 182, 9270);
            AddAlphaRegion(6, 6, 238, 170);

            // Title Bar
            AddImageTiled(10, 10, 230, 28, 2624);
            AddAlphaRegion(10, 10, 230, 28);
            AddHtml(16, 14, 195, 20, "<BASEFONT COLOR=#FFD700>Apex Hunt</BASEFONT>", false, false);

            // Title Bar Minimize Button [-]
            AddButton(218, 14, 2117, 2118, 1, GumpButtonType.Reply, 0);

            // Details Card
            AddBackground(10, 42, 230, 96, 9350);
            AddAlphaRegion(12, 44, 226, 92);

            AddHtml(16, 46, 220, 18, $"<BASEFONT COLOR=#00FFCC>Target: </BASEFONT><BASEFONT COLOR=#FFFFFF>{targetName}</BASEFONT>", false, false);
            AddHtml(16, 66, 220, 18, $"<BASEFONT COLOR=#FFD700>My Kills: </BASEFONT><BASEFONT COLOR=#FFE57F>{playerKills} / {targetGoal}</BASEFONT>", false, false);
            AddHtml(16, 86, 220, 18, $"<BASEFONT COLOR=#80D8FF>Time Left: </BASEFONT><BASEFONT COLOR={timeColor}>{timeStr}</BASEFONT>", false, false);

            // Current 1st Place info
            var leader = ApexHuntEvent.GetLeader();
            string leaderText = leader.Key != Server.Serial.Zero 
                ? $"{ApexHuntEvent.GetMobileName(leader.Key)} ({leader.Value})" 
                : "None yet";
            AddHtml(16, 106, 220, 18, $"<BASEFONT COLOR=#FFB300>Leader: </BASEFONT><BASEFONT COLOR=#FFFFFF>{leaderText}</BASEFONT>", false, false);

            // Bottom Buttons: Ranking (2) and Minimize (1)
            AddButton(12, 146, 4005, 4007, 2, GumpButtonType.Reply, 0);
            AddHtml(48, 148, 80, 20, "<BASEFONT COLOR=#00FFCC>Ranking</BASEFONT>", false, false);

            AddButton(135, 146, 4005, 4007, 1, GumpButtonType.Reply, 0);
            AddHtml(170, 148, 75, 20, "<BASEFONT COLOR=#FFD700>Minimize</BASEFONT>", false, false);
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            Mobile m = sender.Mobile;
            if (m == null || m.Deleted || !(m is PlayerMobile pm))
                return;

            SavedPositions[pm.Serial] = new Point2D(X, Y);

            if (info.ButtonID == 1) // Toggle Minimize / Maximize
            {
                bool newMin = !m_Minimized;
                if (newMin)
                    MinimizedPlayers.Add(pm.Serial);
                else
                    MinimizedPlayers.Remove(pm.Serial);

                pm.CloseGump(typeof(ApexHuntHudGump));
                pm.SendGump(new ApexHuntHudGump(pm, X, Y, newMin));
            }
            else if (info.ButtonID == 2) // Open Leaderboard
            {
                pm.CloseGump(typeof(ApexHuntLeaderboardGump));
                pm.SendGump(new ApexHuntLeaderboardGump(pm));
            }
        }
    }
}
