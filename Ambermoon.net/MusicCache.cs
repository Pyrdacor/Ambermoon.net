using Ambermoon.Data;
using Ambermoon.Data.Audio;
using Ambermoon.Data.Enumerations;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Ambermoon
{
    internal class MusicCache : ISongManager
    {
        Dictionary<Song, ISong> songs = new Dictionary<Song, ISong>();
        static readonly Song[] Songs = Enum.GetValues<Song>().Skip(1).ToArray();
        readonly CachedSongPlayer cachedSongPlayer = new CachedSongPlayer();
        const string CacheFileName = "music.cache";
        Data.Legacy.Audio.SongManager songManager = null;

        public ISong GetSong(Song index) => songs[index];
        public bool Cached => songManager == null;

        public MusicCache(IGameData gameData, Song? immediateLoadSongIndex,
            params string[] searchPaths)
        {
            foreach (var searchPath in searchPaths)
            {
                if (LoadFromCache(searchPath))
                    return;
            }

            // No cache found
            songManager = new Data.Legacy.Audio.SongManager(gameData, immediateLoadSongIndex);

            foreach (var song in Songs)
                songs.Add(song, songManager.GetSong(song));
        }

        public void WaitForAllSongsLoaded() => songManager?.WaitForAllSongsLoaded();

        bool LoadFromCache(string path)
        {
            string file = Path.Combine(path, CacheFileName);

            if (!File.Exists(file))
                return false;

            try
            {
                using var stream = File.OpenRead(file);
                var songs = new Dictionary<Song, ISong>(Songs.Length);

                foreach (var song in Songs)
                {
                    // Each starts with the size in bytes as a 32 bit unsigned integer
                    long size = stream.ReadByte();
                    size <<= 8;
#pragma warning disable CS0675
                    size |= stream.ReadByte();
                    size <<= 8;
                    size |= stream.ReadByte();
                    size <<= 8;
                    size |= stream.ReadByte();
#pragma warning restore CS0675
                    var buffer = new byte[size];
                    stream.Read(buffer);
                    songs.Add(song, new CachedSong(cachedSongPlayer, song, buffer));
                }

                this.songs = songs;

                return true;
            }
            catch
            {
                return false;
            }
        }

        public static void Cache(ISongManager songManager, params string[] possiblePaths)
        {
            void Write(Stream stream)
            {
                foreach (var song in Songs)
                {
                    var songData = songManager.GetSong(song);

                    if (songData is Data.Legacy.Audio.ISongDataProvider songDataProvider)
                        WriteSongData(songDataProvider.GetData());
                    else
                        throw new NotSupportedException();
                }

                void WriteSongData(byte[] data)
                {
                    uint size = (uint)data.Length;
                    stream.WriteByte((byte)((size >> 24) & 0xff));
                    stream.WriteByte((byte)((size >> 16) & 0xff));
                    stream.WriteByte((byte)((size >> 8) & 0xff));
                    stream.WriteByte((byte)(size & 0xff));
                    stream.Write(data);
                }
            }

            foreach (var path in possiblePaths)
            {
                try
                {
                    using var stream = File.Create(Path.Combine(path, CacheFileName));

                    Write(stream);

                    break;
                }
                catch (NotSupportedException)
                {
                    return;
                }
                catch
                {
                    continue;
                }
            }
        }

        class CachedSongPlayer
        {
            byte[] currentStream = null;
            IAudioOutput audioOutput = null;

            public void Start(IAudioOutput audioOutput, byte[] data)
            {
                this.audioOutput = audioOutput ?? throw new ArgumentNullException(nameof(audioOutput));

                if (currentStream != data)
                {
                    Stop();
                    currentStream = data;
                    audioOutput.StreamData(data);
                }
                if (!audioOutput.Streaming)
                    audioOutput.Start();
            }

            public void Stop()
            {
                audioOutput?.Stop();
                audioOutput?.Reset();
                currentStream = null;
            }
        }

        class CachedSong : ISong, Data.Legacy.Audio.ISongDataProvider
        {
            readonly CachedSongPlayer songPlayer;
            readonly byte[] buffer;

            public CachedSong(CachedSongPlayer songPlayer, Song song, byte[] buffer)
            {
                this.songPlayer = songPlayer;
                Song = song;
                this.buffer = buffer;
            }

            public Song Song { get; }

            public byte[] GetData() => buffer;

            public void Play(IAudioOutput audioOutput)
            {
                songPlayer.Start(audioOutput, buffer);
            }

            public void Stop()
            {
                songPlayer.Stop();
            }
        }
    }
}
