using System;

namespace Ambermoon.Data
{
    [Flags]
    public enum MonsterFlags
    {
        None = 0,
        Undead = 0x01, // TODO: not 100% sure
        Demon = 0x02, // TODO: not 100% sure
        Boss = 0x04,
        Animal = 0x08 // TODO: not 100% sure
    }
}
