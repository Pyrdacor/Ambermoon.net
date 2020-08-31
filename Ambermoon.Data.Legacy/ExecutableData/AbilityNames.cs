using System;
using System.Collections.Generic;

namespace Ambermoon.Data.Legacy.ExecutableData
{
    /// <summary>
    /// After the <see cref="RaceNames"/> there are the
    /// ability names like "Attack", "Parry", etc.
    /// </summary>
    public class AbilityNames
    {
        public Dictionary<Ability, string> Entries { get; } = new Dictionary<Ability, string>();
        public Dictionary<Ability, string> ShortNames { get; } = new Dictionary<Ability, string>();

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
                Entries.Add(type, dataReader.ReadNullTerminatedString());
            }
        }

        internal void AddShortNames(IDataReader dataReader)
        {
            foreach (var type in Enum.GetValues<Ability>())
            {
                ShortNames.Add(type, dataReader.ReadNullTerminatedString());
            }
        }
    }
}
