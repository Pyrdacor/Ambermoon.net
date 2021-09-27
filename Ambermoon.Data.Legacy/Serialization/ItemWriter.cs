using Ambermoon.Data.Serialization;
using System;
using System.Linq;

namespace Ambermoon.Data.Legacy.Serialization
{
    using word = UInt16;

    public static class ItemWriter
    {
        public static void WriteItem(Item item, IDataWriter dataWriter)
        {
            void WriteSignedByte(int value) => dataWriter.Write(unchecked((byte)(sbyte)value));

            dataWriter.Write((byte)item.GraphicIndex);
            dataWriter.WriteEnumAsByte(item.Type);
            dataWriter.WriteEnumAsByte(item.EquipmentSlot);
            dataWriter.Write(item.BreakChance);
            dataWriter.WriteEnumAsByte(item.Genders);
            dataWriter.Write((byte)item.NumberOfHands);
            dataWriter.Write((byte)item.NumberOfFingers);
            WriteSignedByte(item.HitPoints);
            WriteSignedByte(item.SpellPoints);
            if (item.Attribute == null)
            {
                dataWriter.Write((byte)0);
                dataWriter.Write((byte)0);
            }
            else
            {
                dataWriter.WriteEnumAsByte(item.Attribute.Value);
                WriteSignedByte(item.AttributeValue);
            }
            if (item.Ability == null)
            {
                dataWriter.Write((byte)0);
                dataWriter.Write((byte)0);
            }
            else
            {
                dataWriter.WriteEnumAsByte(item.Ability.Value);
                WriteSignedByte(item.AbilityValue);
            }
            WriteSignedByte(item.Defense);
            WriteSignedByte(item.Damage);
            dataWriter.WriteEnumAsByte(item.AmmunitionType);
            dataWriter.WriteEnumAsByte(item.UsedAmmunitionType);
            dataWriter.WriteEnumAsByte(item.SkillPenalty1);
            dataWriter.WriteEnumAsByte(item.SkillPenalty2);
            dataWriter.Write((byte)item.SkillPenalty1Value);
            dataWriter.Write((byte)item.SkillPenalty2Value);
            dataWriter.Write(item.SpecialValue);
            dataWriter.Write(item.TextSubIndex);
            dataWriter.WriteEnumAsByte(item.SpellSchool);
            dataWriter.Write(item.SpellIndex);
            dataWriter.Write(item.InitialCharges);
            dataWriter.Write(item.UnknownByte26);
            dataWriter.Write(item.MaxRecharges);
            dataWriter.Write(item.MaxCharges);
            dataWriter.Write(item.UnknownByte29);
            WriteSignedByte(item.MagicArmorLevel);
            WriteSignedByte(item.MagicAttackLevel);
            dataWriter.WriteEnumAsByte(item.Flags);
            dataWriter.WriteEnumAsByte(item.DefaultSlotFlags);
            dataWriter.WriteEnumAsWord(item.Classes);
            dataWriter.Write((word)item.Price);
            dataWriter.Write((word)item.Weight);
            if (item.Name == null)
                dataWriter.Write(Enumerable.Repeat((byte)0, 20).ToArray());
            else
                dataWriter.WriteWithoutLength(item.Name.Substring(0, Math.Min(item.Name.Length, 19)).PadRight(20, '\0'));
        }
    }
}
