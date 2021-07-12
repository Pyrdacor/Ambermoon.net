using System;

namespace Ambermoon.Data
{
    [Flags]
    public enum ItemFlags
    {
        None = 0,
        /// <summary>
        /// If equipped it can't be unequipped until the curse
        /// is removed. Moreover all addtions like attributes,
        /// LP or SP are subtracted instead of added.
        /// </summary>
        Accursed = 0x01,
        /// <summary>
        /// Item is not important and therefore can be sold. Items without this
        /// flag can not be left after battles, in conversations or at merchants.
        /// They also can't be dropped or sold at merchants without this flag.
        /// </summary>
        NotImportant = 0x02,
        Stackable = 0x04,
        /// <summary>
        /// Mostly used for armor but also for some other
        /// equipment like pickaxe or Valdyn's boots.
        /// </summary>
        RemovableDuringFight = 0x08,
        /// <summary>
        /// After using the last charge the item will be destroyed.
        /// </summary>
        DestroyAfterUsage = 0x10,
        /// <summary>
        /// If set for weapons, armor, tools, text and normal items
        /// this will stop the items from breaking.
        /// </summary>
        Indestructible = 0x20,
        /// <summary>
        /// Item can be duplicated.
        /// </summary>
        Clonable = 0x40
    }
}
