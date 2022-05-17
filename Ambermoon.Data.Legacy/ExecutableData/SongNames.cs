using Ambermoon.Data.Enumerations;
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
        readonly Dictionary<Song, string> entries = new Dictionary<Song, string>();
        public IReadOnlyDictionary<Song, string> Entries => entries;

        const int NumSongs = 32;

        internal SongNames(List<string> names)
        {
            if (names.Count != NumSongs)
                throw new AmbermoonException(ExceptionScope.Data, "Invalid number of songs.");

            for (int i = 0; i < names.Count; ++i)
                entries.Add(Song.WhoSaidHiHo + i, names[i]);
        }

        /// <summary>
        /// The position of the data reader should be at
        /// the start of the song names just behind the
        /// option names.
        /// 
        /// It will be behind the song names after this.
        /// </summary>
        internal SongNames(IDataReader dataReader)
        {
            string ReadName()
            {
                var name = dataReader.ReadNullTerminatedString(AmigaExecutable.Encoding);

                if (string.IsNullOrWhiteSpace(name))
                    name = "No name";

                return name;
            }

            for (int i = 1; i <= NumSongs; ++i)
            {
                entries.Add((Song)i, ReadName());
            }

            dataReader.AlignToWord();
        }
    }
}
