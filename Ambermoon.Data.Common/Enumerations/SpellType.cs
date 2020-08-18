using System;

namespace Ambermoon.Data
{
    public enum SpellType
    {
        Healing,
        Alchemistic,
        Mystic,
        Destruction,
        Unknown1,
        Unknown2,
        Function // lockpicking, call eagle, play elf harp etc
    }

    [Flags]
    public enum SpellTypeMastery
    {
        None = 0x00,
        Healing = 0x01,
        Alchemistic = 0x02,
        Mystic = 0x04,
        Destruction = 0x08,
        Mastered = 0x80
    }
}
