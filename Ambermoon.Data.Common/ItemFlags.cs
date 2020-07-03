using System;

namespace Ambermoon.Data
{
    [Flags]
    public enum ItemFlags
    {
        None = 0,
        Broken = 0x02,
        // TODO: curse?, identified?, etc
    }
}
