using Ambermoon.Data.Enumerations;
using System.Collections.Generic;
using System.Linq;

namespace Ambermoon.Data.Legacy.ExecutableData
{
    /// <summary>
    /// After the <see cref="Messages"/> there are the automap
    /// names like "Riddlemouth", "Teleporter", etc.
    /// 
    /// Some automap values have no display name and therefore
    /// an empty text entry (just the terminating 0).
    /// </summary>
    public class AutomapNames
    {
        readonly Dictionary<AutomapType, string> entries = new Dictionary<AutomapType, string>();
        public IReadOnlyDictionary<AutomapType, string> Entries => entries;

        /// <summary>
        /// The position of the data reader should be at
        /// the start of the automap names just behind the
        /// messages.
        /// 
        /// It will be behind the automap names after this.
        /// </summary>
        internal AutomapNames(IDataReader dataReader)
        {
            entries.Add(AutomapType.None, "");
            entries.Add(AutomapType.Wall, "");

            foreach (var type in Enum.GetValues<AutomapType>().Skip(2))
            {
                entries.Add(type, dataReader.ReadNullTerminatedString(AmigaExecutable.Encoding));
            }

            dataReader.AlignToWord();
        }
    }
}
