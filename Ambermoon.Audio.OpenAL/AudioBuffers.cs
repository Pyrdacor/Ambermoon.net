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
        static readonly List<AudioBuffer> queuedBuffers = new List<AudioBuffer>();

        public static AudioBuffers CurrentBuffers { get; private set; } = null;

        static readonly TimeSpan BufferDuration = TimeSpan.FromSeconds(1); // buffer 1 second
        readonly List<AudioBuffer> buffers = new List<AudioBuffer>();
        readonly AL al;
        readonly uint source;
        readonly int channels;
        readonly int sampleRate;
        readonly bool sample8Bit;
        readonly IAudioStream audioStream;
        bool fullyLoaded => audioStream.EndOfStream;

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

            foreach (var buffer in buffers)
                buffer?.Dispose();

            buffers.Clear();
        }

        void Stream(Action<AudioBuffer> finishHandler, CancellationToken cancellationToken)
        {
            if (source == 0)
            {
                finishHandler?.Invoke(null);
                return;
            }

            if (!fullyLoaded)
            {
                Task.Run(() =>
                {
                    var buffer = new AudioBuffer(al, channels, sampleRate, sample8Bit);
                    var data = audioStream.Stream(BufferDuration);
                    buffer.Stream(source, data);
                    buffers.Add(buffer);

                    finishHandler?.Invoke(buffer);
                }, cancellationToken);
            }
            else
            {
                finishHandler?.Invoke(null);
            }
        }

        public void Play(CancellationToken cancellationToken)
        {
            if (CurrentBuffers != this)
            {
                Stop();
                CurrentBuffers = this;

                if (queuedBuffers.Count == 0)
                {
                    if (fullyLoaded)
                    {
                        if (queuedBuffers.Count < buffers.Count)
                        {
                            var newBuffers = buffers.Skip(queuedBuffers.Count).Take(buffers.Count - queuedBuffers.Count);
                            queuedBuffers.AddRange(newBuffers);
                            var bufferIndices = newBuffers.Select(b => b.Index);
                            al.SourceQueueBuffers(source, bufferIndices.ToArray());
                        }
                    }
                    else
                    {
                        Stream(buffer =>
                        {
                            if (buffer == null)
                                return;
                            if (CurrentBuffers == this)
                            {
                                buffer.Queue(source);
                                queuedBuffers.Add(buffer);
                                al.SetSourceProperty(source, SourceInteger.SampleOffset, 0);
                                al.SourcePlay(source);
                            }
                            StreamChain(cancellationToken);
                        }, cancellationToken);
                        return;
                    }
                }
            }

            al.SetSourceProperty(source, SourceInteger.SampleOffset, 0);
            al.SourcePlay(source);
        }

        void StreamChain(CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
                return;

            if (!fullyLoaded)
            {
                Stream(buffer =>
                {
                    if (CurrentBuffers == this)
                    {
                        if (queuedBuffers.Count < buffers.Count - 1)
                        {
                            var newBuffers = buffers.Skip(queuedBuffers.Count).Take(buffers.Count - queuedBuffers.Count);
                            queuedBuffers.AddRange(newBuffers);
                            if (buffer != null)
                                queuedBuffers.Add(buffer);
                            var bufferIndices = buffer != null
                                ? Enumerable.Concat(newBuffers.Select(b => b.Index), new uint[1] { buffer.Index })
                                : newBuffers.Select(b => b.Index);
                            if (bufferIndices.Any())
                                al.SourceQueueBuffers(source, bufferIndices.ToArray());
                        }
                        else if (buffer != null)
                        {
                            buffer.Queue(source);
                            queuedBuffers.Add(buffer);
                        }
                    }
                    StreamChain(cancellationToken);
                }, cancellationToken);
            }
            else if (CurrentBuffers == this)
            {
                if (queuedBuffers.Count < buffers.Count)
                {
                    var newBuffers = buffers.Skip(queuedBuffers.Count).Take(buffers.Count - queuedBuffers.Count);
                    queuedBuffers.AddRange(newBuffers);
                    var bufferIndices = newBuffers.Select(b => b.Index);
                    al.SourceQueueBuffers(source, bufferIndices.ToArray());                    
                }
            }
        }

        public void Stop()
        {
            al.SourceStop(source);
            al.SourceUnqueueBuffers(source, queuedBuffers.Select(b => b.Index).ToArray());
            queuedBuffers.Clear();
            CurrentBuffers = null;
        }
    }
}
