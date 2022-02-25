using Ambermoon.Data.Audio;
using AudioTrack = Android.Media.AudioTrack;
using Media = Android.Media;

namespace Ambermoon.Audio.Android
{
    public class AudioOutput : IAudioOutput, IDisposable
    {
        readonly Dictionary<byte[], AudioTrack> audioTracks = new();
        bool disposed = false;
        float volume = 1.0f;
        bool enabled = true;
        AudioTrack? currentTrack = null;

        public AudioOutput(int channels = 1, int sampleRate = 44100)
        {
            Available = true;
        }

        private AudioTrack GetTrack(byte[] data, int channels, int sampleRate, bool sample8Bit)
        {
            if (audioTracks.TryGetValue(data, out var track))
                return track;

#pragma warning disable 0618
            track = new AudioTrack(Media.Stream.Music, sampleRate,
                    channels == 2 ? Media.ChannelOut.Stereo : Media.ChannelOut.Mono,
                    sample8Bit ? Media.Encoding.Pcm8bit : Media.Encoding.Pcm16bit, data.Length, Media.AudioTrackMode.Static);
#pragma warning restore 0618

            track.Write(data, 0, data.Length);

            audioTracks.Add(data, track);

            return track;
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
                    currentTrack?.Stop();
                    currentTrack?.SetPlaybackHeadPosition(0);
                    currentTrack = null;
                }

                enabled = value;
            }
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public bool Streaming { get; private set; }

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
                {
                    foreach (var track in audioTracks)
                        track.Value.SetVolume(volume);
                }
            }
        }

        public void Dispose()
        {
            if (disposed)
                return;

            if (Available)
            {
                Stop();

                foreach (var track in audioTracks)
                    track.Value.Dispose();
                audioTracks.Clear();
            }

            currentTrack = null;
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

            if (currentTrack == null)
                return;

            Streaming = true;

            currentTrack.Play();
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

            currentTrack!.Stop();
            currentTrack = null;
            Streaming = false;
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public void StreamData(byte[] data, int channels = 1, int sampleRate = 44100, bool sample8Bit = true)
        {
            if (!Available)
                return;

            Stop();

            currentTrack = GetTrack(data, channels, sampleRate, sample8Bit);

            Reset();
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public void Reset()
        {
            if (!Available)
                return;

            currentTrack?.SetPlaybackHeadPosition(0);
        }
    }
}