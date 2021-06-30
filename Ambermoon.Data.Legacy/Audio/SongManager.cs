using Ambermoon.Data.Audio;
using Ambermoon.Data.Legacy.Serialization;
using Ambermoon.Data.Serialization;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Ambermoon.Data.Legacy.Audio
{
    public class SongManager : ISongManager
    {
        readonly Dictionary<Enumerations.Song, Song> songs = new Dictionary<Enumerations.Song, Song>();
        readonly SongPlayer songPlayer = new SongPlayer();
        int numSongsLoaded = 0;

        Song CreateSong(Enumerations.Song song, int songIndex, IDataReader dataReader, bool waitForLoading)
        {
            return new Song(song, songIndex, songPlayer, dataReader as DataReader,
                SonicArranger.Stream.ChannelMode.Mono, true, true, waitForLoading, () => ++numSongsLoaded);
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
                    Enumerations.Song.Menu => introContainer.Files[1],
                    _ => musicContainer.Files[(int)immediateLoadSongIndex.Value],
                };
                if (reader != null)
                {
                    var song = immediateLoadSongIndex.Value;
                    immediateLoadedSong = CreateSong(song, song == Enumerations.Song.Menu ? 1 : 0, reader, true);
                }
            }

            AddSong(Enumerations.Song.Intro, () => introContainer.Files[1] as DataReader);
            AddSong(Enumerations.Song.Menu, () => introContainer.Files[1] as DataReader);

            foreach (var file in musicContainer.Files)
            {
                var song = (Enumerations.Song)file.Key;
                AddSong(song, () => file.Value as DataReader);
            }

            AddSong(Enumerations.Song.Outro, () => outroContainer.Files[1] as DataReader);

            void AddSong(Enumerations.Song song, Func<DataReader> readerProvider)
            {
                if (immediateLoadSongIndex == song)
                    songs.Add(song, immediateLoadedSong);
                else
                {
                    int songIndex = song == Enumerations.Song.Menu ? 1 : 0;
                    songs.Add(song, CreateSong(song, songIndex, readerProvider(), false));
                }
            }
        }

        public void WaitForAllSongsLoaded()
        {
            while (numSongsLoaded < songs.Count)
                Thread.Sleep(25);
        }

        Song GetSongInternal(Enumerations.Song index) => songs.TryGetValue(index, out var song) ? song : null;

        public ISong GetSong(Enumerations.Song index) => GetSongInternal(index);

        public ISonicArrangerSongInfo GetSongInfo(Enumerations.Song index) => GetSongInternal(index);
    }
}
