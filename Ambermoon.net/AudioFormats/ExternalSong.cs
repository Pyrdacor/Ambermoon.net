using Ambermoon.Data.Audio;
using Ambermoon.Data.Enumerations;
using System;
using System.Threading.Tasks;

namespace Ambermoon.AudioFormats
{
    internal abstract class ExternalSong : ISong
    {
        public abstract Song Song { get; }

        public abstract TimeSpan? SongDuration { get; protected set; }

        public abstract void Play(IAudioOutput audioOutput);

        public abstract void Stop();

    }
}
