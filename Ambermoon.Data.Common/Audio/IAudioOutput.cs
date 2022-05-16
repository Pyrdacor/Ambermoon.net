namespace Ambermoon.Data.Audio
{
    public interface IAudioOutput
    {
        /// <summary>
        /// Starts streaming audio data.
        /// </summary>
        public void Start();
        /// <summary>
        /// Stops streaming audio data.
        /// </summary>
        public void Stop();
        /// <summary>
        /// Streams new data.
        /// </summary>
        public void StreamData(IAudioStream audioStream, int channels = 1, int sampleRate = 44100, bool sample8Bit = true);
        /// <summary>
        /// Resets the audio data.
        /// </summary>
        public void Reset();
        /// <summary>
        /// Indicates if an audio output is available.
        /// </summary>
        public bool Available { get; }
        /// <summary>
        /// Enables audio output.
        /// </summary>
        public bool Enabled { get; set; }
        /// <summary>
        /// Indicates if currently audio data is streamed.
        /// </summary>
        public bool Streaming { get; }
        /// <summary>
        /// Output volume (0.0 to 1.0)
        /// </summary>
        public float Volume { get; set; }
    }
}
