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

        /// <summary>
        /// The position of the data reader should be at
        /// the start of the race names just behind the
        /// class names.
        /// 
        /// It will be behind the race names after this.
        /// </summary>
        internal RaceNames(IDataReader dataReader)
        {
            foreach (var type in Enum.GetValues<Race>())
            {
                entries.Add(type, dataReader.ReadNullTerminatedString(AmigaExecutable.Encoding));
            }
        }
    }
}
