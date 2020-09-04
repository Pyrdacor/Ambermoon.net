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
        readonly Dictionary<Ailment, string> entries = new Dictionary<Ailment, string>();
        public IReadOnlyDictionary<Ailment, string> Entries => entries;

        /// <summary>
        /// The position of the data reader should be at
        /// the start of the ailment names just behind the
        /// item type names.
        /// 
        /// It will be behind the ailment names after this.
        /// </summary>
        internal AilmentNames(IDataReader dataReader)
        {
            entries.Add(Ailment.None, "");

            foreach (var type in Enum.GetValues<Ailment>())
            {
                if (type != Ailment.None)
                    entries.Add(type, dataReader.ReadNullTerminatedString(AmigaExecutable.Encoding));
            }

            if (string.IsNullOrWhiteSpace(entries[Ailment.DeadAshes]))
                entries[Ailment.DeadAshes] = entries[Ailment.DeadCorpse];
            if (string.IsNullOrWhiteSpace(entries[Ailment.DeadDust]))
                entries[Ailment.DeadDust] = entries[Ailment.DeadCorpse];
        }
    }
}
