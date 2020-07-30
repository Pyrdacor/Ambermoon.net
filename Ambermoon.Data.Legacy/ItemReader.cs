namespace Ambermoon.Data.Legacy
{
    public class ItemReader : IItemReader
    {
        public void ReadItem(Item item, IDataReader dataReader)
        {
            item.GraphicIndex = dataReader.ReadByte();
            item.Type = (ItemType)dataReader.ReadByte();
            item.EquipmentSlot = (EquipmentSlot)dataReader.ReadByte();
            item.Unknown2 = dataReader.ReadByte();
            item.Genders = (GenderFlag)dataReader.ReadByte();
            item.NumberOfHands = dataReader.ReadByte();
            item.NumberOfFingers = dataReader.ReadByte();
            item.HitPoints = (sbyte)dataReader.ReadByte();
            item.SpellPoints = (sbyte)dataReader.ReadByte();
            Attribute attribute = (Attribute)dataReader.ReadByte();
            int attributeValue = (sbyte)dataReader.ReadByte();
            if (attributeValue != 0)
            {
                item.Attribute = attribute;
                item.AttributeValue = attributeValue;
            }
            Ability ability = (Ability)dataReader.ReadByte();
            int abilityValue = (sbyte)dataReader.ReadByte();
            if (abilityValue != 0)
            {
                item.Ability = ability;
                item.AbilityValue = abilityValue;
            }
            item.Defense = (sbyte)dataReader.ReadByte();
            item.Damage = (sbyte)dataReader.ReadByte();
            item.Unknown3 = dataReader.ReadBytes(6);
            item.SpecialValue = dataReader.ReadByte();
            item.Unknown4 = dataReader.ReadByte();
            item.SpellType = (SpellType)dataReader.ReadByte();
            item.SpellIndex = dataReader.ReadByte();
            item.SpellUsageCount = dataReader.ReadByte();
            item.Unknown5 = dataReader.ReadBytes(4);
            item.MagicArmorLevel = (sbyte)dataReader.ReadByte();
            item.MagicAttackLevel = (sbyte)dataReader.ReadByte();
            item.Flags = (ItemFlags)dataReader.ReadByte();
            item.Unknown6 = dataReader.ReadByte();
            item.Classes = (ClassFlag)dataReader.ReadWord() & ClassFlag.All; // TODO: is this right?
            item.Price = dataReader.ReadWord();
            item.Weight = dataReader.ReadWord();
            item.Name = dataReader.ReadString(19).TrimEnd(' ', '\0');

            if (dataReader.ReadByte() != 0) // end of item
                throw new AmbermoonException(ExceptionScope.Data, "Invalid item data.");
        }
    }
}
