using Ambermoon.Data.Enumerations;

namespace Ambermoon.Data.Audio
{
    public interface ISong
    {
        Song Song { get; }
        void Play(IAudioOutput audioOutput);
        void Stop();
    }
}
