namespace Ambermoon.Data
{
    public class NPC : Character
    {
        private NPC()
            : base(CharacterType.NPC)
        {

        }

        public static NPC Load(INPCReader npcReader, IDataReader dataReader)
        {
            var npc = new NPC();

            npcReader.ReadNPC(npc, dataReader);

            return npc;
        }
    }
}
