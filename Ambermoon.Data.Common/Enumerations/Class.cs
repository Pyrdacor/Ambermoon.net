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
        // Note: Don't add the unknown class types here which were mentioned below.
        //       Data loading will break otherwise!
    }

    /// <summary>
    /// Items store this flags to determine which class can
    /// use or equip the item. This is also done for monsters.
    /// And there is a value of 0x7fff used for "usable by all
    /// classes" so there seem to be 4 more possible but unused
    /// classes beside the known 11.
    /// </summary>
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
        Animal = 0x0200,
        Monster = 0x0400,
        Unknown1 = 0x0800,
        Unknown2 = 0x1000,
        Unknown3 = 0x2000,
        Unknown4 = 0x4000,
        AllWithUnused = 0x7fff,
        All = 0x03ff
    }

    public static class ClassExtensions
    {
        public static bool Contains(this ClassFlag classes, Class @class) => classes.HasFlag((ClassFlag)(1 << (int)@class));
    }
}
