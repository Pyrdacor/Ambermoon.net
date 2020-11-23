using Ambermoon.Data.Serialization;

namespace Ambermoon.Data
{
    public class Merchant : IItemStorage
    {
        public const int SlotsPerRow = 6;
        public const int SlotRows = 4;

        public ItemSlot[,] Slots { get; } = new ItemSlot[6, 4];
        public bool AllowsItemDrop { get; set; } = false;

        private Merchant()
        {

        }

        public static Merchant Load(IMerchantReader merchantReader, IDataReader dataReader)
        {
            var merchant = new Merchant();

            merchantReader.ReadMerchant(merchant, dataReader);

            return merchant;
        }

        public void ResetItem(int slot, ItemSlot item)
        {
            int column = slot % SlotsPerRow;
            int row = slot / SlotsPerRow;

            if (Slots[column, row].Add(item) != 0)
                throw new AmbermoonException(ExceptionScope.Application, "Unable to reset chest item.");
        }

        public ItemSlot GetSlot(int slot) => Slots[slot % SlotsPerRow, slot / SlotsPerRow];
    }
}
