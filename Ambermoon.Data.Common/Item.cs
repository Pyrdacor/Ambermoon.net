using Ambermoon.Data.Serialization;

namespace Ambermoon.Data
{
    // TODO: Usable on world, usable at place, max recharges (most likely in UnknownBytes26To29)
    public class Item
    {
        public uint Index { get; set; }
        public uint GraphicIndex { get; set; }
        public ItemType Type { get; set; }
        public EquipmentSlot EquipmentSlot { get; set; }
        public byte BreakChance { get; set; }
        public GenderFlag Genders { get; set; }
        public uint NumberOfHands { get; set; }
        public uint NumberOfFingers { get; set; }
        public int HitPoints{ get; set; }
        public int SpellPoints { get; set; }
        public Attribute? Attribute { get; set; }
        public int AttributeValue { get; set; }
        public Ability? Ability { get; set; }
        public int AbilityValue { get; set; }
        public int Defense { get; set; }
        public int Damage { get; set; }
        /// <summary>
        /// Used if this is a ammunition.
        /// </summary>
        public AmmunitionType AmmunitionType { get; set; }
        /// <summary>
        /// Used if this is a long-ranged weapon with ammunition.
        /// </summary>
        public AmmunitionType UsedAmmunitionType { get; set; }
        public uint AttackReduction { get; set; }
        public uint ParryReduction { get; set; }
        /// <summary>
        /// This value is used for:
        /// - Special item purposes like clock, compass, etc (<see cref="SpecialItemPurpose"/>)
        /// - Transportation (<see cref="Transportation"/>)
        /// - Text index of text scrolls (<see cref="TextIndex"/>)
        /// </summary>
        public byte SpecialValue { get; set; } // special item purpose, transportation, etc
        public byte TextSubIndex { get; set; }
        public SpellSchool SpellSchool { get; set; }
        public byte SpellIndex { get; set; }
        public byte InitialCharges { get; set; } // 255 = infinite
        public byte[] UnknownBytes26To27 { get; set; } // 2, has something to do with charges
        public byte MaxCharges { get; set; }
        public byte UnknownByte29 { get; set; } // has something to do with charges
        public int MagicArmorLevel { get; set; } // M-B-R
        public int MagicAttackLevel { get; set; } // M-B-W
        public ItemFlags Flags { get; set; }
        public ItemSlotFlags DefaultSlotFlags { get; set; }
        public ClassFlag Classes { get; set; }
        public uint Price { get; set; }
        public uint Weight { get; set; }
        public string Name { get; set; }

        /// <summary>
        /// Used only for special items.
        /// 
        /// Note that this is the same as <see cref="SpecialValue"/>.
        /// </summary>
        public SpecialItemPurpose SpecialItemPurpose
        {
            get => (SpecialItemPurpose)SpecialValue;
            set => SpecialValue = (byte)value;
        }
        /// <summary>
        /// Used only for transportation items.
        /// 
        /// Note that this is the same as <see cref="SpecialValue"/>.
        /// </summary>
        public Transportation Transportation
        {
            get => (Transportation)SpecialValue;
            set => SpecialValue = (byte)value;
        }
        /// <summary>
        /// Used only for text scrolls.
        /// 
        /// Note that this is the same as <see cref="SpecialValue"/>.
        /// </summary>
        public uint TextIndex
        {
            get => SpecialValue;
            set => SpecialValue = (byte)value;
        }
        public Spell Spell => SpellIndex == 0 ? Spell.None : (Spell)((int)SpellSchool * 30 + SpellIndex);

        private Item()
        {

        }

        public bool IsUsable
        {
            get
            {
                return Spell != Spell.None || Type == ItemType.Potion || Type == ItemType.SpecialItem || Type == ItemType.SpellScroll ||
                    Type == ItemType.TextScroll || Type == ItemType.Tool || Type == ItemType.Transportation || Flags.HasFlag(ItemFlags.Readable);
            }
        }

        public bool IsImportant => !Flags.HasFlag(ItemFlags.NotImportant) && !Flags.HasFlag(ItemFlags.Clonable);

        public static Item Load(uint index, IItemReader itemReader, IDataReader dataReader)
        {
            var item = new Item { Index = index };

            itemReader.ReadItem(item, dataReader);

            return item;
        }
    }
}
