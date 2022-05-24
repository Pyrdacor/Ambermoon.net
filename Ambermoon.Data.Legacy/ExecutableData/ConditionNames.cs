using Ambermoon.Data.Legacy.Serialization;
using Ambermoon.Data.Serialization;
using System.Collections.Generic;

namespace Ambermoon.Data.Legacy.ExecutableData
{
    /// <summary>
    /// After the <see cref="ItemTypeNames"/> there are the
    /// condition names like "Sleep", "Panic", etc.
    /// 
    /// Only the first of the 3 dead conditions has a text.
    /// The other two are empty strings and use the other one.
    /// 
    /// The unused condition has an empty text as well. Maybe a
    /// relict from Amberstar data.
    /// </summary>
    public class ConditionNames
    {
        readonly Dictionary<Condition, string> entries = new Dictionary<Condition, string>();
        public IReadOnlyDictionary<Condition, string> Entries => entries;

        internal ConditionNames(List<string> names)
        {
            if (names.Count != 16)
                throw new AmbermoonException(ExceptionScope.Data, "Invalid number of condition names.");

            entries.Add(Condition.None, "");

            for (int i = 0; i < names.Count; ++i)
                entries.Add((Condition)(1 << i), names[i]);

            if (string.IsNullOrWhiteSpace(entries[Condition.DeadAshes]))
                entries[Condition.DeadAshes] = entries[Condition.DeadCorpse];
            if (string.IsNullOrWhiteSpace(entries[Condition.DeadDust]))
                entries[Condition.DeadDust] = entries[Condition.DeadCorpse];
        }

        /// <summary>
        /// The position of the data reader should be at
        /// the start of the condition names just behind the
        /// item type names.
        /// 
        /// It will be behind the condition names after this.
        /// </summary>
        internal ConditionNames(IDataReader dataReader)
        {
            entries.Add(Condition.None, "");

            foreach (var type in Enum.GetValues<Condition>())
            {
                if (type != Condition.None)
                    entries.Add(type, dataReader.ReadNullTerminatedString(AmigaExecutable.Encoding));
            }

            if (string.IsNullOrWhiteSpace(entries[Condition.DeadAshes]))
                entries[Condition.DeadAshes] = entries[Condition.DeadCorpse];
            if (string.IsNullOrWhiteSpace(entries[Condition.DeadDust]))
                entries[Condition.DeadDust] = entries[Condition.DeadCorpse];
        }
    }
}
