using Ambermoon.Data.Legacy.Serialization;
using Ambermoon.Data.Serialization;
using System.Collections.Generic;

namespace Ambermoon.Data.Legacy.ExecutableData
{
    /// <summary>
    /// After the attribute short names there are the item
    /// type names like "Potion", "Shield", etc.
    /// 
    /// The condition item type has an empty string entry.
    /// </summary>
    public class ItemTypeNames
    {
        readonly Dictionary<ItemType, string> entries = new Dictionary<ItemType, string>();
        public IReadOnlyDictionary<ItemType, string> Entries => entries;

        internal ItemTypeNames(List<string> names)
        {
            if (names.Count != 20)
                throw new AmbermoonException(ExceptionScope.Data, "Invalid number of item type names.");

            for (int i = 0; i < names.Count; ++i)
                entries.Add((ItemType)(i + 1), names[i]);
        }

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
