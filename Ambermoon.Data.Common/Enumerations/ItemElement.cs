using System;

namespace Ambermoon.Data;

[Flags]
public enum ItemElement : byte
{
    None = 0,
    Spirit = 1,
    Undead = 3,
    Earth = 4,
    Wind = 5,
    Fire = 6,
    Water = 7
}

public static class ItemElementExtensions
{
    public static ItemElement GetCharacterWeaponElement(this Character character, IItemManager itemManager)
    {
        var element = ItemElement.None;
        var primaryWeaponIndex = character.Equipment.Slots[EquipmentSlot.RightHand].ItemIndex;
        var leftHandItemIndex = character.Equipment.Slots[EquipmentSlot.LeftHand].ItemIndex;

        if (primaryWeaponIndex == 0 && leftHandItemIndex != 0)
        {
            var secondaryWeapon = itemManager.GetItem(leftHandItemIndex);

            if (secondaryWeapon.Type == ItemType.CloseRangeWeapon)
                element = secondaryWeapon.Element;
        }
        else if (primaryWeaponIndex != 0)
        {
            var primaryWeapon = itemManager.GetItem(primaryWeaponIndex);

            element = primaryWeapon.Element;

            // Ammunition overrides the weapon element if it has one.
            if (primaryWeapon.UsedAmmunitionType != AmmunitionType.None && leftHandItemIndex != 0)
            {
                var ammunitionElement = itemManager.GetItem(leftHandItemIndex).Element;

                if (ammunitionElement != ItemElement.None)
                    element = ammunitionElement;
            }
        }

        return element;
    }
}