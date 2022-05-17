using Ambermoon.Data.Serialization;

namespace Ambermoon.Data.Legacy.Serialization
{
    public class ItemReader : IItemReader
    {
        public void ReadItem(Item item, IDataReader dataReader)
        {
            item.GraphicIndex = dataReader.ReadByte();
            item.Type = (ItemType)dataReader.ReadByte();
            item.EquipmentSlot = (EquipmentSlot)dataReader.ReadByte();
            item.BreakChance = dataReader.ReadByte();
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
            Skill ability = (Skill)dataReader.ReadByte();
            int abilityValue = (sbyte)dataReader.ReadByte();
            if (abilityValue != 0)
            {
                item.Ability = ability;
                item.AbilityValue = abilityValue;
            }
            item.Defense = (sbyte)dataReader.ReadByte();
            item.Damage = (sbyte)dataReader.ReadByte();
            item.AmmunitionType = (AmmunitionType)dataReader.ReadByte();
            item.UsedAmmunitionType = (AmmunitionType)dataReader.ReadByte();
            // TODO: There are 4 items in Ambermoon which use this:
            // - Whip: 01 00 0A 00
            // - Banded Armour: 00 00 04 00
            // - Plate Armour: 00 00 06 00
            // - Knight's Armour: 00 00 08 00
            // Only the whip has an effect of -10 Attack ability
            // as for the other 3 the first or second byte is 0.
            // Either the data is wrong or the original code.
            // But it's hard to reverse engineer the meaning this way.
            item.SkillPenalty1 = (Skill)dataReader.ReadByte();
            item.SkillPenalty2 = (Skill)dataReader.ReadByte();
            item.SkillPenalty1Value = dataReader.ReadByte();
            item.SkillPenalty2Value = dataReader.ReadByte();
            item.SpecialValue = dataReader.ReadByte();
            item.TextSubIndex = dataReader.ReadByte();
            item.SpellSchool = (SpellSchool)dataReader.ReadByte();
            item.SpellIndex = dataReader.ReadByte();
            item.InitialCharges = dataReader.ReadByte();
            item.UnknownByte26 = dataReader.ReadByte();
            item.MaxRecharges = dataReader.ReadByte();
            item.MaxCharges = dataReader.ReadByte();
            item.UnknownByte29 = dataReader.ReadByte();
            item.MagicArmorLevel = (sbyte)dataReader.ReadByte();
            item.MagicAttackLevel = (sbyte)dataReader.ReadByte();
            item.Flags = (ItemFlags)dataReader.ReadByte();
            item.DefaultSlotFlags = (ItemSlotFlags)dataReader.ReadByte();
            item.Classes = (ClassFlag)dataReader.ReadWord();
            item.Price = dataReader.ReadWord();
            item.Weight = dataReader.ReadWord();
            item.Name = dataReader.ReadString(19).TrimEnd(' ', '\0');

            if (dataReader.ReadByte() != 0) // end of item
                throw new AmbermoonException(ExceptionScope.Data, "Invalid item data.");
        }
    }
}
