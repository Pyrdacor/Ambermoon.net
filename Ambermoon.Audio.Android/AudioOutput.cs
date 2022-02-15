using Ambermoon.Data.Audio;
using AudioTrack = Android.Media.AudioTrack;
using Media = Android.Media;

namespace Ambermoon.Audio.Android
{
    public class AudioOutput : IAudioOutput, IDisposable
    {
        readonly AudioTrack? audioTrack;
        bool disposed = false;
        float volume = 1.0f;
        bool enabled = true;
        byte[]? currentBuffer = null;

        public AudioOutput(int channels = 1, int sampleRate = 44100)
        {
            if (channels < 1 || channels > 2)
                throw new ArgumentOutOfRangeException(nameof(channels));

            if (sampleRate < 2000 || sampleRate > 200000)
                throw new ArgumentOutOfRangeException(nameof(sampleRate));

            try
            {
#pragma warning disable 0618
                audioTrack = new AudioTrack(Media.Stream.Music, sampleRate,
                    channels == 2 ? Media.ChannelOut.Stereo : Media.ChannelOut.Mono,
                    Media.Encoding.Pcm8bit, 10 * 44100, Media.AudioTrackMode.Static);
#pragma warning restore 0618
                audioTrack.SetVolume(1.0f);
                Available = true;
            }
            catch
            {
                audioTrack?.Dispose();
                Available = false;
                return;
            }
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public bool Available { get; private set; }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public bool Enabled
        {
            get => enabled;
            set
            {
                if (enabled == value)
                    return;

                if (!value && Available && Streaming)
                {
                    audioTrack?.Stop();
                    audioTrack?.SetPlaybackHeadPosition(0);
                }

                enabled = value;
            }
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public bool Streaming { get; private set; } = false;

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public float Volume
        {
            get => volume;
            set
            {
                value = Math.Max(0.0f, Math.Min(value, 1.0f));

                if (Util.FloatEqual(volume, value))
                    return;

                volume = value;

                if (Available)
                    audioTrack?.SetVolume(volume);
            }
        }

        public void Dispose()
        {
            if (disposed)
                return;

            if (Available)
            {
                audioTrack?.Dispose();
            }

            Streaming = false;
            Enabled = false;
            Available = false;
            disposed = true;
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public void Start()
        {
            if (!Available || !Enabled)
                return;

            if (Streaming)
                return;

            if (audioTrack == null)
                throw new NotSupportedException("Start was called without a valid source.");

            if (currentBuffer == null)
                return;

            Streaming = true;

            audioTrack?.Play();
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public void Stop()
        {
            if (!Available || !Enabled)
                return;

            if (!Streaming)
                return;

            Streaming = false;

            audioTrack?.Stop();
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public void StreamData(byte[] data)
        {
            if (!Available)
                return;

            currentBuffer = data;
            Stop();
            if (audioTrack != null)
            {
                if (audioTrack.BufferSizeInFrames < data.Length)
                    audioTrack.SetBufferSizeInFrames(data.Length);
                audioTrack.Write(data, 0, data.Length);
            }
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public void Reset()
        {
            if (!Available)
                return;

            audioTrack?.SetPlaybackHeadPosition(0);
        }
    }
}