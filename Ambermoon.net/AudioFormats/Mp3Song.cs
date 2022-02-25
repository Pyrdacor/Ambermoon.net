using Ambermoon.Data.Audio;
using Ambermoon.Data.Enumerations;
using NAudio.Wave;
using System;
using System.Threading.Tasks;

namespace Ambermoon.AudioFormats
{
#if WINDOWS
    class Mp3Song : ExternalSong
    {
        readonly MusicManager musicManager;
        byte[] buffer;
        int channels;
        int sampleRate;
        bool sample8Bit;

        public Mp3Song(MusicManager musicManager, Song song, string filename, bool waitForLoading = false)
        {
            this.musicManager = musicManager;
            Song = song;

            bool Load()
            {
                try
                {
                    var builder = new Mp3FileReaderBase.FrameDecompressorBuilder(format => new AcmMp3FrameDecompressor(format));
                    using var reader = new Mp3FileReaderBase(filename, builder);
                    if (reader.Mp3WaveFormat.Channels < 1 || reader.Mp3WaveFormat.Channels > 2 ||
                        (reader.Mp3WaveFormat.BitsPerSample != 0 && reader.Mp3WaveFormat.BitsPerSample != 8 && reader.Mp3WaveFormat.BitsPerSample != 16))
                        return false; // invalid format
                    buffer = new byte[reader.Length];
                    reader.Read(buffer, 0, buffer.Length);
                    SongDuration = reader.TotalTime;
                    channels = reader.Mp3WaveFormat.Channels;
                    sampleRate = reader.Mp3WaveFormat.SampleRate;
                    sample8Bit = reader.Mp3WaveFormat.BitsPerSample == 8;
                    return true;
                }
                catch
                {
                    return false;
                }
            }

            if (waitForLoading)
                Load();
            else
                LoadTask = Task.Run(Load);
        }

        public override Song Song { get; }

        public override TimeSpan SongDuration { get; protected set; } = TimeSpan.Zero;

        public override void Play(IAudioOutput audioOutput, bool waitTillLoaded)
        {
            if (buffer == null)
            {
                if (waitTillLoaded)
                {
                    LoadTask.Wait();
                    Start();
                }
                else
                    LoadTask.GetAwaiter().OnCompleted(Start);
            }
            else
                Start();

            void Start() => musicManager.Start(audioOutput, buffer, channels, sampleRate, sample8Bit);
        }

        public override void Stop()
        {
            musicManager.Stop();
        }
    }
#endif
}
