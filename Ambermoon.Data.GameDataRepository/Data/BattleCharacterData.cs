using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;

namespace Ambermoon.Data.GameDataRepository.Data
{
    using Collections;
    using Util;

    public abstract class BattleCharacterData : CharacterData, IEquatable<BattleCharacterData>, INotifyPropertyChanged
    {

        #region Constants

        public const int EquipmentSlotCount = 9;
        public const int InventorySlotCount = 24;

        #endregion


        #region Fields

        private uint _gold;
        private uint _food;
        private uint _baseAttackDamage;
        private uint _baseDefense;
        private int _magicAttackLevel;
        private int _magicDefenseLevel;
        private uint _learnedSpellsHealing;
        private uint _learnedSpellsAlchemistic;
        private uint _learnedSpellsMystic;
        private uint _learnedSpellsDestruction;
        private uint _learnedSpellsType5;
        private uint _learnedSpellsType6;
        private uint _learnedSpellsFunctional;
        private uint _attacksPerRound;
        private Condition _conditions;
        private BattleFlags _battleFlags;
        private CharacterElement _element;
        private SpellTypeImmunity _spellTypeImmunity;
        private SpellTypeMastery _spellMastery;
        private uint _bonusSpellDamage;
        private uint _bonusMaxSpellDamage;
        private int _bonusSpellDamageReduction;
        private int _bonusSpellDamagePercentage;

        #endregion


        #region Properties

        public SpellTypeMastery SpellMastery
        {
            get => _spellMastery;
            set => SetField(ref _spellMastery, value);
        }

        public SpellTypeImmunity SpellTypeImmunity
        {
            get => _spellTypeImmunity;
            set => SetField(ref _spellTypeImmunity, value);
        }

        [Range(0, byte.MaxValue)]
        public uint AttacksPerRound
        {
            get => _attacksPerRound;
            set
            {
                ValueChecker.Check(value, 0, byte.MaxValue);
                SetField(ref _attacksPerRound, value);
            }
        }

        public CharacterElement Element
        {
            get => _element;
            set => SetField(ref _element, value);
        }

        /// <summary>
        /// Note that this is only used for monsters in
        /// the original, but also for party members in
        /// the advanced version. Setting anything but
        /// the elemental spell increase bits for a
        /// party member won't have any effect for them.
        /// </summary>
        public BattleFlags BattleFlags
        {
            get => _battleFlags;
            set => SetField(ref _battleFlags, value);
        }

        [Range(0, ushort.MaxValue)]
        public uint Gold
        {
            get => _gold;
            set
            {
                ValueChecker.Check(value, 0, ushort.MaxValue);
                SetField(ref _gold, value);
            }
        }

        [Range(0, ushort.MaxValue)]
        public uint Food
        {
            get => _food;
            set
            {
                ValueChecker.Check(value, 0, ushort.MaxValue);
                SetField(ref _food, value);
            }
        }

        public Condition Conditions
        {
            get => _conditions;
            set => SetField(ref _conditions, value);
        }

        public CharacterValueCollection<Attribute> Attributes { get; } = new(8);

        public CharacterValueCollection<Skill> Skills { get; } = new(10);

        public CharacterValue HitPoints { get; } = new();

        public CharacterValue SpellPoints { get; } = new();

        [Range(0, ushort.MaxValue)]
        public uint BaseAttackDamage
        {
            get => _baseAttackDamage;
            set
            {
                ValueChecker.Check(value, 0, ushort.MaxValue);
                SetField(ref _baseAttackDamage, value);
            }
        }

        [Range(0, ushort.MaxValue)]
        public uint BaseDefense
        {
            get => _baseDefense;
            set
            {
                ValueChecker.Check(value, 0, ushort.MaxValue);
                SetField(ref _baseDefense, value);
            }
        }

        /// <summary>
        /// This is calculated from equipment.
        /// </summary>
        [Range(short.MinValue, short.MaxValue)]
        public int BonusAttackDamage { get; private set; } = 0;

        /// <summary>
        /// This is calculated from equipment.
        /// </summary>
        [Range(short.MinValue, short.MaxValue)]
        public int BonusDefense { get; private set; } = 0;

        [Range(short.MinValue, short.MaxValue)]
        public int MagicAttackLevel
        {
            get => _magicAttackLevel;
            set
            {
                ValueChecker.Check(value, short.MinValue, short.MaxValue);
                SetField(ref _magicAttackLevel, value);
            }
        }

        [Range(short.MinValue, short.MaxValue)]
        public int MagicDefenseLevel
        {
            get => _magicDefenseLevel;
            set
            {
                ValueChecker.Check(value, short.MinValue, short.MaxValue);
                SetField(ref _magicDefenseLevel, value);
            }
        }

        /// <summary>
        /// All learned healing spells are stored in this bitfield.
        /// The lowest and highest bits are unused. The first healing
        /// spell is represented by the second bit (bit 1), the last
        /// healing spell is represented by the 31st bit (bit 30).
        /// So the 1-based spell index is also the bit index.
        ///
        /// Therefore, valid values range from 0x2 to 0x7fffffff but
        /// a value of 0 is also allowed to state that no spell is learned.
        /// </summary>
        [Range(0, 0x7fffffff)]
        public uint LearnedSpellsHealing
        {
            get => _learnedSpellsHealing;
            set
            {
                ValueChecker.Check(value, 0x2, 0x7fffffff, 0);
                SetField(ref _learnedSpellsHealing, value);
            }
        }

        /// <summary>
        /// All learned alchemistic spells are stored in this bitfield.
        /// The lowest and highest bits are unused. The first alchemistic
        /// spell is represented by the second bit (bit 1), the last
        /// alchemistic spell is represented by the 31st bit (bit 30).
        /// So the 1-based spell index is also the bit index.
        ///
        /// Therefore, valid values range from 0x2 to 0x7fffffff but
        /// a value of 0 is also allowed to state that no spell is learned.
        /// </summary>
        [Range(0, 0x7fffffff)]
        public uint LearnedSpellsAlchemistic
        {
            get => _learnedSpellsAlchemistic;
            set
            {
                ValueChecker.Check(value, 0x2, 0x7fffffff, 0);
                SetField(ref _learnedSpellsAlchemistic, value);
            }
        }

        /// <summary>
        /// All learned mystic spells are stored in this bitfield.
        /// The lowest and highest bits are unused. The first mystic
        /// spell is represented by the second bit (bit 1), the last
        /// mystic spell is represented by the 31st bit (bit 30).
        /// So the 1-based spell index is also the bit index.
        ///
        /// Therefore, valid values range from 0x2 to 0x7fffffff but
        /// a value of 0 is also allowed to state that no spell is learned.
        /// </summary>
        [Range(0, 0x7fffffff)]
        public uint LearnedSpellsMystic
        {
            get => _learnedSpellsMystic;
            set
            {
                ValueChecker.Check(value, 0x2, 0x7fffffff, 0);
                SetField(ref _learnedSpellsMystic, value);
            }
        }

        /// <summary>
        /// All learned destruction spells are stored in this bitfield.
        /// The lowest and highest bits are unused. The first destruction
        /// spell is represented by the second bit (bit 1), the last
        /// destruction spell is represented by the 31st bit (bit 30).
        /// So the 1-based spell index is also the bit index.
        ///
        /// Therefore, valid values range from 0x2 to 0x7fffffff but
        /// a value of 0 is also allowed to state that no spell is learned.
        /// </summary>
        [Range(0, 0x7fffffff)]
        public uint LearnedSpellsDestruction
        {
            get => _learnedSpellsDestruction;
            set
            {
                ValueChecker.Check(value, 0x2, 0x7fffffff, 0);
                SetField(ref _learnedSpellsDestruction, value);
            }
        }

        /// <summary>
        /// All learned type 5 spells are stored in this bitfield.
        /// The lowest and highest bits are unused. The first type 5
        /// spell is represented by the second bit (bit 1), the last
        /// type 5 spell is represented by the 31st bit (bit 30).
        /// So the 1-based spell index is also the bit index.
        ///
        /// Therefore, valid values range from 0x2 to 0x7fffffff but
        /// a value of 0 is also allowed to state that no spell is learned.
        /// </summary>
        [Range(0, 0x7fffffff)]
        public uint LearnedSpellsType5
        {
            get => _learnedSpellsType5;
            set
            {
                ValueChecker.Check(value, 0x2, 0x7fffffff, 0);
                SetField(ref _learnedSpellsType5, value);
            }
        }

        /// <summary>
        /// All learned type 6 spells are stored in this bitfield.
        /// The lowest and highest bits are unused. The first type 6
        /// spell is represented by the second bit (bit 1), the last
        /// type 6 spell is represented by the 31st bit (bit 30).
        /// So the 1-based spell index is also the bit index.
        ///
        /// Therefore, valid values range from 0x2 to 0x7fffffff but
        /// a value of 0 is also allowed to state that no spell is learned.
        /// </summary>
        [Range(0, 0x7fffffff)]
        public uint LearnedSpellsType6
        {
            get => _learnedSpellsType6;
            set
            {
                ValueChecker.Check(value, 0x2, 0x7fffffff, 0);
                SetField(ref _learnedSpellsType6, value);
            }
        }

        /// <summary>
        /// All learned functional spells are stored in this bitfield.
        /// The lowest and highest bits are unused. The first functional
        /// spell is represented by the second bit (bit 1), the last
        /// functional spell is represented by the 31st bit (bit 30).
        /// So the 1-based spell index is also the bit index.
        ///
        /// Therefore, valid values range from 0x2 to 0x7fffffff but
        /// a value of 0 is also allowed to state that no spell is learned.
        /// </summary>
        [Range(0, 0x7fffffff)]
        public uint LearnedSpellsFunctional
        {
            get => _learnedSpellsFunctional;
            set
            {
                ValueChecker.Check(value, 0x2, 0x7fffffff, 0);
                SetField(ref _learnedSpellsFunctional, value);
            }
        }

        protected DataCollection<ItemSlotData> Equipment { get; private protected set; } = new(EquipmentSlotCount);
        protected DataCollection<ItemSlotData> Items { get; private protected set; } = new(InventorySlotCount);

        #endregion


        #region Constructors

        private protected BattleCharacterData()
        {
            InitializeItemSlots();

            HitPoints.PropertyChanged += (_, _) => OnPropertyChanged(nameof(HitPoints));
            SpellPoints.PropertyChanged += (_, _) => OnPropertyChanged(nameof(SpellPoints));
            foreach (var attribute in Attributes)
                attribute.PropertyChanged += (_, _) => OnPropertyChanged(nameof(Attributes));
            foreach (var skill in Skills)
                skill.PropertyChanged += (_, _) => OnPropertyChanged(nameof(Skills));
        }

        #endregion


        #region Methods

        private protected void InitializeItemSlots()
        {
            ItemSlotData CreateEquipmentSlot(int index)
            {
                var slot = new ItemSlotData();
                slot.ItemChanged += (oldIndex, newIndex) => EquipmentSlotChanged((EquipmentSlot)index, oldIndex, newIndex);
                slot.CursedChanged += (wasCursed, isCursed) => EquipmentSlotChanged((EquipmentSlot)index, null, null, wasCursed, isCursed);
                return slot;
            }

            ItemSlotData CreateItemSlot(int index)
            {
                var slot = new ItemSlotData();
                slot.ItemChanged += (oldIndex, newIndex) => ItemSlotChanged(index, oldIndex, newIndex);
                slot.AmountChanged += (oldAmount, newAmount) => ItemSlotChanged(index, null, null, oldAmount, newAmount);
                return slot;
            }

            for (int i = 0; i < EquipmentSlotCount; ++i)
                Equipment[i] = CreateEquipmentSlot(i);

            for (int i = 0; i < InventorySlotCount; ++i)
                Items[i] = CreateItemSlot(i);
        }

        private protected ItemData? FindItem(uint index)
        {
            if (index is 0)
                return null;
            Func<GameDataRepository, bool> predicate = Type == CharacterType.Monster
                ? repo => repo.Monsters.Contains(this)
                : repo => repo.PartyMembers.Contains(this);
            var repo = GameDataRepository
                .GetOpenRepositories()
                .FirstOrDefault(predicate);
            return repo?.Items[index];
        }

        private void EquipmentSlotChanged(EquipmentSlot slot,
            uint? oldIndex,
            uint? newIndex,
            bool? wasCursed = null,
            bool? isCursed = null)
        {
            newIndex ??= Equipment[(int)slot].ItemIndex;
            oldIndex ??= newIndex;
            wasCursed ??= Equipment[(int)slot].Flags.HasFlag(ItemSlotFlags.Cursed);
            isCursed ??= wasCursed;

            if (newIndex is 0)
            {
                if (oldIndex is 0)
                    return;

                var oldItem = FindItem(oldIndex.Value);

                int oldDamage = (int)(oldItem?.Damage ?? 0);
                if (wasCursed.Value) oldDamage = -oldDamage;
                BonusAttackDamage -= oldDamage;
                int oldDefense = (int)(oldItem?.Defense ?? 0);
                if (wasCursed.Value) oldDefense = -oldDefense;
                BonusDefense -= oldDefense;
                int oldMagicAttackLevel = (int)(oldItem?.MagicAttackLevel ?? 0);
                MagicAttackLevel -= oldMagicAttackLevel;
                int oldMagicDefenseLevel = (int)(oldItem?.MagicDefenseLevel ?? 0);
                MagicDefenseLevel -= oldMagicDefenseLevel;
                // TODO: hp, sp, attributes, skills
            }
            else
            {
                var newItem = FindItem(newIndex.Value);
                var oldItem = oldIndex.Value is 0 ? null : FindItem(oldIndex.Value);

                int oldDamage = (int)(oldItem?.Damage ?? 0);
                if (wasCursed.Value) oldDamage = -oldDamage;
                int newDamage = (int)(newItem?.Damage ?? 0);
                if (isCursed.Value) newDamage = -newDamage;
                BonusAttackDamage += newDamage - oldDamage;
                int oldDefense = (int)(oldItem?.Defense ?? 0);
                if (wasCursed.Value) oldDefense = -oldDefense;
                int newDefense = (int)(newItem?.Defense ?? 0);
                if (isCursed.Value) newDefense = -newDefense;
                BonusDefense += newDefense - oldDefense;
                int oldMagicAttackLevel = (int)(oldItem?.MagicAttackLevel ?? 0);
                int newMagicAttackLevel = (int)(newItem?.MagicAttackLevel ?? 0);
                MagicAttackLevel += newMagicAttackLevel - oldMagicAttackLevel;
                int oldMagicDefenseLevel = (int)(oldItem?.MagicDefenseLevel ?? 0);
                int newMagicDefenseLevel = (int)(newItem?.MagicDefenseLevel ?? 0);
                MagicDefenseLevel += newMagicDefenseLevel - oldMagicDefenseLevel;
                // TODO: hp, sp, attributes, skills
            }

            OnPropertyChanged(nameof(Equipment));
        }

        private protected virtual void ItemSlotChanged(int slot,
            uint? oldIndex,
            uint? newIndex,
            uint? oldAmount = null,
            uint? newAmount = null)
        {
            OnPropertyChanged(nameof(Items));
        }

        public ItemSlotData GetEquipmentSlot(EquipmentSlot equipmentSlot)
        {
            return Equipment[(int)equipmentSlot];
        }

        public ItemSlotData GetInventorySlot([Range(0, InventorySlotCount)] int slot)
        {
            return Items[slot];
        }

        #endregion


        #region Equality

        public bool Equals(BattleCharacterData? other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return base.Equals(other) &&
                   SpellMastery == other.SpellMastery &&
                   SpellTypeImmunity == other.SpellTypeImmunity &&
                   AttacksPerRound == other.AttacksPerRound &&
                   Element == other.Element &&
                   BattleFlags == other.BattleFlags &&
                   Gold == other.Gold &&
                   Food == other.Food &&
                   Conditions == other.Conditions &&
                   Attributes.Equals(other.Attributes) &&
                   Skills.Equals(other.Skills) &&
                   HitPoints.Equals(other.HitPoints) &&
                   SpellPoints.Equals(other.SpellPoints) &&
                   BaseAttackDamage == other.BaseAttackDamage &&
                   BaseDefense == other.BaseDefense &&
                   BonusAttackDamage == other.BonusAttackDamage &&
                   BonusDefense == other.BonusDefense &&
                   MagicAttackLevel == other.MagicAttackLevel &&
                   MagicDefenseLevel == other.MagicDefenseLevel &&
                   LearnedSpellsHealing == other.LearnedSpellsHealing &&
                   LearnedSpellsAlchemistic == other.LearnedSpellsAlchemistic &&
                   LearnedSpellsMystic == other.LearnedSpellsMystic &&
                   LearnedSpellsDestruction == other.LearnedSpellsDestruction &&
                   LearnedSpellsType5 == other.LearnedSpellsType5 &&
                   LearnedSpellsType6 == other.LearnedSpellsType6 &&
                   LearnedSpellsFunctional == other.LearnedSpellsFunctional &&
                   Equipment.Equals(other.Equipment) &&
                   Items.Equals(other.Items) &&
                   BonusSpellDamage == other.BonusSpellDamage &&
                   BonusMaxSpellDamage == other.BonusMaxSpellDamage &&
                   BonusSpellDamageReduction == other.BonusSpellDamageReduction &&
                   BonusSpellDamagePercentage == other.BonusSpellDamagePercentage;
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((BattleCharacterData)obj);
        }

        public override int GetHashCode() => base.GetHashCode();

        public static bool operator ==(BattleCharacterData? left, BattleCharacterData? right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(BattleCharacterData? left, BattleCharacterData? right)
        {
            return !Equals(left, right);
        }

        #endregion


        #region Advanced

        /// <summary>
        /// This is a plain value added to the damage of
        /// spells. Therefore, this affects both the minimum
        /// and maximum of the spell damage.
        /// 
        /// Advanced only.
        /// </summary>
        [AdvancedOnly]
        [Range(0, ushort.MaxValue)]
        public uint BonusSpellDamage
        {
            get => _bonusSpellDamage;
            set
            {
                ValueChecker.Check(value, 0, ushort.MaxValue);
                SetField(ref _bonusSpellDamage, value);
            }
        }

        /// <summary>
        /// This is a plain value added to the max damage of
        /// spells. Therefore, this affects only the maximum
        /// of the spell damage. Note that <see cref="BonusSpellDamage"/>
        /// is added in addition to this.
        /// 
        /// Advanced only.
        /// </summary>
        [AdvancedOnly]
        [Range(0, ushort.MaxValue)]
        public uint BonusMaxSpellDamage
        {
            get => _bonusMaxSpellDamage;
            set
            {
                ValueChecker.Check(value, 0, ushort.MaxValue);
                SetField(ref _bonusMaxSpellDamage, value);
            }
        }

        /// <summary>
        /// Reduces incoming spell damage by the given
        /// value in percent. So 50 means -50% damage
        /// and -50 actually increases damage by 50%.
        ///
        /// Note that any value equal or below -100
        /// would reduce the received spell damage to 0.
        /// However, the game logic will deal at least
        /// 1 point of damage if a spell hits.
        /// 
        /// Advanced only.
        /// </summary>
        [AdvancedOnly]
        [Range(-100, short.MaxValue)]
        public int BonusSpellDamageReduction
        {
            get => _bonusSpellDamageReduction;
            set
            {
                ValueChecker.Check(value, -100, short.MaxValue);
                SetField(ref _bonusSpellDamageReduction, value);
            }
        }

        /// <summary>
        /// This increases the spell damage which
        /// is dealt by the given value in percent.
        /// So 100 means +100% spell damage while
        /// negative values act as a penalty.
        /// 
        /// Note that any value equal or below -100
        /// would reduce the dealt spell damage to 0.
        /// However, the game logic will deal at least
        /// 1 point of damage if a spell hits.
        /// 
        /// Advanced only.
        /// </summary>
        [AdvancedOnly]
        [Range(-100, short.MaxValue)]
        public int BonusSpellDamagePercentage
        {
            get => _bonusSpellDamagePercentage;
            set
            {
                ValueChecker.Check(value, -100, short.MaxValue);
                SetField(ref _bonusSpellDamagePercentage, value);
            }
        }

        #endregion


        #region Property Changes

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        #endregion

    }
}
