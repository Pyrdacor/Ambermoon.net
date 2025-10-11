using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;

namespace Ambermoon.Data.GameDataRepository.Data
{
    using Legacy;
    using Serialization;
    
    public sealed class ItemData : IMutableIndex, IIndexedData, IEquatable<ItemData>, INotifyPropertyChanged, IImageProvidingData
    {

        #region Fields

        private uint _graphicIndex;
        private ItemType _type;
        private EquipmentSlot _equipmentSlot;
        private uint _breakChance;
        private GenderFlag _gender;
        private uint _numberOfHands;
        private uint _numberOfFingers;
        private uint _hitPoints;
        private uint _spellPoints;
        private Attribute _attribute;
        private int _attributeValue;
        private Skill _skill;
        private uint _skillValue;
        private uint _defense;
        private uint _damage;
        private AmmunitionType _ammunitionType;
        private AmmunitionType _usedAmmunitionType;
        private Skill _penaltySkill1;
        private Skill _penaltySkill2;
        private uint _penaltyValue1;
        private uint _penaltyValue2;
        private uint? _specialIndex;
        private uint? _textSubIndex;
        private Spell _spell;
        private uint _initialSpellCharges;
        private uint _initialNumberOfRecharges;
        private uint _maxNumberOfRecharges;
        private uint _maxSpellCharges;
        private uint _enchantPrice;
        private uint _magicDefenseLevel;
        private uint _magicAttackLevel;
        private ItemFlags _flags;
        private ItemSlotFlags _defaultSlotFlags;
        private ClassFlag _classes;
        private uint _price;
        private uint _weight;
        private string _name = string.Empty;

        #endregion


        #region Properties

        uint IMutableIndex.Index
        {
            get;
            set;
        }

        public uint Index => (this as IMutableIndex).Index;

        [Range(0, byte.MaxValue)]
        public uint GraphicIndex
        {
            get => _graphicIndex;
            set => SetField(ref _graphicIndex, value);
        }

        public ItemType Type
        {
            get => _type;
            set => SetField(ref _type, value);
        }

        /// <summary>
        /// Slot where the item can be equipped.
        /// Note that there is a special handling
        /// for rings. Rings can be placed in both
        /// ring plots but best specify the right
        /// ring slot to be sure it works as expected.
        ///
        /// Only considered for equipment.
        /// </summary>
        public EquipmentSlot EquipmentSlot
        {
            get => _equipmentSlot;
            set => SetField(ref _equipmentSlot, value);
        }

        /// <summary>
        /// The chance that the item breaks when used.
        /// Note that the given values is in 0.1%
        /// increments, so 1 means 0.1% while 1000
        /// means 100%.
        ///
        /// This is only considered for weapons, shields,
        /// armor, tools, normal items and text scrolls.
        /// </summary>
        [Range(0, 1000)]
        public uint BreakChance
        {
            get => _breakChance;
            set => SetField(ref _breakChance, value);
        }

        /// <summary>
        /// Genders which can use or equip the item.
        /// </summary>
        public GenderFlag Gender
        {
            get => _gender;
            set => SetField(ref _gender, value);
        }

        /// <summary>
        /// Number of hands this item occupies when equipped.
        /// Should only be used for weapons, shields and ammunition.
        ///
        /// Only considered for equipment.
        /// </summary>
        [Range(0, 2)]
        public uint NumberOfHands
        {
            get => _numberOfHands;
            set => SetField(ref _numberOfHands, value);
        }

        /// <summary>
        /// Number of fingers this item occupies when equipped.
        /// Should only be used for rings.
        ///
        /// Only considered for equipment.
        /// </summary>
        [Range(0, 2)]
        public uint NumberOfFingers
        {
            get => _numberOfFingers;
            set => SetField(ref _numberOfFingers, value);
        }

        /// <summary>
        /// Amount of hit points this item adds when equipped.
        ///
        /// Note that this only increases the maximum hit points.
        /// </summary>
        [Range(0, sbyte.MaxValue)]
        public uint HitPoints
        {
            get => _hitPoints;
            set => SetField(ref _hitPoints, value);
        }

        /// <summary>
        /// Amount of spell points this item adds when equipped.
        ///
        /// Note that this only increases the maximum spell points.
        /// </summary>
        [Range(0, sbyte.MaxValue)]
        public uint SpellPoints
        {
            get => _spellPoints;
            set => SetField(ref _spellPoints, value);
        }

        /// <summary>
        /// Attribute this item increases when equipped.
        ///
        /// Note that this only increases the current attribute value
        /// but can exceed the character's maximum attribute value.
        /// </summary>
        public Attribute Attribute
        {
            get => _attribute;
            set => SetField(ref _attribute, value);
        }

        /// <summary>
        /// Amount of attribute points this item increases when equipped.
        ///
        /// Note that this only increases the current attribute value
        /// but can exceed the character's maximum attribute value.
        /// </summary>
        [Range(0, 99)]
        public int AttributeValue
        {
            get => _attributeValue;
            set => SetField(ref _attributeValue, value);
        }

        /// <summary>
        /// Skill this item increases when equipped.
        ///
        /// Note that this only increases the current skill value
        /// but can exceed the character's maximum skill value.
        /// </summary>
        public Skill Skill
        {
            get => _skill;
            set => SetField(ref _skill, value);
        }

        /// <summary>
        /// Amount of skill points this item increases when equipped.
        ///
        /// Note that this only increases the current skill value
        /// but can exceed the character's maximum skill value.
        /// </summary>
        [Range(0, 99)]
        public uint SkillValue
        {
            get => _skillValue;
            set => SetField(ref _skillValue, value);
        }

        /// <summary>
        /// Points of defense this item adds when equipped.
        /// </summary>
        [Range(0, sbyte.MaxValue)]
        public uint Defense
        {
            get => _defense;
            set => SetField(ref _defense, value);
        }

        /// <summary>
        /// Points of damage this item adds when equipped.
        /// </summary>
        [Range(0, sbyte.MaxValue)]
        public uint Damage
        {
            get => _damage;
            set => SetField(ref _damage, value);
        }

        /// <summary>
        /// Type of ammunition this item is.
        ///
        /// Only considered if the item is ammunition.
        /// </summary>
        public AmmunitionType AmmunitionType
        {
            get => _ammunitionType;
            set => SetField(ref _ammunitionType, value);
        }

        /// <summary>
        /// Type of ammunition this item uses.
        ///
        /// Only considered if the item is a long-ranged weapon.
        /// </summary>
        public AmmunitionType UsedAmmunitionType
        {
            get => _usedAmmunitionType;
            set => SetField(ref _usedAmmunitionType, value);
        }

        /// <summary>
        /// First skill which is penalized when equipped.
        ///
        /// See <see cref="PenaltyValue1"/>.
        ///
        /// Only considered for equipment.
        /// </summary>
        public Skill PenaltySkill1
        {
            get => _penaltySkill1;
            set => SetField(ref _penaltySkill1, value);
        }

        /// <summary>
        /// Second skill which is penalized when equipped.
        ///
        /// See <see cref="PenaltyValue2"/>.
        ///
        /// Only considered for equipment.
        /// </summary>
        public Skill PenaltySkill2
        {
            get => _penaltySkill2;
            set => SetField(ref _penaltySkill2, value);
        }

        /// <summary>
        /// Amount of skill penalty when equipped.
        /// This uses <see cref="PenaltySkill1"/> to specify the skill to penalize.
        /// Leave this at 0 to not penalize any skill.
        /// </summary>
        [Range(0, byte.MaxValue)]
        public uint PenaltyValue1
        {
            get => _penaltyValue1;
            set => SetField(ref _penaltyValue1, value);
        }

        /// <summary>
        /// Amount of skill penalty when equipped.
        /// This uses <see cref="PenaltySkill2"/> to specify the skill to penalize.
        /// Leave this at 0 to not penalize any skill.
        /// </summary>
        [Range(0, byte.MaxValue)]
        public uint PenaltyValue2
        {
            get => _penaltyValue2;
            set => SetField(ref _penaltyValue2, value);
        }

        /// <summary>
        /// Special item purpose.
        ///
        /// Special items are things like clocks, compasses, monster eyes, etc.
        /// This gives the type of special item.
        ///
        /// Only considered if this is a special item.
        /// </summary>
        public SpecialItemPurpose? SpecialItemPurpose
        {
            get => Type == ItemType.SpecialItem ? (SpecialItemPurpose?)_specialIndex : null;
            private set => SetField(ref _specialIndex, (uint?)value);
        }

        /// <summary>
        /// Transportation type.
        ///
        /// Only considered if this is a transportation item.
        /// </summary>
        public Transportation? Transportation
        {
            get => Type == ItemType.Transportation ? (Transportation?)_specialIndex : null;
            private set => SetField(ref _specialIndex, (uint?)value);
        }

        /// <summary>
        /// Main text index.
        ///
        /// This references the file index in Object_texts.amb.
        /// Inside each file there may be multiple text entries.
        /// To reference the text entry, use <see cref="TextSubIndex"/>.
        ///
        /// Only considered if this is a text scroll.
        /// </summary>
        [Range(0, byte.MaxValue)]
        public uint? TextIndex
        {
            get => Type == ItemType.TextScroll ? _specialIndex : null;
            private set => SetField(ref _specialIndex, value);
        }

        /// <summary>
        /// Sub text index.
        ///
        /// See <see cref="TextIndex"/>. This is the index to
        /// the text entry inside a text file of Object_texts.amb.
        ///
        /// Only considered if this is a text scroll.
        /// </summary>
        [Range(0, byte.MaxValue)]
        public uint? TextSubIndex
        {
            get => _textSubIndex;
            private set => SetField(ref _textSubIndex, value);
        }

        /// <summary>
        /// Spell this item casts when used.
        ///
        /// Note that everybody who can use the item
        /// can cast the spell, regardless of the
        /// spell school the caster has mastered or
        /// if the caster even is a magic class.
        /// </summary>
        public Spell Spell
        {
            get => _spell;
            set => SetField(ref _spell, value);
        }

        /// <summary>
        /// Initial number of spell charges when the
        /// item is created. Note that items which are
        /// found in the game may have a different value
        /// as it is specified by the item slot of chests
        /// and merchants. So this value is just used when
        /// creating items (like in conversations).
        /// </summary>
        [Range(0, byte.MaxValue)]
        public uint InitialSpellCharges
        {
            get => _initialSpellCharges;
            set => SetField(ref _initialSpellCharges, value);
        }

        /// <summary>
        /// Initial number of recharges when the item
        /// is created. Note that items which are
        /// found in the game may have a different value
        /// as it is specified by the item slot of chests
        /// and merchants. So this value is just used when
        /// creating items (like in conversations).
        /// </summary>
        [Range(0, byte.MaxValue)]
        public uint InitialNumberOfRecharges
        {
            get => _initialNumberOfRecharges;
            set => SetField(ref _initialNumberOfRecharges, value);
        }

        /// <summary>
        /// The maximum number this item can be recharged
        /// (or enchanted). When this limit is reached, it
        /// can never be enchanted again. A value of 0
        /// however, means that there is no such limit.
        /// A value of 255 (0xff) also stands for no limit.
        /// </summary>
        [Range(0, byte.MaxValue)]
        public uint MaxNumberOfRecharges
        {
            get => _maxNumberOfRecharges;
            set => SetField(ref _maxNumberOfRecharges, value);
        }

        /// <summary>
        /// Maximum number of spell charges this item can
        /// possibly have. Recharging the item is only possible
        /// up to this value.
        ///
        /// Note that items are considered magical or rechargeable
        /// only if this value is above 0.
        /// </summary>
        [Range(0, byte.MaxValue)]
        public uint MaxSpellCharges
        {
            get => _maxSpellCharges;
            set => SetField(ref _maxSpellCharges, value);
        }

        /// <summary>
        /// Price to enchant this item.
        /// </summary>
        [Range(0, byte.MaxValue)]
        public uint EnchantPrice
        {
            get => _enchantPrice;
            set => SetField(ref _enchantPrice, value);
        }

        /// <summary>
        /// Magic defense level this item adds when equipped.
        ///
        /// The total magic defense level is the sum of all
        /// equipped item magic defense levels.
        /// </summary>
        [Range(0, byte.MaxValue)]
        public uint MagicDefenseLevel
        {
            get => _magicDefenseLevel;
            set => SetField(ref _magicDefenseLevel, value);
        }

        /// <summary>
        /// Magic attack level this item adds when equipped.
        ///
        /// The total magic attack level is the sum of all
        /// equipped item magic attack levels.
        /// </summary>
        [Range(0, byte.MaxValue)]
        public uint MagicAttackLevel
        {
            get => _magicAttackLevel;
            set => SetField(ref _magicAttackLevel, value);
        }

        /// <summary>
        /// Flags of the item.
        /// </summary>
        public ItemFlags Flags
        {
            get => _flags;
            set => SetField(ref _flags, value);
        }

        /// <summary>
        /// Default item slot flags. This is only
        /// used for items which are created.
        /// Items found in chests, are sold by
        /// merchants or dropped by monsters, will
        /// always come from a specific item slot
        /// which has its own flags. But if items
        /// are created (mainly in conversations),
        /// these flags are used.
        /// </summary>
        public ItemSlotFlags DefaultSlotFlags
        {
            get => _defaultSlotFlags;
            set => SetField(ref _defaultSlotFlags, value);
        }

        /// <summary>
        /// Classes which can use or equip the item.
        /// </summary>
        public ClassFlag Classes
        {
            get => _classes;
            set => SetField(ref _classes, value);
        }

        /// <summary>
        /// Price of the item in gold.
        ///
        /// This is the buying price. The sell price
        /// is much lower and is influenced by the seller's
        /// charisma.
        /// </summary>
        [Range(0, ushort.MaxValue)]
        public uint Price
        {
            get => _price;
            set => SetField(ref _price, value);
        }

        /// <summary>
        /// Weight of the item in grams.
        /// </summary>
        [Range(0, ushort.MaxValue)]
        public uint Weight
        {
            get => _weight;
            set => SetField(ref _weight, value);
        }

        /// <summary>
        /// Name of the item.
        ///
        /// This is stored as 20 bytes but must be
        /// terminated by a zero byte, so the max
        /// length is actually 19 single-byte characters.
        /// </summary>
        [StringLength(19)]
        public string Name
        {
            get => _name;
            set
            {
                if (new AmbermoonEncoding().GetByteCount(value) > 19)
                    throw new ArgumentOutOfRangeException(nameof(Name), "Name length is limited to 19 single-byte characters.");

                SetField(ref _name, value);
            }
        }

        #endregion


        #region Serialization

        public void Serialize(IDataWriter dataWriter, bool advanced)
        {
            dataWriter.Write((byte)GraphicIndex);
            dataWriter.Write((byte)Type);
            dataWriter.Write((byte)EquipmentSlot);
            dataWriter.Write((byte)BreakChance);
            dataWriter.Write((byte)Gender);
            dataWriter.Write((byte)NumberOfHands);
            dataWriter.Write((byte)NumberOfFingers);
            dataWriter.Write((byte)HitPoints);
            dataWriter.Write((byte)SpellPoints);
            dataWriter.Write((byte)Attribute);
            dataWriter.Write((byte)AttributeValue);
            dataWriter.Write((byte)Skill);
            dataWriter.Write((byte)SkillValue);
            dataWriter.Write((byte)Defense);
            dataWriter.Write((byte)Damage);
            dataWriter.Write((byte)AmmunitionType);
            dataWriter.Write((byte)UsedAmmunitionType);
            dataWriter.Write((byte)PenaltySkill1);
            dataWriter.Write((byte)PenaltySkill2);
            dataWriter.Write((byte)PenaltyValue1);
            dataWriter.Write((byte)PenaltyValue2);
            dataWriter.Write((byte)(_specialIndex ?? 0));
            dataWriter.Write((byte)(TextSubIndex ?? 0));
            dataWriter.Write((byte)Spell);
            dataWriter.Write((byte)InitialSpellCharges);
            dataWriter.Write((byte)InitialNumberOfRecharges);
            dataWriter.Write((byte)MaxNumberOfRecharges);
            dataWriter.Write((byte)MaxSpellCharges);
            dataWriter.Write((byte)EnchantPrice);
            dataWriter.Write((byte)MagicDefenseLevel);
            dataWriter.Write((byte)MagicAttackLevel);
            dataWriter.Write((byte)Flags);
            dataWriter.Write((byte)DefaultSlotFlags);
            dataWriter.Write((ushort)Classes);
            dataWriter.Write((ushort)Price);
            dataWriter.Write((ushort)Weight);
            string name = Name;
            if (name.Length > 19)
                name = name[..19];
            dataWriter.WriteWithoutLength(name.PadRight(20, '\0'));
        }

        public static IData Deserialize(IDataReader dataReader, bool advanced)
        {
            var itemData = new ItemData();

            itemData.GraphicIndex = dataReader.ReadByte();
            itemData.Type = (ItemType)dataReader.ReadByte();
            var equipmentSlot = (EquipmentSlot)dataReader.ReadByte();
            itemData.EquipmentSlot = itemData.Type.IsEquipment() ? equipmentSlot : EquipmentSlot.None;
            uint breakChance = dataReader.ReadByte();
            itemData.BreakChance = itemData.Type.IsBreakable() ? breakChance : 0;
            itemData.Gender = (GenderFlag)dataReader.ReadByte();
            uint numberOfHands = dataReader.ReadByte();
            uint numberOfFingers = dataReader.ReadByte();
            itemData.NumberOfHands = itemData.Type.UsesHandCount() ? numberOfHands : 0;
            itemData.NumberOfFingers = itemData.Type.UsesFingerCount() ? numberOfFingers : 0;
            uint hitPoints = dataReader.ReadByte();
            itemData.HitPoints = itemData.Type.IsEquipment() ? hitPoints : 0;
            uint spellPoints = dataReader.ReadByte();
            itemData.SpellPoints = itemData.Type.IsEquipment() ? spellPoints : 0;
            // Note: We could limit attributes and skills to equipment as well
            // but there are some story-related things like that the silver hand
            // grants lock picking. Of course this is not useful, but it underlines
            // the fact, that the owner is a master thief.
            itemData.Attribute = (Attribute)dataReader.ReadByte();
            itemData.AttributeValue = dataReader.ReadByte();
            itemData.Skill = (Skill)dataReader.ReadByte();
            itemData.SkillValue = dataReader.ReadByte();
            uint defense = dataReader.ReadByte();
            itemData.Defense = itemData.Type.IsEquipment() ? defense : 0;
            uint damage = dataReader.ReadByte();
            itemData.Damage = itemData.Type.IsEquipment() ? damage : 0;
            var ammunitionType = (AmmunitionType)dataReader.ReadByte();
            itemData.AmmunitionType = itemData.Type == ItemType.Ammunition ? ammunitionType : AmmunitionType.None;
            var usedAmmunitionType = (AmmunitionType)dataReader.ReadByte();
            itemData.UsedAmmunitionType = itemData.Type == ItemType.LongRangeWeapon ? usedAmmunitionType : AmmunitionType.None;
            var penaltySkill1 = (Skill)dataReader.ReadByte();
            itemData.PenaltySkill1 = itemData.Type.IsEquipment() ? penaltySkill1 : Skill.Attack;
            var penaltySkill2 = (Skill)dataReader.ReadByte();
            itemData.PenaltySkill2 = itemData.Type.IsEquipment() ? penaltySkill2 : Skill.Attack;
            uint penaltyValue1 = dataReader.ReadByte();
            itemData.PenaltyValue1 = itemData.Type.IsEquipment() ? penaltyValue1 : 0;
            uint penaltyValue2 = dataReader.ReadByte();
            itemData.PenaltyValue2 = itemData.Type.IsEquipment() ? penaltyValue2 : 0;
            itemData._specialIndex = dataReader.ReadByte();
            uint textSubIndex = dataReader.ReadByte();
            itemData.TextSubIndex = itemData.Type == ItemType.TextScroll ? textSubIndex : null;
            int spellSchool = dataReader.ReadByte();
            int spellIndex = dataReader.ReadByte();
            itemData.Spell = spellIndex == 0 ? Spell.None : (Spell)(spellSchool * 30 + spellIndex);
            uint initialSpellCharges = dataReader.ReadByte();
            itemData.InitialSpellCharges = itemData.Spell != Spell.None ? initialSpellCharges : 0;
            uint initialNumberOfRecharges = dataReader.ReadByte();
            itemData.InitialNumberOfRecharges = itemData.Spell != Spell.None ? initialNumberOfRecharges : 0;
            uint maxNumberOfRecharges = dataReader.ReadByte();
            itemData.MaxNumberOfRecharges = itemData.Spell != Spell.None ? maxNumberOfRecharges : 0;
            uint maxSpellCharges = dataReader.ReadByte();
            itemData.MaxSpellCharges = itemData.Spell != Spell.None ? maxSpellCharges : 0;
            uint enchantPrice = dataReader.ReadByte();
            itemData.EnchantPrice = itemData.Spell != Spell.None ? enchantPrice : 0;
            uint magicDefenseLevel = dataReader.ReadByte();
            itemData.MagicDefenseLevel = itemData.Type.IsEquipment() ? magicDefenseLevel : 0;
            uint magicAttackLevel = dataReader.ReadByte();
            itemData.MagicAttackLevel = itemData.Type.IsEquipment() ? magicAttackLevel : 0;
            var flags = (ItemFlags)dataReader.ReadByte();
            itemData.Flags = flags & itemData.Type.AllowedFlags();
            var defaultSlotFlags = (ItemSlotFlags)dataReader.ReadByte();
            if (!itemData.Type.IsEquipment())
                defaultSlotFlags &= ~ItemSlotFlags.Cursed; // only equipment can be cursed
            itemData.DefaultSlotFlags = defaultSlotFlags;
            itemData.Classes = (ClassFlag)dataReader.ReadWord();
            itemData.Price = dataReader.ReadWord();
            itemData.Weight = dataReader.ReadWord();
            itemData.Name = dataReader.ReadString(20).TrimEnd('\0', ' ');

            return itemData;
        }

        public static IIndexedData Deserialize(IDataReader dataReader, uint index, bool advanced)
        {
            var itemData = (ItemData)Deserialize(dataReader, advanced);
            (itemData as IMutableIndex).Index = index;
            return itemData;
        }

        #endregion


        #region Equality

        public bool Equals(ItemData? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return Index == other.Index &&
                   GraphicIndex == other.GraphicIndex &&
                   Type == other.Type &&
                   EquipmentSlot == other.EquipmentSlot &&
                   BreakChance == other.BreakChance &&
                   Gender == other.Gender &&
                   NumberOfHands == other.NumberOfHands &&
                   NumberOfFingers == other.NumberOfFingers &&
                   HitPoints == other.HitPoints &&
                   SpellPoints == other.SpellPoints &&
                   Attribute == other.Attribute &&
                   AttributeValue == other.AttributeValue &&
                   Skill == other.Skill &&
                   SkillValue == other.SkillValue &&
                   Defense == other.Defense &&
                   Damage == other.Damage &&
                   AmmunitionType == other.AmmunitionType &&
                   UsedAmmunitionType == other.UsedAmmunitionType &&
                   PenaltySkill1 == other.PenaltySkill1 &&
                   PenaltySkill2 == other.PenaltySkill2 &&
                   PenaltyValue1 == other.PenaltyValue1 &&
                   PenaltyValue2 == other.PenaltyValue2 &&
                   SpecialItemPurpose == other.SpecialItemPurpose &&
                   Transportation == other.Transportation &&
                   TextIndex == other.TextIndex &&
                   TextSubIndex == other.TextSubIndex &&
                   Spell == other.Spell &&
                   InitialSpellCharges == other.InitialSpellCharges &&
                   InitialNumberOfRecharges == other.InitialNumberOfRecharges &&
                   MaxNumberOfRecharges == other.MaxNumberOfRecharges &&
                   MaxSpellCharges == other.MaxSpellCharges &&
                   EnchantPrice == other.EnchantPrice &&
                   MagicDefenseLevel == other.MagicDefenseLevel &&
                   MagicAttackLevel == other.MagicAttackLevel &&
                   Flags == other.Flags &&
                   DefaultSlotFlags == other.DefaultSlotFlags &&
                   Classes == other.Classes &&
                   Price == other.Price &&
                   Weight == other.Weight &&
                   Name == other.Name;
        }

        public override bool Equals(object? obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((ItemData)obj);
        }

        public override int GetHashCode() => (int)Index;

        public static bool operator ==(ItemData? left, ItemData? right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(ItemData? left, ItemData? right)
        {
            return !Equals(left, right);
        }

        #endregion


        #region Cloning

        public ItemData Copy()
        {
            var copy = new ItemData();

            copy.GraphicIndex = GraphicIndex;
            copy.Type = Type;
            copy.EquipmentSlot = EquipmentSlot;
            copy.BreakChance = BreakChance;
            copy.Gender = Gender;
            copy.NumberOfHands = NumberOfHands;
            copy.NumberOfFingers = NumberOfFingers;
            copy.HitPoints = HitPoints;
            copy.SpellPoints = SpellPoints;
            copy.Attribute = Attribute;
            copy.AttributeValue = AttributeValue;
            copy.Skill = Skill;
            copy.SkillValue = SkillValue;
            copy.Defense = Defense;
            copy.Damage = Damage;
            copy.AmmunitionType = AmmunitionType;
            copy.UsedAmmunitionType = UsedAmmunitionType;
            copy.PenaltySkill1 = PenaltySkill1;
            copy.PenaltySkill2 = PenaltySkill2;
            copy.PenaltyValue1 = PenaltyValue1;
            copy.PenaltyValue2 = PenaltyValue2;
            copy.SpecialItemPurpose = SpecialItemPurpose;
            copy.Transportation = Transportation;
            copy.TextIndex = TextIndex;
            copy.TextSubIndex = TextSubIndex;
            copy.Spell = Spell;
            copy.InitialSpellCharges = InitialSpellCharges;
            copy.InitialNumberOfRecharges = InitialNumberOfRecharges;
            copy.MaxNumberOfRecharges = MaxNumberOfRecharges;
            copy.MaxSpellCharges = MaxSpellCharges;
            copy.EnchantPrice = EnchantPrice;
            copy.MagicDefenseLevel = MagicDefenseLevel;
            copy.MagicAttackLevel = MagicAttackLevel;
            copy.Flags = Flags;
            copy.DefaultSlotFlags = DefaultSlotFlags;
            copy.Classes = Classes;
            copy.Price = Price;
            copy.Weight = Weight;
            copy.Name = Name;

            (copy as IMutableIndex).Index = Index;

            return copy;
        }

        public object Clone() => Copy();

        #endregion


        #region Property Changes

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        #endregion

    }
}
