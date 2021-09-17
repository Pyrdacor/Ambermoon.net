using Ambermoon.Data.Enumerations;
using Ambermoon.Data.Serialization;

namespace Ambermoon.Data.Audio
{
    public interface ISongManager
    {
        ISong GetSong(Song index);
    }
}
