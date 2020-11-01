namespace Ambermoon.Data.Serialization
{
    public interface INPCReader
    {
        void ReadNPC(NPC npc, IDataReader dataReader, IDataReader npcTextReader);
    }
}
