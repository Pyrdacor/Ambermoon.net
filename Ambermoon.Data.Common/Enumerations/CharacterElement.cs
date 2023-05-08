using System;

namespace Ambermoon.Data
{
    [Flags]
    public enum CharacterElement : byte
    {
        None = 0,
        Mental = 0x01,
        Spirit = 0x02,
        Physical = 0x04,
        Undead = 0x08,
        Earth = 0x10,
        Wind = 0x20,
        Fire = 0x40,
        Water = 0x80
    }
}
