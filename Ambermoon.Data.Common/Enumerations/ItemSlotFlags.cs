using System;

namespace Ambermoon.Data
{
    [Flags]
    public enum ItemSlotFlags
    {
        None = 0,
        Identified = 0x01, // only used for magic items
        Broken = 0x02,
        // TODO: curse?, identified?, etc
    }
}
