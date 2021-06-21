using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Eleon.Modding;
//using ProtoBuf;
using YamlDotNet.Serialization;


namespace Mailbox
{
    public class MyEmpyrionMod : ModInterface
    {
        public static string ModVersion = "Mailbox v2.0.10";
        public static string ModPath = "..\\Content\\Mods\\Mailbox\\";
        internal static bool debug = true;

        internal static Dictionary<int, Storage.StorableData> SeqNrStorage = new Dictionary<int, Storage.StorableData> { };
        public int thisSeqNr = 2000;
        private Setup.Root SetupYaml = new Setup.Root { };
        public int Ticker = 0;
        public Dictionary<string, int> LoadedPlayfields = new Dictionary<string, int> { };
        internal static Dictionary<string, PlayerYamlDB.PlayerData> PlayersDB = new Dictionary<string, PlayerYamlDB.PlayerData> { };
        internal static Dictionary<string, PlayerYamlDB.PlayerData> PlayersDBSteam = new Dictionary<string, PlayerYamlDB.PlayerData> { };
        internal PlayerYamlDB.Root PlayerYaml = new PlayerYamlDB.Root { };
        public ItemStack[] blankItemStack = new ItemStack[] { };
        internal Dictionary<int, Storage.MLM> MultilineData = new Dictionary<int, Storage.MLM> { };
        public int VoidOpen = 0;
        int UpdateCounter = 0;
        Dictionary<int, List<string>> UpdateDictionary = new Dictionary<int, List<string>> { };
        int LogonDelay = 0;
        Dictionary<int, int> LogonDelayDict = new Dictionary<int, int> { };

        int UsageReadItem = 0;
        int UsageReadMLM = 0;
        int UsageSendItem = 0;
        int UsageSendMLM = 0;
        int UsageSendLoc = 0;


        //########################################################################################################################################################
        //################################################ This is where the actual Empyrion Modding API stuff Begins ############################################
        //########################################################################################################################################################
        public void Game_Start(ModGameAPI gameAPI)
        {
            Storage.GameAPI = gameAPI;
            File.WriteAllText(ModPath + "debug.txt", ""); //Blanks the debug.txt file on server start. This is where I dump all my debuging text using CommonFunctions.LogFile
            SetupYaml = Setup.Retrieve(ModPath + "Setup.yaml"); //reads setup.yaml
            //Setup.WriteYaml(ModPath + "Setup1.yaml", SetupYaml);
            PlayerYaml = PlayerYamlDB.Read(ModPath + "PlayersDB.yaml");
            //PlayerYamlDB.Write(ModPath + "PlayersDB1.yaml", PlayerYaml);
            CommonFunctions.LogFile("debug.txt", "Player Database Count = " + PlayerYaml.Database.Count());
            try
            {
                foreach (PlayerYamlDB.PlayerData player in PlayerYaml.Database)
                {
                    //CommonFunctions.LogFile("debug.txt", "PlayerID = " + player.EmpyrionID);
                    PlayersDB.Add(player.EmpyrionID, player);
                    PlayersDBSteam.Add(player.SteamID, player);
                }
            }
            catch
            {
                CommonFunctions.LogFile("debug.txt", "failed Populating Player Database");
            }
            CommonFunctions.LogFile("debug.txt", "Modpath = " + ModPath);
        }

        public void Game_Event(CmdId cmdId, ushort seqNr, object data)
        {
            try
            {
                switch (cmdId)
                {
                    case CmdId.Event_ChatMessage:
                        //Triggered when player says something in-game
                        ChatInfo Received_ChatInfo = (ChatInfo)data;
                        //Send Items
                        if (Received_ChatInfo.msg.ToLower().StartsWith(SetupYaml.General.DefaultPrefix + SetupYaml.SendItems.Command))
                        {
                            Storage.StorableData function = new Storage.StorableData
                            {
                                function = "Items",
                                Match = Convert.ToString(Received_ChatInfo.playerId),
                                Requested = "PlayerInfo",
                                ChatInfo = Received_ChatInfo
                            };
                            thisSeqNr = API.PlayerInfo(Received_ChatInfo.playerId);
                            SeqNrStorage[thisSeqNr] = function;
                        }

                        //MultiLineMessage
                        else if (Received_ChatInfo.msg.ToLower().StartsWith(SetupYaml.General.DefaultPrefix + SetupYaml.MultiLineMessage.Command))
                        {
                            if (Received_ChatInfo.msg.ToLower().StartsWith("s! " + SetupYaml.General.ReinitializeCommand))
                            {
                                SetupYaml = Setup.Retrieve(ModPath + "Setup.yaml");
                            }
                            if (Received_ChatInfo.msg.ToLower().StartsWith(SetupYaml.General.DefaultPrefix + SetupYaml.MultiLineMessage.Command + SetupYaml.MultiLineMessage.New))
                            {
                                Storage.ChatData chatdata = CommonFunctions.SplitChat(Received_ChatInfo.msg);
                                if (chatdata.Player.Count() == 1)
                                {
                                    List<string> RecipientsList = new List<string> { };
                                    RecipientsList.Add(chatdata.Player[0].SteamID);
                                    List<string> LinesList = new List<string> { };
                                    List<Setup.LocData> LocList = new List<Setup.LocData> { };
                                    Storage.MLM NewMLM = new Storage.MLM
                                    {
                                        Recipient = RecipientsList,
                                        Lines = LinesList,
                                        Location = LocList
                                    };
                                    MultilineData[Received_ChatInfo.playerId] = NewMLM;
                                    API.Alert(Received_ChatInfo.playerId, "New Message Recipient: " + chatdata.Player[0].PlayerName, "Blue", 2);
                                }
                                else if (chatdata.Player.Count() > 1)
                                {
                                    API.Alert(Received_ChatInfo.playerId, "Error, Name Fragment Not Unique", "Yellow", 5);
                                }
                                else if (chatdata.Player.Count() == 0)
                                {
                                    API.Alert(Received_ChatInfo.playerId, "Error, Name Fragment Not Found", "Yellow", 5);
                                }
                            }
                            else if (Received_ChatInfo.msg.ToLower().StartsWith(SetupYaml.General.DefaultPrefix + SetupYaml.MultiLineMessage.Command + SetupYaml.MultiLineMessage.AddRecipient))
                            {
                                Storage.MLM EditMLM = MultilineData[Received_ChatInfo.playerId];
                                List<string> RecipientsList = EditMLM.Recipient;
                                Storage.ChatData chatdata = CommonFunctions.SplitChat(Received_ChatInfo.msg);
                                if (chatdata.Player.Count() == 1)
                                {
                                    RecipientsList.Add(chatdata.Player[0].SteamID);
                                    MultilineData[Received_ChatInfo.playerId] = EditMLM;
                                    API.Alert(Received_ChatInfo.playerId, "New Message Recipient: " + chatdata.Player[0].PlayerName, "Blue", 2);
                                }
                                else if (chatdata.Player.Count() > 1)
                                {
                                    API.Alert(Received_ChatInfo.playerId, "Error, Name Fragment Not Unique", "Yellow", 5);
                                }
                                else if (chatdata.Player.Count() == 0)
                                {
                                    API.Alert(Received_ChatInfo.playerId, "Error, Name Fragment Not Found", "Yellow", 5);
                                }
                            }
                            else if (Received_ChatInfo.msg.ToLower().StartsWith(SetupYaml.General.DefaultPrefix + SetupYaml.MultiLineMessage.Command + SetupYaml.MultiLineMessage.AddLine))
                            {
                                Storage.MLM EditMLM = MultilineData[Received_ChatInfo.playerId];
                                List<string> LinesList = EditMLM.Lines;
                                string message = CommonFunctions.SplitChat2(Received_ChatInfo.msg);
                                LinesList.Add(message);
                                MultilineData[Received_ChatInfo.playerId] = EditMLM;
                                API.Alert(Received_ChatInfo.playerId, "Line Added: " + message, "Blue", 2);
                            }
                            else if (Received_ChatInfo.msg.ToLower().StartsWith(SetupYaml.General.DefaultPrefix + SetupYaml.MultiLineMessage.Command + SetupYaml.MultiLineMessage.Clear))
                            {
                                Storage.MLM EditMLM = MultilineData[Received_ChatInfo.playerId];
                                List<string> LinesList = new List<string> { };
                                MultilineData[Received_ChatInfo.playerId] = EditMLM;
                                API.Alert(Received_ChatInfo.playerId, "Message Deleted", "Blue", 2);
                            }
                            else if (Received_ChatInfo.msg.ToLower().StartsWith(SetupYaml.General.DefaultPrefix + SetupYaml.MultiLineMessage.Command + SetupYaml.MultiLineMessage.Send))
                            {
                                Storage.StorableData function = new Storage.StorableData
                                {
                                    function = "SendMLM",
                                    Match = Convert.ToString(Received_ChatInfo.playerId),
                                    Requested = "PlayerInfo",
                                    ChatInfo = Received_ChatInfo
                                };
                                thisSeqNr = API.PlayerInfo(Received_ChatInfo.playerId);
                                SeqNrStorage[thisSeqNr] = function;
                            }
                            else if (Received_ChatInfo.msg.ToLower().StartsWith(SetupYaml.General.DefaultPrefix + SetupYaml.MultiLineMessage.Command + SetupYaml.MultiLineMessage.Location))
                            {
                                Storage.StorableData function = new Storage.StorableData
                                {
                                    function = "LocMLM",
                                    Match = Convert.ToString(Received_ChatInfo.playerId),
                                    Requested = "PlayerInfo",
                                    ChatInfo = Received_ChatInfo,
                                };
                                thisSeqNr = API.PlayerInfo(Received_ChatInfo.playerId);
                                SeqNrStorage[thisSeqNr] = function;
                            }
                        }

                        //Admin Mail
                        else if (Received_ChatInfo.msg.ToLower().StartsWith(SetupYaml.General.DefaultPrefix + SetupYaml.AdminMail.Command))
                        {
                            Storage.StorableData function = new Storage.StorableData
                            {
                                function = "AdminMail",
                                Match = Convert.ToString(Received_ChatInfo.playerId),
                                Requested = "PlayerInfo",
                                ChatInfo = Received_ChatInfo
                            };
                            thisSeqNr = API.PlayerInfo(Received_ChatInfo.playerId);
                            SeqNrStorage[thisSeqNr] = function;
                        }

                        //Admin Send Reward
                        else if (Received_ChatInfo.msg.ToLower().StartsWith(SetupYaml.General.DefaultPrefix + SetupYaml.RewardMail.Command))
                        {
                            Storage.StorableData function = new Storage.StorableData
                            {
                                function = "RewardMail",
                                Match = Convert.ToString(Received_ChatInfo.playerId),
                                Requested = "PlayerInfo",
                                ChatInfo = Received_ChatInfo
                            };
                            thisSeqNr = API.PlayerInfo(Received_ChatInfo.playerId);
                            SeqNrStorage[thisSeqNr] = function;
                        }

                        //Admin Add News
                        else if (Received_ChatInfo.msg.ToLower().StartsWith(SetupYaml.General.DefaultPrefix + SetupYaml.News.Command))
                        {
                            Storage.StorableData function = new Storage.StorableData
                            {
                                function = "News",
                                Match = Convert.ToString(Received_ChatInfo.playerId),
                                Requested = "PlayerInfo",
                                ChatInfo = Received_ChatInfo
                            };
                            thisSeqNr = API.PlayerInfo(Received_ChatInfo.playerId);
                            SeqNrStorage[thisSeqNr] = function;
                        }

                        //Mail to Faction
                        else if (Received_ChatInfo.msg.ToLower().StartsWith(SetupYaml.General.DefaultPrefix + SetupYaml.MailToFaction.Command))
                        {
                            Storage.StorableData function = new Storage.StorableData
                            {
                                function = "MailToFaction",
                                Match = Convert.ToString(Received_ChatInfo.playerId),
                                Requested = "PlayerInfo",
                                ChatInfo = Received_ChatInfo
                            };
                            thisSeqNr = API.PlayerInfo(Received_ChatInfo.playerId);
                            SeqNrStorage[thisSeqNr] = function;
                        }

                        //Void Box
                        else if (Received_ChatInfo.msg.ToLower().StartsWith(SetupYaml.General.DefaultPrefix + SetupYaml.TheVoid.Command))
                        {
                            if (VoidOpen != 0)
                            {

                            }
                            Storage.StorableData function = new Storage.StorableData
                            {
                                function = "Void",
                                Match = Convert.ToString(Received_ChatInfo.playerId),
                                Requested = "PlayerInfo",
                                ChatInfo = Received_ChatInfo
                            };
                            thisSeqNr = API.PlayerInfo(Received_ChatInfo.playerId);
                            SeqNrStorage[thisSeqNr] = function;
                        }

                        //Inbox
                        //else if (Received_ChatInfo.msg.ToLower().StartsWith(SetupYaml.General.DefaultPrefix + SetupYaml.ReadMail.Command))
                        if (Received_ChatInfo.msg.ToLower() == SetupYaml.General.DefaultPrefix + SetupYaml.ReadMail.Command)
                        {
                            try
                            {
                                PlayerYamlDB.PlayerData Speaker = PlayersDB[Convert.ToString(Received_ChatInfo.playerId)];
                                Mail.Root MailboxData = Mail.Read(ModPath + "MailData\\" + Speaker.SteamID + ".yaml");
                                if (MailboxData.Unread.Count() > 1)
                                {
                                    if (MailboxData.Unread[1].Type == "Multiline")
                                    {
                                        if (MailboxData.Unread[1].LocList.Count() > 0)
                                        {
                                            Storage.StorableData function = new Storage.StorableData
                                            {
                                                function = "SaveCoordinates",
                                                Match = Convert.ToString(Received_ChatInfo.playerId),
                                                Requested = "PlayerInfo",
                                                ChatInfo = Received_ChatInfo
                                            };
                                            thisSeqNr = API.PlayerInfo(Received_ChatInfo.playerId);
                                            SeqNrStorage[thisSeqNr] = function;
                                        }
                                        else
                                        {
                                            string sender = PlayersDBSteam[MailboxData.Unread[1].From].PlayerName;
                                            string[] MessageArray = File.ReadAllLines(ModPath + "MailData\\Multiline\\" + MailboxData.Unread[1].Message + ".txt");
                                            string Button1 = "Close";
                                            string Message1 = CommonFunctions.ArrayConcatenate(0, MessageArray);
                                            string Time = MailboxData.Unread[1].Timestamp;
                                            string Message2 = "From: " + sender + Time + "\r\n\r\n" + Message1;

                                            //Beta2 test code
                                            API.TextWindowOpen(Speaker.EmpyrionID, Message2, Button1, null);
                                            List<Mail.MessageData> UnreadMessages = MailboxData.Unread;
                                            UnreadMessages.Remove(MailboxData.Unread[1]);
                                            Mail.Root Root = new Mail.Root
                                            {
                                                Unread = UnreadMessages
                                            };
                                            Mail.Write(ModPath + "MailData\\" + Speaker.SteamID + ".yaml", Root);
                                            UsageReadMLM++;
                                        }
                                    }
                                    else if (MailboxData.Unread[1].Type == "Items")
                                    {
                                        string sender = PlayersDBSteam[MailboxData.Unread[1].From].PlayerName;
                                        //string[] MessageArray = File.ReadAllLines(ModPath + "MailData\\Multiline\\" + MailboxData.Unread[1].Message + ".txt");
                                        string Button1 = "Close";
                                        string Message = MailboxData.Unread[1].Message;
                                        string Time = MailboxData.Unread[1].Timestamp;
                                        ItemStack[] Items = CommonFunctions.ReadItemStacks(ModPath + "MailData\\Items\\" + MailboxData.Unread[1].ItemStacksFile + ".txt");
                                        Storage.StorableData function = new Storage.StorableData
                                        {
                                            function = "VoidCatcher",
                                            Match = "None",
                                            Requested = "ItemExchange",
                                            MailData = MailboxData,
                                            ChatInfo = Received_ChatInfo
                                        };
                                        thisSeqNr = API.ItemExchange(Convert.ToInt32(Speaker.EmpyrionID), "From: " + sender + " " + Time, Message, Button1, Items);
                                        SeqNrStorage[thisSeqNr] = function;
                                    }
                                }
                                else if (MailboxData.Unread.Count() == 0)
                                {
                                    API.Alert(Convert.ToInt16(Speaker.EmpyrionID), "No Messages", "Blue", 2);
                                }
                            }
                            catch
                            {
                                CommonFunctions.LogFile("debug.txt", "Failed reading message");
                            }
                        }
                        break;


                    case CmdId.Event_Player_Connected:
                        //Triggered when a player logs on
                        Id Received_PlayerConnected = (Id)data;
                        LogonDelayDict.Add(LogonDelay + 5, Received_PlayerConnected.id);
                        /*
                        Storage.StorableData ThisFunction = new Storage.StorableData
                        {
                            function = "Logon",
                            Match = Convert.ToString(Received_PlayerConnected.id),
                            Requested = "PlayerInfo",
                            PlayerConnected = Received_PlayerConnected
                        };
                        thisSeqNr = API.PlayerInfo(Received_PlayerConnected.id);
                        SeqNrStorage[thisSeqNr] = ThisFunction;
                        */
                        break;


                    case CmdId.Event_Player_Disconnected:
                        //Triggered when a player logs off
                        Id Received_PlayerDisconnected = (Id)data;
                        break;


                    case CmdId.Event_Player_ChangedPlayfield:
                        //Triggered when a player changes playfield
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Player_ChangePlayfield, (ushort)CurrentSeqNr, new IdPlayfieldPositionRotation( [PlayerID], [Playfield Name], [PVector3 position], [PVector3 Rotation] ));
                        IdPlayfield Received_PlayerChangedPlayfield = (IdPlayfield)data;
                        CommonFunctions.LogFile("Debug.txt", "Playfield Change Detected");
                        PlayerYamlDB.PlayerData PlayerData = PlayersDB[Convert.ToString(Received_PlayerChangedPlayfield.id)];
                        if (File.Exists(ModPath + "SavedCoordinates\\" + PlayerData.SteamID + ".yaml"))
                        {
                            SavedCoordinates.Root CoordsFile = SavedCoordinates.Retrieve(ModPath + "SavedCoordinates\\" + PlayerData.SteamID + ".yaml");

                            for (int i = 0; i < CoordsFile.Playfield.Count(); ++i)
                            {
                                List<string> SendableList = new List<string> { };
                                SavedCoordinates.Playfields Playfield = CoordsFile.Playfield[i];
                                if (Playfield.PlayfieldName == Received_PlayerChangedPlayfield.playfield)
                                {
                                    foreach (SavedCoordinates.LocationData CoordData in Playfield.Locations)
                                    {
                                        string SendableString = "remoteex cl=" + PlayerData.ClientID + " \'marker add Name=MailMark pos=" + Convert.ToInt16(CoordData.CoordX) + "," + Convert.ToInt16(CoordData.CoordY) + "," + Convert.ToInt16(CoordData.CoordZ) + "\'";
                                        CommonFunctions.LogFile("Debug.txt", "Playfield Change = " + SendableString);
                                        SendableList.Add(SendableString);

                                        //API.Marker(Convert.ToInt16(PlayerData.ClientID), "MailMark", Convert.ToInt16(CoordData.CoordX), Convert.ToInt16(CoordData.CoordY), Convert.ToInt16(CoordData.CoordZ), false, 0, false);
                                    }
                                    CoordsFile.Playfield.Remove(Playfield);
                                    UpdateDictionary.Add(UpdateCounter + 100, SendableList);
                                }
                            }
                            SavedCoordinates.WriteYaml(ModPath + "SavedCoordinates\\" + PlayerData.SteamID + ".yaml", CoordsFile);
                        }
                        break;


                    case CmdId.Event_Playfield_Loaded:
                        //Triggered when a player goes to a playfield that isnt currently loaded in memory
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Load_Playfield, (ushort)CurrentSeqNr, new PlayfieldLoad( [float nSecs], [string nPlayfield], [int nProcessId] ));
                        PlayfieldLoad Received_PlayfieldLoaded = (PlayfieldLoad)data;
                        break;


                    case CmdId.Event_Playfield_Unloaded:
                        //Triggered when there are no players left in a playfield
                        PlayfieldLoad Received_PlayfieldUnLoaded = (PlayfieldLoad)data;
                        break;


                    case CmdId.Event_Faction_Changed:
                        //Triggered when an Entity (player too?) changes faction
                        FactionChangeInfo Received_FactionChange = (FactionChangeInfo)data;
                        break;


                    case CmdId.Event_Statistics:
                        //Triggered on various game events like: Player Death, Entity Power on/off, Remove/Add Core
                        StatisticsParam Received_EventStatistics = (StatisticsParam)data;
                        break;


                    case CmdId.Event_Player_DisconnectedWaiting:
                        //Triggered When a player is having trouble logging into the server
                        Id Received_PlayerDisconnectedWaiting = (Id)data;
                        break;


                    case CmdId.Event_TraderNPCItemSold:
                        //Triggered when a player buys an item from a trader
                        TraderNPCItemSoldInfo Received_TraderNPCItemSold = (TraderNPCItemSoldInfo)data;
                        break;


                    case CmdId.Event_Player_List:
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Player_List, (ushort)CurrentSeqNr, null));
                        IdList Received_PlayerList = (IdList)data;
                        break;


                    case CmdId.Event_Player_Info:
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Player_Info, (ushort)CurrentSeqNr, new Id( [playerID] ));
                        PlayerInfo Received_PlayerInfo = (PlayerInfo)data;
                        if (SeqNrStorage.Keys.Contains(seqNr))
                        {
                            Storage.StorableData RequestTracker = SeqNrStorage[seqNr];
                            if (RequestTracker.Requested == "PlayerInfo" && RequestTracker.function == "Items" && Convert.ToString(Received_PlayerInfo.entityId) == RequestTracker.Match && !SetupYaml.General.RestrictedPlayfields.Contains(Received_PlayerInfo.playfield))
                            {
                                SeqNrStorage.Remove(seqNr);
                                if (SetupYaml.SendItems.Enabled && SetupYaml.SendItems.AllowGlobalChat && RequestTracker.ChatInfo.type == 3 && Received_PlayerInfo.credits > 10000)
                                {

                                    Storage.StorableData function = RequestTracker;
                                    function.function = "Items";
                                    function.Match = Convert.ToString(Received_PlayerInfo.entityId);
                                    function.Requested = "ItemExchange";
                                    Storage.ChatData chatdata = CommonFunctions.SplitChat(function.ChatInfo.msg);
                                    if (chatdata.Player.Count() == 1)
                                    {
                                        API.CreditChange(Received_PlayerInfo.entityId, Convert.ToInt32(Received_PlayerInfo.credits) - 10000);
                                        if (chatdata.Player[0].EmpyrionID != Convert.ToString(Received_PlayerInfo.entityId))
                                        {
                                            function.chatdata = chatdata;
                                            thisSeqNr = API.ItemExchange(Received_PlayerInfo.entityId, "To: " + chatdata.Player[0].PlayerName, chatdata.Message, "Send", blankItemStack);
                                            SeqNrStorage[thisSeqNr] = function;
                                        }
                                    }
                                    else if (chatdata.Player.Count() > 1)
                                    {
                                        API.Alert(Received_PlayerInfo.entityId, "Error: Name Fragment Not Unique", "Yellow", 5);
                                    }
                                    else if (chatdata.Player.Count() == 0)
                                    {
                                        API.Alert(Received_PlayerInfo.entityId, "Error: Name Fragment Not Found", "Yellow", 5);
                                    }
                                }
                                else if (SetupYaml.SendItems.Enabled && SetupYaml.SendItems.AllowFactionChat && RequestTracker.ChatInfo.type == 5 && Received_PlayerInfo.credits > 10000)
                                {
                                    Storage.StorableData function = RequestTracker;
                                    function.function = "Items";
                                    function.Match = Convert.ToString(Received_PlayerInfo.entityId);
                                    function.Requested = "ItemExchange";
                                    Storage.ChatData chatdata = CommonFunctions.SplitChat(function.ChatInfo.msg);
                                    if (chatdata.Player.Count() == 1)
                                    {
                                        API.CreditChange(Received_PlayerInfo.entityId, Convert.ToInt32(Received_PlayerInfo.credits) - 10000);
                                        if (chatdata.Player[0].EmpyrionID != Convert.ToString(Received_PlayerInfo.entityId))
                                        {
                                            function.chatdata = chatdata;
                                            thisSeqNr = API.ItemExchange(Received_PlayerInfo.entityId, "To: " + chatdata.Player[0].PlayerName, chatdata.Message, "Send", blankItemStack);
                                            SeqNrStorage[thisSeqNr] = function;
                                        }
                                    }
                                    else if (chatdata.Player.Count() > 1)
                                    {
                                        API.Alert(Received_PlayerInfo.entityId, "Error: Name Fragment Not Unique", "Yellow", 5);
                                    }
                                    else if (chatdata.Player.Count() == 0)
                                    {
                                        API.Alert(Received_PlayerInfo.entityId, "Error: Name Fragment Not Found", "Yellow", 5);
                                    }
                                }
                                else if (SetupYaml.SendItems.Enabled && SetupYaml.SendItems.AllowSecret && Received_PlayerInfo.credits > 10000)
                                {
                                    Storage.StorableData function = RequestTracker;
                                    function.function = "Items";
                                    function.Match = Convert.ToString(Received_PlayerInfo.entityId);
                                    function.Requested = "ItemExchange";
                                    //change chat message to drop the s!
                                    Storage.ChatData chatdata = CommonFunctions.SplitChat(function.ChatInfo.msg);
                                    if (chatdata.Player.Count() == 1)
                                    {
                                        API.CreditChange(Received_PlayerInfo.entityId, Convert.ToInt32(Received_PlayerInfo.credits) - 10000);
                                        if (chatdata.Player[0].EmpyrionID != Convert.ToString(Received_PlayerInfo.entityId))
                                        {
                                            function.chatdata = chatdata;
                                            thisSeqNr = API.ItemExchange(Received_PlayerInfo.entityId, "To: " + chatdata.Player[0].PlayerName, chatdata.Message, "Send", blankItemStack);
                                            //function.TargetPlayer.Add(Received_PlayerInfo);
                                            SeqNrStorage[thisSeqNr] = function;
                                        }
                                    }
                                    else if (chatdata.Player.Count() > 1)
                                    {
                                        API.Alert(Received_PlayerInfo.entityId, "Error: Name Fragment Not Unique", "Yellow", 5);
                                    }
                                    else if (chatdata.Player.Count() == 0)
                                    {
                                        API.Alert(Received_PlayerInfo.entityId, "Error: Name Fragment Not Found", "Yellow", 5);
                                    }
                                }
                                else if (SetupYaml.SendItems.Enabled && Received_PlayerInfo.credits < 10001)
                                {
                                    API.Alert(Received_PlayerInfo.entityId, "Not Enough Credits", "Red", 3);
                                }

                            }
                            else if (RequestTracker.Requested == "PlayerInfo" && RequestTracker.function == "Logon" && Convert.ToString(Received_PlayerInfo.entityId) == RequestTracker.Match)
                            {
                                SeqNrStorage.Remove(seqNr);
                                PlayerYamlDB.PlayerData NewPlayer = new PlayerYamlDB.PlayerData
                                {
                                    PlayerName = Received_PlayerInfo.playerName,
                                    Playfield = Received_PlayerInfo.playfield,
                                    EmpyrionID = Convert.ToString(Received_PlayerInfo.entityId),
                                    SteamID = Received_PlayerInfo.steamId,
                                    ClientID = Convert.ToString(Received_PlayerInfo.clientId)
                                };
                                PlayersDB[Convert.ToString(Received_PlayerInfo.entityId)] = NewPlayer;
                                PlayersDBSteam[Received_PlayerInfo.steamId] = NewPlayer;
                                PlayerYaml.Database = new List<PlayerYamlDB.PlayerData> { };
                                foreach (string player in PlayersDBSteam.Keys)
                                {
                                    PlayerYaml.Database.Add(PlayersDBSteam[player]);
                                }
                                PlayerYamlDB.Write(ModPath + "PlayersDB.yaml", PlayerYaml);
                            }
                            else if (RequestTracker.Requested == "PlayerInfo" && RequestTracker.function == "PFChange" && Convert.ToString(Received_PlayerInfo.entityId) == RequestTracker.Match)
                            {
                                SeqNrStorage.Remove(seqNr);
                            }
                            else if (RequestTracker.Requested == "PlayerInfo" && RequestTracker.function == "LocMLM" && Convert.ToString(Received_PlayerInfo.entityId) == RequestTracker.Match)
                            {
                                SeqNrStorage.Remove(seqNr);
                                //Storage.MLM EditMLM = MultilineData[Received_ChatInfo.playerId];
                                Storage.MLM EditMLM = MultilineData[RequestTracker.ChatInfo.playerId];
                                List<Setup.LocData> LocList = EditMLM.Location;
                                Setup.LocData NewLoc = new Setup.LocData
                                {
                                    X = Received_PlayerInfo.pos.x,
                                    y = Received_PlayerInfo.pos.y,
                                    z = Received_PlayerInfo.pos.z,
                                    Playfield = Received_PlayerInfo.playfield
                                };
                                LocList.Add(NewLoc);
                                EditMLM.Location = LocList;
                                string LocData = Received_PlayerInfo.playfield + " @ " + Math.Round(Received_PlayerInfo.pos.x) + "," + Math.Round(Received_PlayerInfo.pos.y) + "," + Math.Round(Received_PlayerInfo.pos.z);
                                EditMLM.Lines.Add(LocData);
                                MultilineData[RequestTracker.ChatInfo.playerId] = EditMLM;
                                API.Alert(Received_PlayerInfo.entityId, "Location Added:" + LocData, "Blue", 2);
                                UsageSendLoc++;
                            }
                            else if (RequestTracker.Requested == "PlayerInfo" && RequestTracker.function == "SendMLM" && Convert.ToString(Received_PlayerInfo.entityId) == RequestTracker.Match)
                            {
                                SeqNrStorage.Remove(seqNr);
                                if (!SetupYaml.General.RestrictedPlayfields.Contains(Received_PlayerInfo.playfield))
                                {
                                    Storage.MLM EditMLM = MultilineData[Received_PlayerInfo.entityId];
                                    string TickStamp = Convert.ToString(Storage.GameAPI.Game_GetTickTime());
                                    foreach (string player in EditMLM.Recipient)
                                    {
                                        Mail.Root UserMail = Mail.Read(ModPath + "PlayerMail.txt");
                                        if (File.Exists(ModPath + "MailData\\" + player + ".yaml"))
                                        {
                                            UserMail = Mail.Read(ModPath + "MailData\\" + player + ".yaml");
                                        }
                                        else
                                        {
                                            CommonFunctions.LogFile("MailData\\" + player + ".yaml", "---");
                                        }
                                        DateTime TimeStamp = DateTime.Now;
                                        Mail.MessageData NewMessage = new Mail.MessageData
                                        {
                                            Message = TickStamp,
                                            From = Convert.ToString(PlayersDB[Convert.ToString(Received_PlayerInfo.entityId)].SteamID),
                                            Type = "Multiline",
                                            LocList = EditMLM.Location,
                                            Timestamp = Convert.ToString(TimeStamp),
                                        };
                                        UserMail.Unread.Add(NewMessage);
                                        try
                                        {
                                            Mail.Write(ModPath + "MailData\\" + player + ".yaml", UserMail);
                                            API.Alert(Convert.ToInt32(PlayersDB[player].EmpyrionID) , "Message Received", "Yellow", 3);
                                        }
                                        catch
                                        {
                                            CommonFunctions.LogFile("debug.txt", "Mail.Write");
                                        }
                                    }
                                    //Write All Lines of Message
                                    foreach( string Line in EditMLM.Lines)
                                    {
                                        CommonFunctions.LogFile("MailData\\Multiline\\" + TickStamp + ".txt", Line);
                                    }
                                    API.Alert(Received_PlayerInfo.entityId, "Message Sent", "Blue", 2);
                                    UsageSendMLM++;
                                }
                            }
                            else if (RequestTracker.Requested == "PlayerInfo" && RequestTracker.function == "SaveCoordinates" && Convert.ToString(Received_PlayerInfo.entityId) == RequestTracker.Match)
                            {
                                SeqNrStorage.Remove(seqNr);
                                Mail.Root MailboxData = Mail.Read(ModPath + "MailData\\" + Received_PlayerInfo.steamId + ".yaml");
                                string sender = PlayersDBSteam[MailboxData.Unread[1].From].PlayerName;
                                string[] MessageArray = File.ReadAllLines(ModPath + "MailData\\Multiline\\" + MailboxData.Unread[1].Message + ".txt");
                                string Button1 = "Close";
                                string Message1 = CommonFunctions.ArrayConcatenate(0, MessageArray);
                                string Time = MailboxData.Unread[1].Timestamp;
                                string Message2 = "From: " + sender + Time + "\r\n\r\n" + Message1;

                                Storage.StorableData function = new Storage.StorableData
                                {
                                    function = "SaveCoordinates",
                                    Match = Convert.ToString(Received_PlayerInfo.entityId),
                                    Requested = "DialogButtonIndex",
                                    TriggerPlayer = Received_PlayerInfo,
                                    MailData = MailboxData
                                };
                                string Button0 = "Save Waypoints";
                                thisSeqNr = API.TextWindowOpen(Convert.ToString(Received_PlayerInfo.entityId), Message2, Button0, Button1);
                                SeqNrStorage[thisSeqNr] = function;

                            }

                        }
                        break;


                    case CmdId.Event_Player_Inventory:
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Player_GetInventory, (ushort)CurrentSeqNr, new Id( [playerID] ));
                        Inventory Received_PlayerInventory = (Inventory)data;
                        break;


                    case CmdId.Event_Player_ItemExchange:
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Player_ItemExchange, (ushort)CurrentSeqNr, new ItemExchangeInfo( [id], [title], [description], [buttontext], [ItemStack[]] ));
                        ItemExchangeInfo Received_ItemExchangeInfo = (ItemExchangeInfo)data;
                        if (SeqNrStorage.Keys.Contains(seqNr))
                        {
                            Storage.StorableData RequestTracker = SeqNrStorage[seqNr];
                            if (RequestTracker.Requested == "ItemExchange" && RequestTracker.function == "Items" && Convert.ToString(Received_ItemExchangeInfo.id) == RequestTracker.Match)
                            {
                                SeqNrStorage.Remove(seqNr);
                                Mail.Root UserMail = Mail.Read(ModPath + "PlayerMail.txt");
                                if (File.Exists(ModPath + "MailData\\" + RequestTracker.chatdata.Player[0].SteamID + ".yaml"))
                                {
                                    UserMail = Mail.Read(ModPath + "MailData\\" + RequestTracker.chatdata.Player[0].SteamID + ".yaml");
                                }
                                else
                                {
                                    CommonFunctions.LogFile("MailData\\" + RequestTracker.chatdata.Player[0].SteamID + ".yaml", "---");
                                }
                                string TickStamp = Convert.ToString(Storage.GameAPI.Game_GetTickTime());
                                DateTime TimeStamp = DateTime.Now;
                                //string TimeStamp = CommonFunctions.TimeStamp();
                                Mail.MessageData NewMessage = new Mail.MessageData
                                {
                                    Message = RequestTracker.chatdata.Message,
                                    From = Convert.ToString(PlayersDB[Convert.ToString(RequestTracker.ChatInfo.playerId)].SteamID),
                                    Type = "Items",
                                    Timestamp = Convert.ToString(TimeStamp),
                                    ItemStacksFile = TickStamp
                                };
                                UserMail.Unread.Add(NewMessage);
                                try
                                {
                                    Mail.Write(ModPath + "MailData\\" + RequestTracker.chatdata.Player[0].SteamID + ".yaml", UserMail);
                                    API.Alert(Convert.ToInt16(RequestTracker.chatdata.Player[0].EmpyrionID), PlayersDB[Convert.ToString(RequestTracker.ChatInfo.playerId)].PlayerName +" sent you some items", "Yellow", 3);
                                }
                                catch
                                {
                                    CommonFunctions.LogFile("debug.txt", "Mail.Write Item Message");
                                }
                                CommonFunctions.WriteItemStacks("MailData\\Items\\" + TickStamp + ".txt", Received_ItemExchangeInfo.items, false);
                                UsageSendItem++;
                            }
                            else if(RequestTracker.function == "VoidCatcher" && RequestTracker.Match == "None" && RequestTracker.Requested == "ItemExchange")
                            {
                                SeqNrStorage.Remove(seqNr);
                                foreach (ItemStack item in Received_ItemExchangeInfo.items)
                                {
                                    CommonFunctions.LogFile("TheVoid.txt", item.slotIdx + "," + item.id + "," + item.count + "," + item.decay + "," + item.ammo);
                                }
                                try
                                {
                                    PlayerYamlDB.PlayerData ThisPlayerData = PlayersDB[Convert.ToString(Received_ItemExchangeInfo.id)];
                                    List<Mail.MessageData> UnreadMessages = RequestTracker.MailData.Unread;
                                    UnreadMessages.Remove(RequestTracker.MailData.Unread[1]);
                                    Mail.Root MailRoot = new Mail.Root
                                    {
                                        Unread = UnreadMessages
                                    };
                                    Mail.Write(ModPath + "MailData\\" + ThisPlayerData.SteamID + ".yaml", MailRoot);
                                }
                                catch { CommonFunctions.LogFile("debug.txt", "Error removing Item Message from inbox"); }
                                UsageReadItem++;
                            }
                        }
                        break;


                    case CmdId.Event_DialogButtonIndex:
                        //All of This is a Guess
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_ShowDialog_SinglePlayer, (ushort)CurrentSeqNr, new IdMsgPrio( [int nId], [string nMsg], [byte nPrio], [float nTime] )); //for Prio: 0=Red, 1=Yellow, 2=Blue
                        IdAndIntValue Received_DialogButtonIndex = (IdAndIntValue)data;
                        //Save/Pos = 0, Close/Cancel/Neg = 1
                        if (SeqNrStorage.Keys.Contains(seqNr))
                        {
                            Storage.StorableData RequestTracker = SeqNrStorage[seqNr];
                            if (RequestTracker.Requested == "DialogButtonIndex" && RequestTracker.function == "SaveCoordinates" && Convert.ToString(Received_DialogButtonIndex.Id) == RequestTracker.Match)
                            {
                                SeqNrStorage.Remove(seqNr);
                                if (Received_DialogButtonIndex.Value == 0)
                                {
                                    CommonFunctions.LogFile("debug.txt", "ID= " + Received_DialogButtonIndex.Id + "  Value= " + Received_DialogButtonIndex.Value + "  Save");
                                    Mail.MessageData MailData = RequestTracker.MailData.Unread[1];
                                    SavedCoordinates.Root CoordsFile = new SavedCoordinates.Root
                                    {
                                        Playfield = new List<SavedCoordinates.Playfields>{ }
                                    };
                                    SavedCoordinates.LocationData CoordsList = new SavedCoordinates.LocationData { };
                                    if (File.Exists(ModPath + "SavedCoordinates\\" + RequestTracker.TriggerPlayer.steamId + ".yaml"))
                                    {
                                        CoordsFile = SavedCoordinates.Retrieve(ModPath + "SavedCoordinates\\" + RequestTracker.TriggerPlayer.steamId + ".yaml");
                                    }
                                    foreach(Setup.LocData Loc in MailData.LocList)
                                    {
                                        if(Loc.Playfield == RequestTracker.TriggerPlayer.playfield)
                                        {
                                            //plot
                                            API.Marker(RequestTracker.TriggerPlayer.clientId, "MailMark", Convert.ToInt16(Math.Round(Loc.X)), Convert.ToInt16(Math.Round(Loc.y)), Convert.ToInt16(Math.Round(Loc.z)), false, 0, false);
                                        }
                                        else
                                        {
                                            bool ListExistsInFile = false;
                                            for (int i = 0; i < CoordsFile.Playfield.Count(); ++i)
                                            {
                                                SavedCoordinates.Playfields Playfield = CoordsFile.Playfield[i];
                                                if ( Loc.Playfield == Playfield.PlayfieldName)
                                                {
                                                    ListExistsInFile = true;
                                                    List<SavedCoordinates.LocationData> CurrentLocations = CoordsFile.Playfield[i].Locations;
                                                    SavedCoordinates.LocationData NewLoc = new SavedCoordinates.LocationData
                                                    {
                                                        CoordX = Convert.ToInt16(Math.Round(Loc.X)),
                                                        CoordY = Convert.ToInt16(Math.Round(Loc.y)),
                                                        CoordZ = Convert.ToInt16(Math.Round(Loc.z))
                                                    };
                                                    CurrentLocations.Add(NewLoc);
                                                }
                                            }
                                            if (!ListExistsInFile)
                                            {
                                                SavedCoordinates.LocationData NewLoc = new SavedCoordinates.LocationData
                                                {
                                                    CoordX = Convert.ToInt16(Math.Round(Loc.X)),
                                                    CoordY = Convert.ToInt16(Math.Round(Loc.y)),
                                                    CoordZ = Convert.ToInt16(Math.Round(Loc.z))
                                                };
                                                List<SavedCoordinates.LocationData> newLocList = new List<SavedCoordinates.LocationData> { };
                                                newLocList.Add(NewLoc);
                                                SavedCoordinates.Playfields NewPlayfieldData = new SavedCoordinates.Playfields
                                                {
                                                    PlayfieldName = Loc.Playfield,
                                                    Locations = newLocList
                                                };
                                                CoordsFile.Playfield.Add(NewPlayfieldData);
                                            }
                                        }
                                    }
                                    SavedCoordinates.WriteYaml(ModPath + "SavedCoordinates\\" + RequestTracker.TriggerPlayer.steamId + ".yaml", CoordsFile);

                                    List<Mail.MessageData> UnreadMessages = RequestTracker.MailData.Unread;
                                    UnreadMessages.Remove(RequestTracker.MailData.Unread[1]);
                                    Mail.Root Root = new Mail.Root
                                    {
                                        Unread = UnreadMessages
                                    };
                                    Mail.Write(ModPath + "MailData\\" + RequestTracker.TriggerPlayer.steamId + ".yaml", Root);
                                    UsageReadMLM++;
                            }
                            else if (Received_DialogButtonIndex.Value == 1)
                                {
                                    SeqNrStorage.Remove(seqNr);
                                    CommonFunctions.LogFile("debug.txt", "ID= " + Received_DialogButtonIndex.Id + "  Value= " + Received_DialogButtonIndex.Value + "  Close");
                                    List<Mail.MessageData> UnreadMessages = RequestTracker.MailData.Unread;
                                    UnreadMessages.Remove(RequestTracker.MailData.Unread[1]);
                                    Mail.Root Root = new Mail.Root
                                    {
                                        Unread = UnreadMessages
                                    };
                                    Mail.Write(ModPath + "MailData\\" + RequestTracker.TriggerPlayer.steamId + ".yaml", Root);
                                }
                            }
                        }
                        break;


                    case CmdId.Event_Player_Credits:
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Player_Credits, (ushort)CurrentSeqNr, new Id( [PlayerID] ));
                        IdCredits Received_PlayerCredits = (IdCredits)data;
                        break;


                    case CmdId.Event_Player_GetAndRemoveInventory:
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Player_GetAndRemoveInventory, (ushort)CurrentSeqNr, new Id( [playerID] ));
                        Inventory Received_PlayerGetRemoveInventory = (Inventory)data;
                        break;


                    case CmdId.Event_Playfield_List:
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Playfield_List, (ushort)CurrentSeqNr, null));
                        PlayfieldList Received_PlayfieldList = (PlayfieldList)data;
                        break;


                    case CmdId.Event_Playfield_Stats:
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Playfield_Stats, (ushort)CurrentSeqNr, new PString( [Playfield Name] ));
                        PlayfieldStats Received_PlayfieldStats = (PlayfieldStats)data;
                        break;


                    case CmdId.Event_Playfield_Entity_List:
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Playfield_Entity_List, (ushort)CurrentSeqNr, new PString( [Playfield Name] ));
                        PlayfieldEntityList Received_PlayfieldEntityList = (PlayfieldEntityList)data;
                        break;


                    case CmdId.Event_Dedi_Stats:
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Dedi_Stats, (ushort)CurrentSeqNr, null));
                        DediStats Received_DediStats = (DediStats)data;
                        break;


                    case CmdId.Event_GlobalStructure_List:
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_GlobalStructure_List, (ushort)CurrentSeqNr, null));
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_GlobalStructure_Update, (ushort)CurrentSeqNr, new PString( [Playfield Name] ));
                        GlobalStructureList Received_GlobalStructureList = (GlobalStructureList)data;
                        //foreach (GlobalStructureInfo item in Structs.globalStructures[storedInfo[seqNr].PlayerInfo.playfield])
                        break;


                    case CmdId.Event_Entity_PosAndRot:
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Entity_PosAndRot, (ushort)CurrentSeqNr, new Id( [EntityID] ));
                        IdPositionRotation Received_EntityPosRot = (IdPositionRotation)data;
                        break;


                    case CmdId.Event_Get_Factions:
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Get_Factions, (ushort)CurrentSeqNr, new Id( [int] )); //Requests all factions from a certain Id onwards. If you want all factions use Id 1.
                        FactionInfoList Received_FactionInfoList = (FactionInfoList)data;
                        break;


                    case CmdId.Event_NewEntityId:
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_NewEntityId, (ushort)CurrentSeqNr, null));
                        Id Request_NewEntityId = (Id)data;
                        break;


                    case CmdId.Event_Structure_BlockStatistics:
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Structure_BlockStatistics, (ushort)CurrentSeqNr, new Id( [EntityID] ));
                        IdStructureBlockInfo Received_StructureBlockStatistics = (IdStructureBlockInfo)data;
                        break;


                    case CmdId.Event_AlliancesAll:
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_AlliancesAll, (ushort)CurrentSeqNr, null));
                        AlliancesTable Received_AlliancesAll = (AlliancesTable)data;
                        break;


                    case CmdId.Event_AlliancesFaction:
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_AlliancesFaction, (ushort)CurrentSeqNr, new AlliancesFaction( [int nFaction1Id], [int nFaction2Id], [bool nIsAllied] ));
                        AlliancesFaction Received_AlliancesFaction = (AlliancesFaction)data;
                        break;


                    case CmdId.Event_BannedPlayers:
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_GetBannedPlayers, (ushort)CurrentSeqNr, null ));
                        BannedPlayerData Received_BannedPlayers = (BannedPlayerData)data;
                        break;


                    case CmdId.Event_GameEvent:
                        //Triggered by PDA Events
                        GameEventData Received_GameEvent = (GameEventData)data;
                        break;


                    case CmdId.Event_Ok:
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Player_SetInventory, (ushort)CurrentSeqNr, new Inventory(){ [changes to be made] });
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Player_AddItem, (ushort)CurrentSeqNr, new IdItemStack(){ [changes to be made] });
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Player_SetCredits, (ushort)CurrentSeqNr, new IdCredits( [PlayerID], [Double] ));
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Player_AddCredits, (ushort)CurrentSeqNr, new IdCredits( [PlayerID], [+/- Double] ));
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Blueprint_Finish, (ushort)CurrentSeqNr, new Id( [PlayerID] ));
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Blueprint_Resources, (ushort)CurrentSeqNr, new BlueprintResources( [PlayerID], [List<ItemStack>], [bool ReplaceExisting?] ));
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Entity_Teleport, (ushort)CurrentSeqNr, new IdPositionRotation( [EntityId OR PlayerID], [Pvector3 Position], [Pvector3 Rotation] ));
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Entity_ChangePlayfield , (ushort)CurrentSeqNr, new IdPlayfieldPositionRotation( [EntityId OR PlayerID], [Playfield],  [Pvector3 Position], [Pvector3 Rotation] ));
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Entity_Destroy, (ushort)CurrentSeqNr, new Id( [EntityID] ));
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Entity_Destroy2, (ushort)CurrentSeqNr, new IdPlayfield( [EntityID], [Playfield] ));
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Entity_SetName, (ushort)CurrentSeqNr, new Id( [EntityID] )); Wait, what? This one doesn't make sense. This is what the Wiki says though.
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Entity_Spawn, (ushort)CurrentSeqNr, new EntitySpawnInfo()); Doesn't make sense to me.
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Structure_Touch, (ushort)CurrentSeqNr, new Id( [EntityID] ));
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_InGameMessage_SinglePlayer, (ushort)CurrentSeqNr, new IdMsgPrio( [int nId], [string nMsg], [byte nPrio], [float nTime] )); //for Prio: 0=Red, 1=Yellow, 2=Blue
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_InGameMessage_Faction, (ushort)CurrentSeqNr, new IdMsgPrio( [int nId], [string nMsg], [byte nPrio], [float nTime] )); //for Prio: 0=Red, 1=Yellow, 2=Blue
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_InGameMessage_AllPlayers, (ushort)CurrentSeqNr, new IdMsgPrio( [int nId], [string nMsg], [byte nPrio], [float nTime] )); //for Prio: 0=Red, 1=Yellow, 2=Blue
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_ConsoleCommand, (ushort)CurrentSeqNr, new PString( [Telnet Command] ));

                        //uh? Not Listed in Wiki... Received_ = ()data;
                        break;


                    case CmdId.Event_Error:
                        //Triggered when there is an error coming from the API
                        ErrorInfo Received_ErrorInfo = (ErrorInfo)data;
                        if (SeqNrStorage.Keys.Contains(seqNr))
                        {
                            CommonFunctions.LogFile("Debug.txt", "API Error:");
                            CommonFunctions.LogFile("Debug.txt", "ErrorType: " + Received_ErrorInfo.errorType);
                            CommonFunctions.LogFile("Debug.txt", "");
                        }
                        break;


                    case CmdId.Event_PdaStateChange:
                        //Triggered by PDA: chapter activated/deactivated/completed
                        PdaStateInfo Received_PdaStateChange = (PdaStateInfo)data;
                        break;


                    case CmdId.Event_ConsoleCommand:
                        //Triggered when a player uses a Console Command in-game
                        ConsoleCommandInfo Received_ConsoleCommandInfo = (ConsoleCommandInfo)data;
                        break;


                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                CommonFunctions.LogFile("Debug.txt", "Message: " + ex.Message);
                CommonFunctions.LogFile("Debug.txt", "Data: " + ex.Data);
                CommonFunctions.LogFile("Debug.txt", "HelpLink: " + ex.HelpLink);
                CommonFunctions.LogFile("Debug.txt", "InnerException: " + ex.InnerException);
                CommonFunctions.LogFile("Debug.txt", "Source: " + ex.Source);
                CommonFunctions.LogFile("Debug.txt", "StackTrace: " + ex.StackTrace);
                CommonFunctions.LogFile("Debug.txt", "TargetSite: " + ex.TargetSite);
            }
        }
        public void Game_Update()
        {
            UpdateCounter = UpdateCounter + 1;
            if (UpdateDictionary.Keys.Contains(UpdateCounter))
            {
                foreach (string SendableString in UpdateDictionary[UpdateCounter])
                {
                    CommonFunctions.LogFile("debug.txt", "Update = "+ SendableString);
                    API.ConsoleCommand(SendableString);
                }
            }

            LogonDelay++;
            if (LogonDelayDict.Keys.Contains(LogonDelay))
            {
                Storage.StorableData ThisFunction = new Storage.StorableData
                {
                    function = "Logon",
                    Match = Convert.ToString(LogonDelayDict[LogonDelay]),
                    Requested = "PlayerInfo"
                };
                thisSeqNr = API.PlayerInfo(LogonDelayDict[LogonDelay]);
                SeqNrStorage[thisSeqNr] = ThisFunction;

                
            }
            //Triggered whenever Empyrion experiences "Downtime", roughly 75-100 times per second
        }
        public void Game_Exit()
        {
            //Triggered when the server is Shutting down. Does NOT pause the shutdown.
            CommonFunctions.LogFile("Statistics.txt", ModVersion + "," + UsageReadItem + "," + UsageReadMLM + "," + UsageSendItem + "," + UsageSendLoc + "," + UsageSendMLM);
        }
    }
}