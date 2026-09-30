/*
 * UO Community Script: Apex Hunt - Automated Server-Wide PvM Hunting Competition
 * Original Author: Imagine (Resource #2678)
 * Source: https://www.servuo.dev/archive/release-apex-hunt-automated-server-wide-pvm-hunting-competition.2678/
 *
 * Full Leaderboard gump showing event status, target details, remaining time,
 * prize tiers, and top 10 hunters.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using Server;
using Server.Gumps;
using Server.Mobiles;
using Server.Network;

namespace Server.Custom.ApexHunt
{
    public class ApexHuntLeaderboardGump : Gump
    {
        public const int GumpTypeID = 0x41504559; // "APEY" constant TypeID
        public override int GetTypeID() => GumpTypeID;

        public static readonly Dictionary<Serial, Point2D> SavedPositions = new Dictionary<Serial, Point2D>();

        private readonly PlayerMobile m_Player;

        public static void UpdateLeaderboard(PlayerMobile pm)
        {
            if (pm == null || pm.NetState == null || pm.Deleted)
                return;

            ApexHuntLeaderboardGump existing = pm.FindGump<ApexHuntLeaderboardGump>();
            if (existing == null)
                return;

            int curX = existing.X;
            int curY = existing.Y;
            SavedPositions[pm.Serial] = new Point2D(curX, curY);

            pm.CloseGump(typeof(ApexHuntLeaderboardGump));
            pm.SendGump(new ApexHuntLeaderboardGump(pm, curX, curY));
        }

        public ApexHuntLeaderboardGump(PlayerMobile pm, int x = 120, int y = 80) : base(x, y)
        {
            m_Player = pm;

            if (x == 120 && y == 80 && SavedPositions.TryGetValue(pm.Serial, out Point2D pos))
            {
                X = pos.X;
                Y = pos.Y;
            }
            else if (x != 120 || y != 80)
            {
                SavedPositions[pm.Serial] = new Point2D(x, y);
            }

            Closable = true;
            Disposable = true;
            Dragable = true;
            Resizable = false;

            AddPage(0);

            const int width = 480;
            const int height = 480;

            // Outer Frame
            AddBackground(0, 0, width, height, 9270);
            AddAlphaRegion(8, 8, width - 16, height - 16);

            // Title Banner
            AddImageTiled(12, 12, width - 24, 32, 2624);
            AddAlphaRegion(12, 12, width - 24, 32);
            AddHtml(20, 16, 320, 24, "<BASEFONT COLOR=#FFD700>Apex Hunt</BASEFONT>", false, false);

            string stateTag = ApexHuntEvent.IsEventActive 
                ? (ApexHuntEvent.IsEventEnding ? "<BASEFONT COLOR=#FF4500>[TIE BREAKER]</BASEFONT>" : "<BASEFONT COLOR=#00FF52>[HUNT ACTIVE]</BASEFONT>")
                : "<BASEFONT COLOR=#9E9E9E>[HUNT IDLE]</BASEFONT>";
            AddHtml(350, 16, 110, 24, $"<RIGHT>{stateTag}</RIGHT>", false, false);

            AddImageTiled(12, 48, width - 24, 1, 96);

            // Event Overview Box
            AddBackground(12, 54, width - 24, 76, 9350);
            AddAlphaRegion(14, 56, width - 28, 72);

            if (ApexHuntEvent.IsEventActive)
            {
                string targetName = ApexHuntEvent.FormatCreatureName(ApexHuntEvent.CurrentTargetCreature?.Name ?? "Monster");
                int targetGoal = ApexHuntEvent.CurrentTargetKillCount;
                int playerKills = ApexHuntEvent.GetPlayerKills(pm.Serial);
                TimeSpan remaining = ApexHuntEvent.RemainingTime;
                string timeStr = $"{remaining.Minutes:D2}m {remaining.Seconds:D2}s";

                AddHtml(20, 58, 220, 18, $"<BASEFONT COLOR=#00FFCC>Target Beast:</BASEFONT> <BASEFONT COLOR=#FFFFFF>{targetName}</BASEFONT>", false, false);
                AddHtml(250, 58, 210, 18, $"<BASEFONT COLOR=#FFD700>Apex Goal:</BASEFONT> <BASEFONT COLOR=#FFFFFF>{targetGoal} Kills</BASEFONT>", false, false);

                AddHtml(20, 80, 220, 18, $"<BASEFONT COLOR=#80D8FF>Time Remaining:</BASEFONT> <BASEFONT COLOR=#FFFFFF>{timeStr}</BASEFONT>", false, false);
                AddHtml(250, 80, 210, 18, $"<BASEFONT COLOR=#FFE57F>Your Kills:</BASEFONT> <BASEFONT COLOR=#00FF52>{playerKills} / {targetGoal}</BASEFONT>", false, false);

                AddHtml(20, 104, 430, 18, "<BASEFONT COLOR=#B0BEC5 SIZE=2>Eligible Maps: Trammel, Ilshenar, Malas, Tokuno (Wild Only)</BASEFONT>", false, false);
            }
            else
            {
                if (ApexHuntEvent.IsEventScheduled)
                {
                    TimeSpan startsIn = ApexHuntEvent.NextEventTime - DateTime.UtcNow;
                    string startsStr = startsIn.TotalSeconds > 0 
                        ? $"{startsIn.Minutes:D2}m {startsIn.Seconds:D2}s" 
                        : "Starting now...";
                    AddHtml(20, 68, 430, 20, $"<BASEFONT COLOR=#00FFCC SIZE=3>Next Hunt Commencing: In {startsStr}</BASEFONT>", false, false);
                    AddHtml(20, 94, 430, 18, "<BASEFONT COLOR=#FFE57F>Prepare your weapons and gear! Hunt details will be revealed at launch.</BASEFONT>", false, false);
                }
                else
                {
                    TimeSpan nextCheck = ApexHuntEvent.NextCheckTime - DateTime.UtcNow;
                    string checkStr = nextCheck.TotalSeconds > 0 
                        ? $"{nextCheck.Hours}h {nextCheck.Minutes}m {nextCheck.Seconds}s" 
                        : "Checking shortly...";
                    AddHtml(20, 68, 430, 20, $"<BASEFONT COLOR=#FFE57F SIZE=3>Next Trigger Check: In {checkStr}</BASEFONT>", false, false);
                    AddHtml(20, 94, 430, 18, "<BASEFONT COLOR=#9E9E9E>Hunts trigger dynamically when hunters are online. Win pure Gold bounties!</BASEFONT>", false, false);
                }
            }

            // Rewards Summary Box
            AddBackground(12, 136, width - 24, 46, 9350);
            AddAlphaRegion(14, 138, width - 28, 42);

            AddHtml(20, 142, 440, 16, "<BASEFONT COLOR=#FFD700 SIZE=2>HUNT BOUNTIES (Direct to Bank):</BASEFONT>", false, false);
            AddHtml(20, 160, 440, 16, 
                $"<BASEFONT COLOR=#FFE57F SIZE=1>1st: {ApexHuntConfig.RewardGoldFirstPlace:#,##0}gp  |  2nd: {ApexHuntConfig.RewardGoldSecondPlace:#,##0}gp  |  3rd: {ApexHuntConfig.RewardGoldThirdPlace:#,##0}gp  |  Part: {ApexHuntConfig.RewardGoldParticipation:#,##0}gp</BASEFONT>", false, false);

            // Leaderboard Table Box
            AddBackground(12, 188, width - 24, 236, 9350);
            AddAlphaRegion(14, 190, width - 28, 232);

            // Table Header
            AddHtml(22, 194, 50, 18, "<BASEFONT COLOR=#FFD700>Rank</BASEFONT>", false, false);
            AddHtml(80, 194, 230, 18, "<BASEFONT COLOR=#FFD700>Hunter</BASEFONT>", false, false);
            AddHtml(320, 194, 70, 18, "<BASEFONT COLOR=#FFD700><RIGHT>Kills</RIGHT></BASEFONT>", false, false);
            AddHtml(400, 194, 50, 18, "<BASEFONT COLOR=#FFD700><RIGHT>Goal %</RIGHT></BASEFONT>", false, false);
            AddImageTiled(18, 214, width - 36, 1, 96);

            var topList = ApexHuntEvent.GetTopPlayers(10);
            int startY = 220;
            int goal = Math.Max(1, ApexHuntEvent.CurrentTargetKillCount);

            if (topList.Count == 0)
            {
                AddHtml(20, 270, width - 40, 24, "<CENTER><BASEFONT COLOR=#9E9E9E SIZE=3>No hunters have claimed kills yet for this hunt!</BASEFONT></CENTER>", false, false);
            }
            else
            {
                for (int i = 0; i < topList.Count; i++)
                {
                    int rowY = startY + (i * 20);
                    var entry = topList[i];
                    string hunterName = ApexHuntEvent.GetMobileName(entry.Key);
                    int kills = entry.Value;
                    int pct = Math.Min(100, (int)((double)kills / goal * 100.0));

                    string medal;
                    string color;
                    switch (i)
                    {
                        case 0:
                            medal = "#1";
                            color = "#FFD700";
                            break;
                        case 1:
                            medal = "#2";
                            color = "#E0E0E0";
                            break;
                        case 2:
                            medal = "#3";
                            color = "#CD7F32";
                            break;
                        default:
                            medal = $"#{i + 1}";
                            color = "#80D8FF";
                            break;
                    }

                    // Highlight viewing player
                    bool isSelf = entry.Key == pm.Serial;
                    if (isSelf)
                        color = "#00FF52";

                    AddHtml(22, rowY, 55, 18, $"<BASEFONT COLOR={color}>{medal}</BASEFONT>", false, false);
                    AddHtml(80, rowY, 230, 18, $"<BASEFONT COLOR={color}>{hunterName}{(isSelf ? " (You)" : "")}</BASEFONT>", false, false);
                    AddHtml(320, rowY, 70, 18, $"<BASEFONT COLOR={color}><RIGHT>{kills:#,##0}</RIGHT></BASEFONT>", false, false);
                    AddHtml(400, rowY, 50, 18, $"<BASEFONT COLOR={color}><RIGHT>{pct}%</RIGHT></BASEFONT>", false, false);
                }
            }

            // Bottom Buttons
            AddButton(16, 434, 4005, 4007, 1, GumpButtonType.Reply, 0); // Refresh
            AddHtml(50, 436, 80, 20, "<BASEFONT COLOR=#00FFCC>Refresh</BASEFONT>", false, false);

            AddButton(360, 434, 4011, 4013, 2, GumpButtonType.Reply, 0); // Back to HUD
            AddHtml(394, 436, 90, 20, "<BASEFONT COLOR=#FFD700>Back to HUD</BASEFONT>", false, false);
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            Mobile m = sender.Mobile;
            if (m == null || m.Deleted || !(m is PlayerMobile pm))
                return;

            SavedPositions[pm.Serial] = new Point2D(X, Y);

            if (info.ButtonID == 1) // Refresh
            {
                UpdateLeaderboard(pm);
            }
            else if (info.ButtonID == 2 || info.ButtonID == 0) // Back to HUD or Right-click
            {
                pm.CloseGump(typeof(ApexHuntLeaderboardGump));
                ApexHuntHudGump.ClosedPlayers.Remove(pm.Serial);
                pm.CloseGump(typeof(ApexHuntHudGump));
                pm.SendGump(new ApexHuntHudGump(pm));
            }
        }
    }
}
