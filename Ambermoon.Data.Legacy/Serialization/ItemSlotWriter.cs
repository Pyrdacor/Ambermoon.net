using Ambermoon.Data.Serialization;

namespace Ambermoon.Data.Legacy.Serialization
{
    internal static class ItemSlotWriter
    {
        public static void WriteItemSlot(ItemSlot itemSlot, IDataWriter dataWriter)
        {
            dataWriter.Write((byte)itemSlot.Amount);
            dataWriter.Write((byte)itemSlot.NumRemainingCharges);
            dataWriter.Write(itemSlot.RechargeTimes);
            dataWriter.WriteEnumAsByte(itemSlot.Flags);
            dataWriter.Write((ushort)itemSlot.ItemIndex);
        }
    }
}
