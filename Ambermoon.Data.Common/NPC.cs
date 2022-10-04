using Ambermoon.Data.Serialization;
using Ambermoon.Data.Serialization.Json;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace Ambermoon.Data
{
    [JsonConverter(typeof(EventProviderConverter<NPC>))]
    public class NPC : Character, IConversationPartner, IEventProvider
    {
        public List<string> Texts { get; set; }
        [JsonProperty(ItemConverterType = typeof(EventConverter))]
        public List<Event> Events { get; } = new List<Event>();
        [JsonIgnore]
        public List<Event> EventList { get; } = new List<Event>();

        public NPC()
            : base(CharacterType.NPC)
        {

        }

        public static NPC Load(uint index, INPCReader npcReader, IDataReader dataReader, IDataReader npcTextReader)
        {
            var npc = new NPC
            {
                Index = index
            };

            npcReader.ReadNPC(npc, dataReader, npcTextReader);

            return npc;
        }
    }
}
