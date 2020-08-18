using System;
using System.Collections.Generic;

namespace Ambermoon.Data.Legacy.ExecutableData
{
    /// <summary>
    /// After the <see cref="SongNames"/> there are the spell
    /// type names like "Destruction", "Mystic", etc.
    /// 
    /// There are 2 spell types which are not used and which
    /// have an empty text entry.
    /// </summary>
    public class SpellTypeNames
    {
        public Dictionary<SpellType, string> Entries { get; } = new Dictionary<SpellType, string>();

        /// <summary>
        /// The position of the data reader should be at
        /// the start of the spell type names just behind the
        /// song names.
        /// 
        /// It will be behind the spell type names after this.
        /// </summary>
        internal SpellTypeNames(IDataReader dataReader)
        {
            foreach (SpellType type in Enum.GetValues(typeof(SpellType)))
            {
                Entries.Add(type, dataReader.ReadNullTerminatedString());
            }
        }
    }
}
