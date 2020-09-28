using Ambermoon.Data.Enumerations;
using System.Collections.Generic;

namespace Ambermoon.Data.Legacy.ExecutableData
{
    /// <summary>
    /// After the <see cref="AutomapNames"/> there are the
    /// original option names like "Music", "Fast battle mode", etc.
    /// </summary>
    public class OptionNames
    {
        readonly Dictionary<Option, string> entries = new Dictionary<Option, string>();
        public IReadOnlyDictionary<Option, string> Entries => entries;

        /// <summary>
        /// The position of the data reader should be at
        /// the start of the option names just behind the
        /// automap names.
        /// 
        /// It will be behind the option names after this.
        /// </summary>
        internal OptionNames(IDataReader dataReader)
        {
            foreach (var type in Enum.GetValues<Option>())
            {
                entries.Add(type, dataReader.ReadNullTerminatedString(AmigaExecutable.Encoding));
            }

            dataReader.AlignToWord();
        }
    }
}
