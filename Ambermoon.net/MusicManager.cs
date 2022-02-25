using Ambermoon.AudioFormats;
using Ambermoon.Data;
using Ambermoon.Data.Audio;
using Ambermoon.Data.Enumerations;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Ambermoon
{
    class MusicManager : MusicCache
    {
        delegate ExternalSong MusicLoader(MusicManager musicManager, Song song, string filename, bool waitForLoading);

        static readonly Dictionary<string, MusicLoader> SupportedExtensions = new()
        {
            { "mp3", LoadMp3 }
        };
        readonly IConfiguration configuration;
        readonly Dictionary<Song, ExternalSong> externalSongs = new();
        readonly bool externalMusic;
        IAudioOutput audioOutput = null;
        byte[] currentExternalStream = null;

        public MusicManager(IConfiguration configuration, IGameData gameData,
            Song? immediateLoadSongIndex, params string[] searchPaths)
            : base(gameData, immediateLoadSongIndex, searchPaths)
        {
            // TODO: REMOVE
            configuration.ExternalMusic = true;
            this.configuration = configuration;
            externalMusic = configuration.ExternalMusic;

            LoadExternalSongs();
        }

        static ExternalSong LoadMp3(MusicManager musicManager, Song song, string filename, bool waitForLoading)
        {
#if WINDOWS
            var mp3Song = new Mp3Song(musicManager, song, filename, waitForLoading);

            if (waitForLoading && mp3Song.SongDuration == TimeSpan.Zero)
                return null;

            return mp3Song;
#else
            return null;
#endif
        }

        void LoadExternalSongs()
        {
            string[] files;
            const int songCount = 35;

            try
            {
                string musicPath = Path.Combine(Configuration.ExecutableDirectoryPath, "music");

                if (!Directory.Exists(musicPath))
                    return;

                files = Directory.GetFiles(musicPath);
            }
            catch
            {
                return;
            }

            foreach (var extension in SupportedExtensions)
            {
                LoadExternalSongs(files, extension);

                if (externalSongs.Count >= songCount)
                    return; // all done
            }
        }

        void LoadExternalSongs(string[] files, KeyValuePair<string, MusicLoader> extension)
        {
            var musicFileRegex = new Regex($"^([0-9]+)[.]{extension.Key}$", RegexOptions.IgnoreCase);
            var musicFiles = files.Select(f =>
            {
                var match = musicFileRegex.Match(Path.GetFileName(f));

                if (match.Success)
                    return Tuple.Create(int.Parse(match.Groups[1].Value), f, true);

                return Tuple.Create(0, (string)null, false);
            }).Where(f => f.Item3).ToDictionary(f => f.Item1, f => f.Item2);

            foreach (var song in Enum.GetValues<Song>())
            {
                if (externalSongs.ContainsKey(song))
                    continue; // already loaded

                if (musicFiles.ContainsKey((int)song))
                {
                    var music = extension.Value?.Invoke(this, song, musicFiles[(int)song], externalMusic);

                    if (music is not null)
                        externalSongs.Add(song, music);
                }
            }
        }

        public override void WaitForAllSongsLoaded()
        {
            // TODO: Later use task wait here as well
            base.WaitForAllSongsLoaded();

            var tasks = externalSongs.Select(s => s.Value.LoadTask ?? Task.CompletedTask);
            Task.WaitAll(tasks.ToArray());
        }

        public override ISong GetSong(Song index)
        {
            if (configuration.ExternalMusic)
            {
                if (externalSongs.TryGetValue(index, out var song))
                    return song;
            }

            return base.GetSong(index);
        }

        public void Start(IAudioOutput audioOutput, byte[] data, int channels, int sampleRate, bool sample8Bit)
        {
            this.audioOutput = audioOutput ?? throw new ArgumentNullException(nameof(audioOutput));

            if (currentExternalStream != data)
            {
                Stop();
                currentExternalStream = data;
                audioOutput.StreamData(data, channels, sampleRate, sample8Bit);
            }
            if (!audioOutput.Streaming)
                audioOutput.Start();
        }

        public void Stop()
        {
            audioOutput?.Stop();
            audioOutput?.Reset();
            currentExternalStream = null;
        }
    }
}
