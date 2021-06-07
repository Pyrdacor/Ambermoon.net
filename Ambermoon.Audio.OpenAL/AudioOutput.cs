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
        readonly uint buffer = 0;
        bool disposed = false;
        readonly int channels = 1;
        readonly int sampleRate = 44100;
        bool enabled = true;
        int remainingBufferBytes = 0;
        byte[] lastBuffer = null;

        public AudioOutput()
        {
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
                buffer = al.GenBuffer();
                al.SetSourceProperty(source, SourceBoolean.Looping, true);
                al.SetSourceProperty(source, SourceInteger.Buffer, buffer);
            }
        }

        public AudioOutput(int channels, int sampleRate)
            : this()
        {
            if (channels < 1 || channels > 2)
                throw new ArgumentOutOfRangeException(nameof(channels));

            if (sampleRate < 2000 || sampleRate > 200000)
                throw new ArgumentOutOfRangeException(nameof(sampleRate));

            this.channels = channels;
            this.sampleRate = sampleRate;
        }

        public bool Available { get; private set; }

        public bool Enabled
        {
            get => enabled;
            set
            {
                if (enabled == value)
                    return;

                // TODO
                enabled = value;
            }
        }

        public bool Streaming { get; private set; } = false;

        public void Dispose()
        {
            if (disposed)
                return;

            if (Available)
            {
                al.DeleteSource(source);
                al.DeleteBuffer(buffer);
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

        public void Start()
        {
            if (!Available || !Enabled)
                return;

            if (Streaming)
                return;

            if (source == 0)
                throw new NotSupportedException("Start was called without a valid source.");

            if (remainingBufferBytes == 0)
                throw new System.IO.EndOfStreamException("No audio data present.");

            Streaming = true;

            al.SourcePlay(source);
        }

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

        public void StreamData(byte[] data, double timeToKeep)
        {
            if (!Available)
                return;

            if (lastBuffer == null || lastBuffer.Length == 0 || timeToKeep < 0.00001)
            {
                lastBuffer = data;
                al.BufferData(buffer, channels == 1 ? BufferFormat.Mono8 : BufferFormat.Stereo8, data, sampleRate);
            }
            else
            {
                int bytesToKeep = Math.Min(lastBuffer.Length, (int)Math.Ceiling(sampleRate * timeToKeep * channels));
                var dataBuffer = new byte[bytesToKeep + data.Length];
                Buffer.BlockCopy(lastBuffer, lastBuffer.Length - bytesToKeep, dataBuffer, 0, bytesToKeep);
                Buffer.BlockCopy(data, 0, dataBuffer, bytesToKeep, data.Length);
                lastBuffer = dataBuffer;
                al.BufferData(buffer, channels == 1 ? BufferFormat.Mono8 : BufferFormat.Stereo8, dataBuffer, sampleRate);
            }
        }

        public void Clear()
        {
            lastBuffer = null;
        }
    }
}
