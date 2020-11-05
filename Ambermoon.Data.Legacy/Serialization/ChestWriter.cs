using Ambermoon.Data.Serialization;

namespace Ambermoon.Data.Legacy.Serialization
{
    public class ChestWriter : IChestWriter
    {
        public void WriteChest(Chest chest, IDataWriter dataWriter)
        {
            for (int y = 0; y < 4; ++y)
            {
                for (int x = 0; x < 6; ++x)
                {
                    ItemSlotWriter.WriteItemSlot(chest.Slots[x, y], dataWriter);
                }
            }

            dataWriter.Write((ushort)chest.Gold);
            dataWriter.Write((ushort)chest.Food);
        }
    }
}
