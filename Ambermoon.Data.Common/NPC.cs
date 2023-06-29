using Ambermoon.Data.Serialization;
using System.Collections.Generic;

namespace Ambermoon.Data
{
    public class NPC : Character, IConversationPartner
    {
        public int LookAtTextIndex => LookAtCharTextIndex;
        public List<string> Texts { get; set; }
        public List<Event> Events { get; } = new List<Event>();
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
