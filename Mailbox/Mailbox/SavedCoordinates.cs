using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
//using System.Threading.Tasks;
using YamlDotNet.Serialization;

namespace Mailbox
{
    class SavedCoordinates
    {
        public static Root Retrieve(String filePath)
        {
            using (var input = File.OpenText(filePath))
            { 
                var deserializer = new DeserializerBuilder()
                    .IgnoreUnmatchedProperties()
                    .Build();
                var Output = deserializer.Deserialize<Root>(input);
                return Output;
            }
        }

        public class Root
        {
            public List<Playfields> Playfield { get; set; }
        }

        public class Playfields
        {
            public string PlayfieldName { get; set; }
            public List<LocationData> Locations { get; set; }
        }

        public class LocationData
        {
            public int CoordX { get; set; }
            public int CoordY { get; set; }
            public int CoordZ { get; set; }
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
