using Ambermoon.Data.Audio;
using Ambermoon.Data.Enumerations;
using NLayer;
using System;
using System.Linq;

namespace Ambermoon.AudioFormats
{
    class Mp3Song : ExternalSong, IAudioStream, IDisposable
    {
        readonly MusicManager musicManager;
        MpegFile file;
        readonly int channels;
        readonly int sampleRate;
        readonly bool sample8Bit;
        Action followupSongAction = null;

        public void Dispose()
        {
            file?.Dispose();
            file = null;
        }

        static byte SampleToByte(float sample)
        {
            int value = 128 + Util.Round(sample * 128.0f);

            if (value == 256)
                value = 255;

            return (byte)value;
        }

        public Mp3Song(MusicManager musicManager, Song song, string filename)
        {
            this.musicManager = musicManager;
            Song = song;

            file = new MpegFile(filename);
            file.StereoMode = StereoMode.Both;
            SongDuration = file.Duration;
            channels = file.Channels;
            sampleRate = file.SampleRate;
            sample8Bit = true;
        }

        public override Song Song { get; }

        public override TimeSpan? SongDuration { get; protected set; } = TimeSpan.Zero;

        public bool EndOfStream => file != null && file.Position == file.Length;

        public override void Play(IAudioOutput audioOutput, ISong followupSong = null)
        {
            musicManager.Start(audioOutput, this, channels, sampleRate, sample8Bit);

            if (followupSong != null)
            {
                followupSongAction = () =>
                {
                    followupSong.Play(audioOutput, null);
                };
            }
        }

        public override void Stop()
        {
            musicManager.Stop();
            followupSongAction = null;
        }

        public byte[] Stream(TimeSpan duration)
        {
            long prevPosition = file.Position;
            int samples = Util.Round(duration.TotalSeconds * channels * sampleRate);
            float[] floatBuffer = new float[samples];
            file.ReadSamples(floatBuffer, 0, floatBuffer.Length);
            if (prevPosition == file.Position) // no read
            {
                followupSongAction?.Invoke();
                followupSongAction = null;
                file.Position = 0;
                file.ReadSamples(floatBuffer, 0, floatBuffer.Length);
            }
            return floatBuffer.Select(sample => SampleToByte(sample)).ToArray();
        }

        public void Reset()
        {
            file.Position = 0;
        }
    }
}
