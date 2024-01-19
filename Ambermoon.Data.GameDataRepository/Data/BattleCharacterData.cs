using Ambermoon.Data.GameDataRepository.Util;
using System.ComponentModel.DataAnnotations;

namespace Ambermoon.Data.GameDataRepository.Data
{
    // TODO: property limits, ranges
    public abstract class BattleCharacterData : CharacterData
    {
        public const int EquipmentSlotCount = 9;
        public const int InventorySlotCount = 24;
        private protected DataCollection<ItemSlotData> _equipment = new();
        private protected DataCollection<ItemSlotData> _items = new();

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

        public ItemSlotData GetEquipmentSlot(EquipmentSlot equipmentSlot)
        {
            return _equipment[(int)equipmentSlot];
        }

        public ItemSlotData GetInventorySlot([Range(0, InventorySlotCount)] int slot)
        {
            return _items[slot];
        }

        #region Advanced
        /// <summary>
        /// This is a plain value added to the damage of
        /// spells. Therefore this affects both the minimum
        /// and maximum of the spell damage.
        /// 
        /// Advanced only.
        /// </summary>
        [AdvancedOnly]
        public uint BonusSpellDamage { get; set; }
        /// <summary>
        /// This is a plain value added to the max damage of
        /// spells. Therefore this affects only the maximum
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
        /// However the game logic will deal at least
        /// 1 point of damage if a spell hits.
        /// 
        /// Advanced only.
        /// </summary>
        [AdvancedOnly]
        public int BonusSpellDamagePercentage { get; set; }
        #endregion
    }
}
