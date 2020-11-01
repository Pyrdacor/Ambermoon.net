using Ambermoon.Data.Serialization;
using System.Collections.Generic;

namespace Ambermoon.Data
{
    public class NPC : Character, IConversationPartner
    {
        public List<string> Texts { get; set; }
        public List<Event> Events { get; } = new List<Event>();
        public List<Event> EventList { get; } = new List<Event>();

        private NPC()
            : base(CharacterType.NPC)
        {

        }

        public static NPC Load(INPCReader npcReader, IDataReader dataReader, IDataReader npcTextReader)
        {
            var npc = new NPC();

            npcReader.ReadNPC(npc, dataReader, npcTextReader);

            return npc;
        }
    }
}
