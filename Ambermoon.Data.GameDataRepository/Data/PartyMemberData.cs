using System.ComponentModel.DataAnnotations;

namespace Ambermoon.Data.GameDataRepository.Data
{
    using Ambermoon.Data.GameDataRepository.Collections;
    using Ambermoon.Data.GameDataRepository.Enumerations;
    using Ambermoon.Data.GameDataRepository.Util;
    using Serialization;
    using static Ambermoon.Data.Monster;

    public sealed class PartyMemberData : BattleCharacterData, IConversationCharacter, IIndexedData, IEquatable<PartyMemberData>, IImageProvidingData
    {

        #region Fields

        private uint _age = 1;
        private uint _maxAge = 1;
        private uint _numberOfOccupiedHands = 0;
        private uint _numberOfOccupiedFingers = 0;
        private Language _spokenLanguages = Language.None;
        private ExtendedLanguage _additionalSpokenLanguages = ExtendedLanguage.None;
        private bool _inventoryInaccessible = false;
        private uint _graphicIndex = 0;
        private uint _spellLearningPoints = 0;
        private uint _trainingPoints = 0;
        private uint _characterBitIndex = 0xffff;
        private uint _attacksPerRoundIncreaseLevels = 0;
        private uint _hitPointsPerLevel = 0;
        private uint _spellPointsPerLevel = 0;
        private uint _spellLearningPointsPerLevel = 0;
        private uint _trainingPointsPerLevel = 0;
        private uint _lookAtCharTextIndex = 0;
        private uint _experiencePoints = 0;
        private uint _markOfReturnX = 0;
        private uint _markOfReturnY = 0;
        private uint _markOfReturnMapIndex = 0;

        #endregion


        #region Properties

        public override CharacterType Type => CharacterType.PartyMember;

        [Range(1, ushort.MaxValue)]
        public uint Age
        {
            get => _age;
            set
            {
                ValueChecker.Check(value, 1, ushort.MaxValue);
                SetField(ref _age, value);
            }
        }

        [Range(1, ushort.MaxValue)]
        public uint MaxAge
        {
            get => _maxAge;
            set
            {
                ValueChecker.Check(value, 1, ushort.MaxValue);
                SetField(ref _maxAge, value);
            }
        }

        [Range(0, 2)]
        public uint NumberOfOccupiedHands
        {
            get => _numberOfOccupiedHands;
            set
            {
                ValueChecker.Check(value, 0, 2);
                SetField(ref _numberOfOccupiedHands, value);
            }
        }

        [Range(0, 2)]
        public uint NumberOfOccupiedFingers
        {
            get => _numberOfOccupiedFingers;
            set
            {
                ValueChecker.Check(value, 0, 2);
                SetField(ref _numberOfOccupiedFingers, value);
            }
        }

        public Language SpokenLanguages
        {
            get => _spokenLanguages;
            set => SetField(ref _spokenLanguages, value);
        }

        /// <summary>
        /// Advanced only (episode 4+).
        /// </summary>
        public ExtendedLanguage AdditionalSpokenLanguages
        {
            get => _additionalSpokenLanguages;
            set => SetField(ref _additionalSpokenLanguages, value);
        }

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
        public bool InventoryInaccessible
        {
            get => _inventoryInaccessible;
            set => SetField(ref _inventoryInaccessible, value);
        }

        [Range(0, 255)]
        public uint GraphicIndex
        {
            get => _graphicIndex;
            set
            {
                ValueChecker.Check(value, 0, 255);
                SetField(ref _graphicIndex, value);
            }
        }

        [Range(0, ushort.MaxValue)]
        public uint SpellLearningPoints
        {
            get => _spellLearningPoints;
            set
            {
                ValueChecker.Check(value, 0, ushort.MaxValue);
                SetField(ref _spellLearningPoints, value);
            }
        }

        [Range(0, ushort.MaxValue)]
        public uint TrainingPoints
        {
            get => _trainingPoints;
            set
            {
                ValueChecker.Check(value, 0, ushort.MaxValue);
                SetField(ref _trainingPoints, value);
            }
        }

        [Range(0, ushort.MaxValue)]
        public uint CharacterBitIndex
        {
            get => _characterBitIndex;
            set
            {
                ValueChecker.Check(value, 0, ushort.MaxValue);
                SetField(ref _characterBitIndex, value);
            }
        }

        [Range(0, ushort.MaxValue)]
        public uint AttacksPerRoundIncreaseLevels
        {
            get => _attacksPerRoundIncreaseLevels;
            set
            {
                ValueChecker.Check(value, 0, ushort.MaxValue);
                SetField(ref _attacksPerRoundIncreaseLevels, value);
            }
        }

        [Range(0, ushort.MaxValue)]
        public uint HitPointsPerLevel
        {
            get => _hitPointsPerLevel;
            set
            {
                ValueChecker.Check(value, 0, ushort.MaxValue);
                SetField(ref _hitPointsPerLevel, value);
            }
        }

        [Range(0, ushort.MaxValue)]
        public uint SpellPointsPerLevel
        {
            get => _spellPointsPerLevel;
            set
            {
                ValueChecker.Check(value, 0, ushort.MaxValue);
                SetField(ref _spellPointsPerLevel, value);
            }
        }

        [Range(0, ushort.MaxValue)]
        public uint SpellLearningPointsPerLevel
        {
            get => _spellLearningPointsPerLevel;
            set
            {
                ValueChecker.Check(value, 0, ushort.MaxValue);
                SetField(ref _spellLearningPointsPerLevel, value);
            }
        }

        [Range(0, ushort.MaxValue)]
        public uint TrainingPointsPerLevel
        {
            get => _trainingPointsPerLevel;
            set
            {
                ValueChecker.Check(value, 0, ushort.MaxValue);
                SetField(ref _trainingPointsPerLevel, value);
            }
        }

        [Range(0, ushort.MaxValue)]
        public uint LookAtCharTextIndex
        {
            get => _lookAtCharTextIndex;
            set
            {
                ValueChecker.Check(value, 0, ushort.MaxValue);
                SetField(ref _lookAtCharTextIndex, value);
            }
        }

        public uint ExperiencePoints
        {
            get => _experiencePoints;
            set => SetField(ref _experiencePoints, value);
        }

        [Range(0, ushort.MaxValue)]
        public uint MarkOfReturnX
        {
            get => _markOfReturnX;
            set
            {
                ValueChecker.Check(value, 0, ushort.MaxValue);
                SetField(ref _markOfReturnX, value);
            }
        }

        [Range(0, ushort.MaxValue)]
        public uint MarkOfReturnY
        {
            get => _markOfReturnY;
            set
            {
                ValueChecker.Check(value, 0, ushort.MaxValue);
                SetField(ref _markOfReturnY, value);
            }
        }

        [Range(0, ushort.MaxValue)]
        public uint MarkOfReturnMapIndex
        {
            get => _markOfReturnMapIndex;
            set
            {
                ValueChecker.Check(value, 0, ushort.MaxValue);
                SetField(ref _markOfReturnMapIndex, value);
            }
        }

        /// <summary>
        /// This is calculated from the carried items, gold and rations.
        /// </summary>
        public uint TotalWeight { get; private set; } = 0;

        #endregion


        #region Constructors

        public PartyMemberData()
        {
        }

        #endregion


        #region Serialization

        public void Serialize(IDataWriter dataWriter, IGameData gameData)
        {
            throw new NotImplementedException();
        }

        public void Serialize(IDataWriter dataWriter, int majorVersion, bool advanced)
        {
            throw new NotImplementedException();
        }

        public static IIndexedData Deserialize(IDataReader dataReader, uint index, int majorVersion, bool advanced)
        {
            var partyMemberData = (PartyMemberData)Deserialize(dataReader, majorVersion, advanced);
            (partyMemberData as IMutableIndex).Index = index;
            return partyMemberData;
        }

        public static IData Deserialize(IDataReader dataReader, int majorVersion, bool advanced)
        {
            if (dataReader.ReadByte() != (byte)CharacterType.PartyMember)
                throw new InvalidDataException("The given data is no valid party member data.");

            void SkipBytes(int amount) => dataReader.Position += amount;

            var partyMemberData = new PartyMemberData();

            partyMemberData.Gender = (Gender)dataReader.ReadByte();
            partyMemberData.Race = (Race)dataReader.ReadByte();
            partyMemberData.Class = (Class)dataReader.ReadByte();
            partyMemberData.SpellMastery = (SpellTypeMastery)dataReader.ReadByte();
            partyMemberData.Level = dataReader.ReadByte();
            partyMemberData.NumberOfOccupiedHands = dataReader.ReadByte();
            partyMemberData.NumberOfOccupiedFingers = dataReader.ReadByte();
            partyMemberData.SpokenLanguages = (Language)dataReader.ReadByte();
            partyMemberData.InventoryInaccessible = dataReader.ReadByte() != 0;
            partyMemberData.GraphicIndex = dataReader.ReadByte();
            SkipBytes(2);
            if (advanced && majorVersion >= 4)
            {
                partyMemberData.AdditionalSpokenLanguages = (ExtendedLanguage)dataReader.ReadByte();
            }
            else
            {
                SkipBytes(1);
            }
            SkipBytes(1);
            if (advanced)
            {

            }
            else
            {
                SkipBytes(1);
            }
            partyMemberData.SpellTypeImmunity = (SpellTypeImmunity)dataReader.ReadByte();
            partyMemberData.AttacksPerRound = dataReader.ReadByte();
            partyMemberData.BattleFlags = (BattleFlags)dataReader.ReadByte();
            partyMemberData.Element = (CharacterElement)dataReader.ReadByte();
            partyMemberData.SpellLearningPoints = dataReader.ReadWord();
            partyMemberData.TrainingPoints = dataReader.ReadWord();
            partyMemberData.Gold = dataReader.ReadWord();
            partyMemberData.Food = dataReader.ReadWord();
            partyMemberData.CharacterBitIndex = dataReader.ReadWord();
            partyMemberData.Conditions = (Condition)dataReader.ReadWord();
            SkipBytes(4);
            partyMemberData.MarkOfReturnX = dataReader.ReadWord();
            partyMemberData.MarkOfReturnY = dataReader.ReadWord();
            partyMemberData.MarkOfReturnMapIndex = dataReader.ReadWord();
            for (int i = 0; i < 8; i++)
            {
                var attribute = partyMemberData.Attributes[(Attribute)i];
                attribute.CurrentValue = dataReader.ReadWord();
                attribute.MaxValue = dataReader.ReadWord();
                attribute.BonusValue = dataReader.ReadSignedWord();
                attribute.StoredValue = dataReader.ReadWord();
            }
            partyMemberData.Age = dataReader.ReadWord();
            partyMemberData.MaxAge = dataReader.ReadWord();
            SkipBytes(4);
            if (advanced)
            {
                partyMemberData.BonusSpellDamage = dataReader.ReadWord();
                partyMemberData.BonusMaxSpellDamage = dataReader.ReadWord();
                partyMemberData.BonusSpellDamageReduction = dataReader.ReadSignedWord();
                partyMemberData.BonusSpellDamagePercentage = dataReader.ReadSignedWord();
            }
            else
            {
                SkipBytes(8);
            }
            for (int i = 0; i < 10; i++)
            {
                var skill = partyMemberData.Skills[(Skill)i];
                skill.CurrentValue = dataReader.ReadWord();
                skill.MaxValue = dataReader.ReadWord();
                skill.BonusValue = dataReader.ReadSignedWord();
                skill.StoredValue = dataReader.ReadWord();
            }
            var hitPoints = partyMemberData.HitPoints;
            hitPoints.CurrentValue = dataReader.ReadWord();
            hitPoints.MaxValue = dataReader.ReadWord();
            hitPoints.BonusValue = dataReader.ReadSignedWord();
            var spellPoints = partyMemberData.SpellPoints;
            spellPoints.CurrentValue = dataReader.ReadWord();
            spellPoints.MaxValue = dataReader.ReadWord();
            spellPoints.BonusValue = dataReader.ReadSignedWord();
            partyMemberData.BaseDefense = dataReader.ReadWord();
            int bonusDefense = dataReader.ReadSignedWord(); // TODO: check against equipment
            partyMemberData.BaseAttackDamage = dataReader.ReadWord();
            int bonusAttackDamage = dataReader.ReadSignedWord(); // TODO: check against equipment
            partyMemberData.MagicAttackLevel = dataReader.ReadSignedWord();
            partyMemberData.MagicDefenseLevel = dataReader.ReadSignedWord();
            SkipBytes(16);
            partyMemberData.LearnedSpellsHealing = dataReader.ReadDword();
            partyMemberData.LearnedSpellsAlchemistic = dataReader.ReadDword();
            partyMemberData.LearnedSpellsMystic = dataReader.ReadDword();
            partyMemberData.LearnedSpellsDestruction = dataReader.ReadDword();
            partyMemberData.LearnedSpellsType5 = dataReader.ReadDword();
            partyMemberData.LearnedSpellsType6 = dataReader.ReadDword();
            partyMemberData.LearnedSpellsFunctional = dataReader.ReadDword();
            SkipBytes(4);
            partyMemberData.Name = dataReader.ReadString(16).TrimEnd('\0', ' ');

            #region Equipment and Items
            partyMemberData.Equipment = DataCollection<ItemSlotData>.Deserialize(dataReader, EquipmentSlotCount, advanced);
            partyMemberData.Items = DataCollection<ItemSlotData>.Deserialize(dataReader, InventorySlotCount, advanced);
            partyMemberData.InitializeItemSlots();

            // TODO
            /*uint calculatedBonusDefense = Util.Util.CalculateItemPropertySum(monsterData._equipment, index => ItemManager.GetItem(index), item => item.Defense);
            uint calculatedBonusAttackDamage = Util.Util.CalculateItemPropertySum(monsterData._equipment, index => ItemManager.GetItem(index), item => item.Damage);
            if (bonusDefense != calculatedBonusDefense)
                throw new InvalidDataException("Invalid monster data. Wrong stored bonus defense.");
            if (bonusAttackDamage != calculatedBonusAttackDamage)
                throw new InvalidDataException("Invalid monster data. Wrong stored bonus attack damage.");*/
            #endregion

            #region Monster Display Data
            var allFrameIndices = dataReader.ReadBytes(256); // 8 * 32
            var frameCounts = dataReader.ReadBytes(8);
            for (int i = 0; i < 8; i++)
            {
                var animation = partyMemberData._animations[i] = new Animation();
                int count = animation.UsedAmount = frameCounts[i];
                animation.FrameIndices = count == 0
                    ? Array.Empty<byte>()
                    : allFrameIndices.Skip(i * 32).Take(count).ToArray();
            }
            SkipBytes(16); // Atari palette
            partyMemberData.CustomPalette = dataReader.ReadBytes(32);
            byte animationProgressions = dataReader.ReadByte();
            for (int i = 0; i < 8; i++)
            {
                partyMemberData._animationTypes[i] = (animationProgressions & (1 << i)) == 0
                    ? AnimationType.Cycle
                    : AnimationType.Wave;
            }
            SkipBytes(1); // padding byte
            partyMemberData.OriginalFrameWidth = dataReader.ReadWord();
            partyMemberData.OriginalFrameHeight = dataReader.ReadWord();
            partyMemberData.DisplayFrameWidth = dataReader.ReadWord();
            partyMemberData.DisplayFrameHeight = dataReader.ReadWord();
            #endregion

            return partyMemberData;
        }

        #endregion


        

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
                TotalWeight -= oldWeight;
                TotalWeight += newWeight;                
            }

            base.ItemSlotChanged(slot, oldIndex, newIndex, oldAmount, newAmount);
        }

        
        #region Equality

        public bool Equals(PartyMemberData? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return  base.Equals(other) &&
                    _age == other._age &&
                    _maxAge == other._maxAge &&
                    _numberOfOccupiedHands == other._numberOfOccupiedHands &&
                    _numberOfOccupiedFingers == other._numberOfOccupiedFingers &&
                    _spokenLanguages == other._spokenLanguages &&
                    _additionalSpokenLanguages == other._additionalSpokenLanguages &&
                    _inventoryInaccessible == other._inventoryInaccessible &&
                    _graphicIndex == other._graphicIndex &&
                    _spellLearningPoints == other._spellLearningPoints &&
                    _trainingPoints == other._trainingPoints &&
                    _characterBitIndex == other._characterBitIndex &&
                    _attacksPerRoundIncreaseLevels == other._attacksPerRoundIncreaseLevels &&
                    _hitPointsPerLevel == other._hitPointsPerLevel &&
                    _spellPointsPerLevel == other._spellPointsPerLevel &&
                    _spellLearningPointsPerLevel == other._spellLearningPointsPerLevel &&
                    _trainingPointsPerLevel == other._trainingPointsPerLevel &&
                    _lookAtCharTextIndex == other._lookAtCharTextIndex &&
                    _experiencePoints == other._experiencePoints &&
                    _markOfReturnX == other._markOfReturnX &&
                    _markOfReturnY == other._markOfReturnY &&
                    _markOfReturnMapIndex == other._markOfReturnMapIndex;
        }

        public override bool Equals(object? obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((PartyMemberData)obj);
        }

        public static bool operator ==(PartyMemberData? left, PartyMemberData? right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(PartyMemberData? left, PartyMemberData? right)
        {
            return !Equals(left, right);
        }

        #endregion


        #region Cloning

        public PartyMemberData Copy()
        {
            PartyMemberData copy = new()
            {
                Gender = Gender,
                Race = Race,
                Class = Class,
                SpellMastery = SpellMastery,
                Level = Level,
                NumberOfOccupiedHands = NumberOfOccupiedHands,
                NumberOfOccupiedFingers = NumberOfOccupiedFingers,
                SpokenLanguages = SpokenLanguages,
                InventoryInaccessible = InventoryInaccessible,
                GraphicIndex = GraphicIndex,
                AdditionalSpokenLanguages = AdditionalSpokenLanguages,
                MaxReachedLevel = MaxReachedLevel,
                SpellTypeImmunity = SpellTypeImmunity,
                AttacksPerRound = AttacksPerRound,
                BattleFlags = BattleFlags,
                Element = Element,
                SpellLearningPoints = SpellLearningPoints,
                TrainingPoints = TrainingPoints,
                CharacterBitIndex = CharacterBitIndex,
                Gold = Gold,
                Food = Food,
                Conditions = Conditions,
                MarkOfReturnX = MarkOfReturnX,
                MarkOfReturnY = MarkOfReturnY,
                MarkOfReturnMapIndex = MarkOfReturnMapIndex,
                Age = Age,
                MaxAge = MaxAge,
                BonusSpellDamage = BonusSpellDamage,
                BonusMaxSpellDamage = BonusMaxSpellDamage,
                BonusSpellDamageReduction = BonusSpellDamageReduction,
                BonusSpellDamagePercentage = BonusSpellDamagePercentage,
                BaseDefense = BaseDefense,
                BaseAttackDamage = BaseAttackDamage,
                MagicAttackLevel = MagicAttackLevel,
                MagicDefenseLevel = MagicDefenseLevel,
                LearnedSpellsHealing = LearnedSpellsHealing,
                LearnedSpellsAlchemistic = LearnedSpellsAlchemistic,
                LearnedSpellsMystic = LearnedSpellsMystic,
                LearnedSpellsDestruction = LearnedSpellsDestruction,
                LearnedSpellsType5 = LearnedSpellsType5,
                LearnedSpellsType6 = LearnedSpellsType6,
                LearnedSpellsFunctional = LearnedSpellsFunctional,
                Name = Name

                // TODO: Monster Display Data
            };

            for (int i = 0; i < 8; i++)
                copy.Attributes[(Attribute)i] = Util.Util.Copy(Attributes[(Attribute)i]);
            for (int i = 0; i < 10; i++)
                copy.Skills[(Skill)i] = Util.Util.Copy(Skills[(Skill)i]);
            copy.HitPoints.CurrentValue = HitPoints.CurrentValue;
            copy.HitPoints.MaxValue = HitPoints.MaxValue;
            copy.HitPoints.BonusValue = HitPoints.BonusValue;
            copy.SpellPoints.CurrentValue = SpellPoints.CurrentValue;
            copy.SpellPoints.MaxValue = SpellPoints.MaxValue;
            copy.SpellPoints.BonusValue = SpellPoints.BonusValue;

            copy.Equipment = Equipment.Copy();
            copy.Items = Items.Copy();

            (copy as IMutableIndex).Index = Index;

            return copy;
        }

        public override object Clone() => Copy();

        #endregion
    }
}
