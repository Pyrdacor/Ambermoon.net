using Ambermoon.Data.Serialization;

namespace Ambermoon.Data.Legacy.Serialization
{
    public class MerchantWriter : IMerchantWriter
    {
        public void WriteMerchant(Merchant merchant, IDataWriter dataWriter)
        {
            for (int y = 0; y < 4; ++y)
            {
                for (int x = 0; x < 6; ++x)
                {
                    ItemSlotWriter.WriteItemSlot(merchant.Slots[x, y], dataWriter);
                }
            }
        }
    }
}
