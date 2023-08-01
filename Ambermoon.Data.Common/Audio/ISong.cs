using Ambermoon.Data.Enumerations;
using System;

namespace Ambermoon.Data.Audio
{
    public interface ISong
    {
        Song Song { get; }
        TimeSpan? SongDuration { get; }
        void Play(IAudioOutput audioOutput, ISong followupSong = null);
        void Stop();
    }
}
