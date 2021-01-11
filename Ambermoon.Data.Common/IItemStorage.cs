using System.Collections.Generic;

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

                    if (!item.Flags.HasFlag(ItemFlags.NotImportant))
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

                    if (!item.Flags.HasFlag(ItemFlags.NotImportant))
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

                    if (!item.Flags.HasFlag(ItemFlags.NotImportant))
                        return true;
                }
            }

            return false;
        }
    }
}
