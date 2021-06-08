using Ambermoon.Data.Enumerations;

namespace Ambermoon.Data.Audio
{
    public interface ISongManager
    {
        ISong GetSong(Song index);
    }
}
