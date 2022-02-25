using Silk.NET.OpenAL;
using System;

namespace Ambermoon.Audio.OpenAL
{
    internal class AudioBuffer : IDisposable
    {
        readonly AL al;
        readonly uint bufferIndex;
        readonly BufferFormat format;
        readonly int sampleRate;
        bool disposed = false;

        public AudioBuffer(AL al, int channels, int sampleRate, bool sample8Bit)
        {
            this.al = al;
            bufferIndex = al.GenBuffer();
            format = sample8Bit
                ? (channels == 1 ? BufferFormat.Mono8 : BufferFormat.Stereo8)
                : (channels == 1 ? BufferFormat.Mono16 : BufferFormat.Stereo16);
            this.sampleRate = sampleRate;
        }

        public void Fill(uint source, byte[] data)
        {
            if (disposed)
                throw new InvalidOperationException("Tried to fill a disposed audio buffer.");

            al.SourceStop(source); // ensure stop
            al.BufferData(bufferIndex, format, data, sampleRate);
            Activate(source);
        }

        public void Activate(uint source) => al.SetSourceProperty(source, SourceInteger.Buffer, bufferIndex);

        public void Dispose()
        {
            if (!disposed)
            {
                al.DeleteBuffer(bufferIndex);
                disposed = true;
            }
        }
    }
}
