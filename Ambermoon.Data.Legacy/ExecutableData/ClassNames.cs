using Ambermoon.Data.Legacy.Serialization;
using Ambermoon.Data.Serialization;
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

        internal ClassNames(List<string> names)
        {
            if (names.Count < 9 || names.Count > 11)
                throw new AmbermoonException(ExceptionScope.Data, "Invalid number of class names.");

            for (int i = 0; i < names.Count; ++i)
                entries.Add((Class)i, names[i]);

            if (names.Count < 10)
                entries.Add(Class.Animal, "");
            if (names.Count < 11)
                entries.Add(Class.Monster, "");
        }

        /// <summary>
        /// The position of the data reader should be at
        /// the start of the class names just behind the
        /// language names.
        /// 
        /// It will be behind the class names after this.
        /// </summary>
        internal ClassNames(IDataReader dataReader)
        {
            foreach (var type in EnumHelper.GetValues<Class>())
            {
                entries.Add(type, dataReader.ReadNullTerminatedString(AmigaExecutable.Encoding));
            }
        }
    }
}
