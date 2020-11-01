using Ambermoon.Data.Serialization;

namespace Ambermoon.Data.Legacy.Serialization
{
    public class MerchantReader : IMerchantReader
    {
        public void ReadMerchant(Merchant merchant, IDataReader dataReader)
        {
            for (int y = 0; y < 4; ++y)
            {
                for (int x = 0; x < 6; ++x)
                {
                    var itemSlot = new ItemSlot();
                    ItemSlotReader.ReadItemSlot(itemSlot, dataReader);
                    merchant.Slots[x, y] = itemSlot;
                }
            }
        }
    }
}
