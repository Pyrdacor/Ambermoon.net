using Ambermoon.Data.GameDataRepository.Util;
using Ambermoon.Data.Serialization;

namespace Ambermoon.Data.GameDataRepository.Data
{
    public class ItemData : IIndexed, IMutableIndex, IIndexedData, IEquatable<ItemData>
    {
        // TODO

        uint IMutableIndex.Index
        {
            get;
            set;
        }

        public uint Index => (this as IMutableIndex).Index;

        public uint GraphicIndex { get; set; }
        public ItemType Type { get; set; }
        public EquipmentSlot EquipmentSlot { get; set; }
        public uint BreakChance { get; set; }
        public GenderFlag Gender { get; set; }
        public uint NumberOfHands { get; set; }
        public uint NumberOfFingers { get; set; }
        public uint HitPoints { get; set; }
        public uint SpellPoints { get; set; }
        public Attribute Attribute { get; set; }
        public int AttributeValue { get; set; }
        public Skill Skill { get; set; }
        public uint SkillValue { get; set; }
        public uint Defense { get; set; }
        public uint Damage { get; set; }
        public AmmunitionType AmmunitionType { get; set; }
        public AmmunitionType UsedAmmunitionType { get; set; }
        public Skill PenaltySkill1 { get; set; }
        public Skill PenaltySkill2 { get; set; }
        public uint PenaltyValue1 { get; set; }
        public uint PenaltyValue2 { get; set; }
        public SpecialItemPurpose? SpecialItemPurpose { get; }
        public Transportation? Transportation { get; }
        public uint? TextIndex { get; }
        public uint? TextSubIndex { get; }
        public Spell Spell { get; set; }
        public uint InitialSpellCharges { get; set; }
        public uint InitialNumberOfRecharges { get; set; }
        public uint MaxNumberOfRecharges { get; set; }
        public uint MaxSpellCharges { get; set; }
        public uint EnchantPrice { get; set; }
        public uint MagicDefenseLevel { get; set; }
        public uint MagicAttackLevel { get; set; }
        public ItemFlags Flags { get; set; }
        public ItemSlotFlags DefaultSlotFlags { get; set; }
        public ClassFlag Classes { get; set; }
        public uint Price { get; set; }
        public uint Weight { get; set; }
        public string Name { get; set; }

        public ItemData Copy()
        {
            return new(); // TODO
        }

        public object Clone() => Copy();

        public bool Equals(ItemData? other)
        {
            if (other is null)
                return false;

            // TODO
            return false;
        }

        public static IIndexedData Deserialize(IDataReader dataReader, uint index, bool advanced)
        {
            var itemData = (ItemData)Deserialize(dataReader, advanced);
            (itemData as IMutableIndex).Index = index;
            return itemData;
        }

        public static IData Deserialize(IDataReader dataReader, bool advanced)
        {
            // TODO
            throw new NotImplementedException();
        }

        public void Serialize(IDataWriter dataWriter, bool advanced)
        {
            // TODO
            throw new NotImplementedException();
        }
    }
}
