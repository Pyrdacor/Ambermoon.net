using System.Collections.Generic;
using System.Linq;

namespace Ambermoon.Data
{
    public interface IItemStorage
    {
        void ResetItem(int slot, ItemSlot item);
        ItemSlot GetSlot(int slot);

        ItemSlot[,] Slots { get; }
        bool AllowsItemDrop { get; set; }
    }

    public interface ITreasureStorage : IItemStorage
    {
        uint Gold { get; set; }
        uint Food { get; set; }
        bool IsBattleLoot { get; set; }
        bool Empty { get; }
    }

    public static class ItemStorageExtensions
    {
        public static IEnumerable<ItemSlot> GetImportantItems(this IItemStorage itemStorage, IItemManager itemManager)
        {
            foreach (var slot in itemStorage.Slots)
            {
                if (slot != null && slot.ItemIndex != 0)
                {
                    var item = itemManager.GetItem(slot.ItemIndex);

                    if (!item.Flags.HasFlag(ItemFlags.Sellable))
                        yield return slot;
                }
            }
        }

        public static IEnumerable<string> GetImportantItemNames(this IItemStorage itemStorage, IItemManager itemManager)
        {
            foreach (var slot in itemStorage.Slots)
            {
                if (slot != null && slot.ItemIndex != 0)
                {
                    var item = itemManager.GetItem(slot.ItemIndex);

                    if (!item.Flags.HasFlag(ItemFlags.Sellable))
                        yield return item.Name;
                }
            }
        }

        public static bool HasAnyImportantItem(this IItemStorage itemStorage, IItemManager itemManager)
        {
            foreach (var slot in itemStorage.Slots)
            {
                if (slot != null && slot.ItemIndex != 0)
                {
                    var item = itemManager.GetItem(slot.ItemIndex);

                    if (!item.Flags.HasFlag(ItemFlags.Sellable))
                        return true;
                }
            }

            return false;
        }

        public static bool CanStoreItems(this IItemStorage itemStorage, IItemManager itemManager, ItemSlot itemSlot)
        {
            var slots = itemStorage.Slots.ToList();

            // Test if we have enough inventory slots to store the items.
            if (!slots.Any(s => s.Empty))
            {
                var item = itemManager.GetItem(itemSlot.ItemIndex);

                if (!item.Flags.HasFlag(ItemFlags.Stackable))
                    return false;

                // If no slot is empty but the item is stackable we check
                // if there are slots with the same item and look if the
                // items would fit into these slots.
                int remainingCount = itemSlot.Amount;

                foreach (var slot in slots.Where(s => s.ItemIndex == itemSlot.ItemIndex))
                    remainingCount -= (99 - slot.Amount);

                // TODO: unlimited stack slots (merchants)

                if (remainingCount > 0)
                    return false;
            }

            return true;
        }

        public static bool HasEmptySlots(this IItemStorage itemStorage) => itemStorage.Slots.ToList().Any(slot => slot.Empty);
    }
}
