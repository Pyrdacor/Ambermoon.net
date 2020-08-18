using System;
using System.Collections.Generic;

namespace Ambermoon.Data.Legacy.ExecutableData
{
    /// <summary>
    /// After the <see cref="OptionNames"/> there are the
    /// song names.
    /// </summary>
    public class SongNames
    {
        public List<string> Entries { get; } = new List<string>();

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
                Entries.Add(dataReader.ReadNullTerminatedString());
            }

            dataReader.AlignToWord();
        }
    }
}
