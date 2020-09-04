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
        readonly Dictionary<ItemType, string> entries = new Dictionary<ItemType, string>();
        public IReadOnlyDictionary<ItemType, string> Entries => entries;

        /// <summary>
        /// The position of the data reader should be at
        /// the start of the item type names just behind the
        /// attribute short names.
        /// 
        /// It will be behind the item type names after this.
        /// </summary>
        internal ItemTypeNames(IDataReader dataReader)
        {
            entries.Add(ItemType.None, "");

            foreach (var type in Enum.GetValues<ItemType>())
            {
                if (type != ItemType.None)
                    entries.Add(type, dataReader.ReadNullTerminatedString(AmigaExecutable.Encoding));
            }
        }
    }
}
