namespace Ambermoon.Data.Legacy
{
    public class ItemReader : IItemReader
    {
        public void ReadItem(Item item, IDataReader dataReader)
        {
            item.GraphicIndex = dataReader.ReadByte();
            item.Type = (ItemType)dataReader.ReadByte();
            item.Unknown1 = dataReader.ReadByte();
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
            item.Unknown3 = dataReader.ReadBytes(15);
            item.MagicArmorLevel = (sbyte)dataReader.ReadByte();
            item.MagicAttackLevel = (sbyte)dataReader.ReadByte();
            item.Flags = (ItemFlags)dataReader.ReadByte();
            item.Unknown4 = dataReader.ReadByte();
            item.Classes = (ClassFlag)dataReader.ReadWord();
            item.Price = dataReader.ReadWord();
            item.Weight = dataReader.ReadWord();
            item.Name = dataReader.ReadString(19).TrimEnd(' ', '\0');

            if (dataReader.ReadByte() != 0) // end of item
                throw new AmbermoonException(ExceptionScope.Data, "Invalid item data.");
        }
    }
}
