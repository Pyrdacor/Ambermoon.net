using System;

namespace Ambermoon.Data
{
    public enum Class : byte
    {
        Adventurer,
        Warrior,
        Paladin,
        Thief,
        Ranger,
        Healer,
        Alchemist,
        Mystic,
        Mage,
        Animal, // only Necros the cat NPC on Nera's isle
        Monster // monsters who use none of the above classes
    }

    [Flags]
    public enum ClassFlag : ushort
    {
        None = 0x0000,
        Adventurer = 0x0001,
        Warrior = 0x0002,
        Paladin = 0x0004,
        Thief = 0x0008,
        Ranger = 0x0010,
        Healer = 0x0020,
        Alchemist = 0x0040,
        Mystic = 0x0080,
        Mage = 0x0100,
        All = 0x01ff
    }
}
