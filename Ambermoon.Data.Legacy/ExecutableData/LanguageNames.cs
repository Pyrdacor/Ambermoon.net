using System;
using System.Collections.Generic;

namespace Ambermoon.Data.Legacy.ExecutableData
{
    /// <summary>
    /// After the <see cref="SpellNames"/> there are the
    /// language names like "Elfish", "Felinic", etc.
    /// </summary>
    public class LanguageNames
    {
        public Dictionary<Language, string> Entries { get; } = new Dictionary<Language, string>();

        /// <summary>
        /// The position of the data reader should be at
        /// the start of the language names just behind the
        /// spell names.
        /// 
        /// It will be behind the language names after this.
        /// </summary>
        internal LanguageNames(IDataReader dataReader)
        {
            Entries.Add(Language.None, "");

            foreach (Language type in Enum.GetValues(typeof(Language)))
            {
                if (type != Language.None)
                    Entries.Add(type, dataReader.ReadNullTerminatedString());
            }
        }
    }
}
