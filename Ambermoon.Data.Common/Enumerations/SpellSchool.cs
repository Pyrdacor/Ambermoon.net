using System;

namespace Ambermoon.Data
{
    public enum SpellSchool
    {
        Healing,
        Alchemistic,
        Mystic,
        Destruction,
        Unknown1,
        Unknown2,
        Function // lockpicking, call eagle, play elf harp etc
    }

    public static class SpellSchoolExtensions
    {
        public static SpellSchool? ToSpellSchool(this Class @class) => @class switch
        {
            Class.Adventurer => SpellSchool.Alchemistic,
            Class.Paladin => SpellSchool.Healing,
            Class.Ranger => SpellSchool.Mystic,
            Class.Healer => SpellSchool.Healing,
            Class.Alchemist => SpellSchool.Alchemistic,
            Class.Mystic => SpellSchool.Mystic,
            Class.Mage => SpellSchool.Destruction,
            _ => null
        };
    }

    [Flags]
    public enum SpellTypeMastery
    {
        None = 0x00,
        Healing = 0x01,
        Alchemistic = 0x02,
        Mystic = 0x04,
        Destruction = 0x08,
        Unused1 = 0x10,
        Unused2 = 0x20,
        Function = 0x40,
        All = 0x7f,
        Mastered = 0x80
    }

    [Flags]
    public enum SpellTypeImmunity
    {
        None = 0x00,
        Healing = 0x01,
        Alchemistic = 0x02,
        Mystic = 0x04,
        Destruction = 0x08,
        Unused1 = 0x10,
        Unused2 = 0x20,
        Function = 0x40,
        Unused = 0x80
    }
}
