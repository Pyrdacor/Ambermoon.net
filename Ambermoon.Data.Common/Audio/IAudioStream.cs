using System;

namespace Ambermoon.Data.Audio
{
    public interface IAudioStream
    {
        bool EndOfStream { get; }
        byte[] Stream(TimeSpan duration);
        void Reset();
    }
}
