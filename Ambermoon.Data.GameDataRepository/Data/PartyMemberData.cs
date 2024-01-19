using Ambermoon.Data.GameDataRepository.Util;
using Ambermoon.Data.Serialization;
using System.ComponentModel.DataAnnotations;

namespace Ambermoon.Data.GameDataRepository.Data
{
    public class PartyMemberData : BattleCharacterData, IConversationCharacter, IIndexedData
    {
        private uint _age = 1;
        private uint _maxAge = 1;

        public static PartyMemberData Create(DictionaryList<PartyMemberData> list, uint? index)
        {
            var partyMemberData = new PartyMemberData { Index = index ?? list.Keys.Max() + 1 };
            list.Add(partyMemberData);
            return partyMemberData;
        }

        public static IIndexedData Deserialize(IDataReader dataReader, uint index, bool advanced)
        {
            throw new NotImplementedException();
        }

        public static IData Deserialize(IDataReader dataReader, bool advanced)
        {
            throw new NotImplementedException();
        }

        public void Serialize(IDataWriter dataWriter, IGameData gameData)
        {
            throw new NotImplementedException();
        }

        public void Serialize(IDataWriter dataWriter, bool advanced)
        {
            throw new NotImplementedException();
        }

        public override CharacterType Type => CharacterType.PartyMember;
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
        public byte NumberOfFreeHands { get; set; }
        public byte NumberOfFreeFingers { get; set; }
        public Language SpokenLanguages { get; set; }
        /// <summary>
        /// If active the inventory (and also the stats page) can't be
        /// accessed during the game and a message will popup which
        /// says "X does not allow to look into his belongings.".
        /// 
        /// This is not bound to conditions like madness or fear,
        /// but is instead a manual "inventory is secret" flag.
        /// 
        /// This is actually never set in the original and in
        /// the advanced version only if Mystics transform themselves.
        /// </summary>
        public bool InventoryInaccessible { get; set; }
        public uint PortraitIndex { get; set; }
        public uint SpellLearningPoints { get; set; }
        public uint TrainingPoints { get; set; }
        public uint CharacterBitIndex { get; set; }
        public uint AttacksPerRoundIncreaseLevels { get; set; }
        public uint HitPointsPerLevel { get; set; }
        public uint SpellPointsPerLevel { get; set; }
        public uint SpellLearningPointsPerLevel { get; set; }
        public uint TrainingPointsPerLevel { get; set; }
        public uint LookAtCharTextIndex { get; set; }
        public uint ExperiencePoints { get; set; }
        /// <summary>
        /// This is calculated from the carried items.
        /// </summary>
        public uint TotalWeight { get; }
    }
}
