using Ambermoon.Data.Audio;
using Silk.NET.OpenAL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Ambermoon.Audio.OpenAL
{
    internal class AudioBuffers : IDisposable
    {
        readonly Queue<AudioBuffer> queuedBuffers = new Queue<AudioBuffer>();

        public static AudioBuffers CurrentBuffers { get; private set; } = null;

        static readonly TimeSpan BufferDuration = TimeSpan.FromSeconds(4); // buffer 4 seconds
        readonly AL al;
        readonly uint source;
        readonly int channels;
        readonly int sampleRate;
        readonly bool sample8Bit;
        readonly IAudioStream audioStream;
        int bufferPosition = 0;
        readonly object mutex = new object();

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

        void StreamWhilePlaying(CancellationToken cancellationToken)
        {
            Stream(buffer =>
            {
                if (buffer != null && CurrentBuffers == this)
                {
                    lock (mutex)
                    {
                        if (audioStream.EndOfStream)
                        {
                            bufferPosition = 0;
                            audioStream.Reset();
                        }
                        else
                            bufferPosition += buffer.Size;
                        buffer.Queue(source);
                        queuedBuffers.Enqueue(buffer);
                    }
                    WaitForNextStream(1000, cancellationToken);
                }
            }, cancellationToken);
        }

        void WaitForNextStream(int delayPerWait, CancellationToken cancellationToken)
        {
            if (!al.IsSource(source))
                return;

            if (CurrentBuffers != this)
                return;

            Unqueue();

            al.GetSourceProperty(source, GetSourceInteger.BuffersQueued, out int queuedCount);
            al.GetSourceProperty(source, GetSourceInteger.BuffersProcessed, out int processedCount);

            if (queuedCount - processedCount < 3)
            {
                StreamWhilePlaying(cancellationToken);
            }
            else
            {
                Task.Delay(delayPerWait, cancellationToken).ContinueWith(_ =>
                {
                    WaitForNextStream(delayPerWait, cancellationToken);
                });
            }
        }

        void Stream(Action<AudioBuffer> finishHandler, CancellationToken cancellationToken)
        {
            Task.Run(() =>
            {
                AudioBuffer buffer;

                lock (mutex)
                {
                    if (audioStream.EndOfStream)
                    {
                        audioStream.Reset();
                        bufferPosition = 0;
                    }

                    buffer = new AudioBuffer(al, channels, sampleRate, sample8Bit, bufferPosition);
                    var data = audioStream.Stream(BufferDuration);
                    buffer.Stream(data);
                }

                finishHandler?.Invoke(buffer);
            }, cancellationToken);
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

            Stream(buffer =>
            {
                if (buffer != null && CurrentBuffers == this)
                {
                    lock (mutex)
                    {
                        if (audioStream.EndOfStream)
                        {
                            bufferPosition = 0;
                            audioStream.Reset();
                        }
                        else
                            bufferPosition += buffer.Size;
                        buffer.Queue(source);
                        queuedBuffers.Enqueue(buffer);
                        al.SetSourceProperty(source, SourceInteger.ByteOffset, 0);
                        al.SourcePlay(source);
                    }
                    StreamWhilePlaying(cancellationToken);
                }
            }, cancellationToken);
        }

        public void Stop()
        {
            lock (mutex)
            {
                al.SourceStop(source);
                al.SetSourceProperty(source, SourceInteger.Buffer, 0);
                Unqueue();
                CurrentBuffers = null;
            }
        }

        int Unqueue(int maxAmount = int.MaxValue)
        {
            lock (mutex)
            {
                if (source == 0 || !al.IsSource(source))
                {
                    queuedBuffers.Clear();
                    return 0;
                }

                al.GetSourceProperty(source, GetSourceInteger.BuffersProcessed, out int numProcessed);

                if (numProcessed == 0)
                    return 0;

                int amount = Math.Min(numProcessed, maxAmount);
                al.SourceUnqueueBuffers(source, queuedBuffers.Take(amount).Select(b => b.Index).ToArray());
                var error = al.GetError();

                if (error != AudioError.NoError)
                {
                    Console.WriteLine("OpenAL error while unqueuing buffers: " + error);
                    return 0;
                }

                for (int i = 0; i < amount; ++i)
                {
                    queuedBuffers.Dequeue()?.Dispose();
                }

                return amount;
            }
        }
    }
}
