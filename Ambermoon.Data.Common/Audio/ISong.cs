namespace Ambermoon.Data.Audio
{
    public interface ISong
    {
        void Play(IAudioOutput audioOutput);
        void Stop();
    }
}
