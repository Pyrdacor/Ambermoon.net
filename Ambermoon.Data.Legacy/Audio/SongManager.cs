using Ambermoon.Data.Audio;
using Ambermoon.Data.Legacy.Serialization;
using System.Collections.Generic;

namespace Ambermoon.Data.Legacy.Audio
{
    public class SongManager : ISongManager
    {
        readonly Dictionary<int, ISong> songs = new Dictionary<int, ISong>();
        readonly SongPlayer songPlayer = new SongPlayer();

        public SongManager(IFileContainer fileContainer)
        {
            foreach (var file in fileContainer.Files)
            {
                songs.Add(file.Key, new Song(songPlayer, file.Value as DataReader, false, true, true));
            }
        }

        public ISong GetSong(int index) => songs.TryGetValue(index, out var song) ? song : null;
    }
}
