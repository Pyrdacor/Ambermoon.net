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
        Sellable = 0x02,
        Stackable = 0x04,
        Unknown8 = 0x08, // TODO
        DestroyAfterUsage = 0x10,
        /// <summary>
        /// Text popup when item is viewed.
        /// </summary>
        Readable = 0x20,
        Unknown64 = 0x40 // TODO
    }
}
