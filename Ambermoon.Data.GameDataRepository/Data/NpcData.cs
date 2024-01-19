using Ambermoon.Data.Serialization;

namespace Ambermoon.Data.GameDataRepository.Data
{
    public class NpcData : CharacterData, IConversationCharacter, IIndexedData
    {
        public static IIndexedData Deserialize(IDataReader dataReader, uint index, bool advanced)
        {
            throw new NotImplementedException();
        }

        public static IData Deserialize(IDataReader dataReader, bool advanced)
        {
            throw new NotImplementedException();
        }

        public void Serialize(IDataWriter dataWriter, bool advanced)
        {
            throw new NotImplementedException();
        }

        public override CharacterType Type => CharacterType.NPC;
        public Language SpokenLanguages { get; set; }
        public uint PortraitIndex { get; set; }
        public uint LookAtCharTextIndex { get; set; }

        // TODO: events
    }
}
