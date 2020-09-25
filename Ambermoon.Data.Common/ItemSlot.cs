using System;

namespace Ambermoon.Data
{
    public class ItemSlot
    {
        public uint ItemIndex;
        public int Amount; // 0-255, 255 = unlimited (**)
        public ItemSlotFlags Flags;
        // TODO ...

        public bool Empty => Amount == 0;
        public bool Unlimited => Amount == 255;
        public bool Stacked => Amount > 1;
        public bool Draggable => ItemIndex != 0 && Amount != 0; // TODO: cursed?

        public int Add(ItemSlot item, int maxAmount = 99)
        {
            int amountToAdd = Math.Min(item.Amount, maxAmount);

            if (item.ItemIndex == ItemIndex)
            {
                if (Amount + amountToAdd > 99)
                {
                    item.Amount = Amount + amountToAdd - 99;
                    Amount = 99;
                    return item.Amount;
                }
                else
                {
                    Amount += amountToAdd;
                    item.Amount -= amountToAdd;
                    return item.Amount;
                }
            }
            else if (!Empty)
            {
                return item.Amount;
            }
            else
            {
                ItemIndex = item.ItemIndex;
                Amount = amountToAdd;
                Flags = item.Flags;
                item.Amount -= amountToAdd;
                return item.Amount;
            }
        }

        public void Clear()
        {
            ItemIndex = 0;
            Amount = 0;
            Flags = ItemSlotFlags.None;
        }

        public void Exchange(ItemSlot item)
        {
            uint itemIndex = ItemIndex;
            int amount = Amount;
            var flags = Flags;

            ItemIndex = item.ItemIndex;
            Amount = item.Amount;
            Flags = item.Flags;

            item.ItemIndex = itemIndex;
            item.Amount = amount;
            item.Flags = flags;
        }

        public void Replace(ItemSlot item)
        {
            ItemIndex = item.ItemIndex;
            Amount = item.Amount;
            Flags = item.Flags;
        }
    }
}
