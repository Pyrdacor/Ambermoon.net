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
        // TODO
        DestroyAfterUsage = 0x10,
        // TODO
    }
}
