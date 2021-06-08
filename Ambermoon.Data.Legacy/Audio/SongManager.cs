using Ambermoon.Data.Audio;
using Ambermoon.Data.Legacy.Serialization;
using System;
using System.Collections.Generic;

namespace Ambermoon.Data.Legacy.Audio
{
    public class SongManager : ISongManager
    {
        readonly Dictionary<Enumerations.Song, ISong> songs = new Dictionary<Enumerations.Song, ISong>();
        readonly SongPlayer songPlayer = new SongPlayer();

        public SongManager(IFileContainer fileContainer, int songIndexOffset = 0,
            Enumerations.Song? immediateLoadSongIndex = null)
        {
            var immediateLoadedSong = immediateLoadSongIndex == null ? null :
                new Song(immediateLoadSongIndex.Value, songPlayer,
                    fileContainer.Files[(int)immediateLoadSongIndex.Value - songIndexOffset] as DataReader, false, true, true, true);

            foreach (var file in fileContainer.Files)
            {
                var song = (Enumerations.Song)(songIndexOffset + file.Key);
                if (immediateLoadSongIndex == song)
                    songs.Add(song, immediateLoadedSong);
                else
                    songs.Add(song, new Song(song, songPlayer, file.Value as DataReader, false, true, true, false));
            }
        }

        public SongManager(IGameData gameData, Enumerations.Song? immediateLoadSongIndex = null)
        {
            if (gameData == null)
                throw new AmbermoonException(ExceptionScope.Application, "gameData must not be null.");

            Song immediateLoadedSong = null;
            var introContainer = gameData.Files["Intro_music"];
            var outroContainer = gameData.Files["Extro_music"];
            var musicContainer = gameData.Files["Music.amb"];

            if (immediateLoadSongIndex != null)
            {
                var reader = immediateLoadSongIndex.Value switch
                {
                    Enumerations.Song.Default => null,
                    Enumerations.Song.Intro => introContainer.Files[1],
                    Enumerations.Song.Outro => outroContainer.Files[1],
                    _ => musicContainer.Files[(int)immediateLoadSongIndex.Value],
                };
                if (reader != null)
                    immediateLoadedSong = new Song(immediateLoadSongIndex.Value, songPlayer, reader as DataReader, false, true, true, true);
            }

            foreach (var file in musicContainer.Files)
            {
                var song = (Enumerations.Song)file.Key;
                AddSong(song, () => file.Value as DataReader);
            }

            AddSong(Enumerations.Song.Intro, () => introContainer.Files[1] as DataReader);
            AddSong(Enumerations.Song.Outro, () => outroContainer.Files[1] as DataReader);

            void AddSong(Enumerations.Song song, Func<DataReader> readerProvider)
            {
                if (immediateLoadSongIndex == song)
                    songs.Add(song, immediateLoadedSong);
                else
                    songs.Add(song, new Song(song, songPlayer, readerProvider(), false, true, true, false));
            }
        }

        public ISong GetSong(Enumerations.Song index) => songs.TryGetValue(index, out var song) ? song : null;
    }
}
