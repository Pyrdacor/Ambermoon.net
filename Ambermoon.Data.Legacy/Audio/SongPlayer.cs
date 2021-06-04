using Ambermoon.Data.Audio;
using SonicArranger;
using System;

namespace Ambermoon.Data.Legacy.Audio
{
    class SongPlayer : ISoundInterface
    {
        Stream currentStream = null;
        double nextReadTime = 0.0;
        double? playTime = null;

        /// <summary>
        /// <inheritdoc />
        /// </summary>
        public double Volume { get; set; } = 1.0;

        /// <summary>
        /// <inheritdoc />
        /// </summary>
        public bool Enabled { get; set; } = true;

        public void Start(Stream stream)
        {
            if (currentStream == stream)
                return;

            currentStream = stream;
            Stop();
        }

        public void Stop()
        {
            playTime = null;
            currentStream?.Reset();
        }

        /// <summary>
        /// Runs a song player update and increases the play time.
        /// </summary>
        /// <param name="delta">Delta time in seconds</param>
        public void Update(double delta)
        {
            if (currentStream == null)
                return;

            bool updateBuffer;

            if (playTime == null)
            {
                updateBuffer = true;
                playTime = 0.0;
            }
            else
            {
                playTime += delta;
                updateBuffer = playTime >= nextReadTime;
            }

            if (updateBuffer)
            {
                double secondsToRead = Math.Min(1.0, 0.5 + playTime.Value - nextReadTime);
                var data = currentStream.Read((int)Math.Round(secondsToRead * 1000.0), true);
                // TODO: fill OpenAL buffer etc
                nextReadTime = playTime.Value + 0.5;
            }
        }
    }
}
