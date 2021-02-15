using Ambermoon.Data.Serialization;
using System;
using System.Linq;

namespace Ambermoon.Data
{
    public class Merchant : IItemStorage
    {
        public const int SlotsPerRow = 6;
        public const int SlotRows = 4;

        public ItemSlot[,] Slots { get; } = new ItemSlot[6, 4];
        public bool AllowsItemDrop { get; set; } = false;
        public uint AvailableGold { get; set; } = 0;

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

        public void AddItems(IItemManager itemManager, uint itemIndex, uint amount)
        {
            if (amount == 0)
                return;

            var item = itemManager.GetItem(itemIndex);
            var slots = Slots.ToList();
            uint remainingAmount = amount;

            // Note: Merchants can stack all items.
            foreach (var slot in slots.Where(s => s.ItemIndex == itemIndex))
            {
                if (slot.Amount > 99) // unlimited stack slot
                    return;
                else
                {
                    remainingAmount -= (uint)Math.Min((int)remainingAmount, 99 - slot.Amount);

                    if (remainingAmount == 0)
                        return;
                }
            }

            if (remainingAmount > 99)
                throw new AmbermoonException(ExceptionScope.Application, "Cannot add more than 99 items at once to a merchant.");

            var emptySlot = slots.FirstOrDefault(s => s.Empty);

            if (emptySlot == null)
                throw new AmbermoonException(ExceptionScope.Application, "Tried to add item to a full merchant.");

            emptySlot.ItemIndex = itemIndex;
            emptySlot.Amount = (int)remainingAmount;
            // TODO: flags like curse, charges, etc?
        }

        public void TakeItems(int column, int row, uint amount)
        {
            if (amount == 0)
                return;

            var slot = Slots[column, row];

            if (slot == null || slot.Amount < amount)
                throw new AmbermoonException(ExceptionScope.Application, "Taking more items from a merchant slot than he has.");

            slot.Amount -= (int)amount;

            if (slot.Amount == 0)
            {
                slot.ItemIndex = 0;
                slot.Flags = 0;
                slot.NumRemainingCharges = 0;
            }
        }
    }
}
