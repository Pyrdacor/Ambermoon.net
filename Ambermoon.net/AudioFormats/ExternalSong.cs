using Ambermoon.Data.Audio;
using Ambermoon.Data.Enumerations;
using System;
using System.Threading.Tasks;

namespace Ambermoon.AudioFormats
{
    internal abstract class ExternalSong : ISong
    {
        public Task<bool> LoadTask { get; protected set; } = null;

        public abstract Song Song { get; }

        public abstract TimeSpan SongDuration { get; protected set; }

        public abstract void Play(IAudioOutput audioOutput, bool waitTillLoaded);

        public abstract void Stop();

    }
}
