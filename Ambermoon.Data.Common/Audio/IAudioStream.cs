using System;

namespace Ambermoon.Data.Audio
{
    public interface IAudioStream
    {
        public bool EndOfStream { get; }
        public byte[] Stream(TimeSpan duration);
    }
}
