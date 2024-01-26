using Ambermoon.Data.Legacy.Serialization;
using Ambermoon.Data.Serialization;
using System.Collections.Generic;

namespace Ambermoon.Data.Legacy.ExecutableData
{
    /// <summary>
    /// After the <see cref="ClassNames"/> there are the
    /// race names like "Human", "Dwarf", etc.
    /// </summary>
    public class RaceNames
    {
        readonly Dictionary<Race, string> entries = new Dictionary<Race, string>();
        public IReadOnlyDictionary<Race, string> Entries => entries;

        internal RaceNames(List<string> names)
        {
            if (names.Count != 15)
                throw new AmbermoonException(ExceptionScope.Data, "Invalid number of race names.");

            for (int i = 0; i < names.Count; ++i)
                entries.Add((Race)i, names[i]);

            entries.Add(Race.Unknown15, "");
        }

        /// <summary>
        /// The position of the data reader should be at
        /// the start of the race names just behind the
        /// class names.
        /// 
        /// It will be behind the race names after this.
        /// </summary>
        internal RaceNames(IDataReader dataReader)
        {
            foreach (var type in EnumHelper.GetValues<Race>())
            {
                entries.Add(type, dataReader.ReadNullTerminatedString(AmigaExecutable.Encoding));
            }
        }
    }
}
