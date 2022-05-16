using Ambermoon.Data.Audio;
using System;

namespace Ambermoon.Data.Legacy.Audio
{
    class SongPlayer
    {
        IAudioStream currentStream = null;
        IAudioOutput audioOutput = null;

        public void Start(IAudioOutput audioOutput, IAudioStream audioStream)
        {
            this.audioOutput = audioOutput ?? throw new ArgumentNullException(nameof(audioOutput));

            if (currentStream != audioStream)
            {
                Stop();
                currentStream = audioStream;
                audioOutput.StreamData(audioStream, 1, 44100, true);
            }
            if (!audioOutput.Streaming)
                audioOutput.Start();
        }

        public void Stop()
        {
            audioOutput?.Stop();
            audioOutput?.Reset();
            currentStream = null;
        }
    }
}
