using System;
using System.Collections.Generic;

namespace Ambermoon.Data.Legacy.ExecutableData
{
    /// <summary>
    /// After the <see cref="ClassNames"/> there are the
    /// race names like "Human", "Dwarf", etc.
    /// </summary>
    public class RaceNames
    {
        public Dictionary<Race, string> Entries { get; } = new Dictionary<Race, string>();

        /// <summary>
        /// The position of the data reader should be at
        /// the start of the race names just behind the
        /// class names.
        /// 
        /// It will be behind the race names after this.
        /// </summary>
        internal RaceNames(IDataReader dataReader)
        {
            foreach (Race type in Enum.GetValues(typeof(Race)))
            {
                Entries.Add(type, dataReader.ReadNullTerminatedString());
            }
        }
    }
}
