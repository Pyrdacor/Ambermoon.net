using System.ComponentModel.DataAnnotations;

namespace Ambermoon.Data.GameDataRepository.Data
{
    using Serialization;

    public sealed class PartyMemberData : BattleCharacterData, IConversationCharacter, IIndexedData, IEquatable<PartyMemberData>, IImageProvidingData
    {
        private uint _age = 1;
        private uint _maxAge = 1;

        public PartyMemberData()
        {
        }

        public PartyMemberData Copy()
        {
            return new(); // TODO
        }

        public override object Clone() => Copy();

        public bool Equals(PartyMemberData? other)
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
        public uint GraphicIndex { get; set; }
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
        /// This is calculated from the carried items, gold and rations.
        /// </summary>
        public uint TotalWeight { get; private set; } = 0;

        private protected override void ItemSlotChanged(int slot,
            uint? oldIndex,
            uint? newIndex,
            uint? oldAmount = null,
            uint? newAmount = null)
        {
            newIndex ??= Items[slot].ItemIndex;
            oldIndex ??= newIndex;
            newAmount ??= Items[slot].Amount;
            oldAmount ??= newAmount;

            if (newIndex is 0)
            {
                if (oldIndex is 0)
                    return;

                var oldItem = FindItem(oldIndex.Value);

                uint oldWeight = oldAmount.Value * (oldItem?.Weight ?? 0);
                TotalWeight -= oldWeight;
            }
            else
            {
                var newItem = FindItem(newIndex.Value);
                var oldItem = oldIndex.Value is 0 ? null : FindItem(oldIndex.Value);

                uint oldWeight = oldAmount.Value * (oldItem?.Weight ?? 0);
                uint newWeight = newAmount.Value * (newItem?.Weight ?? 0);
                TotalWeight += newWeight;
                TotalWeight -= oldWeight;
            }

            base.ItemSlotChanged(slot, oldIndex, newIndex, oldAmount, newAmount);
        }
    }
}
