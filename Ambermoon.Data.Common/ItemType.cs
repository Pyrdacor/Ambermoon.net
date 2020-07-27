namespace Ambermoon.Data
{
    public enum ItemType : byte
    {
        Armor,
        Headgear,
        Footgear,
        Shield,
        CloseRangeWeapon,
        LongRangeWeapon,
        Ammunition,
        TextScroll,
        SpellScroll,
        Potion,
        Amulet,
        Brooch,
        Ring,
        Gem,
        Tool,
        Key,
        NormalItem, // collectable / loot
        MagicalItem, // lantern, torch, etc
        SpecialItem, // clock, monster eye, compass, etc
        Transportation, // witch broom, flute, magical flying disc, etc
        Ailment = 21
    }
}
