using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Eleon.Modding;

namespace Mailbox
{
    class API
    {
        public static void Alert(int Target, string Message, string Alert, float Time)
        {
            byte prio = 2;
            if (Alert.ToLower() == "red")
            {
                prio = 0;
            }
            else if (Alert.ToLower() == "yellow")
            {
                prio = 1;
            }
            else
            {
                prio = 2;
            }

            if (Target == 0)
            {
                Storage.GameAPI.Game_Request(CmdId.Request_InGameMessage_AllPlayers, (ushort)Storage.CurrentSeqNr, new IdMsgPrio(Target, Message, prio, Time));
            }
            else if (Target < 999)
            {
                Storage.GameAPI.Game_Request(CmdId.Request_InGameMessage_Faction, (ushort)Storage.CurrentSeqNr, new IdMsgPrio(Target, Message, prio, Time));
            }
            else if (Target > 999)
            {
                Storage.GameAPI.Game_Request(CmdId.Request_InGameMessage_SinglePlayer, (ushort)Storage.CurrentSeqNr, new IdMsgPrio(Target, Message, prio, Time));
            }
        }

        public static int PlayerInfo(int playerID)
        {
            Storage.CurrentSeqNr = CommonFunctions.SeqNrGenerator(Storage.CurrentSeqNr);
            Storage.GameAPI.Game_Request(CmdId.Request_Player_Info, (ushort)Storage.CurrentSeqNr, new Id(playerID));
            return Storage.CurrentSeqNr;
        }

        public static int TextWindowOpen(string TargetPlayer, string Message, String ConfirmText, String CancelText)
        {
            Storage.CurrentSeqNr = CommonFunctions.SeqNrGenerator(Storage.CurrentSeqNr);
            if (CancelText == null)
            {
                Storage.GameAPI.Game_Request(CmdId.Request_ShowDialog_SinglePlayer, (ushort)Storage.CurrentSeqNr, new DialogBoxData()
                {
                    Id = Convert.ToInt32(TargetPlayer),
                    MsgText = Message,
                    NegButtonText = "Close"
                });
            }
            else
            {
                Storage.GameAPI.Game_Request(CmdId.Request_ShowDialog_SinglePlayer, (ushort)Storage.CurrentSeqNr, new DialogBoxData()
                {
                    Id = Convert.ToInt32(TargetPlayer),
                    MsgText = Message,
                    NegButtonText = "Close",
                    PosButtonText = "Save Waypoints"
                });
            }
            return Storage.CurrentSeqNr;
        }

        public static int Gents(string playfield)
        {
            Storage.CurrentSeqNr = CommonFunctions.SeqNrGenerator(Storage.CurrentSeqNr);
            Storage.GameAPI.Game_Request(CmdId.Request_GlobalStructure_Update, (ushort)Storage.CurrentSeqNr, new PString(playfield));
            return Storage.CurrentSeqNr;
        }

        public static void ConsoleCommand(String Sendable)
        {
            Storage.GameAPI.Game_Request(CmdId.Request_ConsoleCommand, (ushort)Storage.CurrentSeqNr, new PString(Sendable));
        }

        public static int Blocks(string Entity)
        {
            Storage.CurrentSeqNr = CommonFunctions.SeqNrGenerator(Storage.CurrentSeqNr);
            Storage.GameAPI.Game_Request(CmdId.Request_Structure_BlockStatistics, (ushort)Storage.CurrentSeqNr, new Id(Convert.ToInt32(Entity)));
            return Storage.CurrentSeqNr;
        }

        public static void Destroy(string Entity)
        {
            try
            {
                Storage.GameAPI.Game_Request(CmdId.Request_Entity_Destroy, (ushort)Storage.CurrentSeqNr, new PString(Entity));
            }
            catch { };
            try
            {
                Storage.GameAPI.Game_Request(CmdId.Request_Entity_Destroy2, (ushort)Storage.CurrentSeqNr, new PString(Entity));
            }
            catch { };
        }

        public static int ItemExchange(int Player, string Title, string Body, string Button, ItemStack[] Items)
        {
            Storage.CurrentSeqNr = CommonFunctions.SeqNrGenerator(Storage.CurrentSeqNr);
            Storage.GameAPI.Game_Request(CmdId.Request_Player_ItemExchange, (ushort)Storage.CurrentSeqNr, new ItemExchangeInfo(Player, Title, Body, Button, Items));
            return Storage.CurrentSeqNr;
        }

        public static int CreditChange(int PlayerID, int Credits)
        {
            Storage.CurrentSeqNr = CommonFunctions.SeqNrGenerator(Storage.CurrentSeqNr);
            Storage.GameAPI.Game_Request(CmdId.Request_Player_SetCredits , (ushort)Storage.CurrentSeqNr, new IdCredits(PlayerID, Credits));
            return Storage.CurrentSeqNr;

        }

        public static void Marker(int ClientID, string Name, int x, int y, int z, bool Waypoint, int Timer, bool Destroy)
        {
            string command = command = "remoteex cl=" + ClientID + " \'marker add name=" + Name + " pos=" + x + "," + y + "," + z;
            if (Waypoint)
            {
                command = command + " W";
            }

            if ( Timer > 0 )
            {
                command = command + " expire=" + Timer;
            }
            else if (Destroy)
            {
                command = command + " WD";
            }

            //command = "remoteex cl=" + Player + " \'marker add name=" + Name + " pos=" + x + "," + y + "," + z;
            command = command + "\'";
            CommonFunctions.LogFile("debug.txt", command);
            API.ConsoleCommand(command);
        }
    }
}
