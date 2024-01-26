using Ambermoon.Data.Legacy.Serialization;
using Ambermoon.Data.Serialization;
using System.Collections.Generic;

namespace Ambermoon.Data.Legacy.ExecutableData
{
    /// <summary>
    /// After the <see cref="SpellNames"/> there are the
    /// language names like "Elfish", "Felinic", etc.
    /// </summary>
    public class LanguageNames
    {
        readonly Dictionary<Language, string> entries = new Dictionary<Language, string>();
        public IReadOnlyDictionary<Language, string> Entries => entries;

        internal LanguageNames(List<string> names)
        {
            if (names.Count != 8)
                throw new AmbermoonException(ExceptionScope.Data, "Invalid number of language names.");

            for (int i = 0; i < names.Count; ++i)
                entries.Add((Language)(1 << i), names[i]);
        }

        /// <summary>
        /// The position of the data reader should be at
        /// the start of the language names just behind the
        /// spell names.
        /// 
        /// It will be behind the language names after this.
        /// </summary>
        internal LanguageNames(IDataReader dataReader)
        {
            entries.Add(Language.None, "");

            foreach (var type in EnumHelper.GetValues<Language>())
            {
                if (type != Language.None)
                    entries.Add(type, dataReader.ReadNullTerminatedString(AmigaExecutable.Encoding));
            }
        }
    }
}
