namespace Ambermoon.Data
{
    public class Item
    {
        public uint Index { get; set; }
        public uint GraphicIndex { get; set; }
        public ItemType Type { get; set; }
        public EquipmentSlot EquipmentSlot { get; set; }
        public byte Unknown2 { get; set; }
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
        public byte[] Unknown3 { get; set; } // 6
        public byte SpecialValue { get; set; } // special item purpose, transportation, etc
        public byte Unknown4 { get; set; }
        public SpellType SpellType { get; set; }
        public byte SpellIndex { get; set; }
        public byte SpellUsageCount { get; set; } // 255 = infinite
        public byte[] Unknown5 { get; set; } // 4
        public int MagicArmorLevel { get; set; } // M-B-R
        public int MagicAttackLevel { get; set; } // M-B-W
        public ItemFlags Flags { get; set; }
        public byte Unknown6 { get; set; }
        public ClassFlag Classes { get; set; }
        public uint Price { get; set; }
        public uint Weight { get; set; }
        public string Name { get; set; }

        /// <summary>
        /// Used only for special items.
        /// </summary>
        public SpecialItemPurpose SpecialItemPurpose => (SpecialItemPurpose)SpecialValue;
        /// <summary>
        /// Used only for transportation items.
        /// </summary>
        public Transportation Transportation => (Transportation)SpecialValue;
        /// <summary>
        /// Used only for text scrolls.
        /// </summary>
        public uint TextIndex => SpecialValue;
        public Spell Spell => SpellIndex == 0 ? Spell.None : (Spell)((int)SpellType * 30 + SpellIndex);

        float GetPriceFactor(PartyMember character) => 2.92f + character.Attributes[Data.Attribute.Charisma].TotalCurrentValue * 0.16f / 100.0f;
        public uint GetBuyPrice(PartyMember buyer) => (uint)Util.Round(Price / GetPriceFactor(buyer));
        public uint GetSellPrice(PartyMember seller) => (uint)Util.Round(0.5f * Price * GetPriceFactor(seller));

        public static Item Load(uint index, IItemReader itemReader, IDataReader dataReader)
        {
            var item = new Item { Index = index };

            itemReader.ReadItem(item, dataReader);

            return item;
        }
    }
}
