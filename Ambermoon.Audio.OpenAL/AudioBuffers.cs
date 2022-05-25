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
        static readonly Queue<AudioBuffer> queuedBuffers = new Queue<AudioBuffer>();

        public static AudioBuffers CurrentBuffers { get; private set; } = null;

        static readonly TimeSpan BufferDuration = TimeSpan.FromSeconds(4); // buffer 4 seconds
        readonly List<AudioBuffer> buffers = new List<AudioBuffer>();
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
            if (CurrentBuffers != this)
                return;

            if (queuedBuffers.Any(b => b.Position == bufferPosition)) // for short music we might have loaded all parts already
                return;

            int queuedCount;

            lock (mutex)
            {
                al.GetSourceProperty(source, GetSourceInteger.ByteOffset, out int position);
                var buffersToRemove = new List<AudioBuffer>();
                while (queuedBuffers.Count != 0 && queuedBuffers.Peek().Size <= position)
                {
                    var bufferToRemove = queuedBuffers.Dequeue();
                    buffersToRemove.Add(bufferToRemove);
                    position -= bufferToRemove.Size;
                }
                if (buffersToRemove.Count != 0)
                {
                    al.SourceUnqueueBuffers(source, buffersToRemove.Select(b => b.Index).ToArray());
                    buffersToRemove.ForEach(b => b.Dispose());
                }
                queuedCount = queuedBuffers.Count;
            }

            if (queuedCount < 2)
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
                    buffer = new AudioBuffer(al, channels, sampleRate, sample8Bit, bufferPosition);
                    var data = audioStream.Stream(BufferDuration);
                    buffer.Stream(data);
                    buffers.Add(buffer);
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
                al.SourceUnqueueBuffers(source, queuedBuffers.Select(b => b.Index).ToArray());
                queuedBuffers.ToList().ForEach(b => b.Dispose());
                queuedBuffers.Clear();
                CurrentBuffers = null;
            }
        }
    }
}
