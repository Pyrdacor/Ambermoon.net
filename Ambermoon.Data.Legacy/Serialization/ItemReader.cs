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
            Skill skill = (Skill)dataReader.ReadByte();
            int skillValue = (sbyte)dataReader.ReadByte();
            if (skillValue != 0)
            {
                item.Skill = skill;
                item.SkillValue = skillValue;
            }
            item.Defense = (sbyte)dataReader.ReadByte();
            item.Damage = (sbyte)dataReader.ReadByte();
            item.AmmunitionType = (AmmunitionType)dataReader.ReadByte();
            item.UsedAmmunitionType = (AmmunitionType)dataReader.ReadByte();
            // Note: The skills are 1-based like in the original code. 0 means no penalty.
            int penaltySkill1 = dataReader.ReadByte();
            int penaltySkill2 = dataReader.ReadByte();
            uint penaltyValue1 = dataReader.ReadByte();
            uint penaltyValue2 = dataReader.ReadByte();
            if (penaltySkill1 != 0)
            {
                item.SkillPenalty1 = (Skill)(penaltySkill1 - 1);
                item.SkillPenalty1Value = penaltyValue1;
            }
            if (penaltySkill2 != 0)
            {
                item.SkillPenalty2 = (Skill)(penaltySkill2 - 1);
                item.SkillPenalty2Value = penaltyValue2;
            }
            item.SpecialValue = dataReader.ReadByte();
            item.TextSubIndex = dataReader.ReadByte();
            item.SpellSchool = (SpellSchool)dataReader.ReadByte();
            item.SpellIndex = dataReader.ReadByte();
            item.InitialCharges = dataReader.ReadByte();
            item.InitialRecharges = dataReader.ReadByte();
            item.MaxRecharges = dataReader.ReadByte();
            item.MaxCharges = dataReader.ReadByte();
            item.RechargePrice = dataReader.ReadByte();
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

            if (item.Flags.HasFlag(ItemFlags.ExtendedGraphicIndex))
                item.GraphicIndex += 256;
        }
    }
}
