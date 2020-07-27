using System;

namespace Ambermoon.Data
{
    [Flags]
    public enum ItemFlags
    {
        None = 0,
        Accursed = 0x01,
        Purchasable = 0x02
    }
}
