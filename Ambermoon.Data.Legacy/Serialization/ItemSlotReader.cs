using System;
namespace Ambermoon.Data.Legacy
{
    internal static class ItemSlotReader
    {
        public static void ReadItemSlot(ItemSlot itemSlot, IDataReader dataReader)
        {
            itemSlot.Amount = dataReader.ReadByte();
            dataReader.ReadBytes(2); // Unknown
            itemSlot.Flags = (ItemSlotFlags)dataReader.ReadByte();
            itemSlot.ItemIndex = dataReader.ReadWord();
        }
    }
}
