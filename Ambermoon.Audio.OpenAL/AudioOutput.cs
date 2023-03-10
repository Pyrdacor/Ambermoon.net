using Ambermoon.Data.Audio;
using Silk.NET.OpenAL;
using System;
using System.Collections.Generic;
using System.Threading;

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
        readonly Dictionary<IAudioStream, AudioBuffers> audioBuffers = new Dictionary<IAudioStream, AudioBuffers>();
        AudioBuffers currentBuffer = null;
        CancellationTokenSource cancellationTokenSource;

        public AudioOutput()
        {
            try
            {
                al = AL.GetApi(true);
                alContext = ALContext.GetApi(true);
            }
            catch
            {
                try
                {
                    al = AL.GetApi(false);
                    alContext = ALContext.GetApi(false);
                }
                catch
                {
                    Available = false;
                    return;
                }
            }
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
                al.SetSourceProperty(source, SourceBoolean.Looping, false);
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

            cancellationTokenSource?.Dispose();

            if (Available)
            {
                al.DeleteSource(source);
                foreach (var audioBuffer in audioBuffers)
                    audioBuffer.Value?.Dispose();
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

            if (currentBuffer == null)
                return;

            Streaming = true;

            cancellationTokenSource?.Dispose();
            cancellationTokenSource = new CancellationTokenSource();

            currentBuffer?.Play(cancellationTokenSource.Token);
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
            cancellationTokenSource?.Cancel();

            if (currentBuffer != null)
                currentBuffer.Stop();
            else
                al.SourceStop(source);

            cancellationTokenSource?.Dispose();
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public void StreamData(IAudioStream audioStream, int channels = 1, int sampleRate = 44100, bool sample8Bit = true)
        {
            if (!Available)
                return;

            if (!audioBuffers.TryGetValue(audioStream, out var buffer))
                currentBuffer = audioBuffers[audioStream] = new AudioBuffers(al, source, channels, sampleRate, sample8Bit, audioStream);
            else
                currentBuffer = buffer;
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public void Reset()
        {
            if (!Available)
                return;

            al.SetSourceProperty(source, SourceInteger.Buffer, 0u);
        }
    }
}
