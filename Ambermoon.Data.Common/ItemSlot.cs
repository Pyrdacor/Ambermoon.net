using System;

namespace Ambermoon.Data
{
    public class ItemSlot
    {
        public uint ItemIndex;
        public int Amount; // 0-255, 255 = unlimited (**)
        public int NumRemainingCharges; // 0-255, 255 = unlimited (**)
        public ItemSlotFlags Flags;
        /// <summary>
        /// How often the item was recharged. Only enchanters count, not the spell ChargeItem.
        /// </summary>
        public byte RechargeTimes;

        public bool Empty => Amount == 0;
        public bool Unlimited => Amount == 255;
        public bool Stacked => Amount > 1;
        public bool Draggable => ItemIndex != 0 && Amount != 0 && !Flags.HasFlag(ItemSlotFlags.Locked);

        public static ItemSlot CreateFromItem(IItemManager itemManager, uint itemIndex, ref int amount, bool forceInSingleSlot = false)
        {
            if (itemIndex == 0)
                return new ItemSlot();

            var item = itemManager.GetItem(itemIndex);
            int usedAmount = forceInSingleSlot || item.Flags.HasFlag(ItemFlags.Stackable) ? Math.Min(amount, 99) : 1;
            amount -= usedAmount;

            return new ItemSlot
            {
                ItemIndex = itemIndex,
                Amount = usedAmount,
                Flags = item.DefaultSlotFlags,
                NumRemainingCharges = item.MaxCharges == 0 ? 0 : Math.Max(1, (int)item.InitialCharges),
                RechargeTimes = item.InitialRecharges
            };
        }

        public void FillWithNewItem(IItemManager itemManager, uint itemIndex, ref int amount)
        {
            var itemSlot = CreateFromItem(itemManager, itemIndex, ref amount);

            ItemIndex = itemSlot.ItemIndex;
            Amount = itemSlot.Amount;
            Flags = itemSlot.Flags;
            NumRemainingCharges = itemSlot.NumRemainingCharges;
            RechargeTimes = itemSlot.RechargeTimes;
        }

        public int Add(ItemSlot item, int maxAmount = 99)
        {
            int amountToAdd = Math.Min(item.Amount, maxAmount);

            if (item.ItemIndex == ItemIndex)
            {
                if (Amount + amountToAdd > 99)
                {
                    item.Amount = Amount + item.Amount - 99;
                    Amount = 99;
                    return item.Amount;
                }
                else
                {
                    Amount += amountToAdd;
                    item.Remove(amountToAdd);
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
                NumRemainingCharges = item.NumRemainingCharges;
                RechargeTimes = item.RechargeTimes;
                item.Remove(amountToAdd);
                return item.Amount;
            }
        }

        public void Remove(int amount)
        {
            if (amount < 0)
                throw new AmbermoonException(ExceptionScope.Application, "Remove should not be called with negative amount.");
            if (amount > Amount)
                throw new AmbermoonException(ExceptionScope.Application, "Tried to remove more items than existed.");

            if (amount == Amount)
                Clear();
            else
                Amount -= amount;
        }

        public void Clear()
        {
            ItemIndex = 0;
            Amount = 0;
            Flags = ItemSlotFlags.None;
            NumRemainingCharges = 0;
            RechargeTimes = 0;
        }

        public void Exchange(ItemSlot item)
        {
            var itemIndex = ItemIndex;
            var amount = Amount;
            var flags = Flags;
            var numRemainingCharges = NumRemainingCharges;
            var rechargeTimes = RechargeTimes;

            ItemIndex = item.ItemIndex;
            Amount = item.Amount;
            Flags = item.Flags;
            NumRemainingCharges = item.NumRemainingCharges;
            RechargeTimes = item.RechargeTimes;

            item.ItemIndex = itemIndex;
            item.Amount = amount;
            item.Flags = flags;
            item.NumRemainingCharges = numRemainingCharges;
            item.RechargeTimes = rechargeTimes;
        }

        public void Replace(ItemSlot item)
        {
            ItemIndex = item.ItemIndex;
            Amount = item.Amount;
            Flags = item.Flags;
            NumRemainingCharges = item.NumRemainingCharges;
            RechargeTimes = item.RechargeTimes;
        }

        public ItemSlot Copy()
        {
            return new ItemSlot
            {
                ItemIndex = ItemIndex,
                Amount = Amount,
                Flags = Flags,
                NumRemainingCharges = NumRemainingCharges,
                RechargeTimes = RechargeTimes
            };
        }

        public bool Equals(ItemSlot other, bool includeChargesAndFlags = true)
        {
            if (other is null)
                return false;

            if (other == this)
                return true;

            if (includeChargesAndFlags)
            {
                return ItemIndex == other.ItemIndex &&
                    Amount == other.Amount &&
                    Flags == other.Flags &&
                    NumRemainingCharges == other.NumRemainingCharges &&
                    RechargeTimes == other.RechargeTimes;
            }

            return ItemIndex == other.ItemIndex && Amount == other.Amount;
        }
    }
}
