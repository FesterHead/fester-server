using System;
using Server;
using Server.Commands;

namespace Server.Custom
{
    public static class RemoteBank
    {
        public static void Initialize()
        {
            // Registers command for all players
            CommandSystem.Register("rbank", AccessLevel.Player, new CommandEventHandler(RemoteBank_OnCommand));
        }

        [Usage("rbank")]
        [Description("Opens your bank box remotely regardless of location.")]
        public static void RemoteBank_OnCommand(CommandEventArgs e)
        {
            Mobile from = e.Mobile;

            if (from == null || from.Deleted)
                return;

            if (!from.Alive)
            {
                from.SendMessage(38, "You cannot access your bank while dead.");
                return;
            }

            if (from.Criminal)
            {
                from.SendMessage(38, "You are a criminal and cannot access your bank.");
                return;
            }

            if (from.BankBox == null)
                return;

            from.BankBox.Open();
        }
    }
}
