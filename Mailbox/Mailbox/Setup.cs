using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
//using System.Threading.Tasks;
using YamlDotNet.Serialization;

namespace Mailbox
{
    class Setup
    {
        public static Root Retrieve(String filePath)
        {
            var input = File.OpenText(filePath);
            var deserializer = new DeserializerBuilder()
                .IgnoreUnmatchedProperties()
                .Build();
            var Output = deserializer.Deserialize<Root>(input);
            return Output;
        }

        public class Root
        {
            public GeneralSettings General { get; set; }
            public Settings SendItems { get; set; }
            public Settings MultiLineMessage { get; set; }
            public Settings AdminMail { get; set; }
            public Settings RewardMail { get; set; }
            public Settings News { get; set; }
            public Settings MailToFaction { get; set; }
            public Settings TheVoid { get; set; }
            public Settings ReadMail { get; set; }
        }

        public class GeneralSettings
        {
            public string DefaultPrefix { get; set; }
            public string ReinitializeCommand { get; set; }
            public List<string> RestrictedPlayfields { get; set; }
        }

        public class CostSetup
        {
            public string Type { get; set; }
            public string ItemID { get; set; }
            public int Quantity { get; set; }
        }

        public class LocData
        {
            public float X { get; set; }
            public float y { get; set; }
            public float z { get; set; }
            public string Playfield { get; set; }
        }

        public class Settings
        {
            public bool Enabled { get; set; }
            public bool AllowSecret { get; set; }
            public bool AllowFactionChat { get; set; }
            public bool AllowGlobalChat { get; set; }
            public bool SuperStack { get; set; }
            public string Command { get; set; }
            public string Items { get; set; }
            public List<CostSetup> Cost { get; set; }
            public string New { get; set; }
            public string AddRecipient { get; set; }
            public string AddLine { get; set; }
            public string Location { get; set; }
            public string Clear { get; set; }
            public string Send { get; set; }
            public string Read { get; set; }
            public string RecipientKeyword { get; set; }
            public string ReleaseDate { get; set; }
            public string UseFile { get; set; }
            public string FileAs { get; set; }
            public string OnlineOnly { get; set; }
            public string SetMaxLevel { get; set; }
            public string DefaultMaxLevel { get; set; }
        }

        public static void WriteYaml(string Path, Root ConfigData)
        {
            File.WriteAllText(Path, "---\r\n");
            Serializer serializer = new SerializerBuilder()
                .Build();
            string WriteThis = serializer.Serialize(ConfigData);
            File.AppendAllText(Path, WriteThis);

        }
    }
}
