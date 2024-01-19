using Ambermoon.Data.Serialization;

namespace Ambermoon.Data.GameDataRepository.Data
{
    public class PartyMemberData : BattleCharacterData, IConversationCharacter, IIndexedData
    {
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
