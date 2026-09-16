using System;
using Server;
using Server.Commands;
using Server.Network;

namespace Server.Custom
{
    public static class GlobalChat
    {
        public const int ChatHue = 0x482; // Crisp cyan / light blue

        public static void Initialize()
        {
            CommandSystem.Register("c", AccessLevel.Player, new CommandEventHandler(Chat_OnCommand));
            CommandSystem.Register("chat", AccessLevel.Player, new CommandEventHandler(Chat_OnCommand));
        }

        [Usage("c <message>")]
        [Aliases("chat")]
        [Description("Broadcasts a global message to all online players.")]
        public static void Chat_OnCommand(CommandEventArgs e)
        {
            Mobile from = e.Mobile;

            if (from == null)
                return;

            string message = e.ArgString != null ? e.ArgString.Trim() : string.Empty;

            if (string.IsNullOrEmpty(message))
            {
                from.SendMessage(0x35, "Usage: [c <message>");
                return;
            }

            string formatted = string.Format("[Chat] {0}: {1}", from.Name, message);

            CommandHandlers.BroadcastMessage(AccessLevel.Player, ChatHue, formatted);
        }
    }
}
