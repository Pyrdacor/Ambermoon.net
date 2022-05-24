using System.ComponentModel;

namespace Ambermoon.Data
{
    public enum ItemType : byte
    {
        None,
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
        Condition
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class ItemTypeExtensions
    {
        public static bool IsEquipment(this ItemType itemType) => itemType switch
        {
            ItemType.Armor => true,
            ItemType.Headgear => true,
            ItemType.Footgear => true,
            ItemType.Shield => true,
            ItemType.CloseRangeWeapon => true,
            ItemType.LongRangeWeapon => true,
            ItemType.Ammunition => true,
            ItemType.Amulet => true,
            ItemType.Brooch => true,
            ItemType.Ring => true,
            _ => false
        };

        public static EquipmentSlot ToEquipmentSlot(this ItemType itemType) => itemType switch
        {
            ItemType.Armor => EquipmentSlot.Body,
            ItemType.Headgear => EquipmentSlot.Head,
            ItemType.Footgear => EquipmentSlot.Feet,
            ItemType.Shield => EquipmentSlot.LeftHand,
            ItemType.CloseRangeWeapon => EquipmentSlot.RightHand,
            ItemType.LongRangeWeapon => EquipmentSlot.RightHand,
            ItemType.Ammunition => EquipmentSlot.LeftHand,
            ItemType.Amulet => EquipmentSlot.Neck,
            ItemType.Brooch => EquipmentSlot.Chest,
            ItemType.Ring => EquipmentSlot.RightFinger,
            _ => EquipmentSlot.None
        };
    }
}
