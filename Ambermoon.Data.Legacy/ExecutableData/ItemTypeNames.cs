using System;
using System.Collections.Generic;

namespace Ambermoon.Data.Legacy.ExecutableData
{
    /// <summary>
    /// After the attribute short names there are the item
    /// type names like "Potion", "Shield", etc.
    /// 
    /// The ailment item type has an empty string entry.
    /// </summary>
    public class ItemTypeNames
    {
        public Dictionary<ItemType, string> Entries { get; } = new Dictionary<ItemType, string>();

        /// <summary>
        /// The position of the data reader should be at
        /// the start of the item type names just behind the
        /// attribute short names.
        /// 
        /// It will be behind the item type names after this.
        /// </summary>
        internal ItemTypeNames(IDataReader dataReader)
        {
            Entries.Add(ItemType.None, "");

            foreach (ItemType type in Enum.GetValues(typeof(ItemType)))
            {
                if (type != ItemType.None)
                    Entries.Add(type, dataReader.ReadNullTerminatedString());
            }
        }
    }
}
