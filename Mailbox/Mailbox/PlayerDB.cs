using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using YamlDotNet.Serialization;

namespace Mailbox
{
    class PlayerYamlDB
    {
        public static Root Read(String filePath)
        {
            var input = File.OpenText(filePath);
            var deserializer = new DeserializerBuilder()
                .IgnoreUnmatchedProperties()
                .Build();
            var Output = deserializer.Deserialize<Root>(input);
            return Output;
        }

        public class PlayerData
        {
            public string PlayerName { get; set; }
            public string SteamID { get; set; }
            public string EmpyrionID { get; set; }
            public string ClientID { get; set; }
            public string Playfield { get; set; }
        }

        public class Root
        {
            public List<PlayerData> Database { get; set; }
        }

        public static void Write(string Path, Root ConfigData)
        {
            File.WriteAllText(Path, "---\r\n");
            Serializer serializer = new SerializerBuilder()
                .Build();
            string WriteThis = serializer.Serialize(ConfigData);
            File.AppendAllText(Path, WriteThis);
        }
    }
}
