using System.ComponentModel.DataAnnotations;

namespace Ambermoon.Data.GameDataRepository.Data
{
    using Serialization;

    public sealed class NpcData : CharacterData, IConversationCharacter, IIndexedData, IEquatable<NpcData>
    {
        private uint _age = 1;
        private uint _maxAge = 1;

        internal NpcData()
        {
        }

        public NpcData Copy()
        {
            return new(); // TODO
        }

        public override object Clone() => Copy();

        public bool Equals(NpcData? other)
        {
            if (other is null)
                return false;

            // TODO
            return false;
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
