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

        public uint Index => bufferIndex;
        public int Position { get; }
        public int Size { get; private set; } = 0;

        public AudioBuffer(AL al, int channels, int sampleRate, bool sample8Bit, int position)
        {
            this.al = al;
            bufferIndex = al.GenBuffer();
            format = sample8Bit
                ? (channels == 1 ? BufferFormat.Mono8 : BufferFormat.Stereo8)
                : (channels == 1 ? BufferFormat.Mono16 : BufferFormat.Stereo16);
            this.sampleRate = sampleRate;
            Position = position;
        }

        public void Stream(byte[] data)
        {
            if (disposed)
                throw new InvalidOperationException("Tried to fill a disposed audio buffer.");

            Size = data.Length;
            al.BufferData(bufferIndex, format, data, sampleRate);
        }

        public void Dispose()
        {
            if (!disposed)
            {
                if (al.IsBuffer(bufferIndex))
                {
                    al.DeleteBuffer(bufferIndex);
                    var error = al.GetError();
                    if (error != AudioError.NoError)
                    {
                        Console.WriteLine($"OpenAL error while deleting buffer: " + error);
                    }
                }
                Size = 0;
                disposed = true;
            }
        }

        public void Queue(uint source)
        {
            al.SourceQueueBuffers(source, new uint[1] { bufferIndex });
        }
    }
}
