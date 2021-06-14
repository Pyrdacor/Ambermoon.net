using Ambermoon.Data.Audio;
using System;

namespace Ambermoon.Data.Legacy.Audio
{
    class SongPlayer
    {
        byte[] currentStream = null;
        IAudioOutput audioOutput = null;

        public void Start(IAudioOutput audioOutput, byte[] data)
        {
            this.audioOutput = audioOutput ?? throw new ArgumentNullException(nameof(audioOutput));

            if (currentStream != data)
            {
                Stop();
                currentStream = data;
                audioOutput.StreamData(data);
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
