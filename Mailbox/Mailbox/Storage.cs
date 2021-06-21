using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Eleon.Modding;

namespace Mailbox
{
    class Storage
    {
        internal static ModGameAPI GameAPI;
        public static int CurrentSeqNr = 500;

        
        internal class PlayerIDs
        {
            public string EmpyrionID;
            public string SteamID;
            public string ClientID;
            public string PlayerName;
        }

        internal class ScriptData
        {
            public string CurrentLine;
            public List<string> Nesting;
            public string[] Script;
            public List<string> Waiting;
            public DataTypes Variables;
            public StorableData EventData;
            public Dictionary<int, PlayerIDs> PlayerDB;
            public ushort seqNr;
        }
        internal class DataTypes
        {
            public Dictionary<string, string> Strings;
            public Dictionary<string, int> Ints;
            public Dictionary<string, ItemStack[]> ItemStackArrays;
            public Dictionary<string, PlayerIDs> Players;
        }
        internal class StorableData
        {
            public string Requested;
            public string Match;
            public string function;
            public PlayerInfo TriggerPlayer;
            public List<PlayerInfo> TargetPlayer;
            public ChatInfo ChatInfo;
            public IdStructureBlockInfo StructureBlockInfo;
            public PlayfieldEntityList PlayfieldEntities;
            public ConsoleCommandInfo ConsoleCommand;
            public Id PlayerConnected;
            public Id PlayerDisconnected;
            public IdList PlayerList;
            public ItemExchangeInfo ItemExchange;
            public IdCredits PlayerCredits;
            public IdPlayfield PlayfieldChange;
            public PlayfieldList PlayfieldList;
            public PlayfieldStats PlayfieldStats;
            public PlayfieldLoad PlayfieldLoad;
            public PlayfieldLoad PlayfieldUnload;
            public DediStats DediStats;
            public GameEventData GameEvent;
            public GlobalStructureList GlobalStructsList;
            public IdPositionRotation EntityPosRot;
            public FactionChangeInfo FactionChange;
            public FactionInfoList GetFactions;
            public StatisticsParam EventStatistics;
            public Id NewEntityId;
            public string OK;
            public ErrorInfo ErrorInfo;
            public Id PlayerDisconnectedWaiting;
            public IdStructureBlockInfo StructureBlockStatistics;
            public AlliancesTable AlliancesAll;
            public AlliancesFaction AlliancesFaction;
            public BannedPlayerData BannedPlayers;
            public TraderNPCItemSoldInfo TraderNPCItemSold;
            public Inventory PlayerGetAndRemoveInventory;
            public PdaStateChange PdaStateChange;
            public PlayfieldEntityList PlayfieldEntityList;
            public DialogBoxData DialogButtonIndex;
            public ChatData chatdata;
            public Mail.Root MailData;
        }

        internal class ChatData
        {
            public string Command { get; set; }
            public List<PlayerYamlDB.PlayerData> Player { get; set; }
            public string Message { get; set; }
        }

        internal class MLM
        {
            public List<string> Recipient { get; set; }
            public List<string> Lines { get; set; }
            public List<Setup.LocData> Location { get; set; }

        }
    }
}

