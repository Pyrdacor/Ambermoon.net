using System.Collections.Generic;

namespace Ambermoon.Data.Legacy.ExecutableData
{
    /// <summary>
    /// After the <see cref="LanguageNames"/> there are the
    /// class names like "Adventurer", "Warrior", etc.
    /// </summary>
    public class ClassNames
    {
        readonly Dictionary<Class, string> entries = new Dictionary<Class, string>();
        public IReadOnlyDictionary<Class, string> Entries => entries;

        /// <summary>
        /// The position of the data reader should be at
        /// the start of the class names just behind the
        /// language names.
        /// 
        /// It will be behind the class names after this.
        /// </summary>
        internal ClassNames(IDataReader dataReader)
        {
            foreach (var type in Enum.GetValues<Class>())
            {
                entries.Add(type, dataReader.ReadNullTerminatedString(AmigaExecutable.Encoding));
            }
        }
    }
}
