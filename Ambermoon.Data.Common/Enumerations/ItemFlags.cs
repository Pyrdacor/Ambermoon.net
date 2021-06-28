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
        DestroyAfterUsage = 0x10,
        /// <summary>
        /// Text popup when item is viewed.
        /// </summary>
        Readable = 0x20,
        /// <summary>
        /// Item can be duplicated.
        /// </summary>
        Clonable = 0x40,
        /// <summary>
        /// Same bit as <see cref="Readable"/> but only used
        /// for weapons, armor, tools and normal items.
        /// </summary>
        Indestructible = Readable
    }
}
