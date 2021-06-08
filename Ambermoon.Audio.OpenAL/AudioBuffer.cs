using Silk.NET.OpenAL;
using System;

namespace Ambermoon.Audio.OpenAL
{
    internal class AudioBuffer : IDisposable
    {
        const int NumBuffers = 10;
        readonly AL al;
        readonly uint[] bufferIndices;
        readonly BufferFormat format;
        readonly int sampleRate;
        int firstBufferIndex = 0;
        int queuedBuffers = 0;
        bool disposed = false;

        public AudioBuffer(AL al, int channels, int sampleRate)
        {
            this.al = al;
            bufferIndices = al.GenBuffers(NumBuffers);
            format = channels == 1 ? BufferFormat.Mono8 : BufferFormat.Stereo8;
            this.sampleRate = sampleRate;
        }

        unsafe void QueueBuffer(uint source, int index)
        {
            fixed (uint* bufferIndex = &bufferIndices[index])
                al.SourceQueueBuffers(source, 1, bufferIndex);

            ++queuedBuffers;
        }

        unsafe void UnqueueBuffer(uint source, int index)
        {
            fixed (uint* bufferIndex = &bufferIndices[index])
                al.SourceUnqueueBuffers(source, 1, bufferIndex);

            --queuedBuffers;
        }

        public void Reset(uint source)
        {
            if (disposed)
                throw new InvalidOperationException("Tried to reset a disposed audio buffer.");

            al.GetSourceProperty(source, GetSourceInteger.BuffersQueued, out int queued);
            al.GetSourceProperty(source, GetSourceInteger.BuffersProcessed, out int processed);
            firstBufferIndex += processed;

            for (int i = 0; i < queued; ++i)
                UnqueueBuffer(source, (firstBufferIndex + i) % NumBuffers);

            firstBufferIndex = 0;
            queuedBuffers = 0;
        }

        public void Fill(uint source, byte[] data)
        {
            if (disposed)
                throw new InvalidOperationException("Tried to fill a disposed audio buffer.");

            al.GetSourceProperty(source, GetSourceInteger.BuffersQueued, out int queued);
            al.GetSourceProperty(source, GetSourceInteger.BuffersProcessed, out int processed);

            if (queued == NumBuffers && processed == 0)
                throw new InsufficientMemoryException("All audio buffers are already full and queued.");

            while (processed != 0)
            {
                UnqueueBuffer(source, firstBufferIndex);
                firstBufferIndex = (firstBufferIndex + 1) % NumBuffers;
                --processed;
            }

            int index = (firstBufferIndex + queued) % NumBuffers;
            al.BufferData(bufferIndices[index], format, data, sampleRate);
            QueueBuffer(source, index);
        }

        public void Dispose()
        {
            if (!disposed)
            {
                al.DeleteBuffers(bufferIndices);
                disposed = true;
            }
        }
    }
}
