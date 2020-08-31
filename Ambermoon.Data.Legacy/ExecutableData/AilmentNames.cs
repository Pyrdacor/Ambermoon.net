using System;
using System.Collections.Generic;

namespace Ambermoon.Data.Legacy.ExecutableData
{
    /// <summary>
    /// After the <see cref="ItemTypeNames"/> there are the
    /// ailment names like "Sleep", "Panic", etc.
    /// 
    /// Only the first of the 3 dead ailments has a text.
    /// The other two are empty strings and use the other one.
    /// 
    /// The unused ailment has an empty text as well. Maybe a
    /// relict from Amberstar data.
    /// </summary>
    public class AilmentNames
    {
        public Dictionary<Ailment, string> Entries { get; } = new Dictionary<Ailment, string>();

        /// <summary>
        /// The position of the data reader should be at
        /// the start of the ailment names just behind the
        /// item type names.
        /// 
        /// It will be behind the ailment names after this.
        /// </summary>
        internal AilmentNames(IDataReader dataReader)
        {
            Entries.Add(Ailment.None, "");

            foreach (var type in Enum.GetValues<Ailment>())
            {
                if (type != Ailment.None)
                    Entries.Add(type, dataReader.ReadNullTerminatedString());
            }

            if (string.IsNullOrWhiteSpace(Entries[Ailment.DeadAshes]))
                Entries[Ailment.DeadAshes] = Entries[Ailment.DeadCorpse];
            if (string.IsNullOrWhiteSpace(Entries[Ailment.DeadDust]))
                Entries[Ailment.DeadDust] = Entries[Ailment.DeadCorpse];
        }
    }
}
