using Ambermoon.Data.Audio;
using Silk.NET.OpenAL;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Ambermoon.Audio.OpenAL
{
    internal class AudioBuffers : IDisposable
    {
        static AudioBuffers CurrentBuffers { get; set; } = null;
        static readonly TimeSpan BufferDuration = TimeSpan.FromSeconds(4.0); // buffer 4 seconds
        const int BufferCount = 3;
        readonly Queue<AudioBuffer> queuedBuffers = new(BufferCount);
        readonly AL al;
        readonly uint source;
        readonly int channels;
        readonly int sampleRate;
        readonly bool sample8Bit;
        readonly IAudioStream audioStream;
        int bufferPosition = 0;
        readonly object mutex = new();
        Task playbackTask;

        public AudioBuffers(AL al, uint source, int channels, int sampleRate, bool sample8Bit, IAudioStream audioStream)
        {
            this.al = al;
            this.source = source;
            this.channels = channels;
            this.sampleRate = sampleRate;
            this.sample8Bit = sample8Bit;
            this.audioStream = audioStream;
        }

        public void Dispose()
        {
            Stop();
        }

        public void Play(CancellationToken cancellationToken)
        {
            CurrentBuffers?.Stop();

            if (CurrentBuffers != this)
            {
                CurrentBuffers = this;

                audioStream.Reset();

                lock (mutex)
                {
                    bufferPosition = 0;
                }
            }

            playbackTask = PlaybackLoopAsync(cancellationToken);
        }

        public void Stop()
        {
            lock (mutex)
            {
                if (playbackTask != null && !playbackTask.IsCompleted)
                    playbackTask.Wait();
                else
                {
                    al.SourceStop(source);
                    al.SetSourceProperty(source, SourceInteger.Buffer, 0);
                }
                CurrentBuffers = null;
            }
        }

        async Task PlaybackLoopAsync(CancellationToken cancellationToken)
        {
            void SetupNextBuffers(int count = 1)
            {
                if (queuedBuffers.Count != 0)
                {
                    for (int i = 0; i < Math.Min(count, queuedBuffers.Count); ++i)
                    {
                        var buffer = queuedBuffers.Dequeue();
                        al.SourceUnqueueBuffers(source, new uint[1] { buffer.Index });
                        buffer.Dispose();
                    }
                }
                var newBuffers = new uint[count];
                for (int i = 0; i < count && !cancellationToken.IsCancellationRequested; ++i)
                {
                    var nextBuffer = new AudioBuffer(al, channels, sampleRate, sample8Bit, bufferPosition);
                    nextBuffer.Stream(audioStream.Stream(BufferDuration));
                    if (audioStream.EndOfStream)
                    {
                        audioStream.Reset();
                        bufferPosition = 0;
                    }
                    else
                    {
                        bufferPosition += nextBuffer.Size;
                    }
                    queuedBuffers.Enqueue(nextBuffer);
                    newBuffers[i] = nextBuffer.Index;
                }
                if (!cancellationToken.IsCancellationRequested)
                    al.SourceQueueBuffers(source, newBuffers);
            }

            if (cancellationToken.IsCancellationRequested)
                return;

            // Start with 3 buffers (up to 12 seconds)
            SetupNextBuffers(BufferCount);

            if (cancellationToken.IsCancellationRequested)
                return;

            // Start playing the source
            al.SourcePlay(source);

            while (!cancellationToken.IsCancellationRequested)
            {
                // Wait for a buffer to finish playing
                int buffersProcessed;
                do
                {
                    await Task.Delay(10, CancellationToken.None);
                    al.GetSourceProperty(source, GetSourceInteger.BuffersProcessed, out buffersProcessed);
                } while (buffersProcessed == 0 && !cancellationToken.IsCancellationRequested);

                if (!cancellationToken.IsCancellationRequested)
                    SetupNextBuffers(buffersProcessed);
            }

            // Stop playing the source
            al.SourceStop(source);

            while (queuedBuffers.Count != 0)
            {
                var buffer = queuedBuffers.Dequeue();
                al.SourceUnqueueBuffers(source, new uint[1] { buffer.Index });
                buffer.Dispose();
            }

            al.SetSourceProperty(source, SourceInteger.Buffer, 0);
        }
    }
}
