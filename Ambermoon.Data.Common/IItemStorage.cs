namespace Ambermoon.Data
{
    public interface IItemStorage
    {
        void ResetItem(int slot, ItemSlot item);
        ItemSlot GetSlot(int slot);

        ItemSlot[,] Slots { get; }
        bool AllowsItemDrop { get; set; }
    }
}
