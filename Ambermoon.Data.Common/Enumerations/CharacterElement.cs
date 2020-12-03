using System;

namespace Ambermoon.Data
{
    [Flags]
    public enum CharacterElement
    {
        None = 0,
        Unknown0 = 0x01,
        Psychic = 0x02,
        Ghost = 0x04,
        Undead = 0x08,
        Earth = 0x10,
        Wind = 0x20,
        Fire = 0x40,
        Water = 0x80
    }
}
