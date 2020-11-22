namespace Ambermoon.Data
{
    public interface IItemStorage
    {
        void ResetItem(int slot, ItemSlot item);
        ItemSlot GetSlot(int slot);

        ItemSlot[,] Slots { get; }
        uint Gold { get; set; }
        uint Food { get; set; }
        bool AllowsItemDrop { get; set; }
    }
}
