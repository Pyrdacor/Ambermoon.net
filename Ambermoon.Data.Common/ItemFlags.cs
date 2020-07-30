using System;

namespace Ambermoon.Data
{
    [Flags]
    public enum ItemFlags
    {
        None = 0,
        Accursed = 0x01,
        Purchasable = 0x02,
        Stackable = 0x04,
        Unknown8 = 0x08, // TODO
        DestroyAfterUsage = 0x10,
        Readable = 0x20, // text pops up when eye cursor is used on it
        Unknown64 = 0x40 // TODO
    }
}
