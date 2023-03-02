using Ambermoon.Data.Audio;
using Ambermoon.Data.Legacy.Serialization;
using Ambermoon.Data.Serialization;
using System;
using System.Collections.Generic;

namespace Ambermoon.Data.Legacy.Audio
{
    public class SongManager : ISongManager
    {
        readonly Dictionary<Enumerations.Song, Song> songs = new Dictionary<Enumerations.Song, Song>();
        readonly SongPlayer songPlayer = new SongPlayer();

        Song CreateSong(Enumerations.Song song, int songIndex, IDataReader dataReader)
        {
            return new Song(song, songIndex, songPlayer, dataReader as DataReader,
                SonicArranger.Stream.ChannelMode.Mono, true, true);
        }

        public SongManager(ILegacyGameData gameData)
        {
            if (gameData == null)
                throw new AmbermoonException(ExceptionScope.Application, "gameData must not be null.");

            var introContainer = gameData.Files["Intro_music"];
            var outroContainer = gameData.Files["Extro_music"];
            var musicContainer = gameData.Files["Music.amb"];

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
                int songIndex = song == Enumerations.Song.Menu ? 1 : 0;
                songs.Add(song, CreateSong(song, songIndex, readerProvider()));
            }
        }

        Song GetSongInternal(Enumerations.Song index) => songs.TryGetValue(index, out var song) ? song : null;

        public ISong GetSong(Enumerations.Song index) => GetSongInternal(index);

        public ISonicArrangerSongInfo GetSongInfo(Enumerations.Song index) => GetSongInternal(index);

        public ISong LoadSong(IDataReader dataReader, int songIndex, bool lpf, bool pal)
        {
            return new Song(Enumerations.Song.Default, songIndex, songPlayer, dataReader as DataReader,
                SonicArranger.Stream.ChannelMode.Mono, lpf, pal);
        }

        public static ISong LoadCustomSong(IDataReader dataReader, int songIndex, bool lpf, bool pal)
        {
            return new Song(Enumerations.Song.Default, songIndex, new SongPlayer(), dataReader as DataReader,
                SonicArranger.Stream.ChannelMode.Mono, lpf, pal);
        }
    }
}
