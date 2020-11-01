using Ambermoon.Data.Legacy.Serialization;
using Ambermoon.Data.Serialization;
using System.Collections.Generic;

namespace Ambermoon.Data.Legacy.ExecutableData
{
    /// <summary>
    /// After the <see cref="RaceNames"/> there are the
    /// ability names like "Attack", "Parry", etc.
    /// </summary>
    public class AbilityNames
    {
        readonly Dictionary<Ability, string> entries = new Dictionary<Ability, string>();
        readonly Dictionary<Ability, string> shortNames = new Dictionary<Ability, string>();
        public IReadOnlyDictionary<Ability, string> Entries => entries;
        public IReadOnlyDictionary<Ability, string> ShortNames => shortNames;

        /// <summary>
        /// The position of the data reader should be at
        /// the start of the ability names just behind the
        /// race names.
        /// 
        /// It will be behind the ability names after this.
        /// </summary>
        internal AbilityNames(IDataReader dataReader)
        {
            foreach (var type in Enum.GetValues<Ability>())
            {
                entries.Add(type, dataReader.ReadNullTerminatedString(AmigaExecutable.Encoding));
            }
        }

        internal void AddShortNames(IDataReader dataReader)
        {
            foreach (var type in Enum.GetValues<Ability>())
            {
                shortNames.Add(type, dataReader.ReadNullTerminatedString(AmigaExecutable.Encoding));
            }
        }
    }
}
