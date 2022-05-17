using Ambermoon.Data.Legacy.Serialization;
using Ambermoon.Data.Serialization;
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
        readonly Dictionary<SpellSchool, string> entries = new Dictionary<SpellSchool, string>();
        public IReadOnlyDictionary<SpellSchool, string> Entries => entries;

        internal SpellTypeNames(List<string> names)
        {
            if (names.Count != 7)
                throw new AmbermoonException(ExceptionScope.Data, "Invalid number of spell school names.");

            for (int i = 0; i < names.Count; ++i)
                entries.Add((SpellSchool)i, names[i]);
        }

        /// <summary>
        /// The position of the data reader should be at
        /// the start of the spell type names just behind the
        /// song names.
        /// 
        /// It will be behind the spell type names after this.
        /// </summary>
        internal SpellTypeNames(IDataReader dataReader)
        {
            foreach (var type in Enum.GetValues<SpellSchool>())
            {
                entries.Add(type, dataReader.ReadNullTerminatedString(AmigaExecutable.Encoding));
            }
        }
    }
}
