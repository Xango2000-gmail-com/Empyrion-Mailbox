using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using YamlDotNet.Serialization;

namespace Mailbox
{
    class Mail
    {
        public static Root Read(String filePath)
        {
            using (var input = File.OpenText(filePath))
            {
                // var input = File.OpenText(filePath);
                var deserializer = new DeserializerBuilder()
                    .IgnoreUnmatchedProperties()
                    .Build();
                var Output = deserializer.Deserialize<Root>(input);
                return Output;
            }
        }

        public class MessageData
        {
            public string From { get; set; }
            public string Type { get; set; }
            public string ItemStacksFile { get; set; }
            public string Message { get; set; }
            public List<Setup.LocData> LocList { get; set; }
            public string Timestamp { get; set; }
        }

        public class Root
        {
            public List<MessageData> Unread { get; set; }
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
