namespace Ambermoon.Data
{
    public class ItemSlot
    {
        public uint ItemIndex;
        public int Amount; // 0-255, 255 = unlimited (**)
        public ItemSlotFlags Flags;
        // TODO ...
    }
}
