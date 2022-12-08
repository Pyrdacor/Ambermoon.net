using Ambermoon.AudioFormats;
using Ambermoon.Data;
using Ambermoon.Data.Audio;
using Ambermoon.Data.Enumerations;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Ambermoon
{
    class MusicManager : ISongManager, IDisposable
    {
        delegate ExternalSong MusicLoader(MusicManager musicManager, Song song, string filename);

        static readonly Dictionary<string, MusicLoader> SupportedExtensions = new()
        {
            { "mp3", LoadMp3 }
        };
        readonly IConfiguration configuration;
        readonly Dictionary<Song, ISong> externalSongs = new();
        IAudioOutput audioOutput = null;
        IAudioStream currentStream = null;
        protected static readonly Song[] Songs = Enum.GetValues<Song>().Skip(1).ToArray();
        readonly Data.Legacy.Audio.SongManager songManager = null;
        readonly object startMutex = new object();

        public MusicManager(IConfiguration configuration, ILegacyGameData gameData)
        {
            songManager = new Data.Legacy.Audio.SongManager(gameData);

            this.configuration = configuration;

            LoadExternalSongs();
        }

        public void Dispose()
        {
            foreach (var song in externalSongs)
            {
                if (song.Value is IDisposable d)
                    d.Dispose();
            }

            externalSongs.Clear();
        }

        static ExternalSong LoadMp3(MusicManager musicManager, Song song, string filename)
        {
            var mp3Song = new Mp3Song(musicManager, song, filename);

            if (mp3Song.SongDuration == TimeSpan.Zero)
                return null;

            return mp3Song;
        }

        void LoadExternalSongs()
        {
            string[] files;
            const int songCount = 35;

            try
            {
                string musicPath = Path.Combine(Configuration.ReadonlyBundleDirectory, "music");

                if (OperatingSystem.IsMacOS() &&
                    Configuration.ReadonlyBundleDirectory != Configuration.ExecutableDirectoryPath &&
                    !Directory.Exists(musicPath))
                {
                    musicPath = Path.Combine(Configuration.ExecutableDirectoryPath, "music");
                }

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
            var comparer = new EqualityComparer<Tuple<int, string, bool>, int>(t => t.Item1);
            var fileNameComparer = new FileNameComparer();
            var musicFileRegex = new Regex($"^([0-9]+)([ _-][^.]+)?[.]{extension.Key}$", RegexOptions.IgnoreCase);
            var musicFiles = files.OrderBy(f => f, fileNameComparer).Select(f =>
            {
                var match = musicFileRegex.Match(Path.GetFileName(f));

                if (match.Success)
                    return Tuple.Create(int.Parse(match.Groups[1].Value), f, true);

                return Tuple.Create(0, (string)null, false);
            }).Where(f => f.Item3).Distinct(comparer).ToDictionary(f => f.Item1, f => f.Item2);

            foreach (var song in Enum.GetValues<Song>())
            {
                if (externalSongs.ContainsKey(song))
                    continue; // already loaded

                if (musicFiles.ContainsKey((int)song))
                {
                    var music = extension.Value?.Invoke(this, song, musicFiles[(int)song]);

                    if (music is not null)
                        externalSongs.Add(song, music);
                }
            }
        }

        public ISong GetSong(Song index)
        {
            if (configuration.ExternalMusic)
            {
                if (externalSongs.TryGetValue(index, out var song))
                    return song;
            }

            return songManager.GetSong(index);
        }

        public void Start(IAudioOutput audioOutput, IAudioStream audioStream, int channels, int sampleRate, bool sample8Bit)
        {
            lock (startMutex)
            {
                this.audioOutput = audioOutput ?? throw new ArgumentNullException(nameof(audioOutput));

                if (currentStream != audioStream)
                {
                    Stop();
                    currentStream = audioStream;
                    audioOutput.StreamData(audioStream, channels, sampleRate, sample8Bit);
                }
                if (!audioOutput.Streaming)
                    audioOutput.Start();
            }
        }

        public void Stop()
        {
            audioOutput?.Stop();
            audioOutput?.Reset();
            currentStream = null;
        }
    }
}
