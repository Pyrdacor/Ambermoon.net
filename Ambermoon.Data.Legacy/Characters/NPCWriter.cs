using Ambermoon.Data.Legacy.Serialization;
using Ambermoon.Data.Serialization;

namespace Ambermoon.Data.Legacy.Characters
{
    public class NPCWriter : CharacterWriter
    {
        public void WriteNPC(NPC npc, IDataWriter dataWriter)
        {
            WriteCharacter(npc, dataWriter);
            EventWriter.WriteEvents(dataWriter, npc.Events, npc.EventList);
        }

        public void WriteNPC(NPC npc, IDataWriter dataWriter, IDataWriter npcTextWriter)
        {
            WriteNPC(npc, dataWriter);
            TextWriter.WriteTexts(npcTextWriter, npc.Texts);
        }
    }
}
