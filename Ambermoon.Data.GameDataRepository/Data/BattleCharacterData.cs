using Ambermoon.Data.GameDataRepository.Util;
using System.ComponentModel.DataAnnotations;

namespace Ambermoon.Data.GameDataRepository.Data
{
    // TODO: property limits, ranges
    public abstract class BattleCharacterData : CharacterData, IEquatable<BattleCharacterData>
    {

        #region Constants

        public const int EquipmentSlotCount = 9;
        public const int InventorySlotCount = 24;

        #endregion
        

        #region Properties

        public SpellTypeMastery SpellMastery { get; set; }
        public SpellTypeImmunity SpellTypeImmunity { get; set; }
        public uint AttacksPerRound { get; set; }
        public CharacterElement Element { get; set; }
        /// <summary>
        /// Note that this is only used for monsters in
        /// the original, but also for party members in
        /// the advanced version. Setting anything but
        /// the elemental spell increase bits for a
        /// party member won't have any effect for them.
        /// </summary>
        public BattleFlags BattleFlags { get; set; }
        public uint Gold { get; set; }
        public uint Food { get; set; }
        public Condition Conditions { get; set; }
        public CharacterValueCollection<Attribute> Attributes { get; } = new CharacterValueCollection<Attribute>(8);
        public CharacterValueCollection<Skill> Skills { get; } = new CharacterValueCollection<Skill>(10);
        public CharacterValue HitPoints { get; } = new CharacterValue();
        public CharacterValue SpellPoints { get; } = new CharacterValue();
        public uint BaseAttackDamage { get; set; }
        public uint BaseDefense { get; set; }
        /// <summary>
        /// This is calculated from equipment.
        /// </summary>
        public int BonusAttackDamage { get; }
        /// <summary>
        /// This is calculated from equipment.
        /// </summary>
        public int BonusDefense { get; }
        public int MagicAttackLevel { get; set; }
        public int MagicDefenseLevel { get; set; }
        public uint LearnedSpellsHealing { get; set; }
        public uint LearnedSpellsAlchemistic { get; set; }
        public uint LearnedSpellsMystic { get; set; }
        public uint LearnedSpellsDestruction { get; set; }
        public uint LearnedSpellsType5 { get; set; }
        public uint LearnedSpellsType6 { get; set; }
        public uint LearnedSpellsFunctional { get; set; }
        protected DataCollection<ItemSlotData> Equipment { get; private protected set; } = new();
        protected DataCollection<ItemSlotData> Items { get; private protected set; } = new();

        #endregion


        #region Methods

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
        public uint BonusSpellDamage { get; set; }
        /// <summary>
        /// This is a plain value added to the max damage of
        /// spells. Therefore, this affects only the maximum
        /// of the spell damage. Note that <see cref="BonusSpellDamage"/>
        /// is added in addition to this.
        /// 
        /// Advanced only.
        /// </summary>
        [AdvancedOnly]
        public uint BonusMaxSpellDamage { get; set; }
        /// <summary>
        /// Reduces incoming spell damage by the given
        /// value in percent. So 50 means -50% damage
        /// and -50 actually increases damage by 50%.
        /// 
        /// Advanced only.
        /// </summary>
        [AdvancedOnly]
        public int BonusSpellDamageReduction { get; set; }
        /// <summary>
        /// This increases the spell damage which
        /// is dealt by the given value in percent.
        /// So 100 means +100% spell damage while
        /// negative values act as a penalty.
        /// Note that any value equal or below -100
        /// would reduce the dealt spell damage to 0.
        /// However, the game logic will deal at least
        /// 1 point of damage if a spell hits.
        /// 
        /// Advanced only.
        /// </summary>
        [AdvancedOnly]
        public int BonusSpellDamagePercentage { get; set; }
        #endregion

    }
}
