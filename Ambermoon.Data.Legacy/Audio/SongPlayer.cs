using Ambermoon.Data.Audio;
using System;

namespace Ambermoon.Data.Legacy.Audio
{
    class SongPlayer
    {
        IAudioStream currentStream = null;
        IAudioOutput audioOutput = null;
        readonly object startMutex = new object();

        public void Start(IAudioOutput audioOutput, IAudioStream audioStream)
        {
            lock (startMutex)
            {
                this.audioOutput = audioOutput ?? throw new ArgumentNullException(nameof(audioOutput));

                if (currentStream != audioStream)
                {
                    Stop(true);
                    currentStream = audioStream;
                    audioOutput.StreamData(audioStream, 1, 44100, true);
                }
                if (!audioOutput.Streaming)
                    audioOutput.Start();
            }
        }

        public void Stop(bool keepStreamEndedEvent = false)
        {
            audioOutput?.Stop(keepStreamEndedEvent);
            audioOutput?.Reset();
            currentStream = null;
        }
    }
}
