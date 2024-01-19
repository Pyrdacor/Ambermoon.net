using Ambermoon.Data.GameDataRepository.Util;
using Ambermoon.Data.Serialization;
using System.ComponentModel.DataAnnotations;

namespace Ambermoon.Data.GameDataRepository.Data
{
    public class NpcData : CharacterData, IConversationCharacter, IIndexedData
    {
        private uint _age = 1;
        private uint _maxAge = 1;

        public static NpcData Create(DictionaryList<NpcData> list, uint? index)
        {
            var npcData = new NpcData { Index = index ?? list.Keys.Max() + 1 };
            list.Add(npcData);
            return npcData;
        }

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
        [Range(1, ushort.MaxValue)]
        public uint Age
        {
            get => _age;
            set
            {
                if (value == 0 || value > ushort.MaxValue)
                    throw new ArgumentOutOfRangeException(nameof(Age), $"Age is limited to the range 1 to {ushort.MaxValue}.");

                _age = value;
            }
        }
        [Range(1, ushort.MaxValue)]
        public uint MaxAge
        {
            get => _maxAge;
            set
            {
                if (value == 0 || value > ushort.MaxValue)
                    throw new ArgumentOutOfRangeException(nameof(MaxAge), $"Max age is limited to the range 1 to {ushort.MaxValue}.");

                _maxAge = value;
            }
        }
        public Language SpokenLanguages { get; set; }
        public uint PortraitIndex { get; set; }
        public uint LookAtCharTextIndex { get; set; }

        // TODO: events
    }
}
