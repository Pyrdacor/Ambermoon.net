using Ambermoon.Data.Audio;
using Ambermoon.Data.Legacy.Serialization;
using System.Collections.Generic;

namespace Ambermoon.Data.Legacy.Audio
{
    public class SongManager : ISongManager
    {
        readonly Dictionary<Enumerations.Song, ISong> songs = new Dictionary<Enumerations.Song, ISong>();
        readonly SongPlayer songPlayer = new SongPlayer();

        public SongManager(IFileContainer fileContainer)
        {
            foreach (var file in fileContainer.Files)
            {
                var song = (Enumerations.Song)file.Key;
                songs.Add(song, new Song(song, songPlayer, file.Value as DataReader, false, true, true));
            }
        }

        public ISong GetSong(Enumerations.Song index) => songs.TryGetValue(index, out var song) ? song : null;
    }
}
