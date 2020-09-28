namespace Ambermoon.Data
{
    public interface IItemStorage
    {
        void ResetItem(int slot, ItemSlot item);

        ItemSlot[,] Slots { get; }
        uint Gold { get; set; }
        uint Food { get; set; }
    }
}
