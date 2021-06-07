using Ambermoon.Data.Legacy.Serialization;
using Ambermoon.Data.Serialization;
using System.Collections.Generic;

namespace Ambermoon.Data.Legacy.ExecutableData
{
    /// <summary>
    /// After the <see cref="OptionNames"/> there are the
    /// song names.
    /// </summary>
    public class SongNames
    {
        readonly List<string> entries = new List<string>();
        public IReadOnlyList<string> Entries => entries.AsReadOnly();

        const int NumSongs = 32;

        /// <summary>
        /// The position of the data reader should be at
        /// the start of the song names just behind the
        /// option names.
        /// 
        /// It will be behind the song names after this.
        /// </summary>
        internal SongNames(IDataReader dataReader)
        {
            for (int i = 0; i < NumSongs; ++i)
            {
                entries.Add(dataReader.ReadNullTerminatedString(AmigaExecutable.Encoding));
            }

            dataReader.AlignToWord();
        }
    }
}
