using System.Collections.Generic;

namespace Ambermoon.Data
{
    public class NPC : Character
    {
        public List<string> Texts { get; set; }

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
