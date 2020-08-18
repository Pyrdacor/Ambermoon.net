using Ambermoon.Data.Enumerations;
using System;
using System.Collections.Generic;

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
        public Dictionary<AutomapType, string> Entries { get; } = new Dictionary<AutomapType, string>();

        /// <summary>
        /// The position of the data reader should be at
        /// the start of the automap names just behind the
        /// messages.
        /// 
        /// It will be behind the automap names after this.
        /// </summary>
        internal AutomapNames(IDataReader dataReader)
        {
            foreach (var type in Enum.GetValues(typeof(AutomapType)))
            {
                Entries.Add((AutomapType)type, dataReader.ReadNullTerminatedString());
            }

            dataReader.AlignToWord();
        }
    }
}
