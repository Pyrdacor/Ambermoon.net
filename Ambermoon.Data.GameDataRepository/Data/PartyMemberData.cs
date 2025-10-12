using System.ComponentModel.DataAnnotations;

namespace Ambermoon.Data.GameDataRepository.Data;

using System.Reflection;
using Ambermoon.Data.GameDataRepository.Enumerations;
using Collections;
using Serialization;
using Util;

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

    /// <summary>
    /// Calculated from equipped items.
    /// </summary>
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

    /// <summary>
    /// Calculated from equipped items.
    /// </summary>
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


    #region Methods

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

    public void EnsureCorrectCalculatedValues(GameDataRepository gameDataRepository)
    {
        BonusDefense = (short)Math.Clamp
        (
            Util.CalculateItemPropertySum(Equipment,
                index => gameDataRepository.Items[index],
                (item, slotFlags) => slotFlags.HasFlag(ItemSlotFlags.Cursed) ? -(long)item.Defense : item.Defense) +
                Attributes[Attribute.Stamina].TotalCurrentValue / 25,
            short.MinValue,
            short.MaxValue
        );

        BonusAttackDamage = (short)Math.Clamp
        (
            Util.CalculateItemPropertySum(Equipment,
                index => gameDataRepository.Items[index],
                (item, slotFlags) => slotFlags.HasFlag(ItemSlotFlags.Cursed) ? -(long)item.Damage : item.Damage) +
                Attributes[Attribute.Strength].TotalCurrentValue / 25,
            short.MinValue,
            short.MaxValue
        );

        TotalWeight = (uint)Math.Clamp
        (
            Util.CalculateItemPropertySum(Items,
                index => gameDataRepository.Items[index],
                (item, _) => item.Weight) +
                Gold * gameDataRepository.GoldWeight +
                Food * gameDataRepository.FoodWeight,
            uint.MinValue,
            uint.MaxValue
        );

        MagicAttackLevel = (short)Math.Clamp
        (
            Util.CalculateItemPropertySum(Equipment,
                index => gameDataRepository.Items[index],
                (item, _) => item.MagicAttackLevel),
            0,
            short.MaxValue
        );

        MagicDefenseLevel = (short)Math.Clamp
        (
            Util.CalculateItemPropertySum(Equipment,
                index => gameDataRepository.Items[index],
                (item, _) => item.MagicDefenseLevel),
            0,
            short.MaxValue
        );

        for (int i = 0; i < 8; i++)
        {
            var attributeType = (Attribute)i;
            var attribute = Attributes[attributeType];
            attribute.BonusValue = (short)Math.Clamp
            (
                Util.CalculateItemPropertySum(Equipment,
                    index => gameDataRepository.Items[index],
                    (item, slotFlags) => item.Attribute == attributeType && item.AttributeValue != 0 ? (slotFlags.HasFlag(ItemSlotFlags.Cursed) ? -(long)item.AttributeValue : item.AttributeValue) : 0),
                short.MinValue,
                short.MaxValue
            );
        }

        for (int i = 0; i < 10; i++)
        {
            var skillType = (Skill)i;
            var skill = Skills[skillType];
            skill.BonusValue = (short)Math.Clamp
            (
                Util.CalculateItemPropertySum(Equipment,
                    index => gameDataRepository.Items[index],
                    (item, slotFlags) => item.Skill == skillType && item.SkillValue != 0 ? (slotFlags.HasFlag(ItemSlotFlags.Cursed) ? -(long)item.SkillValue : item.SkillValue) : 0),
                short.MinValue,
                short.MaxValue
            );
        }

        HitPoints.BonusValue = (short)Math.Clamp
        (
            Util.CalculateItemPropertySum(Equipment,
                index => gameDataRepository.Items[index],
                (item, slotFlags) => slotFlags.HasFlag(ItemSlotFlags.Cursed) ? -(long)item.HitPoints : item.HitPoints),
            short.MinValue,
            short.MaxValue
        );

        SpellPoints.BonusValue = (short)Math.Clamp
        (
            Util.CalculateItemPropertySum(Equipment,
                index => gameDataRepository.Items[index],
                (item, slotFlags) => slotFlags.HasFlag(ItemSlotFlags.Cursed) ? -(long)item.SpellPoints : item.SpellPoints),
            short.MinValue,
            short.MaxValue
        );

        var rightHand = GetEquipmentSlot(EquipmentSlot.RightHand);

        if (rightHand.Amount == 0 || rightHand.ItemIndex == 0)
            NumberOfOccupiedHands = GetEquipmentSlot(EquipmentSlot.LeftHand).Amount == 0 ? 0u : 1u;
        else if (gameDataRepository.Items[rightHand.ItemIndex].NumberOfHands == 2)
            NumberOfOccupiedHands = 2;
        else
            NumberOfOccupiedHands = GetEquipmentSlot(EquipmentSlot.LeftHand).Amount == 0 ? 1u : 2u;

        var rightFinger = GetEquipmentSlot(EquipmentSlot.RightFinger);

        if (rightFinger.Amount == 0 || rightFinger.ItemIndex == 0)
            NumberOfOccupiedFingers = GetEquipmentSlot(EquipmentSlot.LeftFinger).Amount == 0 ? 0u : 1u;
        else
            NumberOfOccupiedFingers = 1 + (GetEquipmentSlot(EquipmentSlot.LeftFinger).Amount == 0 ? 0u : 1u);
    }

    #endregion


    #region Serialization

    public void Serialize(IDataWriter dataWriter, int majorVersion, bool advanced)
    {
        void WriteFillBytes(int count)
        {
            for (int i = 0; i < count; i++)
                dataWriter.Write(0);
        }

        dataWriter.Write((byte)Type);
        dataWriter.Write((byte)Gender);
        dataWriter.Write((byte)Race);
        dataWriter.Write((byte)Class);
        dataWriter.Write((byte)SpellMastery);
        dataWriter.Write((byte)Level);
        dataWriter.Write((byte)NumberOfOccupiedHands);
        dataWriter.Write((byte)NumberOfOccupiedFingers);
        dataWriter.Write((byte)SpokenLanguages);
        dataWriter.Write((byte)(InventoryInaccessible ? 1 : 0));
        dataWriter.Write((byte)GraphicIndex);
        WriteFillBytes(2);
        if (advanced && majorVersion >= 4)
        {
            dataWriter.Write((byte)AdditionalSpokenLanguages);
        }
        else
        {
            WriteFillBytes(1);
        }
        WriteFillBytes(1);
        if (advanced)
        {
            dataWriter.Write((byte)MaxReachedLevel);
        }
        else
        {
            WriteFillBytes(1);
        }
        dataWriter.Write((byte)SpellTypeImmunity);
        dataWriter.Write((byte)AttacksPerRound);
        var battleFlags = (byte)((byte)BattleFlags & 0xf0);
        if (!advanced)
            battleFlags = 0;
        dataWriter.Write(battleFlags);
        if (advanced)
            dataWriter.Write((byte)Element);
        else
            WriteFillBytes(1);
        dataWriter.Write((ushort)SpellLearningPoints);
        dataWriter.Write((ushort)TrainingPoints);
        dataWriter.Write((ushort)Gold);
        dataWriter.Write((ushort)Food);
        dataWriter.Write((ushort)CharacterBitIndex);
        dataWriter.Write((ushort)Conditions);
        WriteFillBytes(4);
        dataWriter.Write((ushort)MarkOfReturnX);
        dataWriter.Write((ushort)MarkOfReturnY);
        dataWriter.Write((ushort)MarkOfReturnMapIndex);
        for (int i = 0; i < 8; i++)
        {
            var attribute = Attributes[(Attribute)i];
            dataWriter.Write((ushort)attribute.CurrentValue);
            dataWriter.Write((ushort)attribute.MaxValue);
            dataWriter.WriteSignedWord(attribute.BonusValue);
            dataWriter.Write((ushort)attribute.StoredValue);
        }
        dataWriter.Write((ushort)Age);
        dataWriter.Write((ushort)MaxAge);
        WriteFillBytes(4);
        if (advanced)
        {
            dataWriter.Write((ushort)BonusSpellDamage);
            dataWriter.Write((ushort)BonusMaxSpellDamage);
            dataWriter.WriteSignedWord(BonusSpellDamageReduction);
            dataWriter.WriteSignedWord(BonusSpellDamagePercentage);
        }
        else
        {
            WriteFillBytes(8);
        }
        for (int i = 0; i < 10; i++)
        {
            var skill = Skills[(Skill)i];
            dataWriter.Write((ushort)skill.CurrentValue);
            dataWriter.Write((ushort)skill.MaxValue);
            dataWriter.WriteSignedWord(skill.BonusValue);
            dataWriter.Write((ushort)skill.StoredValue);
        }
        var hitPoints = HitPoints;
        dataWriter.Write((ushort)hitPoints.CurrentValue);
        dataWriter.Write((ushort)hitPoints.MaxValue);
        dataWriter.WriteSignedWord(hitPoints.BonusValue);
        var spellPoints = SpellPoints;
        dataWriter.Write((ushort)spellPoints.CurrentValue);
        dataWriter.Write((ushort)spellPoints.MaxValue);
        dataWriter.WriteSignedWord(spellPoints.BonusValue);
        dataWriter.Write((ushort)BaseDefense);
        dataWriter.WriteSignedWord(BonusDefense);
        dataWriter.Write((ushort)BaseAttackDamage);
        dataWriter.WriteSignedWord(BonusAttackDamage);
        dataWriter.WriteSignedWord(MagicAttackLevel);
        dataWriter.WriteSignedWord(MagicDefenseLevel);
        dataWriter.Write((ushort)AttacksPerRoundIncreaseLevels);
        dataWriter.Write((ushort)HitPointsPerLevel);
        dataWriter.Write((ushort)SpellPointsPerLevel);
        dataWriter.Write((ushort)SpellLearningPointsPerLevel);
        dataWriter.Write((ushort)TrainingPointsPerLevel);
        dataWriter.Write((ushort)LookAtCharTextIndex);
        dataWriter.Write(ExperiencePoints);
        dataWriter.Write(LearnedSpellsHealing);
        dataWriter.Write(LearnedSpellsAlchemistic);
        dataWriter.Write(LearnedSpellsMystic);
        dataWriter.Write(LearnedSpellsDestruction);
        dataWriter.Write(LearnedSpellsType5);
        dataWriter.Write(LearnedSpellsType6);
        dataWriter.Write(LearnedSpellsFunctional);
        dataWriter.Write(TotalWeight);
        string name = Name;
        if (name.Length > 15)
            name = name[..15];
        dataWriter.WriteWithoutLength(name.PadRight(16, '\0'));

        #region Equipment and Items
        Equipment.Serialize(dataWriter, majorVersion, advanced);
        Items.Serialize(dataWriter, majorVersion, advanced);
        #endregion
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
        if (advanced)
            partyMemberData.BattleFlags = (BattleFlags)(dataReader.ReadByte() & 0xf0);
        else
            SkipBytes(1);
        if (advanced)
            partyMemberData.Element = (CharacterElement)dataReader.ReadByte();
        else
            SkipBytes(1);
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
        partyMemberData.BonusDefense = dataReader.ReadSignedWord();
        partyMemberData.BaseAttackDamage = dataReader.ReadWord();
        partyMemberData.BonusAttackDamage = dataReader.ReadSignedWord();
        partyMemberData.MagicAttackLevel = dataReader.ReadSignedWord();
        partyMemberData.MagicDefenseLevel = dataReader.ReadSignedWord();
        partyMemberData.AttacksPerRoundIncreaseLevels = dataReader.ReadWord();
        partyMemberData.HitPointsPerLevel = dataReader.ReadWord();
        partyMemberData.SpellPointsPerLevel = dataReader.ReadWord();
        partyMemberData.SpellLearningPointsPerLevel = dataReader.ReadWord();
        partyMemberData.TrainingPointsPerLevel = dataReader.ReadWord();
        partyMemberData.LookAtCharTextIndex = dataReader.ReadWord();
        partyMemberData.ExperiencePoints = dataReader.ReadDword();
        partyMemberData.LearnedSpellsHealing = dataReader.ReadDword();
        partyMemberData.LearnedSpellsAlchemistic = dataReader.ReadDword();
        partyMemberData.LearnedSpellsMystic = dataReader.ReadDword();
        partyMemberData.LearnedSpellsDestruction = dataReader.ReadDword();
        partyMemberData.LearnedSpellsType5 = dataReader.ReadDword();
        partyMemberData.LearnedSpellsType6 = dataReader.ReadDword();
        partyMemberData.LearnedSpellsFunctional = dataReader.ReadDword();
        partyMemberData.TotalWeight = dataReader.ReadDword();
        partyMemberData.Name = dataReader.ReadString(16).TrimEnd('\0', ' ');

        #region Equipment and Items
        partyMemberData.Equipment = DataCollection<ItemSlotData>.Deserialize(dataReader, EquipmentSlotCount, majorVersion, advanced);
        partyMemberData.Items = DataCollection<ItemSlotData>.Deserialize(dataReader, InventorySlotCount, majorVersion, advanced);
        partyMemberData.InitializeItemSlots();
        #endregion

        // TODO: Events

        return partyMemberData;
    }

    #endregion

    
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

    public override int GetHashCode() => base.GetHashCode();

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
            BonusDefense = BonusDefense,
            BaseAttackDamage = BaseAttackDamage,
            BonusAttackDamage = BonusAttackDamage,
            MagicAttackLevel = MagicAttackLevel,
            MagicDefenseLevel = MagicDefenseLevel,
            AttacksPerRoundIncreaseLevels = AttacksPerRoundIncreaseLevels,
            HitPointsPerLevel = HitPointsPerLevel,
            SpellPointsPerLevel = SpellPointsPerLevel,
            SpellLearningPointsPerLevel = SpellLearningPointsPerLevel,
            TrainingPointsPerLevel = TrainingPointsPerLevel,
            LookAtCharTextIndex = LookAtCharTextIndex,
            ExperiencePoints = ExperiencePoints,
            LearnedSpellsHealing = LearnedSpellsHealing,
            LearnedSpellsAlchemistic = LearnedSpellsAlchemistic,
            LearnedSpellsMystic = LearnedSpellsMystic,
            LearnedSpellsDestruction = LearnedSpellsDestruction,
            LearnedSpellsType5 = LearnedSpellsType5,
            LearnedSpellsType6 = LearnedSpellsType6,
            LearnedSpellsFunctional = LearnedSpellsFunctional,
            TotalWeight = TotalWeight,
            Name = Name
        };

        for (int i = 0; i < 8; i++)
            copy.Attributes[(Attribute)i] = Util.Copy(Attributes[(Attribute)i]);
        for (int i = 0; i < 10; i++)
            copy.Skills[(Skill)i] = Util.Copy(Skills[(Skill)i]);
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
