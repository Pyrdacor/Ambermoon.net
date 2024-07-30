using System;

namespace Ambermoon.Data
{
    public class Inventory
    {
        public const int Width = 3;
        public const int Height = 8;
        public const int VisibleWidth = 3;
        public const int VisibleHeight = 4;
        public ItemSlot[] Slots { get; } = new ItemSlot[Width * Height];

        public Inventory()
        {
            for (int i = 0; i < Slots.Length; ++i)
            {
                Slots[i] = new ItemSlot
                {
                    ItemIndex = 0,
                    Amount = 0,
                    Flags = ItemSlotFlags.None
                };
            }
        }
    }
}
