using System;

namespace Ambermoon.Data.Audio
{
    public interface IAudioOutput
    {
        /// <summary>
        /// Starts streaming audio data.
        /// </summary>
        void Start();
        /// <summary>
        /// Stops streaming audio data.
        /// </summary>
        void Stop(bool keepStreamEndedEvent = false);
        /// <summary>
        /// Streams new data.
        /// </summary>
        void StreamData(IAudioStream audioStream, int channels = 1, int sampleRate = 44100, bool sample8Bit = true);
        /// <summary>
        /// Resets the audio data.
        /// </summary>
        void Reset();
        /// <summary>
        /// Indicates if an audio output is available.
        /// </summary>
        bool Available { get; }
        /// <summary>
        /// Enables audio output.
        /// </summary>
        bool Enabled { get; set; }
        /// <summary>
        /// Indicates if currently audio data is streamed.
        /// </summary>
        bool Streaming { get; }
        /// <summary>
        /// Output volume (0.0 to 1.0)
        /// </summary>
        float Volume { get; set; }
        /// <summary>
        /// Stream ended event. This happens also
        /// while looping.
        /// </summary>
        event Action StreamEnded;
    }
}
