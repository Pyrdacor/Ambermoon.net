using System;
using System.Collections.Generic;

namespace Ambermoon.Data
{
    public class Equipment
    {
        public Dictionary<EquipmentSlot, ItemSlot> Slots { get; } = new Dictionary<EquipmentSlot, ItemSlot>(9);

        public Equipment()
        {
            foreach (EquipmentSlot equipmentSlot in EnumHelper.GetValues<EquipmentSlot>())
            {
                if (equipmentSlot != EquipmentSlot.None)
                {
                    Slots.Add(equipmentSlot, new ItemSlot
                    {
                        ItemIndex = 0,
                        Amount = 0,
                        Flags = ItemSlotFlags.None
                    });
                }
            }
        }
    }
}
