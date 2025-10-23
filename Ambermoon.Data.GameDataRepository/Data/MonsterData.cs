﻿using System.ComponentModel.DataAnnotations;

namespace Ambermoon.Data.GameDataRepository.Data;

using Collections;
using Enumerations;
using Serialization;
using Util;
using static Monster;

public enum MonsterAnimation
{
    /// <summary>
    /// Played when the monster is standing (and moving).
    /// </summary>
    Stand,
    /// <summary>
    /// Played when the monster attacks with a close-ranged weapon.
    /// </summary>
    ShortRangeAttack,
    /// <summary>
    /// Played when the monster attacks with a long-ranged weapon.
    /// </summary>
    LongRangeAttack,
    /// <summary>
    /// Played when the monster casts a spell.
    /// </summary>
    SpellCast,
    /// <summary>
    /// Played when the monster receives damage.
    /// </summary>
    ReceiveDamage,
    /// <summary>
    /// Played just before the monster dies.
    /// </summary>
    Die,
    /// <summary>
    /// Played when the monster appears (like Nera or Ancient Guard transformations).
    /// </summary>
    Appear,
    /// <summary>
    /// Not used in Ambermoon. And technically can't be used by the original code. So never use it!
    /// </summary>
    Invalid
}

public sealed class MonsterData : BattleCharacterData, IIndexedData, IEquatable<MonsterData>, IImageProvidingData
{

    #region Fields

    private uint _morale = 0;
    private uint _defeatExperience = 0;
    private AdvancedMonsterFlags _attackImmunity = AdvancedMonsterFlags.None;
    private uint _graphicIndex = 0;
    private uint _originalFrameWidth = 0;
    private uint _originalFrameHeight = 0;
    private uint _displayFrameWidth = 0;
    private uint _displayFrameHeight = 0;
    private readonly Animation[] _animations = new Animation[8];
    private readonly AnimationType[] _animationTypes = new AnimationType[8];

    #endregion


    #region Properties

    public override CharacterType Type => CharacterType.Monster;

    [Range(1, byte.MaxValue)]
    public uint GraphicIndex
    {
        get => _graphicIndex;
        set
        {
            ValueChecker.Check(value, 1, byte.MaxValue);
            SetField(ref _graphicIndex, value);
        }
    }

    [Range(0, 100)]
    public uint Morale
    {
        get => _morale;
        set
        {
            ValueChecker.Check(value, 0, 100);
            SetField(ref _morale, value);
        }
    }

    [Range(0, ushort.MaxValue)]
    public uint DefeatExperience
    {
        get => _defeatExperience;
        set
        {
            ValueChecker.Check(value, 0, ushort.MaxValue);
            SetField(ref _defeatExperience, value);
        }
    }

    public AdvancedMonsterFlags AttackImmunity
    {
        get => _attackImmunity;
        set => SetField(ref _attackImmunity, value);
    }

    public byte[] CustomPalette { get; set; } = new byte[32];

    [Range(16, ushort.MaxValue)]
    public uint OriginalFrameWidth
    {
        get => _originalFrameWidth;
        set
        {
            ValueChecker.Check(value, 16, ushort.MaxValue);
            SetField(ref _originalFrameWidth, value);
        }
    }

    [Range(16, ushort.MaxValue)]
    public uint OriginalFrameHeight
    {
        get => _originalFrameHeight;
        set
        {
            ValueChecker.Check(value, 16, ushort.MaxValue);
            SetField(ref _originalFrameHeight, value);
        }
    }

    [Range(16, ushort.MaxValue)]
    public uint DisplayFrameWidth
    {
        get => _displayFrameWidth;
        set
        {
            ValueChecker.Check(value, 16, ushort.MaxValue);
            SetField(ref _displayFrameWidth, value);
        }
    }

    [Range(16, ushort.MaxValue)]
    public uint DisplayFrameHeight
    {
        get => _displayFrameHeight;
        set
        {
            ValueChecker.Check(value, 16, ushort.MaxValue);
            SetField(ref _displayFrameHeight, value);
        }
    }

    #endregion


    #region Constructors

    public MonsterData()
    {
        for (int i = 0; i < 8; i++)
            _animations[i] = new Animation();
    }

    #endregion


    #region Methods

    public void SetCombatGraphic([Range(0, byte.MaxValue)] uint index,
        [Range(0, ushort.MaxValue)] uint originalFrameWidth, [Range(0, ushort.MaxValue)] uint originalFrameHeight,
        [Range(0, ushort.MaxValue)] uint displayFrameWidth, [Range(0, ushort.MaxValue)] uint displayFrameHeight)
    {
        GraphicIndex = index;
        OriginalFrameWidth = originalFrameWidth;
        OriginalFrameHeight = originalFrameHeight;
        DisplayFrameWidth = displayFrameWidth;
        DisplayFrameHeight = displayFrameHeight;
    }

    public uint[] GetAnimationFrames(MonsterAnimation monsterAnimation)
    {
        var animation = _animations[(int)monsterAnimation];
        return animation.FrameIndices.Take(Math.Min(32, animation.UsedAmount)).Select(f => (uint)f).ToArray();
    }

    public void SetAnimationFrames(MonsterAnimation monsterAnimation, uint[] frameIndices)
    {
        if (frameIndices.Length > 32)
            throw new ArgumentOutOfRangeException(nameof(frameIndices), "Only 32 frames are possible.");

        var animation = _animations[(int)monsterAnimation];
        animation.FrameIndices = frameIndices.Select(frameIndex =>
        {
            if (frameIndex > byte.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(frameIndices), $"A frame index must not be larger than {byte.MaxValue}.");
            return (byte)frameIndex;
        }).ToArray();
        animation.UsedAmount = frameIndices.Length;
    }

		public AnimationType GetAnimationType(MonsterAnimation monsterAnimation)
		{
        return _animationTypes[(int)monsterAnimation];
		}

		public void SetAnimationType(MonsterAnimation monsterAnimation, AnimationType animationType)
		{
			_animationTypes[(int)monsterAnimation] = animationType;
		}

    public void EnsureCorrectCalculatedValues(GameDataRepository gameDataRepository)
    {
        BonusDefense = (int)Math.Clamp
        (
            Util.CalculateItemPropertySum(Equipment,
                index => gameDataRepository.Items[index],
                (item, slotFlags) => slotFlags.HasFlag(ItemSlotFlags.Cursed) ? -(long)item.Defense : item.Defense) +
                Attributes[Attribute.Stamina].TotalCurrentValue / 25,
            int.MinValue,
            int.MaxValue
        );

        BonusAttackDamage = (int)Math.Clamp
        (
            Util.CalculateItemPropertySum(Equipment,
                index => gameDataRepository.Items[index],
                (item, slotFlags) => slotFlags.HasFlag(ItemSlotFlags.Cursed) ? -(long)item.Damage : item.Damage) +
                Attributes[Attribute.Strength].TotalCurrentValue / 25,
            int.MinValue,
            int.MaxValue
        );
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
        WriteFillBytes(5);
        if (advanced)
            dataWriter.Write((byte)AttackImmunity);
        else
            WriteFillBytes(1);
        dataWriter.Write((byte)GraphicIndex);
        WriteFillBytes(2);
        dataWriter.Write((byte)Math.Min(Morale, 100));
        dataWriter.Write((byte)SpellTypeImmunity);
        dataWriter.Write((byte)AttacksPerRound);
        var battleFlags = (byte)BattleFlags;
        if (!advanced)
            battleFlags &= 0xf;
        dataWriter.Write(battleFlags);
        dataWriter.Write((byte)Element);
        WriteFillBytes(4);
        dataWriter.Write((ushort)Gold);
        dataWriter.Write((ushort)Food);
        WriteFillBytes(2);
        dataWriter.Write((ushort)Conditions);
        dataWriter.Write((ushort)DefeatExperience);
        WriteFillBytes(8);
        for (int i = 0; i < 8; i++)
        {
            var attribute = Attributes[(Attribute)i];
            dataWriter.Write((ushort)attribute.CurrentValue);
            dataWriter.Write((ushort)attribute.MaxValue);
            dataWriter.WriteSignedWord(attribute.BonusValue);
            dataWriter.Write((ushort)attribute.StoredValue);
        }
        WriteFillBytes(8);
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
        WriteFillBytes(16);
        dataWriter.Write(LearnedSpellsHealing);
        dataWriter.Write(LearnedSpellsAlchemistic);
        dataWriter.Write(LearnedSpellsMystic);
        dataWriter.Write(LearnedSpellsDestruction);
        dataWriter.Write(LearnedSpellsType5);
        dataWriter.Write(LearnedSpellsType6);
        dataWriter.Write(LearnedSpellsFunctional);
        WriteFillBytes(4);
        string name = Name;
        if (name.Length > 15)
            name = name[..15];
        dataWriter.WriteWithoutLength(name.PadRight(16, '\0'));

        #region Equipment and Items
        Equipment.Serialize(dataWriter, majorVersion, advanced);
        Items.Serialize(dataWriter, majorVersion, advanced);
        #endregion

        #region Monster Display Data
        for (int i = 0; i < 8; i++)
        {
            int n;

            for (n = 0; n < _animations[i].UsedAmount && n < 32; n++)
                dataWriter.Write(_animations[i].FrameIndices[n]);

            for (; n < 32; n++)
                dataWriter.Write(0);
        }
        for (int i = 0; i < 8; i++)
        {
            dataWriter.Write((byte)Math.Min(_animations[i].UsedAmount, 32));
        }
        foreach (var c in Enumerable.Range(0, 16)) // Atari palette
            dataWriter.Write((byte)c); // just 0x0 to 0xf
        foreach (var c in CustomPalette)
            dataWriter.Write(c);
        byte animationProgressions = 0;
        for (int i = 0; i < 8; i++)
        {
            if (_animationTypes[i] == AnimationType.Wave)
                animationProgressions |= (byte)(1 << i);
        }
        dataWriter.Write(animationProgressions);
        WriteFillBytes(1); // padding byte
        dataWriter.Write((ushort)OriginalFrameWidth);
        dataWriter.Write((ushort)OriginalFrameHeight);
        dataWriter.Write((ushort)DisplayFrameWidth);
        dataWriter.Write((ushort)DisplayFrameHeight);
        #endregion
    }

    public static IData Deserialize(IDataReader dataReader, int majorVersion, bool advanced)
    {
        if (dataReader.ReadByte() != (byte)CharacterType.Monster)
            throw new InvalidDataException("The given data is no valid monster data.");

        void SkipBytes(int amount) => dataReader.Position += amount;

        var monsterData = new MonsterData();

        monsterData.Gender = (Gender)dataReader.ReadByte();
        monsterData.Race = (Race)dataReader.ReadByte();
        monsterData.Class = (Class)dataReader.ReadByte();
        monsterData.SpellMastery = (SpellTypeMastery)dataReader.ReadByte();
        monsterData.Level = dataReader.ReadByte();
        SkipBytes(5);
        monsterData.AttackImmunity = advanced ? (AdvancedMonsterFlags)dataReader.ReadByte() : AdvancedMonsterFlags.None;
        if (!advanced)
            SkipBytes(1);
        monsterData.GraphicIndex = dataReader.ReadByte();
        SkipBytes(2);
        monsterData.Morale = dataReader.ReadByte();
        monsterData.SpellTypeImmunity = (SpellTypeImmunity)dataReader.ReadByte();
        monsterData.AttacksPerRound = dataReader.ReadByte();
        monsterData.BattleFlags = (BattleFlags)dataReader.ReadByte();
        monsterData.Element = (CharacterElement)dataReader.ReadByte();
        SkipBytes(4);
        monsterData.Gold = dataReader.ReadWord();
        monsterData.Food = dataReader.ReadWord();
        SkipBytes(2);
        monsterData.Conditions = (Condition)dataReader.ReadWord();
        monsterData.DefeatExperience = dataReader.ReadWord();
        SkipBytes(8);
        for (int i = 0; i < 8; i++)
        {
            var attribute = monsterData.Attributes[(Attribute)i];
            attribute.CurrentValue = dataReader.ReadWord();
            attribute.MaxValue = dataReader.ReadWord();
            attribute.BonusValue = dataReader.ReadSignedWord();
            attribute.StoredValue = dataReader.ReadWord();
        }
        SkipBytes(8);
        if (advanced)
        {
            monsterData.BonusSpellDamage = dataReader.ReadWord();
            monsterData.BonusMaxSpellDamage = dataReader.ReadWord();
            monsterData.BonusSpellDamageReduction = dataReader.ReadSignedWord();
            monsterData.BonusSpellDamagePercentage = dataReader.ReadSignedWord();
        }
        else
        {
            SkipBytes(8);
        }
        for (int i = 0; i < 10; i++)
        {
            var skill = monsterData.Skills[(Skill)i];
            skill.CurrentValue = dataReader.ReadWord();
            skill.MaxValue = dataReader.ReadWord();
            skill.BonusValue = dataReader.ReadSignedWord();
            skill.StoredValue = dataReader.ReadWord();
        }
        var hitPoints = monsterData.HitPoints;
        hitPoints.CurrentValue = dataReader.ReadWord();
        hitPoints.MaxValue = dataReader.ReadWord();
        hitPoints.BonusValue = dataReader.ReadSignedWord();
        var spellPoints = monsterData.SpellPoints;
        spellPoints.CurrentValue = dataReader.ReadWord();
        spellPoints.MaxValue = dataReader.ReadWord();
        spellPoints.BonusValue = dataReader.ReadSignedWord();
        monsterData.BaseDefense = dataReader.ReadWord();
        monsterData.BonusDefense = dataReader.ReadSignedWord();
        monsterData.BaseAttackDamage = dataReader.ReadWord();
        monsterData.BonusAttackDamage = dataReader.ReadSignedWord();
        monsterData.MagicAttackLevel = dataReader.ReadSignedWord();
        monsterData.MagicDefenseLevel = dataReader.ReadSignedWord();
        SkipBytes(16);
        monsterData.LearnedSpellsHealing = dataReader.ReadDword();
        monsterData.LearnedSpellsAlchemistic = dataReader.ReadDword();
        monsterData.LearnedSpellsMystic = dataReader.ReadDword();
        monsterData.LearnedSpellsDestruction = dataReader.ReadDword();
        monsterData.LearnedSpellsType5 = dataReader.ReadDword();
        monsterData.LearnedSpellsType6 = dataReader.ReadDword();
        monsterData.LearnedSpellsFunctional = dataReader.ReadDword();
        SkipBytes(4);
        monsterData.Name = dataReader.ReadString(16).TrimEnd('\0', ' ');

        #region Equipment and Items
        monsterData.Equipment = DataCollection<ItemSlotData>.Deserialize(dataReader, EquipmentSlotCount, majorVersion, advanced, 0);
        monsterData.Items = DataCollection<ItemSlotData>.Deserialize(dataReader, InventorySlotCount, majorVersion, advanced, 0);
        #endregion

        #region Monster Display Data
        var allFrameIndices = dataReader.ReadBytes(256); // 8 * 32
        var frameCounts = dataReader.ReadBytes(8);
        for (int i = 0; i < 8; i++)
        {
            var animation = monsterData._animations[i] = new Animation();
            int count = animation.UsedAmount = frameCounts[i];
            animation.FrameIndices = count == 0
                ? []
                : [.. allFrameIndices.Skip(i * 32).Take(count)];
        }
        SkipBytes(16); // Atari palette
        monsterData.CustomPalette = dataReader.ReadBytes(32);
        byte animationProgressions = dataReader.ReadByte();
        for (int i = 0; i < 8; i++)
        {
            monsterData._animationTypes[i] = (animationProgressions & (1 << i)) == 0
                ? AnimationType.Cycle
                : AnimationType.Wave;
        }
        SkipBytes(1); // padding byte
        monsterData.OriginalFrameWidth = dataReader.ReadWord();
        monsterData.OriginalFrameHeight = dataReader.ReadWord();
        monsterData.DisplayFrameWidth = dataReader.ReadWord();
        monsterData.DisplayFrameHeight = dataReader.ReadWord();
        #endregion

        return monsterData;
    }

    public static IIndexedData Deserialize(IDataReader dataReader, uint index, int majorVersion, bool advanced)
    {
        var monsterData = (MonsterData)Deserialize(dataReader, majorVersion, advanced);
        (monsterData as IMutableIndex).Index = index;
        return monsterData;
    }

    #endregion


    #region Equality

    public bool Equals(MonsterData? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return base.Equals(other) &&
                _morale == other._morale &&
               _defeatExperience == other._defeatExperience &&
               _graphicIndex == other._graphicIndex &&
               _originalFrameWidth == other._originalFrameWidth &&
               _originalFrameHeight == other._originalFrameHeight &&
               _displayFrameWidth == other._displayFrameWidth &&
               _displayFrameHeight == other._displayFrameHeight &&
               _animations.Equals(other._animations) &&
               _animationTypes.Equals(other._animationTypes) &&
               CustomPalette.Equals(other.CustomPalette);
    }

    public override bool Equals(object? obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((MonsterData)obj);
    }

    public static bool operator ==(MonsterData? left, MonsterData? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(MonsterData? left, MonsterData? right)
    {
        return !Equals(left, right);
    }

    public override int GetHashCode() => base.GetHashCode();

    #endregion


    #region Cloning

    public MonsterData Copy()
    {
        MonsterData copy = new()
        {
            Gender = Gender,
            Race = Race,
            Class = Class,
            SpellMastery = SpellMastery,
            Level = Level,
            GraphicIndex = GraphicIndex,
            Morale = Morale,
            SpellTypeImmunity = SpellTypeImmunity,
            AttacksPerRound = AttacksPerRound,
            BattleFlags = BattleFlags,
            Element = Element,
            Gold = Gold,
            Food = Food,
            Conditions = Conditions,
            DefeatExperience = DefeatExperience,
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
