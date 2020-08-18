using System;
using System.Collections.Generic;

namespace Ambermoon.Data.Legacy.ExecutableData
{
    /// <summary>
    /// After the <see cref="LanguageNames"/> there are the
    /// class names like "Adventurer", "Warrior", etc.
    /// </summary>
    public class ClassNames
    {
        public Dictionary<Class, string> Entries { get; } = new Dictionary<Class, string>();

        /// <summary>
        /// The position of the data reader should be at
        /// the start of the class names just behind the
        /// language names.
        /// 
        /// It will be behind the class names after this.
        /// </summary>
        internal ClassNames(IDataReader dataReader)
        {
            foreach (Class type in Enum.GetValues(typeof(Class)))
            {
                Entries.Add(type, dataReader.ReadNullTerminatedString());
            }
        }
    }
}
