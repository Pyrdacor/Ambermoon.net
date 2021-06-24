using Ambermoon.Data.Audio;
using Silk.NET.OpenAL;
using System;

namespace Ambermoon.Audio.OpenAL
{
    public unsafe class AudioOutput : IAudioOutput, IDisposable
    {
        readonly AL al = null;
        readonly ALContext alContext = null;
        readonly Device* device = null;
        readonly Context* context = null;
        readonly uint source = 0;
        bool disposed = false;
        float volume = 1.0f;
        bool enabled = true;
        readonly AudioBuffer audioBuffer;

        public AudioOutput(int channels = 1, int sampleRate = 44100)
        {
            if (channels < 1 || channels > 2)
                throw new ArgumentOutOfRangeException(nameof(channels));

            if (sampleRate < 2000 || sampleRate > 200000)
                throw new ArgumentOutOfRangeException(nameof(sampleRate));

            al = AL.GetApi();
            alContext = ALContext.GetApi();
            device = alContext.OpenDevice("");

            Available = device != null;

            if (Available)
            {
                context = alContext.CreateContext(device, null);
                alContext.MakeContextCurrent(context);
                if (al.GetError() != AudioError.NoError)
                {
                    Available = false;
                    if (context != null)
                        alContext.DestroyContext(context);
                    alContext.CloseDevice(device);
                    al.Dispose();
                    alContext.Dispose();
                    disposed = true;
                    return;
                }
                source = al.GenSource();
                audioBuffer = new AudioBuffer(al, channels, sampleRate);
                al.SetSourceProperty(source, SourceBoolean.Looping, true);
                al.SetSourceProperty(source, SourceFloat.Gain, 1.0f);
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
                    Stop();
                    Reset();
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
                    al.SetSourceProperty(source, SourceFloat.Gain, volume);
            }
        }

        public void Dispose()
        {
            if (disposed)
                return;

            if (Available)
            {
                al.DeleteSource(source);
                audioBuffer?.Dispose();
                alContext.DestroyContext(context);
                alContext.CloseDevice(device);
                al.Dispose();
                alContext.Dispose();
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

            if (source == 0)
                throw new NotSupportedException("Start was called without a valid source.");

            Streaming = true;

            al.SourcePlay(source);
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

            if (source == 0)
                throw new NotSupportedException("Stop was called without a valid source.");

            Streaming = false;

            al.SourceStop(source);
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public void StreamData(byte[] data)
        {
            if (!Available)
                return;

            audioBuffer.Fill(source, data);
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public void Reset()
        {
            if (!Available)
                return;

            audioBuffer.Reset(source);
        }
    }
}
