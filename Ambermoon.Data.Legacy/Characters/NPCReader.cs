using Ambermoon.Data.Legacy.Serialization;

namespace Ambermoon.Data.Legacy.Characters
{
    public class NPCReader : CharacterReader, INPCReader
    {
        public void ReadNPC(NPC npc, IDataReader dataReader, IDataReader npcTextReader)
        {
            ReadCharacter(npc, dataReader);
            EventReader.ReadEvents(dataReader, npc.Events, npc.EventList);
            npc.Texts = TextReader.ReadTexts(npcTextReader);
        }
    }
}
