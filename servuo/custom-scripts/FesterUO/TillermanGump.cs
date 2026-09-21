/************************************************************************************
 * Script: TillermanGump.cs                                                         *
 * Origin: FesterUO Custom Scripts (servuo/custom-scripts/FesterUO/)                *
 * Purpose: Compact, intelligently organized boat navigation control gump.         *
 *                                                                                  *
 * Command: [tillerman                                                               *
 * Features:                                                                        *
 * - Continuous Sail (Forward, Left, Right, Back)                                   *
 * - One Tile Nudge (Forward One, Left One, Right One, Back One)                    *
 * - Directional Maneuver (Turn Left, Turn Right, Turn Around)                      *
 * - Emergency Halt (Stop) with prominent alert styling                             *
 * - Seamless re-display upon button press for uninterrupted navigation             *
 * - Validation checks for vessel boarding, dead/scuttled states, and command auth  *
 ************************************************************************************/

using System;
using Server;
using Server.Commands;
using Server.Gumps;
using Server.Multis;
using Server.Network;

namespace Server.Custom
{
    public class TillermanGump : Gump
    {
        private readonly BaseBoat m_Boat;

        public static void Initialize()
        {
            CommandSystem.Register("tillerman", AccessLevel.Player, new CommandEventHandler(Tillerman_OnCommand));
        }

        [Usage("tillerman")]
        [Description("Opens a compact control gump for commanding the ship's tillerman.")]
        public static void Tillerman_OnCommand(CommandEventArgs e)
        {
            Mobile from = e.Mobile;

            if (from == null || from.Deleted)
                return;

            if (!from.Alive)
            {
                from.SendMessage(38, "You cannot command the tillerman while dead.");
                return;
            }

            BaseBoat boat = BaseBoat.FindBoatAt(from, from.Map);
            if (boat == null || !boat.Contains(from))
            {
                from.SendMessage(38, "You must be aboard a ship to command the tillerman.");
                return;
            }

            if (!boat.CanCommand(from))
            {
                from.SendMessage(38, "You do not have permission to command this ship.");
                return;
            }

            if (boat.Scuttled)
            {
                from.SendMessage(38, "This ship is too damaged to sail.");
                return;
            }

            from.SendGump(new TillermanGump(from, boat));
        }

        public enum Buttons
        {
            Close = 0,
            Forward = 1,
            Back = 2,
            Right = 3,
            Left = 4,
            ForwardOne = 5,
            BackOne = 6,
            RightOne = 7,
            LeftOne = 8,
            Stop = 9,
            TurnAround = 10,
            TurnLeft = 11,
            TurnRight = 12
        }

        public TillermanGump(Mobile from, BaseBoat boat) : base(100, 100)
        {
            m_Boat = boat;

            from.CloseGump(typeof(TillermanGump));

            Closable = true;
            Disposable = true;
            Dragable = true;
            Resizable = false;

            AddPage(0);

            // Compact 260x225 dark aesthetic layout
            AddBackground(0, 0, 260, 225, 9200);
            AddAlphaRegion(8, 8, 244, 209);

            // Title
            string title = boat != null && !string.IsNullOrEmpty(boat.ShipName) ? boat.ShipName : "TILLERMAN";
            AddHtml(0, 10, 260, 20, string.Format("<basefont color=#FFD700><center><b>{0}</b></center></basefont>", title), false, false);
            AddImageTiled(14, 30, 232, 1, 0x2711);

            // Column Headers
            AddHtml(14, 34, 110, 18, "<basefont color=#87CEEB><center><b>CONTINUOUS</b></center></basefont>", false, false);
            AddHtml(136, 34, 110, 18, "<basefont color=#87CEEB><center><b>ONE TILE</b></center></basefont>", false, false);

            // Row 1: Forward / Forward One
            AddButton(18, 54, 4005, 4007, (int)Buttons.Forward, GumpButtonType.Reply, 0);
            AddLabel(50, 54, 1152, "Forward");
            AddButton(138, 54, 4005, 4007, (int)Buttons.ForwardOne, GumpButtonType.Reply, 0);
            AddLabel(170, 54, 1152, "Forward One");

            // Row 2: Left / Left One
            AddButton(18, 78, 4005, 4007, (int)Buttons.Left, GumpButtonType.Reply, 0);
            AddLabel(50, 78, 1152, "Left");
            AddButton(138, 78, 4005, 4007, (int)Buttons.LeftOne, GumpButtonType.Reply, 0);
            AddLabel(170, 78, 1152, "Left One");

            // Row 3: Right / Right One
            AddButton(18, 102, 4005, 4007, (int)Buttons.Right, GumpButtonType.Reply, 0);
            AddLabel(50, 102, 1152, "Right");
            AddButton(138, 102, 4005, 4007, (int)Buttons.RightOne, GumpButtonType.Reply, 0);
            AddLabel(170, 102, 1152, "Right One");

            // Row 4: Back / Back One
            AddButton(18, 126, 4005, 4007, (int)Buttons.Back, GumpButtonType.Reply, 0);
            AddLabel(50, 126, 1152, "Back");
            AddButton(138, 126, 4005, 4007, (int)Buttons.BackOne, GumpButtonType.Reply, 0);
            AddLabel(170, 126, 1152, "Back One");

            // Separator
            AddImageTiled(14, 150, 232, 1, 0x2711);

            // Row 5: Turn Left / Turn Right
            AddButton(18, 156, 4005, 4007, (int)Buttons.TurnLeft, GumpButtonType.Reply, 0);
            AddLabel(50, 156, 1152, "Turn Left");
            AddButton(138, 156, 4005, 4007, (int)Buttons.TurnRight, GumpButtonType.Reply, 0);
            AddLabel(170, 156, 1152, "Turn Right");

            // Row 6: Turn Around / Stop
            AddButton(18, 180, 4005, 4007, (int)Buttons.TurnAround, GumpButtonType.Reply, 0);
            AddLabel(50, 180, 1152, "Turn Around");
            AddButton(138, 180, 4005, 4007, (int)Buttons.Stop, GumpButtonType.Reply, 0);
            AddLabel(170, 180, 38, "Stop"); // Red text (hue 38) for quick emergency recognition

            // Footer
            AddHtml(0, 204, 260, 16, "<basefont color=#808080><center><small>Right-click to close</small></center></basefont>", false, false);
        }

        public override void OnResponse(NetState state, RelayInfo info)
        {
            Mobile from = state.Mobile;

            if (from == null || from.Deleted || !from.Alive)
                return;

            if (info.ButtonID == (int)Buttons.Close || info.ButtonID == 0)
                return;

            BaseBoat boat = m_Boat;
            if (boat == null || boat.Deleted || !boat.Contains(from))
            {
                from.SendMessage(38, "You must be aboard your ship to command the tillerman.");
                return;
            }

            if (!boat.CanCommand(from))
            {
                from.SendMessage(38, "You do not have permission to command this ship.");
                return;
            }

            if (boat.Scuttled)
            {
                from.SendMessage(38, "This ship is too damaged to sail.");
                return;
            }

            switch ((Buttons)info.ButtonID)
            {
                case Buttons.Forward:
                    boat.StartMove(Direction.North, true);
                    break;
                case Buttons.Back:
                    boat.StartMove(Direction.South, true);
                    break;
                case Buttons.Left:
                    boat.StartMove(Direction.West, true);
                    break;
                case Buttons.Right:
                    boat.StartMove(Direction.East, true);
                    break;
                case Buttons.ForwardOne:
                    boat.OneMove(Direction.North);
                    break;
                case Buttons.BackOne:
                    boat.OneMove(Direction.South);
                    break;
                case Buttons.LeftOne:
                    boat.OneMove(Direction.West);
                    break;
                case Buttons.RightOne:
                    boat.OneMove(Direction.East);
                    break;
                case Buttons.TurnLeft:
                    boat.StartTurn(-2, true);
                    break;
                case Buttons.TurnRight:
                    boat.StartTurn(2, true);
                    break;
                case Buttons.TurnAround:
                    boat.StartTurn(-4, true);
                    break;
                case Buttons.Stop:
                    boat.StopMove(true);
                    break;
            }

            // Keep the gump open for uninterrupted sailing
            from.SendGump(new TillermanGump(from, boat));
        }
    }
}
